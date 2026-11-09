module WasmTests.TestsDictionary

open Wasmecto
open System.Collections.Generic

[<AutoOpen>]
module DictionaryTests =
    let tests =
        testList "Dictionary<K,V>" [
            testCase "Add and retrieve" <| fun () ->
                let d = Dictionary<string, int>()
                d.["hello"] <- 42
                Expect.equal d.["hello"] 42 "retrieve after set"

            testCase "ContainsKey" <| fun () ->
                let d = Dictionary<int, int>()
                d.[1] <- 10
                Expect.isTrue  (d.ContainsKey 1) "contains 1"
                Expect.isFalse (d.ContainsKey 2) "not contains 2"

            testCase "Remove" <| fun () ->
                let d = Dictionary<string, int>()
                d.["x"] <- 1
                let removed = d.Remove "x"
                Expect.isTrue  removed         "Remove returns true"
                Expect.isFalse (d.ContainsKey "x") "no longer present"

            testCase "TryGetValue" <| fun () ->
                let d = Dictionary<int, string>()
                d.[7] <- "seven"
                let mutable v = ""
                let ok = d.TryGetValue(7, &v)
                Expect.isTrue ok          "TryGetValue returns true"
                Expect.equal v "seven"    "TryGetValue yields correct value"

            testCase "Duplicate key throws" <| fun () ->
                let d = Dictionary<int, int>()
                d.Add(1, 1)
                Expect.throws (fun () -> d.Add(1, 2)) "duplicate Add throws"

            testCase "Count lifecycle" <| fun () ->
                let d = Dictionary<int, int>()
                Expect.equal d.Count 0 "empty count"
                d.[1] <- 1; d.[2] <- 2
                Expect.equal d.Count 2 "count after 2 adds"
                d.Remove 1 |> ignore
                Expect.equal d.Count 1 "count after remove"

            testCase "Clear" <| fun () ->
                let d = Dictionary<int, int>()
                d.[1] <- 1; d.[2] <- 2
                d.Clear()
                Expect.equal d.Count 0 "count after Clear"

            testCase "Hash collision stress (999 entries)" <| fun () ->
                let d = Dictionary<int, int>()
                for i in 1 .. 999 do d.[i] <- i * 2
                Expect.equal d.Count 999 "all 999 entries stored"
                for i in 1 .. 999 do
                    Expect.equal d.[i] (i * 2) (sprintf "entry %d" i)

            testCase "Enumerate KVPs" <| fun () ->
                let d = Dictionary<int, int>()
                for i in 1..5 do d.[i] <- i
                let sum = d.SumValues id   // Dictionary deliberately has no IEnumerable<KVP> (WasmGC vtable limitation)
                Expect.equal sum 15 "sum of values 1..5 = 15"
        ]
