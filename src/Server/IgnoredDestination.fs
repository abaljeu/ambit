namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

/// Reject graph disk effects whose destination is ignored by `.gitignore`.
[<RequireQualifiedAccess>]
module IgnoredDestination =

    let private artifactRelative (graph: Graph) (nodeId: NodeId) =
        match Map.tryFind nodeId graph.nodes with
        | None -> None
        | Some node ->
            match node.kind with
            | Special File ->
                DocumentPartition.artifactFileRelative graph nodeId
            | Special (Workspace | Directory) ->
                DocumentPartition.artifactDirectoryRelative graph nodeId
            | _ -> None

    let private ignoreScope
        (dataDir: string)
        (graph: Graph)
        (nodeId: NodeId)
        (artifactRel: string)
        : (string * string) option =
        let rel = GitCheckIgnore.normalizeRel artifactRel
        let dataRoot = DataDir.normalize dataDir

        match GraphQuery.enclosingWorkspace graph nodeId with
        | Some wsId when Graph.isCanonicalDataRoot wsId ->
            Some(dataRoot, rel)
        | Some wsId ->
            match Map.tryFind wsId graph.nodes with
            | Some node ->
                match Filename.tryValue node.name with
                | Some name ->
                    let prefix = name + "/"
                    let scoped =
                        if rel = name || rel = name + "/" then ""
                        elif rel.StartsWith(prefix, StringComparison.Ordinal) then
                            rel.Substring(prefix.Length)
                        else
                            rel

                    Some(Path.Combine(dataRoot, name), scoped)
                | None -> None
            | None -> None
        | None -> Some(dataRoot, rel)

    let private destinationEffects
        (preGraph: Graph)
        (postGraph: Graph)
        : (NodeId * string) list =
        postGraph.nodes
        |> Map.toSeq
        |> Seq.choose (fun (nodeId, _) ->
            if not (DocumentPartition.isDocumentRootNode postGraph nodeId) then
                None
            else
                match artifactRelative postGraph nodeId with
                | None -> None
                | Some newPath ->
                    match Map.tryFind nodeId postGraph.nodes with
                    | Some node when Filename.isDirectoryFileFilename node.name ->
                        None
                    | _ ->
                        let oldPath = artifactRelative preGraph nodeId

                        match oldPath with
                        | Some old when
                            GitCheckIgnore.normalizeRel old
                            = GitCheckIgnore.normalizeRel newPath
                            ->
                            None
                        | _ -> Some(nodeId, newPath))
        |> Seq.toList
        |> List.sortBy (fun (nodeId, _) -> nodeId.Value)

    let private classifyChecks
        (checks: (NodeId * string * string * string) list)
        : Result<(NodeId * string * bool) list, string> =
        checks
        |> List.groupBy (fun (_, _, workTree, _) -> workTree)
        |> List.fold
            (fun result (workTree, items) ->
                match result with
                | Error err -> Error err
                | Ok groups ->
                    let paths =
                        items |> List.map (fun (_, _, _, path) -> path)

                    match GitCheckIgnore.classify workTree paths with
                    | Error err -> Error err
                    | Ok classified ->
                        let group =
                            (items, classified)
                            ||> List.map2 (fun item classification ->
                                let nodeId, artifactRel, _, _ = item
                                let _, ignored = classification
                                nodeId, artifactRel, ignored)
                        Ok(group :: groups))
            (Ok [])
        |> Result.map List.concat

    let validateGraphDiskEffects
        (dataDir: string)
        (preGraph: Graph)
        (postGraph: Graph)
        : Result<unit, string> =
        let checks =
            destinationEffects preGraph postGraph
            |> List.choose (fun (nodeId, artifactRel) ->
                if GitCheckIgnore.isGitignorePath artifactRel then
                    None
                else
                    match ignoreScope dataDir postGraph nodeId artifactRel with
                    | Some(workTree, relativePath) when
                        not (String.IsNullOrWhiteSpace relativePath)
                        ->
                        Some(nodeId, artifactRel, workTree, relativePath)
                    | _ -> None)

        match classifyChecks checks with
        | Error err -> Error err
        | Ok classified ->
            classified
            |> List.sortBy (fun (nodeId, _, _) -> nodeId.Value)
            |> List.tryFind (fun (_, _, ignored) -> ignored)
            |> function
                | None -> Ok ()
                | Some(_, artifactRel, _) ->
                    Error (
                        "destination path ignored by .gitignore: "
                        + GitCheckIgnore.normalizeRel artifactRel)
