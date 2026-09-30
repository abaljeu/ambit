namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

/// Live-persist result: stamped graph plus optional file-write status message.
type PersistGraphOk = {
    graph: Graph
    message: string option
}

[<RequireQualifiedAccess>]
module DocumentPersistChange =

    let private resolveArtifactDirectoryPath
        (dataDir: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<string, string> =
        match DocumentPartition.artifactDirectoryRelative graph documentRootId with
        | None -> Error "no artifact directory path for document root"
        | Some relativePath ->
            DocumentPersistPath.resolveUnderDataDir dataDir relativePath

    let private resolveMovePath
        (dataDir: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<bool * string, string> =
        match Map.tryFind documentRootId graph.nodes with
        | None -> Error "node not found for document path move"
        | Some node when Filename.isDirectoryFileFilename node.name ->
            Error "directory file basename must not drive artifact path moves"
        | Some node ->
            match node.kind with
            | Special File ->
                DocumentPersistPath.resolveArtifactPath dataDir graph documentRootId
                |> Result.map (fun path -> false, path)
            | Special (Workspace | Directory) ->
                resolveArtifactDirectoryPath dataDir graph documentRootId
                |> Result.map (fun path -> true, path)
            | _ -> Error "node is not a movable document root"

    let private artifactRelativeForMove
        (graph: Graph)
        (nodeId: NodeId)
        : string option =
        match Map.tryFind nodeId graph.nodes with
        | Some { kind = Special File } ->
            DocumentPartition.artifactFileRelative graph nodeId
        | Some { kind = Special (Workspace | Directory) } ->
            DocumentPartition.artifactDirectoryRelative graph nodeId
        | _ -> None

    let private refuseSystemDirectoryPathMoves
        (preGraph: Graph)
        (postGraph: Graph)
        (move: DocumentPathMove)
        : Result<unit, string> =
        let check graph =
            match artifactRelativeForMove graph move.nodeId with
            | None -> Ok ()
            | Some relativePath ->
                SystemDirectoryPersist.refuseWrite relativePath

        check preGraph
        |> Result.bind (fun () -> check postGraph)

    let private resolveMovePaths
        (dataDir: string)
        (preGraph: Graph)
        (postGraph: Graph)
        (move: DocumentPathMove)
        : Result<(bool * string) * (bool * string), string> =
        match refuseSystemDirectoryPathMoves preGraph postGraph move with
        | Error msg -> Error msg
        | Ok () ->
            match resolveMovePath dataDir preGraph move.nodeId with
            | Error msg -> Error msg
            | Ok oldPath ->
                match resolveMovePath dataDir postGraph move.nodeId with
                | Error msg -> Error msg
                | Ok newPath -> Ok (oldPath, newPath)

    let private sameFullPath (left: string) (right: string) =
        String.Equals(
            Path.GetFullPath left,
            Path.GetFullPath right,
            StringComparison.OrdinalIgnoreCase)

    let private pathExists (path: string) =
        File.Exists path || Directory.Exists path

    let private validateDestinationAvailable
        (oldFullPath: string)
        (newFullPath: string)
        : Result<unit, string> =
        if sameFullPath oldFullPath newFullPath then
            Ok ()
        elif pathExists newFullPath then
            Error $"disk path already exists: {newFullPath}"
        else
            Ok ()

    let private createParentDirectory (fullPath: string) =
        let parent = Path.GetDirectoryName fullPath

        if not (String.IsNullOrEmpty parent) then
            Directory.CreateDirectory parent |> ignore

    let validatePathMoves
        (dataDir: string)
        (preGraph: Graph)
        (postGraph: Graph)
        : Result<unit, string> =
        DocumentPathMove.planPathMovesBetweenGraphs preGraph postGraph
        |> DocumentPathMove.coalescePathMoves preGraph
        |> List.fold
            (fun acc move ->
                match acc with
                | Error msg -> Error msg
                | Ok () ->
                    match resolveMovePaths dataDir preGraph postGraph move with
                    | Error msg -> Error msg
                    | Ok ((_, oldFullPath), (_, newFullPath)) ->
                        validateDestinationAvailable oldFullPath newFullPath)
            (Ok ())

    let validateGraphDiskEffects
        (dataDir: string)
        (preGraph: Graph)
        (postGraph: Graph)
        : Result<unit, string> =
        IgnoredDestination.validateGraphDiskEffects dataDir preGraph postGraph

    let private executePathMove
        ((oldIsDirectory, oldFullPath): bool * string)
        ((newIsDirectory, newFullPath): bool * string)
        : Result<unit, string> =
        if oldIsDirectory <> newIsDirectory then
            Error "document path move changed artifact kind"
        elif sameFullPath oldFullPath newFullPath then
            Ok ()
        else
            match validateDestinationAvailable oldFullPath newFullPath with
            | Error msg -> Error msg
            | Ok () ->
                try
                    createParentDirectory newFullPath

                    if oldIsDirectory then
                        if Directory.Exists oldFullPath then
                            Directory.Move(oldFullPath, newFullPath)
                        Ok ()
                    elif File.Exists oldFullPath then
                        File.Move(oldFullPath, newFullPath)
                        Ok ()
                    else
                        Ok ()
                with ex ->
                    Error ex.Message

    let executePathMoves
        (dataDir: string)
        (preGraph: Graph)
        (postGraph: Graph)
        (moves: DocumentPathMove list)
        : Result<unit, string> =
        moves
        |> DocumentPathMove.coalescePathMoves preGraph
        |> List.fold
            (fun acc move ->
                match acc with
                | Error msg -> Error msg
                | Ok () ->
                    match resolveMovePaths dataDir preGraph postGraph move with
                    | Error msg -> Error msg
                    | Ok (oldPath, newPath) -> executePathMove oldPath newPath)
            (Ok ())

    /// Path-moves and successful writes. A failed write is not in this set.
    let private persistedContentIds
        (moveIds: NodeId list)
        (affected: Set<NodeId>)
        (writtenIds: Set<NodeId>)
        : Set<NodeId> =
        let failed =
            Set.filter
                (fun id -> not (Set.contains id writtenIds))
                affected

        Set.difference (Set.union writtenIds (Set.ofList moveIds)) failed

    let private persistGraphChangeWith
        (affectedRoots: NodeId list -> Set<NodeId>)
        (existingStampRoots: NodeId list -> NodeId list)
        (dataDir: string)
        (preGraph: Graph)
        (postGraph: Graph)
        : Result<PersistGraphOk, string> =
        let moves = DocumentPathMove.planPathMovesBetweenGraphs preGraph postGraph
        let moveIds = moves |> List.map (fun move -> move.nodeId)
        let affected = affectedRoots moveIds
        let gatedIds = Set.toList affected @ moveIds
        let workTreeRoots =
            [ for nodeId in gatedIds do
                  DocumentPersistPath.workspaceRootFor dataDir preGraph nodeId
                  DocumentPersistPath.workspaceRootFor dataDir postGraph nodeId ]
            |> List.choose id
            |> DocumentPersistPath.normalizedWorkTreeRoots
        DocumentPersistPath.withWorkTreeGates workTreeRoots (fun () ->
            match executePathMoves dataDir preGraph postGraph moves with
            | Error msg -> Error msg
            | Ok () ->
                let stamped, message, writtenIds =
                    affected
                    |> Set.toList
                    |> DocumentPersistWrite.writeDocumentsSoft dataDir postGraph

                Ok {
                    graph =
                        DocumentPersistPath.stampExistingDocuments
                            dataDir
                            (persistedContentIds moveIds affected writtenIds)
                            (existingStampRoots moveIds)
                            stamped
                    message = message
                })

    /// Snapshot fallback when no accepted operation batch is available.
    let persistGraphChange
        (dataDir: string)
        (preGraph: Graph)
        (postGraph: Graph)
        : Result<PersistGraphOk, string> =
        persistGraphChangeWith
            (DocumentPartition.documentRootsAffectedByGraphChange preGraph postGraph)
            (fun _ -> DocumentPersistPath.enumerateDocumentRoots postGraph)
            dataDir
            preGraph
            postGraph

    /// Immediate live-save using accepted operations instead of a full graph diff.
    /// File compute/write failures are reported in PersistGraphOk.message, not as Error.
    let persistGraphOps
        (dataDir: string)
        (preGraph: Graph)
        (postGraph: Graph)
        (ops: Op list)
        : Result<PersistGraphOk, string> =
        persistGraphChangeWith
            (DocumentOpImpact.documentRootsAffectedByOps preGraph postGraph ops)
            id
            dataDir
            preGraph
            postGraph

    let private tryMtime
        (pathByRel: Map<string, string>)
        (rel: string)
        : DateTime option =
        let norm = rel.Replace('\\', '/')
        match Map.tryFind norm pathByRel with
        | Some full -> Some(File.GetLastWriteTimeUtc full)
        | None ->
            pathByRel
            |> Map.toList
            |> List.choose (fun (discovered, full) ->
                if discovered = norm
                   || discovered.EndsWith("/" + norm, StringComparison.Ordinal)
                then
                    Some full
                else
                    None)
            |> function
                | [ full ] -> Some(File.GetLastWriteTimeUtc full)
                | _ -> None

    let private tryMtimeByNameSuffix
        (pathByRel: Map<string, string>)
        (name: string)
        (suffix: string)
        : DateTime option =
        let needle = name + suffix
        pathByRel
        |> Map.toList
        |> List.choose (fun (discovered, full) ->
            if discovered = needle
               || discovered.EndsWith("/" + needle, StringComparison.Ordinal)
            then
                Some full
            else
                None)
        |> function
            | [ full ] -> Some(File.GetLastWriteTimeUtc full)
            | _ -> None

    let private stampFromNodeName
        (pathByRel: Map<string, string>)
        (documentRootId: NodeId)
        (node: Node)
        (stamps: Map<NodeId, DateTime>)
        : Map<NodeId, DateTime> =
        match node.kind, Filename.tryValue node.name with
        | Special Directory, Some name ->
            match tryMtimeByNameSuffix pathByRel name "/.amb" with
            | Some mtime -> Map.add documentRootId mtime stamps
            | None -> stamps
        | Special File, Some name ->
            match tryMtimeByNameSuffix pathByRel name "" with
            | Some mtime -> Map.add documentRootId mtime stamps
            | None -> stamps
        | _ -> stamps

    let private addRootStamp
        (dataDir: string)
        (graph: Graph)
        (pathByRel: Map<string, string>)
        (stamps: Map<NodeId, DateTime>)
        (documentRootId: NodeId)
        : Map<NodeId, DateTime> =
        match DocumentPartition.artifactFileRelative graph documentRootId with
        | Some rel ->
            match tryMtime pathByRel rel with
            | Some mtime -> Map.add documentRootId mtime stamps
            | None ->
                // Stub-only File bodies are not in pathByRel
                // (Directory Files only); stamp from DataDir when present.
                match DocumentPersistPath.resolveUnderDataDir dataDir rel with
                | Ok full when File.Exists full ->
                    Map.add
                        documentRootId
                        (File.GetLastWriteTimeUtc full)
                        stamps
                | _ -> stamps
        | None ->
            match Map.tryFind documentRootId graph.nodes with
            | Some node ->
                stampFromNodeName pathByRel documentRootId node stamps
            | None -> stamps

    let private stampLoadedGraph
        (dataDir: string)
        (pathByRel: Map<string, string>)
        (graph: Graph)
        : Graph =
        DocumentPersistPath.enumerateDocumentRoots graph
        |> List.fold (addRootStamp dataDir graph pathByRel) Map.empty
        |> fun stamps -> DocumentPersistPath.stampNodes stamps graph

    let private resolveDirectoryFilePaths
        (dataDir: string)
        (relatives: string list)
        : Result<Map<string, string>, string> =
        let normalize (rel: string) = rel.Replace('\\', '/')
        relatives
        |> List.filter DocumentArtifactPath.isDirectoryFile
        |> List.fold
            (fun acc rel ->
                match acc with
                | Error msg -> Error msg
                | Ok paths ->
                    match DocumentPersistPath.resolveUnderDataDir dataDir rel with
                    | Error msg -> Error msg
                    | Ok fullPath ->
                        Ok(Map.add (normalize rel) fullPath paths))
            (Ok Map.empty)

    let private readDirectoryFileTexts
        (pathByRel: Map<string, string>)
        : Result<Map<string, string>, string> =
        pathByRel
        |> Map.fold
            (fun acc rel fullPath ->
                match acc with
                | Error msg -> Error msg
                | Ok artifacts ->
                    try
                        let text = File.ReadAllText fullPath
                        match DocumentParseLimits.refuseText text with
                        | Error msg ->
                            eprintfn
                                "Gambol: skipping oversized document '%s': %s"
                                rel
                                msg
                            Ok artifacts
                        | Ok () ->
                            Ok(Map.add rel text artifacts)
                    with ex ->
                        Error ex.Message)
            (Ok Map.empty)

    let readAllDocuments (dataDir: string) : Result<Graph, string> =
        match DocumentPersistPath.discoverArtifactRelatives dataDir with
        | Error msg -> Error msg
        | Ok relatives ->
            // Cold bootstrap reads Directory File outline only; discovered File bodies
            // become Unparsed stubs until Parse / selective load.
            let normalize (rel: string) = rel.Replace('\\', '/')
            let stubOnlyPaths =
                relatives
                |> List.map normalize
                |> List.filter (DocumentArtifactPath.isDirectoryFile >> not)
                |> Set.ofList

            resolveDirectoryFilePaths dataDir relatives
            |> Result.bind (fun pathByRel ->
                readDirectoryFileTexts pathByRel
                |> Result.bind (fun artifacts ->
                    DocumentAssembly.assembleFromArtifactsBounded
                        artifacts
                        stubOnlyPaths)
                |> Result.map (stampLoadedGraph dataDir pathByRel))
