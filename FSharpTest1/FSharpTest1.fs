module FSharpTest1

open NUnit.Framework
open Swensen.Unquote

[<Test>]
let ``test get sales people``() =
    let salesPeople =
        async {
            let! result = FSharpWeb1.DataAccess.getSalesPeople(3L, "United States", 1000000M)
            return result |> Seq.toArray
        }
        |> Async.RunSynchronously
    test <@ salesPeople.Length = 3 @>
