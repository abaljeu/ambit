namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

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
                if File.Exists fullPath then
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
        match resolveArtifactPath dataDir graph fileId with
        | Error msg -> Error msg
        | Ok fullPath ->
            try
                let parent = Path.GetDirectoryName fullPath

                if not (String.IsNullOrEmpty parent) then
                    Directory.CreateDirectory parent |> ignore

                let tmpPath = fullPath + ".tmp"
                File.WriteAllText(tmpPath, text)
                File.Move(tmpPath, fullPath, true)
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
            let textResult =
                match textOpt with
                | Some text ->
                    writeArtifactText dataDir graph fileId text
                    |> Result.map (fun () -> text)
                | None ->
                    match DocumentPartition.artifactFileRelative graph fileId with
                    | None -> Error "selected File has no occurrence on the server"
                    | Some relativePath ->
                        readFileTextAtRelative dataDir relativePath

            textResult
            |> Result.bind (ImportDocument.planParseFile graph fileId)
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
        | Some node ->
            match node.kind with
            | Special File ->
                resolveArtifactPath dataDir graph documentRootId
                |> Result.map (fun path -> false, path)
            | Special (Workspace | Directory) ->
                resolveArtifactDirectoryPath dataDir graph documentRootId
                |> Result.map (fun path -> true, path)
            | _ -> Error "node is not a movable document root"

    let private resolveMovePaths
        (dataDir: string)
        (preGraph: Graph)
        (postGraph: Graph)
        (move: DocumentPathMove)
        : Result<(bool * string) * (bool * string), string> =
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

    let writeDocument
        (dataDir: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<string, string> =
        match resolveArtifactPath dataDir graph documentRootId with
        | Error msg -> Error msg
        | Ok fullPath ->
            let createDirResult =
                match DocumentPartition.artifactDirectoryRelative graph documentRootId with
                | None -> Ok ()
                | Some dirRel ->
                    match resolveUnderDataDir dataDir dirRel with
                    | Error msg -> Error msg
                    | Ok dirFull ->
                        try
                            Directory.CreateDirectory dirFull |> ignore
                            match Map.tryFind documentRootId graph.nodes with
                            | Some node ->
                                match node.kind with
                                | Special Workspace ->
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

                let relativePath =
                    DocumentPartition.artifactFileRelative graph documentRootId
                    |> function
                        | None -> Error "no file relative artifact path for document root"
                        | Some rel -> Ok rel

                match relativePath with
                | Error msg -> Error msg
                | Ok rel ->
                    let previousText =
                        if File.Exists fullPath then Some(File.ReadAllText fullPath) else None

                    match
                        DocumentWarm.writeArtifact
                            OutlineLcs.diffTexts
                            graph
                            documentRootId
                            rel
                            previousText
                    with
                    | Error msg -> Error msg
                    | Ok text ->
                        try
                            let tmpPath = fullPath + ".tmp"
                            File.WriteAllText(tmpPath, text)
                            File.Move(tmpPath, fullPath, true)
                            Ok fullPath
                        with ex ->
                            Error ex.Message

    let writeAllDocuments (dataDir: string) (graph: Graph) : Result<string list, string> =
        let baseDir = dataDirBase dataDir
        Directory.CreateDirectory baseDir |> ignore

        enumerateDocumentRoots graph
        |> List.filter (fun documentRootId ->
            graph.nodes.[documentRootId].documentState = Current)
        |> List.fold
            (fun acc documentRootId ->
                match acc with
                | Error msg -> Error msg
                | Ok paths ->
                    match writeDocument dataDir graph documentRootId with
                    | Error msg -> Error msg
                    | Ok path -> Ok (paths @ [ path ]))
            (Ok [])

    let persistGraphChange
        (dataDir: string)
        (preGraph: Graph)
        (postGraph: Graph)
        : Result<string list, string> =
        let moves = DocumentPathMove.planPathMovesBetweenGraphs preGraph postGraph

        match executePathMoves dataDir preGraph postGraph moves with
        | Error msg -> Error msg
        | Ok () -> writeAllDocuments dataDir postGraph

    let private shouldSkipDiscoveryFile (fileName: string) =
        Filename.isReservedSystemName fileName
        || fileName = "gambol"
        || fileName.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase)
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

    let hasArtifactSet (dataDir: string) : bool =
        match discoverArtifactRelatives dataDir with
        | Error _ -> false
        | Ok paths ->
            paths
            |> List.exists (fun rel ->
                rel <> ".amb"
                && rel.EndsWith("/.amb", StringComparison.Ordinal))

    let readAllDocuments (dataDir: string) : Result<Graph, string> =
        match discoverArtifactRelatives dataDir with
        | Error msg -> Error msg
        | Ok relatives ->
            relatives
            |> List.fold
                (fun acc rel ->
                    match acc with
                    | Error msg -> Error msg
                    | Ok artifacts ->
                        match resolveUnderDataDir dataDir rel with
                        | Error msg -> Error msg
                        | Ok fullPath ->
                            try
                                let text = File.ReadAllText fullPath
                                Ok (Map.add rel text artifacts)
                            with ex ->
                                Error ex.Message)
                (Ok Map.empty)
            |> Result.bind DocumentAssembly.assembleFromArtifacts
