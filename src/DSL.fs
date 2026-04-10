/// DSL for building test trees — mirrors Expecto's user-facing API.
[<AutoOpen>]
module Wasmecto.Tests

open Wasmecto

// ─── Builders ────────────────────────────────────────────────────────────────

let testCase (name: string) (body: unit -> unit) : Test =
    TestCase(name, Sync body, Normal)

/// Alias for testCase — matches Expecto's `test` CE syntax sugar.
let test (name: string) (body: unit -> unit) : Test =
    TestCase(name, Sync body, Normal)

let testList (name: string) (tests: Test list) : Test =
    TestList(name, tests, Normal)

/// Mark a single test as pending (skipped).
let ptestCase (name: string) (_body: unit -> unit) : Test =
    TestCase(name, Sync(fun () -> ()), Pending)

let pending (name: string) : Test =
    TestCase(name, Sync(fun () -> ()), Pending)

/// Mark an entire list as pending.
let ptestList (name: string) (tests: Test list) : Test =
    TestList(name, tests, Pending)

/// Focus a single test (all unfocused tests skip).
let ftestCase (name: string) (body: unit -> unit) : Test =
    TestCase(name, Sync body, Focused)

let ftestList (name: string) (tests: Test list) : Test =
    TestList(name, tests, Focused)
