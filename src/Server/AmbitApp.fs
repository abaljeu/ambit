namespace Gambol.Server

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration

type Authentication =
    {
        ExpectedUser: string
        ExpectedPass: string
        GitToken: string
        IsAuthenticated: HttpRequest -> bool
        /// Smart HTTP only: Basic username + git PAT (not browser cookie).
        IsGitAuthenticated: HttpRequest -> bool
        SetCookie: HttpResponse -> unit
        ClearCookie: HttpResponse -> unit
    }

/// Adapter/host composition. Not a Core door. WebApplication stays a field.
type AmbitApp =
    {
        Config: IConfiguration
        Auth: Authentication
        PublicAssetBaseOpt: string option
        DataDirResult: Result<string, exn>
        App: WebApplication
        HttpLogFile: string
    }
    member this.WebRootPath = this.App.Environment.WebRootPath
    member this.Lifetime = this.App.Lifetime
    member this.DataDir =
        match this.DataDirResult with
        | Ok d -> d
        | Error _ -> ""
    member this.Use(middleware: Func<HttpContext, RequestDelegate, Task>) =
        this.App.Use(middleware)
    member this.MapGet(pattern: string, handler: Delegate) =
        this.App.MapGet(pattern, handler)
    member this.MapPost(pattern: string, handler: Delegate) =
        this.App.MapPost(pattern, handler)
    member this.MapFallback(handler: RequestDelegate) =
        this.App.MapFallback(handler)
    member this.MapMethods
        (pattern: string, httpMethods: string[], handler: Delegate) =
        this.App.MapMethods(pattern, httpMethods, handler)

module AmbitApp =

    let create
        config
        auth
        publicAssetBaseOpt
        dataDirResult
        app
        httpLogFile
        : AmbitApp =
        {
            Config = config
            Auth = auth
            PublicAssetBaseOpt = publicAssetBaseOpt
            DataDirResult = dataDirResult
            App = app
            HttpLogFile = httpLogFile
        }
