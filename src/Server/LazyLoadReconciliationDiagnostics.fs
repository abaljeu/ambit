namespace Gambol.Server

open System
open System.Collections.Concurrent
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Gambol.Shared
open Thoth.Json.Core

/// Workspace-keyed latest reconcile failures (overwrite; consume-once GET).
[<RequireQualifiedAccess>]
module LazyLoadReconciliationDiagnostics =

    module JsonEncode = Thoth.Json.Newtonsoft.Encode

    let private slots =
        ConcurrentDictionary<string, LazyLoadReconciliationReport.Failure list>(
            StringComparer.Ordinal)

    let set (workspaceLabel: string) (failures: LazyLoadReconciliationReport.Failure list) =
        slots.[workspaceLabel] <- failures

    let take (workspaceLabel: string) : LazyLoadReconciliationReport.Failure list =
        match slots.TryRemove(workspaceLabel) with
        | true, failures -> failures
        | false, _ -> []

    let encodeFailures
        (failures: LazyLoadReconciliationReport.Failure list)
        : string =
        let items =
            failures
            |> List.map (fun f ->
                Encode.object
                    [ "path", Encode.string f.path
                      "message", Encode.string f.message ])
        Encode.object [ "failures", Encode.list items ]
        |> JsonEncode.toString 0

    let registerRoute
        (app: WebApplication)
        (isAuthenticated: HttpRequest -> bool)
        =
        app.MapGet(
            "/ambit/workspace/reconciliation/latest",
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
                        let failures = take workspace
                        Results.Content(encodeFailures failures, "application/json"))
        )
        |> ignore
