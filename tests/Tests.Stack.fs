module WasmTests.TestsStack

open Wasmecto
open System.Collections.Generic

[<AutoOpen>]
module StackTests =
    let tests =
        testList "Stack<T>" [
            testCase "New stack is empty" <| fun () ->
                let s = Stack<int>()
                Expect.equal s.Count 0 "empty count"

            testCase "Push / Pop LIFO" <| fun () ->
                let s = Stack<int>()
                s.Push 1; s.Push 2; s.Push 3
                Expect.equal (s.Pop()) 3 "pop 3"
                Expect.equal (s.Pop()) 2 "pop 2"
                Expect.equal (s.Pop()) 1 "pop 1"

            testCase "Peek does not remove" <| fun () ->
                let s = Stack<string>()
                s.Push "top"
                Expect.equal (s.Peek()) "top" "peek = top"
                Expect.equal s.Count 1 "count unchanged"

            testCase "Pop on empty throws" <| fun () ->
                let s = Stack<int>()
                Expect.throws (fun () -> s.Pop() |> ignore) "pop empty throws"

            testCase "TryPop empty returns false" <| fun () ->
                let s = Stack<int>()
                let mutable v = 0
                Expect.isFalse (s.TryPop(&v)) "TryPop returns false"

            testCase "TryPop returns value" <| fun () ->
                let s = Stack<int>()
                s.Push 99
                let mutable v = 0
                let ok = s.TryPop(&v)
                Expect.isTrue ok    "TryPop returns true"
                Expect.equal v 99   "value is 99"

            testCase "Contains" <| fun () ->
                let s = Stack<string>()
                s.Push "a"; s.Push "b"
                Expect.isTrue  (s.Contains "a") "contains a"
                Expect.isFalse (s.Contains "z") "not contains z"

            testCase "Clear" <| fun () ->
                let s = Stack<int>()
                s.Push 1; s.Push 2
                s.Clear()
                Expect.equal s.Count 0 "clear resets count"

            testCase "ToArray — top first" <| fun () ->
                let s = Stack<int>()
                s.Push 1; s.Push 2; s.Push 3
                let arr = s.ToArray()
                Expect.equal arr.[0] 3 "top-first"
                Expect.equal arr.[2] 1 "bottom-last"
        ]
