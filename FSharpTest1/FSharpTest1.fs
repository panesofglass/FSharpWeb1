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


open System
open System.Net
open System.Net.Http
open System.Web.Http

// Rather than using an if/then/else branch structure, let's leverage F# Active Patterns.
let (|JSON|_|) (response: HttpResponseMessage) =
    if response.StatusCode = HttpStatusCode.OK &&
       response.Content.Headers.ContentType.MediaType = "application/json" then
        let content = response.Content.ReadAsAsync<Newtonsoft.Json.Linq.JToken>()
        Some(response.Headers, content)
    else None

let (|OK|_|) (response: HttpResponseMessage) =
    if response.StatusCode = HttpStatusCode.OK then
        Some(response.Headers, response.Content)
    else None
  
let (|BadRequest|_|) (response: HttpResponseMessage) =
    if response.StatusCode = HttpStatusCode.BadRequest then
        Some(response.Headers, response.Content)
    else None
 
let (|NotFound|_|) (response: HttpResponseMessage) =
    if response.StatusCode = HttpStatusCode.NotFound then
        Some(response.Headers, response.Content)
    else None


[<Test>]
let ``Test client can use active patterns``() = 
    let config = new HttpConfiguration()
    config |> HttpResource.register [FSharpWeb1.SalesPeople.salesPeopleResource]
    let server = new HttpServer(config)
    let client = new HttpClient(server)

    // Set up your request
    let request = new HttpRequestMessage()
    request.RequestUri <- Uri("http://localhost:48213/salespeople?topN=3&region=United%20States&sales=100000")

    async {
        let! token = Async.CancellationToken
        use! response = Async.AwaitTask <| client.SendAsync(request, token)
        match response with
        | JSON(_, content) ->
            let! json = content |> Async.AwaitTask
            Assert.That(response.StatusCode = HttpStatusCode.OK)
            // In the case above, we will retrieve a JSON array.
            Assert.IsAssignableFrom<Newtonsoft.Json.Linq.JArray>(json)
            client.Dispose()
        | OK(_, content) -> // content removed for clarity
            let! result = content.ReadAsStringAsync() |> Async.AwaitTask
            Assert.That(response.StatusCode = HttpStatusCode.OK)
            client.Dispose()
        | BadRequest(_, content) ->
            let! result = content.ReadAsStringAsync() |> Async.AwaitTask
            Assert.That(response.StatusCode = HttpStatusCode.BadRequest)
            client.Dispose()
        | NotFound(_, content) ->
            let! result = content.ReadAsStringAsync() |> Async.AwaitTask
            Assert.That(response.StatusCode = HttpStatusCode.NotFound)
            client.Dispose()
        | _ -> Assert.Fail("Received an unexpected response")
    } |> Async.RunSynchronously
