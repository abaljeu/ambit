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
