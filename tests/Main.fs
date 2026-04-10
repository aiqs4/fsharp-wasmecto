/// Entry point — register all test modules.
/// The canonical WASM export `wasmecto_run` is defined in Wasmecto.Runner
/// and automatically compiled into the output; this module just populates
/// the registry before that export is called.
module WasmTests.Main

open Wasmecto
open Wasmecto.Runner
open WasmTests.TestsMath
open WasmTests.TestsDictionary
open WasmTests.TestsStack
open WasmTests.TestsStringBuilder

// Register all test suites (module-initializer runs at WASM startup)
do
    register [
        MathTests.tests
        DictionaryTests.tests
        StackTests.tests
        StringBuilderTests.tests
    ]
