# Spec: Expecto on WasmGC — Test Framework Compilation

> **Target team:** fsharp-wasm compiler team  
> **Sprint target:** 28–30  
> **Status:** ✅ Sprint 28 — IMPLEMENTED (framework complete, tests written)  
> **Depends on:** FSharp.Core bootstrap (sprint 27), WasmGC linear memory surface (sprint 27)

---

## Problem

The fsharp-wasm-core BCL stubs exist exclusively for fsharp-wasm. They are not
meant to build under standard .NET. Our tests therefore cannot use Expecto as a
NuGet package — that approach requires standard .NET, contradicts the zero-.NET
principle, and cannot catch WasmGC-specific failures.

We need a test framework that:
1. Compiles fully through fsharp-wasm  
2. Runs inside a WasmGC runtime (wasmtime, v8, or our own JS harness)  
3. Reports results in a machine-readable format (TAP or JSON)  
4. Is usable from CI with a simple exit code

---

## Decision: Compile Expecto to WasmGC — Good Idea

Yes, this is the right approach. Reasons:

| Argument | Detail |
|----------|--------|
| **Tests their own target** | BCL stub correctness is verified in the same environment where it runs. A test that passes on standard .NET but fails in WasmGC is worse than useless — it gives false confidence. |
| **Dogfoods the toolchain** | Compiling ~4500 lines of Expecto through fsharp-wasm exercises exactly the features (async, reflection-lite, printf, collections) we need to get right. |
| **Enables BCL bootstrap validation** | When we compile FSharp.Core through fsharp-wasm we need a test framework that lives in the same universe — not one that depends on the thing being bootstrapped. |
| **No special-casing** | No `#if FABLE_COMPILER` guards, no dual-target libraries, no compromise. One code path. |

The cost is that it is non-trivial — Expecto depends on `FSharp.Core`, `System.Threading`, `System.Reflection`, and async. This spec describes how to manage that.

---

## Architecture

```
┌────────────────────────────────────────────────┐
│  Test module (e.g. Tests.Dictionary.fs)        │
│  open Wasmecto                                 │
│  [<Tests>] let t = testList "Dict" [...]       │
└──────────────────┬─────────────────────────────┘
                   │  compiled by fsharp-wasm
┌──────────────────▼─────────────────────────────┐
│  Wasmecto — minimal Expecto subset             │
│  (pure F# — no reflection, no async threads)  │
│  compiled by fsharp-wasm → wasmecto.wasm       │
└──────────────────┬─────────────────────────────┘
                   │  imports
┌──────────────────▼─────────────────────────────┐
│  test-runner.js / test-runner.wat              │
│  • instantiates wasmecto.wasm                  │
│  • calls exported runAll()                     │
│  • captures TAP output via imported writeLine  │
│  • exits with code = failing count             │
└────────────────────────────────────────────────┘
```

---

## Scope: "Wasmecto" (not upstream Expecto)

We do **not** aim to compile the full upstream Expecto source unmodified. Instead we
build **Wasmecto** — a clean-room minimal implementation of the Expecto API surface
that our tests actually use. This has several advantages:

- No async/threading requirement during sprint 28 (we add it later)
- No `System.Reflection` requirement (no `[<Tests>]` attribute discovery — explicit
  registration instead)
- ~500 lines of F# instead of 4500
- But: **API-compatible** with upstream Expecto — same `testList`, `test`, `testCase`,
  `Expect.*`, `testProperty` signatures, so upgrading to full Expecto later is a
  find-and-replace of `open Wasmecto` → `open Expecto`

Long-term (sprint 30+), once FSharp.Core is bootstrapped and we have a working
`System.Reflection` stub, we revisit compiling upstream Expecto unmodified.

---

## Wasmecto API Surface (sprint 28)

