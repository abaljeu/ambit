namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

/// Path written plus optional status for the succeeded graph-change ack.
type DocumentWriteOk = {
    path: string
    message: string option
}

/// Live-persist result: stamped graph plus optional file-write status message.
type PersistGraphOk = {
    graph: Graph
    message: string option
}

[<RequireQualifiedAccess>]
module DocumentPersistence =

    let private splitRelativeSegments (relativePath: string) =
        relativePath.Split([| '/'; '\\' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.toList

    let private dataDirBase (dataDir: string) =
        let normalized = DataDir.normalize dataDir
        normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)

    let private resolveUnderDataDir (dataDir: string) (relativePath: string) : Result<string, string> =
        let segments = splitRelativeSegments relativePath

        if segments |> List.exists (fun segment -> segment = "..") then
            Error "invalid relative path"
        else
            let combined =
                segments
                |> List.fold (fun acc segment -> Path.Combine(acc, segment)) (dataDirBase dataDir)

            let full = Path.GetFullPath combined
            let prefix = DataDir.normalize dataDir

            if full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
                Ok full
            else
                Error "path escapes data directory"

    let private enumerateDocumentRoots (graph: Graph) : NodeId list =
        graph.nodes
        |> Map.toSeq
        |> Seq.choose (fun (id, _) ->
            if DocumentPartition.isDocumentRootNode graph id then Some id else None)
        |> Seq.sortBy (fun id ->
            let depth =
                match DocumentPartition.artifactFileRelative graph id with
                | None -> 0
                | Some rel ->
                    rel.Split([| '/'; '\\' |], StringSplitOptions.RemoveEmptyEntries).Length
            -depth, id.Value)
        |> Seq.toList

    let private noArtifactPathError (graph: Graph) (documentRootId: NodeId) =
        let prefix = $"no artifact path for document root: id={documentRootId.Value}"

        match Map.tryFind documentRootId graph.nodes with
        | None -> $"{prefix}; node=missing"
        | Some node ->
            let kind = sprintf "%A" node.kind
            let name = Filename.tryValue node.name |> Option.defaultValue "<none>"
            $"{prefix}; kind={kind}; name={name}; owner={node.owner.Value}"

    let resolveArtifactPath
        (dataDir: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<string, string> =
        match DocumentPartition.artifactFileRelative graph documentRootId with
        | None -> Error (noArtifactPathError graph documentRootId)
        | Some relativePath -> resolveUnderDataDir dataDir relativePath

    let fileStatusForReference
        (dataDir: string)
        (nodeReference: string)
        : Result<DesktopFileStatusResponse, string> =
        match NodeDesktopPath.artifactRelativeForReference nodeReference with
        | Error _ ->
            Ok
                { path = nodeReference
                  status = InvalidPath
                  sourceModifiedUtc = None }
        | Ok relativePath ->
            match resolveUnderDataDir dataDir relativePath with
            | Error _ ->
                Ok
                    { path = nodeReference
                      status = InvalidPath
                      sourceModifiedUtc = None }
            | Ok fullPath ->
                // Workspace/directory refs resolve to `…/.amb`. Treat marker paths as
                // folders (not "file"), including when only the parent dir exists
                // after WebDAV MKCOL without an `.amb` body yet.
                let markerName = Path.GetFileName fullPath
                let parentDir = Path.GetDirectoryName fullPath
                let isAmbMarker =
                    String.Equals(
                        markerName,
                        ".amb",
                        StringComparison.OrdinalIgnoreCase)

                if isAmbMarker then
                    if
                        not (isNull parentDir)
                        && Directory.Exists parentDir
                    then
                        Ok
                            { path = nodeReference
                              status = ExistingFolder
                              sourceModifiedUtc =
                                  Some (Directory.GetLastWriteTimeUtc parentDir) }
                    elif File.Exists fullPath then
                        Ok
                            { path = nodeReference
                              status = ExistingFolder
                              sourceModifiedUtc =
                                  Some (File.GetLastWriteTimeUtc fullPath) }
                    else
                        Ok
                            { path = nodeReference
                              status = MissingArtifact
                              sourceModifiedUtc = None }
                elif File.Exists fullPath then
                    Ok
                        { path = nodeReference
                          status = ExistingFile
                          sourceModifiedUtc = Some (File.GetLastWriteTimeUtc fullPath) }
                elif Directory.Exists fullPath then
                    Ok
                        { path = nodeReference
                          status = ExistingFolder
                          sourceModifiedUtc = None }
                else
                    Ok
                        { path = nodeReference
                          status = MissingArtifact
                          sourceModifiedUtc = None }

    /// Read a workspace file under DataDir and build a desktop-compatible import package.
    let importPackageForReference
        (dataDir: string)
        (nodeReference: string)
        : Result<DesktopImportPackage, string> =
        match NodeDesktopPath.artifactRelativeForReference nodeReference with
        | Error err -> Error err
        | Ok relativePath ->
            match resolveUnderDataDir dataDir relativePath with
            | Error err -> Error err
            | Ok fullPath when Directory.Exists fullPath ->
                Error "path is a directory"
            | Ok fullPath when not (File.Exists fullPath) ->
                Error "file not found"
            | Ok fullPath ->
                if DocumentBinary.isBinaryExtension relativePath then
                    Error DocumentBinary.parseError
                else
                    try
                        let text = File.ReadAllText fullPath
                        ImportDocument.buildFilePackage nodeReference text
                    with
                    | :? IOException as ex ->
                        Error ("read failed: " + ex.Message)

    let private readFileTextAtRelative
        (dataDir: string)
        (relativePath: string)
        : Result<string, string> =
        match resolveUnderDataDir dataDir relativePath with
        | Error err -> Error err
        | Ok fullPath when Directory.Exists fullPath ->
            Error "path is a directory"
        | Ok fullPath when not (File.Exists fullPath) ->
            Error "file not found"
        | Ok fullPath ->
            try
                Ok(File.ReadAllText fullPath)
            with
            | :? IOException as ex ->
                Error ("read failed: " + ex.Message)

    let private writeArtifactText
        (dataDir: string)
        (graph: Graph)
        (fileId: NodeId)
        (text: string)
        : Result<unit, string> =
        match DocumentPartition.artifactFileRelative graph fileId with
        | None -> Error (noArtifactPathError graph fileId)
        | Some relativePath ->
            match SystemDirectoryPersist.refuseWrite relativePath with
            | Error msg -> Error msg
            | Ok () ->
                match resolveUnderDataDir dataDir relativePath with
                | Error msg -> Error msg
                | Ok fullPath ->
                    try
                        let parent = Path.GetDirectoryName fullPath

                        if not (String.IsNullOrEmpty parent) then
                            Directory.CreateDirectory parent |> ignore

                        let preservedMtime =
                            if File.Exists fullPath then
                                Some(File.GetLastWriteTimeUtc fullPath)
                            else
                                None

                        let tmpPath = fullPath + ".tmp"
                        File.WriteAllText(tmpPath, text)
                        File.Move(tmpPath, fullPath, true)

                        match preservedMtime with
                        | Some utc -> File.SetLastWriteTimeUtc(fullPath, utc)
                        | None -> ()

                        Ok ()
                    with ex ->
                        Error ex.Message

    /// Plan ParseFile ops on the live graph. `textOpt` from desktop upload
    /// (writes artifact to DataDir first); otherwise read artifact text from DataDir.
    let planParseFile
        (dataDir: string)
        (graph: Graph)
        (fileId: NodeId)
        (textOpt: string option)
        : Result<Op list, string> =
        match Map.tryFind fileId graph.nodes with
        | Some { kind = Special File } ->
            match DocumentPartition.artifactFileRelative graph fileId with
            | None -> Error "selected File has no occurrence on the server"
            | Some relativePath ->
                let textResult =
                    match textOpt with
                    | Some text ->
                        DocumentParseLimits.refuseText text
                        |> Result.bind (fun () ->
                            DocumentBinary.refuseParse relativePath text)
                        |> Result.bind (fun () ->
                            writeArtifactText dataDir graph fileId text
                            |> Result.map (fun () -> text))
                    | None ->
                        if DocumentBinary.isBinaryExtension relativePath then
                            Error DocumentBinary.parseError
                        else
                            readFileTextAtRelative dataDir relativePath
                            |> Result.bind (fun text ->
                                DocumentParseLimits.refuseText text
                                |> Result.bind (fun () ->
                                    DocumentBinary.refuseParse relativePath text)
                                |> Result.map (fun () -> text))

                textResult
                |> Result.bind (fun text ->
                    ImportDocument.planParseFile graph fileId text
                    |> Result.map (fun parseOps ->
                        match resolveArtifactPath dataDir graph fileId with
                        | Ok fullPath when File.Exists fullPath ->
                            let mtime =
                                NodeUpdateTime.toDbPrecision(
                                    File.GetLastWriteTimeUtc fullPath)
                            let node = graph.nodes.[fileId]

                            if node.updateTime = mtime then
                                parseOps
                            else
                                parseOps
                                @ [ Op.SetUpdateTime(
                                        fileId,
                                        node.updateTime,
                                        mtime) ]
                        | _ -> parseOps))
        | _ -> Error "file not found or not a File document"

    let private resolveArtifactDirectoryPath
        (dataDir: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<string, string> =
        match DocumentPartition.artifactDirectoryRelative graph documentRootId with
        | None -> Error "no artifact directory path for document root"
        | Some relativePath -> resolveUnderDataDir dataDir relativePath

    let private resolveMovePath
        (dataDir: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<bool * string, string> =
        match Map.tryFind documentRootId graph.nodes with
        | None -> Error "node not found for document path move"
        | Some node when Filename.isAmbMarkerFilename node.name ->
            Error "amb marker basename must not drive artifact path moves"
        | Some node ->
            match node.kind with
            | Special File ->
                resolveArtifactPath dataDir graph documentRootId
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

    /// Refuse persist when the graph node *name* is `.amb` (not path basename).
    /// Legitimate Workspace/Directory markers are `xx/.amb` for node named `xx`.
    let refuseAmbMarkerNamedDocument (node: Node) : Result<unit, string> =
        if Filename.isAmbMarkerFilename node.name then
            Error "amb marker basename must not drive artifact writes"
        else
            Ok ()

    /// Live-save write outcomes returned with a succeeded graph change.
    let fileCouldNotSave (path: string) = $"file couldn't save: {path}"

    let stableFileUpdateFailed (path: string) =
        $"partial file update failed.  full file rewrite completed: {path}"

    let private softWritePathHint (graph: Graph) (documentRootId: NodeId) =
        match DocumentPartition.artifactFileRelative graph documentRootId with
        | Some rel -> rel
        | None -> $"id={documentRootId.Value}"

    let writeDocument
        (dataDir: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<DocumentWriteOk, string> =
        let refuse =
            match Map.tryFind documentRootId graph.nodes with
            | Some node -> refuseAmbMarkerNamedDocument node
            | None -> Ok ()

        match refuse with
        | Error msg -> Error msg
        | Ok () ->
            match resolveArtifactPath dataDir graph documentRootId with
            | Error msg -> Error msg
            | Ok fullPath ->
                let relativePath =
                    DocumentPartition.artifactFileRelative graph documentRootId
                    |> function
                        | None ->
                            Error "no file relative artifact path for document root"
                        | Some rel -> Ok rel

                match relativePath with
                | Error msg -> Error msg
                | Ok rel ->
                    match SystemDirectoryPersist.refuseWrite rel with
                    | Error msg -> Error msg
                    | Ok () ->
                        let previousText =
                            if File.Exists fullPath then
                                Some(File.ReadAllText fullPath)
                            else
                                None

                        match
                            DocumentWarm.writeArtifact
                                OutlineLcs.diffTexts
                                graph
                                documentRootId
                                rel
                                previousText
                        with
                        | Error msg -> Error msg
                        | Ok artifact ->
                            let message =
                                if artifact.stableUpdateFailed then
                                    Some(stableFileUpdateFailed rel)
                                else
                                    None

                            if previousText = Some artifact.text then
                                Ok {
                                    path = fullPath
                                    message = message
                                }
                            else
                                let createDirResult =
                                    match DocumentPartition.artifactDirectoryRelative graph documentRootId with
                                    | None -> Ok ()
                                    | Some dirRel ->
                                        match resolveUnderDataDir dataDir dirRel with
                                        | Error msg -> Error msg
                                        | Ok dirFull ->
                                            try
                                                Directory.CreateDirectory dirFull
                                                |> ignore
                                                match Map.tryFind documentRootId graph.nodes with
                                                | Some node ->
                                                    match node.kind with
                                                    | Special Workspace ->
                                                        if WorkspaceGit.isRepo dirFull then
                                                            Ok ()
                                                        else
                                                            WorkspaceGit.ensureInit dirFull
                                                    | _ -> Ok ()
                                                | None -> Ok ()
                                            with ex ->
                                                Error ex.Message

                                match createDirResult with
                                | Error msg -> Error msg
                                | Ok () ->
                                    let parent = Path.GetDirectoryName fullPath

                                    if not (String.IsNullOrEmpty parent) then
                                        Directory.CreateDirectory parent |> ignore

                                    try
                                        let tmpPath = fullPath + ".tmp"
                                        File.WriteAllText(tmpPath, artifact.text)
                                        File.Move(tmpPath, fullPath, true)
                                        Ok {
                                            path = fullPath
                                            message = message
                                        }
                                    with ex ->
                                        Error ex.Message

    let private stampNodes
        (stamps: Map<NodeId, DateTime>)
        (graph: Graph)
        : Graph =
        stamps
        |> Map.fold
            (fun g id time ->
                match Map.tryFind id g.nodes with
                | None -> g
                | Some node ->
                    { g with
                        nodes =
                            Map.add
                                id
                                (NodeUpdateTime.withStamp time node)
                                g.nodes })
            graph

    let private stampExistingDocuments
        (dataDir: string)
        (documentRootIds: NodeId list)
        (graph: Graph)
        : Graph =
        documentRootIds
        |> List.fold
            (fun stamps documentRootId ->
                match resolveArtifactPath dataDir graph documentRootId with
                | Ok path when File.Exists path ->
                    Map.add documentRootId (File.GetLastWriteTimeUtc path) stamps
                | _ -> stamps)
            Map.empty
        |> fun stamps -> stampNodes stamps graph

    let private joinWriteMessages (messages: string list) : string option =
        match messages |> List.distinct with
        | [] -> None
        | msgs -> Some(String.concat "; " msgs)

    /// Strict writes for bootstrap/tests: any writeDocument Error fails the fold.
    let private writeDocuments
        (dataDir: string)
        (graph: Graph)
        (rootIds: NodeId list)
        : Result<Graph, string> =
        let baseDir = dataDirBase dataDir
        Directory.CreateDirectory baseDir |> ignore

        rootIds
        |> List.fold
            (fun acc documentRootId ->
                match acc with
                | Error msg -> Error msg
                | Ok stamps ->
                    match writeDocument dataDir graph documentRootId with
                    | Error msg -> Error msg
                    | Ok written ->
                        let mtime = File.GetLastWriteTimeUtc written.path
                        Ok(Map.add documentRootId mtime stamps))
            (Ok Map.empty)
        |> Result.map (fun stamps -> stampNodes stamps graph)

    /// Live-save writes: compute/IO failures never fail the fold; they become messages.
    let private writeDocumentsSoft
        (dataDir: string)
        (graph: Graph)
        (rootIds: NodeId list)
        : Graph * string option =
        let baseDir = dataDirBase dataDir
        Directory.CreateDirectory baseDir |> ignore

        let stamps, messages =
            rootIds
            |> List.fold
                (fun (stamps, messages) documentRootId ->
                    match writeDocument dataDir graph documentRootId with
                    | Error _ ->
                        let msg =
                            softWritePathHint graph documentRootId
                            |> fileCouldNotSave
                        stamps, msg :: messages
                    | Ok written ->
                        let mtime = File.GetLastWriteTimeUtc written.path
                        let stamps' = Map.add documentRootId mtime stamps
                        let messages' =
                            match written.message with
                            | Some msg -> msg :: messages
                            | None -> messages
                        stamps', messages')
                (Map.empty, [])

        stampNodes stamps graph, joinWriteMessages (List.rev messages)

    /// Test/bootstrap helper that materializes a complete file layout from a generated graph.
    /// Normal accepted graph changes use persistGraphOps/JIT live-save; this intentionally
    /// bypasses normal production persistence.
    let writeAllDocuments (dataDir: string) (graph: Graph) : Result<Graph, string> =
        enumerateDocumentRoots graph
        |> List.filter (fun documentRootId ->
            DocumentPartition.shouldWriteDocumentRoot graph.nodes.[documentRootId])
        |> writeDocuments dataDir graph

    let private persistGraphChangeWith
        (affectedRoots: NodeId list -> Set<NodeId>)
        (existingStampRoots: NodeId list -> NodeId list)
        (dataDir: string)
        (preGraph: Graph)
        (postGraph: Graph)
        : Result<PersistGraphOk, string> =
        let moves = DocumentPathMove.planPathMovesBetweenGraphs preGraph postGraph

        match executePathMoves dataDir preGraph postGraph moves with
        | Error msg -> Error msg
        | Ok () ->
            let moveIds = moves |> List.map (fun m -> m.nodeId)
            let affected = affectedRoots moveIds
            let stamped, message =
                affected
                |> Set.toList
                |> writeDocumentsSoft dataDir postGraph

            Ok {
                graph =
                    stampExistingDocuments
                        dataDir
                        (existingStampRoots moveIds)
                        stamped
                message = message
            }

    /// Snapshot fallback when no accepted operation batch is available.
    let persistGraphChange
        (dataDir: string)
        (preGraph: Graph)
        (postGraph: Graph)
        : Result<PersistGraphOk, string> =
        persistGraphChangeWith
            (DocumentPartition.documentRootsAffectedByGraphChange preGraph postGraph)
            (fun _ -> enumerateDocumentRoots postGraph)
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

    let private shouldSkipDiscoveryFile (fileName: string) =
        Filename.isReservedSystemName fileName
        || fileName.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
        || fileName.Contains(".bak.", StringComparison.OrdinalIgnoreCase)

    /// Skip git metadata under any workspace (or DataDir) `.git` tree.
    let private shouldSkipDiscoveryRel (rel: string) =
        let n = rel.Replace('\\', '/')
        n = ".git"
        || n.StartsWith(".git/", StringComparison.Ordinal)
        || n.Contains("/.git/", StringComparison.Ordinal)

    let private relativePathFromDataDir (dataDir: string) (fullPath: string) =
        let basePrefix = dataDirBase dataDir + string Path.DirectorySeparatorChar
        let full = Path.GetFullPath fullPath

        if full.StartsWith(basePrefix, StringComparison.OrdinalIgnoreCase) then
            full.Substring(basePrefix.Length).Replace('\\', '/')
        else
            full

    let discoverArtifactRelatives (dataDir: string) : Result<string list, string> =
        let baseDir = dataDirBase dataDir

        if not (Directory.Exists baseDir) then
            Ok []
        else
            try
                Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories)
                |> Seq.filter (fun fullPath ->
                    let rel = relativePathFromDataDir dataDir fullPath
                    not (shouldSkipDiscoveryFile (Path.GetFileName fullPath))
                    && not (shouldSkipDiscoveryRel rel))
                |> Seq.map (fun fullPath ->
                    relativePathFromDataDir dataDir fullPath,
                    resolveUnderDataDir dataDir (relativePathFromDataDir dataDir fullPath))
                |> Seq.fold
                    (fun acc (rel, resolved) ->
                        match acc with
                        | Error msg -> Error msg
                        | Ok paths ->
                            match resolved with
                            | Error msg -> Error msg
                            | Ok _ -> Ok (rel :: paths))
                    (Ok [])
                |> Result.map List.rev
            with ex ->
                Error ex.Message

    let readAllDocuments (dataDir: string) : Result<Graph, string> =
        match discoverArtifactRelatives dataDir with
        | Error msg -> Error msg
        | Ok relatives ->
            let relatives =
                relatives
                |> List.filter DocumentArtifactPath.isMarker
            let resolved =
                relatives
                |> List.fold
                    (fun acc rel ->
                        match acc with
                        | Error msg -> Error msg
                        | Ok paths ->
                            match resolveUnderDataDir dataDir rel with
                            | Error msg -> Error msg
                            | Ok fullPath ->
                                Ok(Map.add (rel.Replace('\\', '/')) fullPath paths))
                    (Ok Map.empty)

            match resolved with
            | Error msg -> Error msg
            | Ok pathByRel ->
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
                |> Result.bind DocumentAssembly.assembleFromArtifacts
                |> Result.map (fun graph ->
                    let tryMtime (rel: string) =
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

                    let tryMtimeByNameSuffix (name: string) (suffix: string) =
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

                    let stamps =
                        enumerateDocumentRoots graph
                        |> List.fold
                            (fun acc documentRootId ->
                                match DocumentPartition.artifactFileRelative graph documentRootId with
                                | Some rel ->
                                    match tryMtime rel with
                                    | Some mtime -> Map.add documentRootId mtime acc
                                    | None -> acc
                                | None ->
                                    match Map.tryFind documentRootId graph.nodes with
                                    | Some node ->
                                        match node.kind, Filename.tryValue node.name with
                                        | Special Directory, Some name ->
                                            match tryMtimeByNameSuffix name "/.amb" with
                                            | Some mtime -> Map.add documentRootId mtime acc
                                            | None -> acc
                                        | Special File, Some name ->
                                            match tryMtimeByNameSuffix name "" with
                                            | Some mtime -> Map.add documentRootId mtime acc
                                            | None -> acc
                                        | _ -> acc
                                    | None -> acc)
                            Map.empty

                    stampNodes stamps graph)
