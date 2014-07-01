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
        SELECT TOP(@TopN) s.BusinessEntityID, p.FirstName, p.LastName, s.SalesYTD 
        FROM [Sales].[SalesPerson] s
            INNER JOIN [HumanResources].[Employee] e 
            ON e.[BusinessEntityID] = s.[BusinessEntityID]
            INNER JOIN [Person].[Person] p
            ON p.[BusinessEntityID] = s.[BusinessEntityID]
            INNER JOIN [Person].[BusinessEntityAddress] bea 
            ON bea.[BusinessEntityID] = s.[BusinessEntityID] 
            INNER JOIN [Person].[Address] a 
            ON a.[AddressID] = bea.[AddressID]
            INNER JOIN [Person].[StateProvince] sp 
            ON sp.[StateProvinceID] = a.[StateProvinceID]
            INNER JOIN [Person].[CountryRegion] cr 
            ON cr.[CountryRegionCode] = sp.[CountryRegionCode]
        WHERE cr.Name = @regionName AND s.SalesYTD > @salesMoreThan
        ORDER BY s.SalesYTD DESC
        ", connectionString>
    
    let getSalesPeople(topN, regionName, salesMoreThan) =
        async {
            use cmd = new SalesPeople()
            return! cmd.AsyncExecute(TopN = topN, regionName = regionName, salesMoreThan = salesMoreThan)
        }
        |> Async.Catch
    
    type Result<'TSuccess, 'TFailure> =
        | Success of 'TSuccess
        | Failure of 'TFailure
    
    let validate (people: seq<SalesPeople.Record>) =
        let people = people |> Seq.cache
        if Seq.length people > 5 then
            Choice2Of2(exn "Requested result set too large.")
        else Choice1Of2(people)
    
    let toResponse (request: HttpRequestMessage) =
        function
        | Choice1Of2 people   -> request.CreateResponse people
        | Choice2Of2 (e: exn) -> request.CreateErrorResponse(HttpStatusCode.BadRequest, e)
    
    let bind<'T, 'U> (f: 'T -> Choice<'U, exn>) =
        function
        | Choice1Of2 res -> f res
        | Choice2Of2 e   -> Choice2Of2 e
    
    let (>>=) result f = bind f result
    
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
                return
                    result
                    >>= validate
                    |> toResponse request
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
