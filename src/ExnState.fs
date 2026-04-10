/// Module-level mutable state that communicates exception information across
/// a throw/catch boundary.
///
/// RATIONALE: The fsharp-wasm WasmGC exception system currently uses a
/// zero-payload signal tag (`$fsharp_exn` with no parameters) together with
/// `catch_all`.  A `catch_all` handler receives no value from the stack, so
/// the catch branch has no way to inspect the exception object directly.
///
/// Instead of changing the compiler now, we use the "exception tracker"
/// idiom: any code that is about to raise an exception writes its type/message
/// into this module BEFORE throwing.  The catch handler reads the tracker
/// after catching the (anonymous) signal.
///
/// A proper typed-catch implementation (where `catch $fsharp_exn` pushes the
/// exception struct onto the stack) is planned as a compiler sprint and will
/// make this module redundant.
module internal Wasmecto.ExnState

/// 0 = no exception in flight
/// 1 = FailException (assertion failure)
/// 2 = unknown runtime exception
let mutable Kind : int = 0

/// Descriptive message for the exception.
let mutable Msg  : string = ""

/// Call this before every test to clear any left-over state.
let reset () =
    Kind <- 0
    Msg  <- ""
