module WasmTests.TestsMath

open Wasmecto
open System

[<AutoOpen>]
module MathTests =
    let tests =
        testList "System.Math" [
            testCase "Abs int" <| fun () ->
                Expect.equal (Math.Abs -5) 5 "abs(-5) = 5"
                Expect.equal (Math.Abs  5) 5 "abs(5)  = 5"
                Expect.equal (Math.Abs  0) 0 "abs(0)  = 0"

            testCase "Abs float" <| fun () ->
                Expect.equal (Math.Abs -3.14) 3.14 "abs(-3.14)"

            testCase "Min / Max int" <| fun () ->
                Expect.equal (Math.Min(2, 5)) 2 "min(2,5)=2"
                Expect.equal (Math.Max(2, 5)) 5 "max(2,5)=5"

            testCase "Min / Max float" <| fun () ->
                Expect.equal (Math.Min(1.0, 2.0)) 1.0 "min(1,2)=1"
                Expect.equal (Math.Max(1.0, 2.0)) 2.0 "max(1,2)=2"

            testCase "Clamp" <| fun () ->
                Expect.equal (Math.Clamp(5, 1, 10)) 5  "clamp(5,1,10)=5"
                Expect.equal (Math.Clamp(0, 1, 10)) 1  "clamp(0,1,10)=1"
                Expect.equal (Math.Clamp(15,1, 10)) 10 "clamp(15,1,10)=10"

            testCase "Sqrt" <| fun () ->
                Expect.approxEqual (Math.Sqrt 4.0) 2.0 1e-10 "sqrt(4)=2"
                Expect.approxEqual (Math.Sqrt 9.0) 3.0 1e-10 "sqrt(9)=3"

            testCase "Pow" <| fun () ->
                Expect.approxEqual (Math.Pow(2.0, 10.0)) 1024.0 1e-9 "2^10=1024"

            testCase "Floor / Ceiling / Round" <| fun () ->
                Expect.equal (Math.Floor   3.7) 3.0 "floor(3.7)=3"
                Expect.equal (Math.Ceiling 3.2) 4.0 "ceil(3.2)=4"
                Expect.equal (Math.Round   3.5) 4.0 "round(3.5)=4"

            testCase "Sign" <| fun () ->
                Expect.equal (Math.Sign -7) -1 "sign(-7)=-1"
                Expect.equal (Math.Sign  0)  0 "sign(0)=0"
                Expect.equal (Math.Sign  7)  1 "sign(7)=1"

            testCase "PI and E constants" <| fun () ->
                Expect.approxEqual Math.PI 3.14159265358979 1e-10 "PI"
                Expect.approxEqual Math.E  2.71828182845904 1e-10 "E"
        ]
