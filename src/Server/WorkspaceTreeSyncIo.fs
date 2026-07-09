namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

[<RequireQualifiedAccess>]
module WorkspaceTreeSyncIo =

    let private directoryRelative (graph: Graph) (nodeId: NodeId) : Result<string, string> =
        match Map.tryFind nodeId graph.nodes with
        | None -> Error "node not found"
        | Some node ->
            match node.kind with
            | Special Workspace ->
                match Filename.tryValue node.name with
                | Some name when name <> "" -> Ok name
                | _ -> Ok ""
            | Special Directory ->
                DocumentPartition.artifactDirectoryRelative graph nodeId
                |> function
                    | Some path -> Ok (path.TrimEnd('/'))
                    | None -> Error "directory has no disk path"
            | _ -> Error "sync target must be a Workspace or Directory"

    let private listImmediateChildrenAt (fullDir: string) : DiskTreeEntry list =
        if not (Directory.Exists fullDir) then
            []
        else
            let files =
                Directory.EnumerateFiles(fullDir)
                |> Seq.choose (fun path ->
                    let name = Path.GetFileName path
                    if WorkspaceTreeSync.shouldSkipEntry name then
                        None
                    else
                        let info = FileInfo(path)
                        Some
                            { name = name
                              kind = File
                              mtimeUtc = info.LastWriteTimeUtc.Ticks })

            let dirs =
                Directory.EnumerateDirectories(fullDir)
                |> Seq.choose (fun path ->
                    let name = Path.GetFileName path
                    if WorkspaceTreeSync.shouldSkipEntry name then
                        None
                    else
                        let info = DirectoryInfo(path)
                        Some
                            { name = name
                              kind = Directory
                              mtimeUtc = info.LastWriteTimeUtc.Ticks })

            List.ofSeq (Seq.append dirs files)

    let rec private buildBranches (fullDir: string) : DiskTreeBranch list =
        listImmediateChildrenAt fullDir
        |> List.map (fun entry ->
            let children =
                if entry.kind = Directory then
                    buildBranches (Path.Combine(fullDir, entry.name))
                else
                    []

            { entry = entry; children = children })

    let listImmediateChildren (dataDir: string) (graph: Graph) (nodeId: NodeId) : Result<DiskTreeEntry list, string> =
        match directoryRelative graph nodeId with
        | Error msg -> Error msg
        | Ok relative ->
            let fullDir =
                if String.IsNullOrEmpty relative then
                    dataDir
                else
                    Path.Combine(dataDir, relative.Replace('/', Path.DirectorySeparatorChar))

            try
                Ok(listImmediateChildrenAt fullDir)
            with ex ->
                Error ex.Message

    let listRecursiveTree (dataDir: string) (graph: Graph) (nodeId: NodeId) : Result<DiskTreeBranch list, string> =
        match directoryRelative graph nodeId with
        | Error msg -> Error msg
        | Ok relative ->
            let fullDir =
                if String.IsNullOrEmpty relative then
                    dataDir
                else
                    Path.Combine(dataDir, relative.Replace('/', Path.DirectorySeparatorChar))

            try
                Ok(buildBranches fullDir)
            with ex ->
                Error ex.Message

    let readFileArtifact
        (dataDir: string)
        (graph: Graph)
        (fileNodeId: NodeId)
        : Result<string * string * int64, string> =
        match DocumentPartition.artifactFileRelative graph fileNodeId with
        | None -> Error "file has no disk path"
        | Some relative ->
            let fullPath = Path.Combine(dataDir, relative.Replace('/', Path.DirectorySeparatorChar))

            if not (File.Exists fullPath) then
                Error ("file not found on disk: " + relative)
            else
                try
                    let info = FileInfo(fullPath)
                    let text = File.ReadAllText fullPath
                    Ok(relative, text, info.LastWriteTimeUtc.Ticks)
                with ex ->
                    Error ex.Message
