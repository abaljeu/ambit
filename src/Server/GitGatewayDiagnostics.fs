namespace Gambol.Server

open System
open System.Collections.Concurrent
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Thoth.Json.Core

/// Workspace-keyed latest git gateway HTTP error (overwrite; consume-once GET).
[<RequireQualifiedAccess>]
module GitGatewayDiagnostics =

    type GatewayError =
        { status: int
          message: string }

    module JsonEncode = Thoth.Json.Newtonsoft.Encode

    let private slots =
        ConcurrentDictionary<string, GatewayError>(StringComparer.Ordinal)

    let set (workspaceLabel: string) (error: GatewayError) =
        slots.[workspaceLabel] <- error

    let take (workspaceLabel: string) : GatewayError option =
        match slots.TryRemove(workspaceLabel) with
        | true, err -> Some err
        | false, _ -> None

    let encode (error: GatewayError option) : string =
        match error with
        | None -> Encode.object [] |> JsonEncode.toString 0
        | Some e ->
            Encode.object
                [ "status", Encode.int e.status
                  "message", Encode.string e.message ]
            |> JsonEncode.toString 0

    let registerRoute
        (app: WebApplication)
        (isAuthenticated: HttpRequest -> bool)
        =
        app.MapGet(
            "/ambit/git/gateway-error",
            Func<HttpRequest, IResult>(fun req ->
                if not (isAuthenticated req) then
                    Results.Unauthorized()
                else
                    let workspace =
                        match req.Query.TryGetValue("workspace") with
                        | true, values -> string values.[0]
                        | _ -> ""
                    if String.IsNullOrWhiteSpace workspace then
                        Results.BadRequest("missing workspace query")
                    else
                        let err = take workspace
                        Results.Content(encode err, "application/json")))
        |> ignore
