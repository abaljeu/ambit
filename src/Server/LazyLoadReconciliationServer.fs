namespace Gambol.Server

open System
open System.IO
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

    let private isDirInfoPath (path: string) =
        let n = path.Replace('\\', '/')
        n = ".amb" || n.EndsWith("/.amb", StringComparison.Ordinal)

    let private markerPathsFromChanges
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        =
        changedPaths
        |> List.collect (function
            | LazyLoadReconciliation.Added path
            | LazyLoadReconciliation.Modified path when isDirInfoPath path ->
                [ path ]
            | LazyLoadReconciliation.Renamed(_, newPath) when isDirInfoPath newPath ->
                [ newPath ]
            | _ -> [])
        |> List.distinct

    let private readDirInfoArtifacts
        (dataDir: string)
        (workspaceLabel: string)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        : Map<string, string> =
        let root = Path.Combine(dataDir, workspaceLabel)
        markerPathsFromChanges changedPaths
        |> List.choose (fun relative ->
            let full = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar))
            if File.Exists full then
                try
                    Some(relative.Replace('\\', '/'), File.ReadAllText full)
                with _ ->
                    None
            else
                None)
        |> Map.ofList

    let reconcileChangedPaths
        (handle: AgentHandle)
        (dataDir: string)
        (workspaceLabel: string)
        (changedPaths: LazyLoadReconciliation.ChangedPath list)
        : Async<Result<unit, string>> =
        async {
            let! stateJson = handle.getState ()
            match decodeGraphState stateJson with
            | Error err -> return Error err
            | Ok(revision, graph) ->
                let artifacts = readDirInfoArtifacts dataDir workspaceLabel changedPaths
                match
                    LazyLoadReconciliation.planChangedPathsWithArtifacts
                        graph
                        workspaceLabel
                        changedPaths
                        artifacts
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
        (dataDir: string)
        (workspaceLabel: string)
        (addedPaths: string list)
        : Async<Result<unit, string>> =
        addedPaths
        |> List.map LazyLoadReconciliation.Added
        |> reconcileChangedPaths handle dataDir workspaceLabel
