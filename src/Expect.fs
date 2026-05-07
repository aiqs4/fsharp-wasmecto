/// Assertion helpers — mirrors Expecto.Expect API.
/// All functions raise FailException on failure; the runner catches them.
///
/// WasmGC note: the fsharp-wasm runtime currently cannot pass an exception
/// value through a catch handler (signal-only tag).  To work around this,
/// `fail` writes into ExnState BEFORE throwing so the runner can read the
/// message even though it uses a plain `catch_all`.  A future compiler sprint
/// will implement typed catch and make ExnState redundant.
///
/// IMPORTANT: for now just workaround of missing features in WasmGC
/// WasmGC-compatible: no reflection, no typeof<'E>, uses ExnState for throw signalling.
module Wasmecto.Expect

open System

exception FailException of string

// ─── Core helpers ────────────────────────────────────────────────────────────

let inline private fail (msg: string) =
    ExnState.Kind <- 1
    ExnState.Msg  <- msg
    raise (FailException msg)

let inline private failtestf fmt = Printf.ksprintf fail fmt

// ─── Equality ─────────────────────────────────────────────────────────────────

let equal (actual: 'a) (expected: 'a) (msg: string) =
    if actual <> expected then
        fail (sprintf "%s.\n  Expected: %A\n  Actual:   %A" msg expected actual)

let notEqual (actual: 'a) (expected: 'a) (msg: string) =
    if actual = expected then
        fail (sprintf "%s. Did not expect: %A" msg actual)

// ─── Boolean ──────────────────────────────────────────────────────────────────

let isTrue (actual: bool) (msg: string) =
    if not actual then fail msg

let isFalse (actual: bool) (msg: string) =
    if actual then fail (sprintf "%s. Expected false but got true." msg)

// ─── Null checks / option ────────────────────────────────────────────────────────────

let isNull (actual: obj) (msg: string) =
    if not (obj.ReferenceEquals(actual, null)) then
        fail (sprintf "%s. Expected null but got: %A" msg actual)

let isNotNull (actual: obj) (msg: string) =
    if obj.ReferenceEquals(actual, null) then
        fail (sprintf "%s. Expected non-null." msg)

