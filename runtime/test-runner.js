#!/usr/bin/env node
// test-runner.js — WasmGC test harness for Wasmecto
// Usage: node test-runner.js <wasmfile> [--tap-output]
//
// Protocol:
//   The .wasm module imports:
//     (import "wasmecto" "writeLine" (func (param (ref $WasmStr))))
//   The .wasm module exports:
//     (func (export "wasmecto_run") (result i32))   ← returns number of failures
//
// Exit code = number of failing tests (0 = success).

import { readFileSync } from 'fs';
import { argv, exit } from 'process';

const wasmFile = argv[2];
if (!wasmFile) {
  console.error('Usage: node test-runner.js <wasmfile>');
  exit(1);
}

const bytes = readFileSync(wasmFile);
let failCount = 0;

// writeLine import: the test framework calls this for every TAP line
const importObject = {
  wasmecto: {
    writeLine: (str) => {
      process.stdout.write(str + '\n');
    }
  }
};

const { instance } = await WebAssembly.instantiate(bytes, importObject);

if (!instance.exports.wasmecto_run) {
  console.error('ERROR: wasm module does not export wasmecto_run');
  exit(2);
}

failCount = instance.exports.wasmecto_run();

if (failCount === 0) {
  process.stderr.write(`\nAll tests passed.\n`);
} else {
  process.stderr.write(`\n${failCount} test(s) FAILED.\n`);
}

exit(failCount);
