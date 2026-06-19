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

    let resolveArtifactPath
        (dataDir: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<string, string> =
        match DocumentPartition.artifactFileRelative graph documentRootId with
        | None -> Error "no artifact path for document root"
        | Some relativePath -> resolveUnderDataDir dataDir relativePath

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
                            Ok ()
                        with ex ->
                            Error ex.Message

            match createDirResult with
            | Error msg -> Error msg
            | Ok () ->
                let parent = Path.GetDirectoryName fullPath

                if not (String.IsNullOrEmpty parent) then
                    Directory.CreateDirectory parent |> ignore

                match AmbDocument.write graph documentRootId with
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
        |> List.fold
            (fun acc documentRootId ->
                match acc with
                | Error msg -> Error msg
                | Ok paths ->
                    match writeDocument dataDir graph documentRootId with
                    | Error msg -> Error msg
                    | Ok path -> Ok (paths @ [ path ]))
            (Ok [])

    let private shouldSkipDiscoveryFile (fileName: string) =
        fileName = "gambol"
        || fileName.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".log", StringComparison.OrdinalIgnoreCase)
        || fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
        || fileName.Contains(".bak.", StringComparison.OrdinalIgnoreCase)

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
                |> Seq.filter (fun fullPath -> not (shouldSkipDiscoveryFile (Path.GetFileName fullPath)))
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
                && (rel.StartsWith("@") || rel.EndsWith("/.amb", StringComparison.Ordinal)))

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
