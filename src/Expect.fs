/// Assertion helpers — mirrors Expecto.Expect API.
/// All functions raise FailException on failure; the runner catches them.
///
/// WasmGC note: the fsharp-wasm runtime currently cannot pass an exception
/// value through a catch handler (signal-only tag).  To work around this,
/// `fail` writes into ExnState BEFORE throwing so the runner can read the
/// message even though it uses a plain `catch_all`.  A future compiler sprint
/// will implement typed catch and make ExnState redundant.
module Wasmecto.Expect

open System

exception FailException of string

// ─── Core helpers ────────────────────────────────────────────────────────────

let inline private fail msg =
    ExnState.Kind <- 1
    ExnState.Msg  <- msg
    raise (FailException msg)

let equal (actual: 'a) (expected: 'a) (msg: string) =
    if actual <> expected then
        fail (sprintf "%s.\n  Expected: %A\n  Actual:   %A" msg expected actual)

let notEqual (actual: 'a) (expected: 'a) (msg: string) =
    if actual = expected then
        fail (sprintf "%s. Did not expect: %A" msg actual)

// ─── Boolean ─────────────────────────────────────────────────────────────────

let isTrue (actual: bool) (msg: string) =
    if not actual then fail msg

let isFalse (actual: bool) (msg: string) =
    if actual then fail (sprintf "%s. Expected false but got true." msg)

// ─── Null checks ─────────────────────────────────────────────────────────────

let isNull (actual: obj) (msg: string) =
    if not (obj.ReferenceEquals(actual, null)) then
        fail (sprintf "%s. Expected null but got: %A" msg actual)

let isNotNull (actual: obj) (msg: string) =
    if obj.ReferenceEquals(actual, null) then
        fail (sprintf "%s. Expected non-null." msg)

// ─── Exception ───────────────────────────────────────────────────────────────

/// Verifies that `f` throws any exception.
let throws (f: unit -> unit) (msg: string) =
    let threw =
        try f (); false
        with _ -> true
    if not threw then
        fail (sprintf "%s. Expected f to throw but it did not." msg)

/// Verifies that `f` throws any exception (type parameter is documentation-only
/// on WasmGC — typed catches require a future compiler sprint).
[<RequiresExplicitTypeArguments>]
let throwsT<'E when 'E :> exn> (f: unit -> unit) (msg: string) =
    // WasmGC limitation: catch_all cannot type-test the exception.
    // We verify that *something* was thrown; type checking is a no-op for now.
    let threw =
        try f (); false
        with _ -> true
    if not threw then
        fail (sprintf "%s. Expected f to throw %s." msg (typeof<'E>.Name))

// ─── String ──────────────────────────────────────────────────────────────────

let stringContains (actual: string) (substring: string) (msg: string) =
    if not (actual.Contains(substring)) then
        fail (sprintf "%s.\n  Expected '%s'\n  to contain '%s'." msg actual substring)

let stringStarts (actual: string) (prefix: string) (msg: string) =
    if not (actual.StartsWith(prefix)) then
        fail (sprintf "%s.\n  Expected '%s'\n  to start with '%s'." msg actual prefix)

let stringEnds (actual: string) (suffix: string) (msg: string) =
    if not (actual.EndsWith(suffix)) then
        fail (sprintf "%s.\n  Expected '%s'\n  to end with '%s'." msg actual suffix)

let stringHasLength (actual: string) (expected: int) (msg: string) =
    if actual.Length <> expected then
        fail (sprintf "%s. Expected length %d but got %d." msg expected actual.Length)

// ─── Sequences ───────────────────────────────────────────────────────────────

let sequenceEqual (actual: 'a seq) (expected: 'a seq) (msg: string) =
    let al = Seq.toList actual
    let el = Seq.toList expected
    if al <> el then
        fail (sprintf "%s.\n  Expected: %A\n  Actual:   %A" msg el al)

let isEmpty (actual: 'a seq) (msg: string) =
    if not (Seq.isEmpty actual) then
        fail (sprintf "%s. Expected empty sequence but got %d elements." msg (Seq.length actual))

let isNonEmpty (actual: 'a seq) (msg: string) =
    if Seq.isEmpty actual then
        fail (sprintf "%s. Expected non-empty sequence." msg)

let hasLength (actual: 'a seq) (expected: int) (msg: string) =
    let len = Seq.length actual
    if len <> expected then
        fail (sprintf "%s. Expected length %d but got %d." msg expected len)

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

// ─── General ─────────────────────────────────────────────────────────────────

let wantSome (opt: 'a option) (msg: string) : 'a =
    match opt with
    | Some v -> v
    | None   -> fail (sprintf "%s. Expected Some but got None." msg); Unchecked.defaultof<'a>

let wantOk (r: Result<'a,'e>) (msg: string) : 'a =
    match r with
    | Ok v    -> v
    | Error e -> fail (sprintf "%s. Expected Ok but got Error: %A." msg e); Unchecked.defaultof<'a>