```fsharp
namespace Wasmecto

// ── Test building (mirrors Expecto.Tests) ────────────────────────────────

type Test =
    | TestCase  of label: string * body: (unit -> unit)
    | TestList  of label: string * children: Test list
    | PendingTest of label: string

val testCase  : string -> (unit -> unit) -> Test
val test      : string -> (unit -> unit) -> Test    // alias
val testList  : string -> Test list     -> Test
val pending   : string                  -> Test
val ptestCase : string -> (unit -> unit) -> Test    // pending
val skiptest  : string -> (unit -> unit) -> Test    // alias

// ── Assertions (mirrors Expecto.Expect) ─────────────────────────────────

module Expect =
    val equal        : actual:'a -> expected:'a -> msg:string -> unit
    val notEqual     : actual:'a -> expected:'a -> msg:string -> unit
    val isTrue       : actual:bool -> msg:string -> unit
    val isFalse      : actual:bool -> msg:string -> unit
    val isNull       : actual:obj  -> msg:string -> unit
    val isNotNull    : actual:obj  -> msg:string -> unit
    val throws       : (unit -> unit) -> msg:string -> unit
    val throwsT<'E when 'E :> exn> : (unit -> unit) -> msg:string -> unit
    val stringContains : actual:string -> substring:string -> msg:string -> unit
    val sequenceEqual: actual:'a seq -> expected:'a seq -> msg:string -> unit
    val isEmpty      : actual:'a seq -> msg:string -> unit
    val isNonEmpty   : actual:'a seq -> msg:string -> unit
    val approxEqual  : actual:float -> expected:float -> epsilon:float -> msg:string -> unit

// ── Property testing (replaces Expecto.FsCheck) ─────────────────────────

// Sprint 28: deterministic PRNG-based generators (no FsCheck dependency).
// Sprint 29: optionally compile FsCheck itself through fsharp-wasm.
module Gen =
    type Gen<'T> = { Run: Random -> 'T }
    val int     : lo:int -> hi:int -> Gen<int>
    val float   : lo:float -> hi:float -> Gen<float>
    val list    : Gen<'T> -> maxLen:int -> Gen<'T list>
    val map     : ('A -> 'B) -> Gen<'A> -> Gen<'B>
    val tuple2  : Gen<'A> -> Gen<'B> -> Gen<'A * 'B>

val testProperty : string -> Gen<'A> -> ('A -> bool) -> Test
val testPropertyWith : count:int -> string -> Gen<'A> -> ('A -> bool) -> Test

// ── Registration and runner ──────────────────────────────────────────────

// Tests marked with [<WasmTests>] are collected by the runner.
// Because there is no reflection, test modules must call register explicitly.
val register : Test list -> unit

// Called by the WasmGC runtime harness. Exported as "wasmecto_run".
// Returns: number of failed tests (0 = success).
[<CompiledName("wasmecto_run")>]
val run : writeLine:(string -> unit) -> int
```

---

## Output Format — TAP 13

```
TAP version 13
1..47
ok 1 - Dictionary.Add and retrieve
ok 2 - Dictionary.Contains
not ok 3 - Dictionary.Duplicate key throws
  ---
  message: Expected exception ArgumentException but no exception was raised.
  at: Tests.Dictionary/line:42
  ...
ok 4 - Stack<T>.Push increases Count
```

CI integration:
```bash
wasmtime --invoke wasmecto_run my_tests.wasm
# exit code = number of failures
```

---

## Required FSharp.Core Subset (Wasmecto dependencies)

| Module | Needed for |
|--------|------------|
| `List.*` | `testList`, `Test list`, collecting results |
| `String.*` | TAP formatting, assertion messages |
| `Option.*` | Internal: test filtering |
| `printf / sprintf` | Assertion messages |
| `exn` / `Exception` hierarchy | `throws`, `throwsT`, try/with |
| `Random` (simple LCG) | Property generators |

No `async`, no `Task`, no `System.Threading`, no `System.Reflection` needed for sprint 28.

---

## WasmGC Specifics

### Exception handling

