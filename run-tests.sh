#!/usr/bin/env bash
# run-tests.sh — compile and run Wasmecto tests via wasmtime
# Usage: ./run-tests.sh [test-project-dir]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$SCRIPT_DIR"
FSHARP_WASM="${FSHARP_WASM:-$ROOT/..}"
TEST_PROJ="${1:-$ROOT/tests}"
OUT="$ROOT/dist"

mkdir -p "$OUT"

# The test project reaches fsharp-wasm-core through a ProjectReference. FCS wants the
# reference assembly (obj/<cfg>/<tfm>/ref/*.dll) on disk, so build it first.
echo "==> Building project references (reference assemblies)..."
dotnet build "$FSHARP_WASM/fsharp-wasm-core/FSharp.Wasm.Core/FSharp.WasmCore.fsproj" \
    -c Release --nologo -v q

echo "==> Compiling Wasmecto + tests via fsharp-wasm..."
dotnet run --project "$FSHARP_WASM/tools/FSharp.Wasm.Cli" -- \
    "$TEST_PROJ/WasmTests.fsproj" \
    --outDir "$OUT"

echo "==> Running tests in wasmtime..."
STATUS=0
wasmtime run "$OUT/wasmtests.wasm" 2>&1 || STATUS=$?

if [ "$STATUS" -eq 0 ]; then
    echo "All tests passed."
else
    echo "FAILED: exit code $STATUS."
fi

exit "$STATUS"
