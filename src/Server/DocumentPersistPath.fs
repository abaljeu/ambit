namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

[<RequireQualifiedAccess>]
module DocumentPersistPath =

    let private splitRelativeSegments (relativePath: string) =
        relativePath.Split([| '/'; '\\' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.toList

    let dataDirBase (dataDir: string) =
        let normalized = DataDir.normalize dataDir
        normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)

    let resolveUnderDataDir
        (dataDir: string)
        (relativePath: string)
        : Result<string, string> =
        let segments = splitRelativeSegments relativePath

        if segments |> List.exists (fun segment -> segment = "..") then
            Error "invalid relative path"
        else
            let combined =
                segments
                |> List.fold
                    (fun acc segment -> Path.Combine(acc, segment))
                    (dataDirBase dataDir)

            let full = Path.GetFullPath combined
            let prefix = DataDir.normalize dataDir

            if full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
                Ok full
            else
                Error "path escapes data directory"

    let workspaceRootFor
        (dataDir: string)
        (graph: Graph)
        (nodeId: NodeId)
        : string option =
        GraphQuery.enclosingWorkspace graph nodeId
        |> Option.bind (DocumentPartition.artifactDirectoryRelative graph)
        |> Option.bind (fun relativePath ->
            resolveUnderDataDir dataDir relativePath |> Result.toOption)

    let rec withWorkTreeGates
        (workspaceRoots: string list)
        (action: unit -> Result<'value, string>)
        : Result<'value, string> =
        match workspaceRoots with
        | [] -> action ()
        | root :: rest ->
            WorkspaceGit.withWorkTreeGate root (fun () ->
                withWorkTreeGates rest action)

    let normalizedWorkTreeRoots (workspaceRoots: string list) =
        WorkspaceGit.normalizeWorkTreeRoots workspaceRoots

    let enumerateDocumentRoots (graph: Graph) : NodeId list =
        graph.nodes
        |> Map.toSeq
        |> Seq.choose (fun (id, _) ->
            if DocumentPartition.isDocumentRootNode graph id then Some id else None)
        |> Seq.sortBy (fun id ->
            let depth =
                match DocumentPartition.artifactFileRelative graph id with
                | None -> 0
                | Some rel ->
                    rel.Split(
                        [| '/'; '\\' |],
                        StringSplitOptions.RemoveEmptyEntries).Length
            -depth, id.Value)
        |> Seq.toList

    let noArtifactPathError (graph: Graph) (documentRootId: NodeId) =
        let prefix =
            $"no artifact path for document root: id={documentRootId.Value}"

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

    let private fileStatusResponse
        (nodeReference: string)
        (status: DesktopFileStatus)
        (sourceModifiedUtc: DateTime option)
        : Result<DesktopFileStatusResponse, string> =
        Ok
            { path = nodeReference
              status = status
              sourceModifiedUtc = sourceModifiedUtc }

    let private directoryFileStatus
        (nodeReference: string)
        (fullPath: string)
        : Result<DesktopFileStatusResponse, string> =
        let parentDir = Path.GetDirectoryName fullPath

        if
            not (isNull parentDir)
            && Directory.Exists parentDir
        then
            fileStatusResponse
                nodeReference
                ExistingFolder
                (Some (Directory.GetLastWriteTimeUtc parentDir))
        elif File.Exists fullPath then
            fileStatusResponse
                nodeReference
                ExistingFolder
                (Some (File.GetLastWriteTimeUtc fullPath))
        else
            fileStatusResponse nodeReference MissingArtifact None

    let private existingPathStatus
        (nodeReference: string)
        (fullPath: string)
        : Result<DesktopFileStatusResponse, string> =
        if File.Exists fullPath then
            fileStatusResponse
                nodeReference
                ExistingFile
                (Some (File.GetLastWriteTimeUtc fullPath))
        elif Directory.Exists fullPath then
            fileStatusResponse nodeReference ExistingFolder None
        else
            fileStatusResponse nodeReference MissingArtifact None

    let fileStatusForReference
        (dataDir: string)
        (nodeReference: string)
        : Result<DesktopFileStatusResponse, string> =
        match NodeDesktopPath.artifactRelativeForReference nodeReference with
        | Error _ ->
            fileStatusResponse nodeReference InvalidPath None
        | Ok relativePath ->
            match resolveUnderDataDir dataDir relativePath with
            | Error _ ->
                fileStatusResponse nodeReference InvalidPath None
            | Ok fullPath ->
                // Workspace/directory refs resolve to `…/.amb`. Treat Directory File
                // paths as folders (not "file"), including when only the parent dir
                // exists after WebDAV MKCOL without an `.amb` body yet.
                let directoryFileBasename = Path.GetFileName fullPath
                let isDirectoryFile =
                    String.Equals(
                        directoryFileBasename,
                        ".amb",
                        StringComparison.OrdinalIgnoreCase)

                if isDirectoryFile then
                    directoryFileStatus nodeReference fullPath
                else
                    existingPathStatus nodeReference fullPath

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

    let readFileTextAtRelative
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

    let stampNodes
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

    let stampExistingDocuments
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
                    resolveUnderDataDir
                        dataDir
                        (relativePathFromDataDir dataDir fullPath))
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