let isSome (opt: 'a option) (msg: string) =
    match opt with
    | None -> fail (sprintf "%s. Expected Some but got None." msg)
    | _    -> ()

let isNone (opt: 'a option) (msg: string) =
    match opt with
    | Some _ -> fail (sprintf "%s. Expected None but got Some." msg)
    | _      -> ()

let wantSome (opt: 'a option) (msg: string) : 'a =
    match opt with
    | Some v -> v
    | None   ->
        fail (sprintf "%s. Expected Some but got None." msg)
        Unchecked.defaultof<'a>

let wantOk (r: Result<'a,'e>) (msg: string) : 'a =
    match r with
    | Ok v    -> v
    | Error e ->
        fail (sprintf "%s. Expected Ok but got Error: %A." msg e)
        Unchecked.defaultof<'a>

// ─── Exception ────────────────────────────────────────────────────────────────

/// Verifies that `f` throws any exception.
let throws (f: unit -> unit) (msg: string) =
    let mutable threw = false
    try f ()
    with _ -> threw <- true
    if not threw then
        fail (sprintf "%s. Expected exception but none was thrown." msg)

/// Verifies that `f` throws any exception (type parameter is documentation-only
/// on WasmGC — typed catches require a future compiler sprint).
/// TODO: for now: WasmGC note: catch_all cannot distinguish exception types.
/// Use this when you only care that SOME exception is thrown.
[<RequiresExplicitTypeArguments>]
let throwsT<'E when 'E :> exn> (f: unit -> unit) (msg: string) =
    // WasmGC limitation: catch_all cannot type-test the exception.
    // We verify that *something* was thrown; type checking is a no-op for now.
    let threw =
        try f (); false
        with _ -> true
    if not threw then
        fail (sprintf "%s. Expected an exception but none was thrown." msg)
        // TODO: no typeof support in WasmGC now
        // fail (sprintf "%s. Expected f to throw %s." msg (typeof<'E>.Name))
    // Note: type check (typeof<'E>) omitted — Fable erases generic parameters.

/// Verify that f() does NOT throw.
let doesNotThrow (f: unit -> unit) (msg: string) =
    try f ()
    with _ ->
        let exnMsg =
            match ExnState.Kind with
            | 1 -> ExnState.Msg
            | _ -> "unexpected exception"
        fail (sprintf "%s. Expected no exception but got: %s" msg exnMsg)

// ─── String ──────────────────────────────────────────────────────────────────

let stringContains (actual: string) (substring: string) (msg: string) =
    if not (actual.Contains(substring)) then
        fail (sprintf "%s.\n  '%s'\n  does not contain '%s'." msg actual substring)

let stringStarts (actual: string) (prefix: string) (msg: string) =
    if not (actual.StartsWith(prefix)) then
        fail (sprintf "%s.\n  '%s'\n  does not start with '%s'." msg actual prefix)

let stringEnds (actual: string) (suffix: string) (msg: string) =
    if not (actual.EndsWith(suffix)) then
        fail (sprintf "%s.\n  '%s'\n  does not end with '%s'." msg actual suffix)

let stringHasLength (actual: string) (expected: int) (msg: string) =
    if actual.Length <> expected then
        fail (sprintf "%s. Expected length %d but got %d." msg expected actual.Length)

// ─── Sequences (list for now untill seq lands in compiler) ─────────────────────────────────────────

// TODO: we use list till seq is supported in the compiler
let isEmpty (actual: 'a list) (msg: string) =
    match actual with
    | [] -> ()
    | _  -> fail (sprintf "%s. Expected empty list but got %d element(s)." msg actual.Length)

// TODO: we use list till seq is supported in the compiler
let sequenceEqual (actual: 'a seq) (expected: 'a seq) (msg: string) =
    let al = Seq.toList actual
    let el = Seq.toList expected
    if al <> el then
        fail (sprintf "%s.\n  Expected: %A\n  Actual:   %A" msg el al)

// TODO: we use list till seq is supported in the compiler
// let isEmpty (actual: 'a seq) (msg: string) =
//     if not (Seq.isEmpty actual) then
//         fail (sprintf "%s. Expected empty sequence but got %d elements." msg (Seq.length actual))

// TODO: we use list till seq is supported in the compiler
// let isNonEmpty (actual: 'a seq) (msg: string) =
//     if Seq.isEmpty actual then
//         fail (sprintf "%s. Expected non-empty sequence." msg)

// TODO: we use list till seq is supported in the compiler
let isNonEmpty (actual: 'a list) (msg: string) =
    match actual with
    | [] -> fail (sprintf "%s. Expected non-empty list." msg)
    | _  -> ()

// TODO: we use list till seq is supported in the compiler
let hasLength (actual: 'a list) (expected: int) (msg: string) =
    if actual.Length <> expected then
        fail (sprintf "%s. Expected length %d but got %d." msg expected actual.Length)

// TODO: we use list till seq is supported in the compiler
// let hasLength (actual: 'a seq) (expected: int) (msg: string) =
//     let len = Seq.length actual
//     if len <> expected then
//         fail (sprintf "%s. Expected length %d but got %d." msg expected len)

// ─── Numeric ─────────────────────────────────────────────────────────────────

let approxEqual (actual: float) (expected: float) (epsilon: float) (msg: string) =
    if abs (actual - expected) > epsilon then
        fail (sprintf "%s.\n  Expected ~%g (±%g) but got %g." msg expected epsilon actual)

let isLessThan (actual: 'a when 'a: comparison) (limit: 'a) (msg: string) =
    if not (actual < limit) then
        fail (sprintf "%s. Expected %A < %A." msg actual limit)

let isGreaterThan (actual: 'a when 'a: comparison) (limit: 'a) (msg: string) =
    if not (actual > limit) then
        fail (sprintf "%s. Expected %A > %A." msg actual limit)

let isLessThanOrEqual (actual: 'a when 'a: comparison) (limit: 'a) (msg: string) =
    if not (actual <= limit) then
        fail (sprintf "%s. Expected %A ≤ %A." msg actual limit)

let isGreaterThanOrEqual (actual: 'a when 'a: comparison) (limit: 'a) (msg: string) =
    if not (actual >= limit) then
        fail (sprintf "%s. Expected %A ≥ %A." msg actual limit)

// ─── Array / sequence helpers ─────────────────────────────────────────────────

let arrayEqual (actual: 'a[]) (expected: 'a[]) (msg: string) =
    if actual.Length <> expected.Length then
        fail (sprintf "%s. Length mismatch: expected %d, got %d." msg expected.Length actual.Length)
    let mutable i = 0
    while i < actual.Length do
        if actual.[i] <> expected.[i] then
            fail (sprintf "%s. Mismatch at index %d: expected %A, got %A." msg i expected.[i] actual.[i])
        i <- i + 1

let listEqual (actual: 'a list) (expected: 'a list) (msg: string) =
    if actual <> expected then
        fail (sprintf "%s.\n  Expected: %A\n  Actual:   %A" msg expected actual)

// ─── Convenience ─────────────────────────────────────────────────────────────

let inline skip () = ()   // mark a test body as intentionally skipped (no-op)
