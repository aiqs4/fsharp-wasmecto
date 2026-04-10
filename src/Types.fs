/// Wasmecto — minimal Expecto-compatible test framework for WasmGC.
/// Pure F# — no reflection, no System.Threading, no async (until sprint 29).
/// API-compatible with Expecto: replace `open Expecto` with `open Wasmecto`.
namespace Wasmecto

open System

// ─── Focus state (mirrors Expecto) ──────────────────────────────────────────

type FocusState =
    | Normal
    | Pending
    | Focused

// ─── Test tree ───────────────────────────────────────────────────────────────

type TestCode =
    | Sync of (unit -> unit)
    // Async will be added in sprint 29 once WasmGC async lands in the compiler

type Test =
    | TestCase  of name: string * code: TestCode * state: FocusState
    | TestList  of name: string * tests: Test list * state: FocusState
    | TestLabel of name: string * test: Test * state: FocusState

// ─── Test result ─────────────────────────────────────────────────────────────

type TestResult =
    | Passed
    | Failed  of message: string
    | Errored of message: string  // holds the error message; exn object not accessible under WasmGC catch_all
    | Skipped of reason: string

type TestSummary =
    { Name    : string
      Result  : TestResult }
