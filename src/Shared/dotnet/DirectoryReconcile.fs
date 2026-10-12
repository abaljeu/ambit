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
          directoryId: NodeId
          discover: string -> Result<string list, string> }

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

    [<RequireQualifiedAccess>]
    type private DiskMember =
        | File of string * DateTime
        | Dir of string * DateTime

    let private namedEntry (dirFull: string) (rel: string) =
        let trimmed = rel.Replace('\\', '/').Trim('/')
        if trimmed = "" || trimmed.Contains "/" then
            None
        else
            let full = Path.Combine(dirFull, trimmed)
            match Filename.create trimmed with
            | Filename.Ok name when Directory.Exists full ->
                let mtime = Directory.GetLastWriteTimeUtc full
                Some(DiskMember.Dir(name, mtime))
            | Filename.Ok name when File.Exists full ->
                let mtime = File.GetLastWriteTimeUtc full
                Some(DiskMember.File(name, mtime))
            | _ -> None

    let private splitMembers (dirFull: string) (rels: string list) =
        let entries = rels |> List.choose (namedEntry dirFull)
        let files =
            entries
            |> List.choose (function
                | DiskMember.File(name, mtime) -> Some(name, mtime)
                | DiskMember.Dir _ -> None)
        let dirs =
            entries
            |> List.choose (function
                | DiskMember.Dir(name, mtime) -> Some(name, mtime)
                | DiskMember.File _ -> None)
        files, dirs

    let private membersOf
        (discover: string -> Result<string list, string>)
        (dirFull: string)
        =
        match discover dirFull with
        | Error err -> Error err
        | Ok rels ->
            try
                Ok(splitMembers dirFull rels)
            with ex ->
                Error ex.Message

    type private CreatePlan =
        { graph: Graph
          directoryId: NodeId
          kind: SpecialKind }

    let private knownNamed (plan: CreatePlan) =
        DocumentPartition.memberNodeIds plan.graph plan.directoryId
        |> Set.fold
            (fun acc id ->
                if id = plan.directoryId then
                    acc
                else
                    match Map.tryFind id plan.graph.nodes with
                    | Some { kind = Special found; name = Filename.Ok name }
                        when found = plan.kind ->
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

    type private Created =
        { ops: Op list
          ids: NodeId list
          children: ChildNode list }

    let private planOne (plan: CreatePlan) (reserved, acc) name =
        let unique =
            GraphQuery.unusedOwnedName
                plan.graph
                plan.directoryId
                name
                reserved
        let id = NodeId.New()
        let op = Op.NewSpecialNode(id, plan.kind, unique)
        let next = Set.add (unique.ToLowerInvariant()) reserved
        next, (id, op) :: acc

    let private createMissing
        (plan: CreatePlan)
        (children: ChildNode list)
        (names: string list)
        =
        match names with
        | [] ->
            Ok
                { ops = []
                  ids = []
                  children = children }
        | _ ->
            let _, plannedRev =
                List.fold (planOne plan) (Set.empty, []) names
            let planned = List.rev plannedRev
            let appended =
                planned |> List.map (fun (id, _) -> ChildNode.owner id)
            let created = planned |> List.map snd
            let ids = planned |> List.map fst
            let appendOp =
                ChildListWire.append plan.directoryId children appended
            Ok
                { ops = created @ [ appendOp ]
                  ids = ids
                  children = children @ appended }

    let private reconcileKind
        (plan: CreatePlan)
        (entries: (string * DateTime) list)
        (children: ChildNode list)
        =
        let names = missingNames (knownNamed plan) entries
        createMissing plan children names

    let private nameSet (entries: (string * DateTime) list) =
        entries
        |> List.map (fun (name, _) -> name.ToLowerInvariant())
        |> Set.ofList

    let private absentIds (known: Map<string, NodeId>) (present: Set<string>) =
        known
        |> Map.fold
            (fun acc name id ->
                if Set.contains name present then acc else id :: acc)
            []

    let private goneByParent (graph: Graph) (ids: NodeId list) =
        ids
        |> List.fold
            (fun acc id ->
                match Map.tryFind id graph.ownerParentByChild with
                | None -> acc
                | Some parentId ->
                    let prior =
                        Map.tryFind parentId acc
                        |> Option.defaultValue Set.empty
                    Map.add parentId (Set.add id prior) acc)
            Map.empty

    let private childrenAfterDrops
        (graph: Graph)
        (directoryId: NodeId)
        (dropIds: Set<NodeId>)
        =
        GraphChildren.get graph directoryId
        |> List.filter (fun child ->
            Node.childOwnership graph directoryId child <> Ownership.Owner
            || not (Set.contains child.id dropIds))

    let private markCurrent (graph: Graph) (directoryId: NodeId) =
        match Map.tryFind directoryId graph.nodes with
        | Some node when node.documentState = Unparsed ->
            [ Op.SetDocumentState(directoryId, Unparsed, Current) ]
        | _ -> []

    let private planFromEntries
        (graph: Graph)
        (directoryId: NodeId)
        (files: (string * DateTime) list)
        (dirs: (string * DateTime) list)
        =
        let filePlan =
            { graph = graph
              directoryId = directoryId
              kind = SpecialKind.File }
        let dirPlan = { filePlan with kind = SpecialKind.Directory }
        let gone =
            absentIds (knownNamed filePlan) (nameSet files)
            @ absentIds (knownNamed dirPlan) (nameSet dirs)
        let dropIds = Set.ofList gone
        let deleteOps =
            let grouped = goneByParent graph gone
            if Map.isEmpty grouped then
                []
            else
                ViewModelDeleteOps.planDeleteDroppedOwnedMany graph grouped
        let children = childrenAfterDrops graph directoryId dropIds
        reconcileKind filePlan files children
        |> Result.bind (fun fileCreated ->
            reconcileKind dirPlan dirs fileCreated.children
            |> Result.map (fun dirCreated ->
                { ops =
                    markCurrent graph directoryId
                    @ deleteOps
                    @ fileCreated.ops
                    @ dirCreated.ops
                  push = [] }))

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
                    membersOf input.discover full
                    |> Result.bind (fun (files, dirs) ->
                        planFromEntries
                            input.graph
                            input.directoryId
                            files
                            dirs))
