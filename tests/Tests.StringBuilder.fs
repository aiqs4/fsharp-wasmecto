module WasmTests.TestsStringBuilder

open Wasmecto
open System.Text

[<AutoOpen>]
module StringBuilderTests =
    let tests =
        testList "StringBuilder" [
            testCase "Empty has length 0" <| fun () ->
                let sb = StringBuilder()
                Expect.equal sb.Length 0 "empty length"

            testCase "Append string" <| fun () ->
                let sb = StringBuilder()
                sb.Append("hello") |> ignore
                sb.Append(" world") |> ignore
                Expect.equal (sb.ToString()) "hello world" "append two strings"

            testCase "Append int" <| fun () ->
                let sb = StringBuilder()
                sb.Append(42) |> ignore
                Expect.equal (sb.ToString()) "42" "int appended"

            testCase "AppendLine" <| fun () ->
                let sb = StringBuilder()
                sb.AppendLine("line1") |> ignore
                sb.AppendLine("line2") |> ignore
                let r = sb.ToString()
                Expect.isTrue (r.Contains("line1")) "contains line1"
                Expect.isTrue (r.Contains("line2")) "contains line2"

            testCase "AppendFormat" <| fun () ->
                let sb = StringBuilder()
                sb.AppendFormat("x={0}", 7) |> ignore
                Expect.equal (sb.ToString()) "x=7" "AppendFormat"

            testCase "Clear" <| fun () ->
                let sb = StringBuilder("nonempty")
                sb.Clear() |> ignore
                Expect.equal sb.Length 0 "length 0 after Clear"
                Expect.equal (sb.ToString()) "" "empty after Clear"

            testCase "Constructor from string" <| fun () ->
                let sb = StringBuilder("init")
                Expect.equal (sb.ToString()) "init" "constructor value"

            testCase "Length tracking" <| fun () ->
                let sb = StringBuilder()
                sb.Append("abc") |> ignore
                Expect.equal sb.Length 3 "length 3 after abc"
                sb.Append("de") |> ignore
                Expect.equal sb.Length 5 "length 5 after de"
        ]
