namespace Gambol.Shared

open System
open System.IO

/// Scan one directory and update its body. The body is the nodes tied to
/// that directory. This does not read the Directory File.
[<RequireQualifiedAccess>]
module DirectoryReconcile =

    type Input =
        { dataDir: string
          graph: Graph
          directoryId: NodeId }

    type Output =
        { ops: Op list
          push: NodeId list }

    let private isReconcileTarget (node: Node) =
        match node.kind with
        | Special (Directory | Workspace) -> true
        | _ -> false

    let private splitSegments (relativePath: string) =
        relativePath.Split(
            [| '/'; '\\' |],
            StringSplitOptions.RemoveEmptyEntries)
        |> Array.toList

    let private resolveDirectory (dataDir: string) (relativeDir: string) =
        let segments = splitSegments relativeDir
        if List.exists (fun segment -> segment = "..") segments then
            Error "invalid relative path"
        else
            let combined =
                segments
                |> List.fold
                    (fun acc segment -> Path.Combine(acc, segment))
                    dataDir
            let full = Path.GetFullPath combined
            let root = Path.GetFullPath dataDir
            let sep = string Path.DirectorySeparatorChar
            let rootPrefix = if root.EndsWith sep then root else root + sep
            let sameRoot =
                String.Equals(
                    full.TrimEnd(Path.DirectorySeparatorChar),
                    root.TrimEnd(Path.DirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase)
            if sameRoot
               || full.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) then
                Ok full
            else
                Error "path escapes data directory"

    let private diskFiles (dirFull: string) =
        try
            if not (Directory.Exists dirFull) then
                Ok []
            else
                Directory.EnumerateFiles dirFull
                |> Seq.choose (fun full ->
                    let name = Path.GetFileName full
                    if Filename.isDirectoryFileBasename name then
                        None
                    else
                        match Filename.create name with
                        | Filename.Ok okName ->
                            Some(okName, File.GetLastWriteTimeUtc full)
                        | _ -> None)
                |> Seq.toList
                |> Ok
        with ex ->
            Error ex.Message

    let private bodyFileIds (graph: Graph) (directoryId: NodeId) =
        DocumentPartition.memberNodeIds graph directoryId
        |> Set.fold
            (fun acc id ->
                if id = directoryId then
                    acc
                else
                    match Map.tryFind id graph.nodes with
                    | Some { kind = Special File; name = Filename.Ok name } ->
                        Map.add (name.ToLowerInvariant()) id acc
                    | _ -> acc)
            Map.empty

    let private byName (a: string) (b: string) =
        String.Compare(a, b, StringComparison.OrdinalIgnoreCase)

    let private missingNames
        (known: Map<string, NodeId>)
        (files: (string * DateTime) list)
        =
        files
        |> List.choose (fun (name, _) ->
            if Map.containsKey (name.ToLowerInvariant()) known then
                None
            else
                Some name)
        |> List.sortWith byName

    let private diskNewerIds
        (graph: Graph)
        (known: Map<string, NodeId>)
        (files: (string * DateTime) list)
        =
        files
        |> List.sortWith (fun (a, _) (b, _) -> byName a b)
        |> List.choose (fun (name, mtime) ->
            Map.tryFind (name.ToLowerInvariant()) known
            |> Option.bind (fun id ->
                Map.tryFind id graph.nodes
                |> Option.bind (fun node ->
                    let nodeTime = NodeUpdateTime.toDbPrecision node.updateTime
                    let diskTime = NodeUpdateTime.toDbPrecision mtime
                    if nodeTime < diskTime then Some id else None)))

    let private planOne
        (graph: Graph)
        (directoryId: NodeId)
        (reserved, acc)
        name
        =
        let unique =
            GraphQuery.unusedOwnedName graph directoryId name reserved
        let id = NodeId.New()
        let op = Op.NewSpecialNode(id, SpecialKind.File, unique)
        let next = Set.add (unique.ToLowerInvariant()) reserved
        next, (id, op) :: acc

    let private createMissing
        (graph: Graph)
        (directoryId: NodeId)
        (names: string list)
        =
        match names with
        | [] -> Ok []
        | _ ->
            let _, plannedRev =
                List.fold (planOne graph directoryId) (Set.empty, []) names
            let planned = List.rev plannedRev
            let children = GraphChildren.get graph directoryId
            let appended =
                planned |> List.map (fun (id, _) -> ChildNode.owner id)
            let created = planned |> List.map snd
            let appendOp =
                ChildListWire.append directoryId children appended
            Ok (created @ [ appendOp ])

    let private planFromFiles
        (graph: Graph)
        (directoryId: NodeId)
        (files: (string * DateTime) list)
        =
        let known = bodyFileIds graph directoryId
        let names = missingNames known files
        createMissing graph directoryId names
        |> Result.map (fun createOps ->
            let newer = diskNewerIds graph known files
            { ops = createOps
              push = newer })

    let planDirectoryReconcile (input: Input) : Result<Output, string> =
        match Map.tryFind input.directoryId input.graph.nodes with
        | None -> Error "directory node not found"
        | Some node when not (isReconcileTarget node) ->
            Error "not a directory or workspace node"
        | Some _ ->
            match
                DocumentPartition.artifactDirectoryRelative
                    input.graph
                    input.directoryId
            with
            | None -> Error "directory has no disk path"
            | Some relative ->
                resolveDirectory input.dataDir relative
                |> Result.bind (fun full ->
                    diskFiles full
                    |> Result.bind (planFromFiles input.graph input.directoryId))
