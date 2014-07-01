module FSharpTest1

open NUnit.Framework
open Swensen.Unquote

[<Test>]
let ``test get sales people``() =
    let salesPeople =
        async {
            let! result = FSharpWeb1.SalesPeople.getSalesPeople(3L, "United States", 1000000M)
            match result with
            | Choice1Of2 res ->
                return res |> Seq.toArray
            | Choice2Of2 e   -> return [||]
        }
        |> Async.RunSynchronously
    test <@ salesPeople.Length = 3 @>
