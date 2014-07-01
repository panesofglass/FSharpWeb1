namespace FSharpWeb1

open System
open System.Net
open System.Net.Http
open System.Web
open System.Web.Http
open System.Web.Http.HttpResource
open System.Web.Routing

module SalesPeople =
    open FSharp.Data

    [<Literal>]
    let connectionString = "name=AdventureWorks"

    type AdventureWorks = SqlProgrammabilityProvider<connectionString>

    let db = new AdventureWorks()

    type SalesPeople = SqlCommandProvider<"
        SELECT TOP(@TopN) BusinessEntityID, FirstName, LastName, SalesYTD 
        FROM Sales.vSalesPerson
        WHERE CountryRegionName = @regionName AND SalesYTD > @salesMoreThan
        ORDER BY SalesYTD", connectionString>
    
    let getSalesPeople(topN, regionName, salesMoreThan) =
        async {
            use cmd = new SalesPeople()
            return! cmd.AsyncExecute(TopN = topN, regionName = regionName, salesMoreThan = salesMoreThan)
        }
        |> Async.Catch
    
    let salesPeopleHandler (request: HttpRequestMessage) =
        let queryString =
            request.GetQueryNameValuePairs()
            |> Seq.toArray
            |> Array.Parallel.map (fun (KeyValue(key, value)) -> key, value)
        let topN = queryString |> Array.tryFind (fun (key, _) -> key = "topN") |> Option.map (snd >> int64)
        let region = queryString |> Array.tryFind (fun (key, _) -> key = "region") |> Option.map snd
        let sales = queryString |> Array.tryFind (fun x -> (fst x) = "sales") |> Option.map (snd >> decimal)
        async {
            match topN, region, sales with
            | Some(topN), Some(regionName), Some(salesMoreThan) ->
                let! result = getSalesPeople(topN, regionName, salesMoreThan)
                match result with
                | Choice1Of2 res -> return request.CreateResponse(res |> Seq.toArray)
                | Choice2Of2 e   -> return request.CreateErrorResponse(HttpStatusCode.InternalServerError, e)
            | _ -> return request.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing query string parameters")
        }

    let salesPeopleResource = route "/salespeople" <| get salesPeopleHandler

type HttpRoute = {
    controller : string
    id : RouteParameter }

type Global() =
    inherit System.Web.HttpApplication() 

    static member RegisterWebApi(config: HttpConfiguration) =
        config |> register [SalesPeople.salesPeopleResource]

        // Configure routing
        config.MapHttpAttributeRoutes()
        config.Routes.MapHttpRoute(
            "DefaultApi", // Route name
            "api/{controller}/{id}", // URL with parameters
            { controller = "{controller}"; id = RouteParameter.Optional } // Parameter defaults
        ) |> ignore

        // Configure serialization
        config.Formatters.XmlFormatter.UseXmlSerializer <- true
        config.Formatters.JsonFormatter.SerializerSettings.ContractResolver <- Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()

        // Additional Web API settings

    member x.Application_Start() =
        GlobalConfiguration.Configure(Action<_> Global.RegisterWebApi)
