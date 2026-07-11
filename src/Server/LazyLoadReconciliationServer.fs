namespace Gambol.Server

open System
open Gambol.Shared

[<RequireQualifiedAccess>]
module LazyLoadReconciliationServer =

    module JsonDecode = Thoth.Json.Newtonsoft.Decode
    module JsonEncode = Thoth.Json.Newtonsoft.Encode

    let decodeGraphState (json: string) : Result<int * Graph, string> =
        let decoder =
            Thoth.Json.Core.Decode.object (fun get ->
                let revision =
                    get.Required.Field
                        "revision"
                        Serialization.decodeRevision
                let graph =
                    get.Required.Field
                        "graph"
                        Serialization.decodeGraph
                revision.Value, graph)
        JsonDecode.fromString decoder json

    let private encodeChange revision ops =
        let change =
            { id = revision
              changeId = Guid.NewGuid()
              ops = ops }
        JsonEncode.toString 0 (
            Serialization.encodeChangeBatch { changes = [ change ] })

    let reconcileChangedPaths
        (handle: AgentHandle)
        (workspaceLabel: string)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        : Async<Result<unit, string>> =
        async {
            let! stateJson = handle.getState ()
            match decodeGraphState stateJson with
            | Error err -> return Error err
            | Ok(revision, graph) ->
                match
                    LazyLoadReconciliation.planChangedPaths
                        graph
                        workspaceLabel
                        changedPaths
                with
                | Error err -> return Error err
                | Ok [] -> return Ok ()
                | Ok ops ->
                    let! result =
                        handle.postGraphOnlyChange (encodeChange revision ops)
                    return result |> Result.map (fun _ -> ())
        }

    let reconcileAddedPaths
        (handle: AgentHandle)
        (workspaceLabel: string)
        (addedPaths: string list)
        : Async<Result<unit, string>> =
        addedPaths
        |> List.map LazyLoadReconciliation.Added
        |> reconcileChangedPaths handle workspaceLabel