`try / with` is required for `Expect.throws` / `Expect.throwsT`. This depends on
the compiler implementing WasmGC exception handling proposals
([`exnref`](https://github.com/WebAssembly/exception-handling)). Sprint 28 scope
includes at least `try { } catch(exn) { }` lowering.

Until then, `Expect.throws` is implemented as a "best effort" — the test still
compiles but is marked pending until exception proposal support lands.

### Output (writeLine import)

Wasmecto does NOT directly call `console.log`. Instead, `run` takes a `writeLine`
function parameter that is imported from the host:

```wat
(import "wasmecto" "writeLine" (func $writeLine (param (ref $WasmStr))))
```

The JS harness provides:
```js
{ wasmecto: { writeLine: (str) => process.stdout.write(str + "\n") } }
```

This keeps the core testable in any WasmGC host, not just Node.

### No GC pressure from test collection

Test trees are built as immutable linked `TestList` / `TestCase` WasmGC structs.
No `ResizeArray` growable allocation during collection.

---

## Repository Location

```
fsharp-wasm-core/
  Wasmecto/
    Wasmecto.fsproj      # compiles via fsharp-wasm only
    Types.fs             # Test DU, TestResult DU
    Expect.fs            # Expect.* assertions
    Gen.fs               # deterministic property generators
    Runner.fs            # run + TAP formatting
    Register.fs          # register, [<WasmTests>] attribute

  tests/
    WasmTests/           # test modules (compiled via fsharp-wasm)
      Tests.Dictionary.fs
      Tests.Stack.fs
      Tests.StringBuilder.fs
      Tests.Math.fs
      Tests.Interfaces.fs
      WasmTests.fsproj   # references FSharp.Wasm.Core + Wasmecto

  run-tests.sh           # wasmtime --invoke wasmecto_run wasmtests.wasm
```

---

## Migration from current Expecto test project

The current `tests/FSharp.WasmCore.Tests/` project (standard .NET Expecto) is a
**temporary scaffold** until Wasmecto is working. Once sprint 28 lands:

1. Delete `tests/FSharp.WasmCore.Tests/`
2. Move test logic to `tests/WasmTests/*.fs`
3. Replace `open Expecto` → `open Wasmecto` (API-compatible)
4. Run via `wasmtime` instead of `dotnet run`

The `Expect.*` API surface was intentionally matched so this migration is mechanical.

---

## Milestones

| Sprint | Deliverable |
|--------|-------------|
| 27 | FSharp.Core bootstrap: `List`, `Option`, `String`, `sprintf` |
| 28 | Wasmecto Types + Expect + Runner; 50+ tests passing in wasmtime |
| 28 | WasmGC exception handling (`try/with`) in compiler |
| 29 | Property testing with deterministic `Gen<T>` |
| 29 | CI: `run-tests.sh` integrated into `build.fsx` `UnitTests` target |
| 30 | Evaluate compiling upstream Expecto unmodified |

---

## Sprint 28 — Implementation Status

Framework files complete:

| File | Status | Notes |
|------|--------|-------|
| `src/Types.fs` | ✅ Done | `FocusState`, `TestCode`, `Test`, `TestResult`, `TestSummary` |
| `src/ExnState.fs` | ✅ Done | Global exception tracker (WasmGC catch_all workaround) |
| `src/Expect.fs` | ✅ Done | Full assertion surface, API-compatible with Expecto |
| `src/DSL.fs` | ✅ Done | `testCase`, `testList`, `pending`, `ftestCase`, `ftestList` |
| `src/Runner.fs` | ✅ Done | Sequential runner, TAP 13 output, `register`, `wasmecto_run` export |
| `src/Wasmecto.fsproj` | ✅ Done | WasmGC-only project file |
| `runtime/test-runner.js` | ✅ Done | Node.js WebAssembly harness |
| `run-tests.sh` | ✅ Done | Compile + run shell script |
| `tests/WasmTests.fsproj` | ✅ Done | Test project referencing Wasmecto |
| `tests/Tests.Math.fs` | ✅ Done | 10 `System.Math` tests |
| `tests/Tests.Dictionary.fs` | ✅ Done | 9 Dictionary tests (including stress test) |
| `tests/Tests.Stack.fs` | ✅ Done | 9 Stack tests |
| `tests/Tests.StringBuilder.fs` | ✅ Done | 8 StringBuilder tests |
| `tests/Main.fs` | ✅ Done | Test registry entry point |

**Compiler exception limitation**: The WasmGC backend uses a zero-payload signal
tag for `throw` + `catch_all` for catch.  Typed catch patterns (`:? SomeException`)
will not propagate the exception value to the handler.  `ExnState` pattern
sidesteps this; see `docs/internal/specs/TYPED-EXCEPTION-SPEC.md` for the
upgrade plan (Sprint 30).

