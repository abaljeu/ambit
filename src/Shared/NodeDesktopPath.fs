namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module NodeDesktopPath =

    [<Literal>]
    let rootPrefix = "//"

    let private joinSegment (prefix: string) (name: string) =
        if prefix = rootPrefix then rootPrefix + name
        elif prefix.EndsWith("/") then prefix + name
        else prefix + "/" + name

    let rec private pathForNode (graph: Graph) (visited: Set<NodeId>) (node: Node) : string option =
        if Set.contains node.id visited then
            None
        else
            match node.kind with
            | Normal ->
                match FileReference.parseFirst node.text with
                | FileReference path -> Some path
                | _ -> None
            | Special Workspaces -> None
            | Special Workspace when node.id = Graph.rootId -> Some rootPrefix
            | Special (Workspace | Directory | File) ->
                match Filename.tryValue node.name with
                | None -> None
                | Some name ->
                    let prefixOpt =
                        match node.kind with
                        | Special Directory ->
                            match Map.tryFind node.owner graph.nodes with
                            | Some { kind = Special File } -> Some rootPrefix
                            | _ -> parentPrefix graph (Set.add node.id visited) node.owner
                        | _ -> parentPrefix graph (Set.add node.id visited) node.owner

                    prefixOpt
                    |> Option.map (fun prefix ->
                        let path = joinSegment prefix name

                        match node.kind with
                        | Special Directory -> path + "/"
                        | _ -> path)

    and private parentPrefix (graph: Graph) (visited: Set<NodeId>) (ownerId: NodeId) =
        if ownerId = Graph.workspacesId then
            Some rootPrefix
        else
            match Map.tryFind ownerId graph.nodes with
            | Some ({ kind = Normal } as owner) ->
                if Set.contains ownerId visited then
                    None
                else
                    parentPrefix graph (Set.add ownerId visited) owner.owner
            | ownerOpt -> ownerOpt |> Option.bind (pathForNode graph visited)

    let rec private expandedPathForNode (graph: Graph) (visited: Set<NodeId>) (node: Node) : string option =
        if Set.contains node.id visited then
            None
        else
            match node.kind with
            | Normal ->
                match FileReference.parseFirst node.text with
                | FileReference path -> Some path
                | _ -> None
            | Special Workspaces -> None
            | Special Workspace when node.id = Graph.rootId -> Some rootPrefix
            | Special (Workspace | Directory | File) ->
                match Filename.tryValue node.name with
                | None -> None
                | Some name ->
                    expandedParentPrefix graph (Set.add node.id visited) node.owner
                    |> Option.map (fun prefix ->
                        let path = joinSegment prefix name

                        match node.kind with
                        | Special Directory -> path + "/"
                        | _ -> path)

    and private expandedParentPrefix (graph: Graph) (visited: Set<NodeId>) (ownerId: NodeId) =
        if ownerId = Graph.workspacesId then
            Some rootPrefix
        else
            match Map.tryFind ownerId graph.nodes with
            | Some ({ kind = Normal } as owner) ->
                if Set.contains ownerId visited then
                    None
                else
                    expandedParentPrefix graph (Set.add ownerId visited) owner.owner
            | ownerOpt -> ownerOpt |> Option.bind (expandedPathForNode graph visited)

    let pathForNodeId (graph: Graph) (nodeId: NodeId) : string option =
        Map.tryFind nodeId graph.nodes
        |> Option.bind (pathForNode graph Set.empty)

    let expandedPathForNodeId (graph: Graph) (nodeId: NodeId) : string option =
        Map.tryFind nodeId graph.nodes
        |> Option.bind (expandedPathForNode graph Set.empty)

    let fileReferenceForNodeId (graph: Graph) (nodeId: NodeId) : FileReference option =
        match Map.tryFind nodeId graph.nodes with
        | None -> None
        | Some node ->
            match pathForNode graph Set.empty node with
            | Some path -> Some (FileReference path)
            | None ->
                match node.kind with
                | Normal -> Some (FileReference.parseFirst node.text)
                | _ -> Some NoFileReference

    let private firstSegment (rest: string) =
        let slash = rest.IndexOf('/')
        if slash < 0 then rest, ""
        else rest.Substring(0, slash), rest.Substring(slash + 1)

    let private toDiskPath (rest: string) =
        if String.IsNullOrEmpty rest then
            None
        else
            let segment, tail = firstSegment rest

            if String.IsNullOrEmpty tail then
                Some segment
            else
                Some (segment + "/" + tail)

    /// A leading segment is a File only when that node's kind is File.
    /// A Workspace or Directory keeps a dot in its name.
    let private nodeNamedUnder
        (graph: Graph)
        (parentId: NodeId)
        (segment: string)
        : Node option =
        GraphChildren.get graph parentId
        |> List.tryPick (fun child ->
            match Map.tryFind child.id graph.nodes with
            | Some node ->
                match Filename.tryValue node.name with
                | Some name when name = segment -> Some node
                | _ -> None
            | None -> None)

    let private fileIdForSegment
        (graph: Graph)
        (parents: NodeId list)
        (segment: string)
        : NodeId option =
        parents
        |> List.tryPick (fun parentId ->
            match nodeNamedUnder graph parentId segment with
            | Some { kind = Special File; id = id } -> Some id
            | _ -> None)

    let private stripLeadingFileSegments (graph: Graph) (inner: string) =
        let rec strip (parents: NodeId list) (rest: string) =
            if String.IsNullOrEmpty rest || not (rest.Contains '/') then
                rest
            else
                let segment, tail = firstSegment rest

                match fileIdForSegment graph parents segment with
                | Some fileId -> strip [ fileId ] (tail.TrimStart '/')
                | None -> rest

        strip [ Graph.rootId; Graph.workspacesId ] (inner.TrimEnd '/')

    let canonicalDesktopPath (graph: Graph) (path: string) : string option =
        let trimmed = path.Trim()

        if not (trimmed.StartsWith(rootPrefix, StringComparison.Ordinal)) then
            None
        elif trimmed = rootPrefix then
            Some rootPrefix
        elif trimmed.EndsWith "/" then
            let inner = trimmed.Substring(rootPrefix.Length).TrimEnd('/')

            if String.IsNullOrEmpty inner then
                None
            else
                let canonical = stripLeadingFileSegments graph inner
                Some (rootPrefix + canonical + "/")
        else
            Some trimmed

    let private directoryArtifactRelative (inner: string) : Result<string, string> =
        let canonical = inner.TrimEnd('/')

        if String.IsNullOrEmpty canonical then
            Error ("invalid node reference: //" + inner + "/")
        elif canonical.Contains '/' then
            match toDiskPath canonical with
            | Some disk -> Ok (disk + "/.amb")
            | None -> Error ("invalid node reference: //" + inner + "/")
        else
            Ok (canonical + "/.amb")

    let private escapeDavSegment (segment: string) =
        Uri.EscapeDataString segment

    let private davFileUrl (label: string) (relative: string) =
        let head = "/ambit/dav/" + escapeDavSegment label
        let parts =
            relative.Split(
                [| '/' |],
                StringSplitOptions.RemoveEmptyEntries)
            |> Array.map escapeDavSegment
        if parts.Length = 0 then head
        else head + "/" + String.Join("/", parts)

    /// Path tail under `//label`, or empty when the path is the Workspace itself.
    let relativeUnderLabel (label: string) (path: string) : string option =
        if String.IsNullOrEmpty label then
            None
        else
            let exact = rootPrefix + label
            let prefix = exact + "/"
            let trimmed = path.Trim()

            if trimmed = exact then
                Some ""
            elif trimmed.StartsWith(prefix, StringComparison.Ordinal) then
                Some (trimmed.Substring prefix.Length)
            else
                None

    let tryParseWorkspacePath (path: string) : (string * string) option =
        let trimmed = path.Trim()

        if not (trimmed.StartsWith(rootPrefix, StringComparison.Ordinal)) then
            None
        else
            let rest = trimmed.Substring rootPrefix.Length

            if String.IsNullOrEmpty rest then
                Some ("", "")
            else
                let segment, tail = firstSegment (rest.TrimStart('/'))
                Some (segment, tail)

    let private isNamedWorkspaceNode (node: Node) : bool =
        node.id <> Graph.rootId
        && match node.kind with
           | Special Workspace ->
               match Filename.tryValue node.name with
               | Some label -> not (String.IsNullOrEmpty label)
               | None -> false
           | _ -> false

    /// Named workspace label for git connect/clone/pull/push (not ROOT / empty).
    /// Resolves from the node or any owner ancestor (File/Directory/Normal under a workspace).
    let enclosingWorkspaceName (graph: Graph) (nodeId: NodeId) : string option =
        GraphQuery.enclosing graph isNamedWorkspaceNode nodeId
        |> Option.bind (fun wsId ->
            Map.tryFind wsId graph.nodes
            |> Option.bind (fun n -> Filename.tryValue n.name))

    /// Jump to Target: only a File node opens a file. A Workspace or Directory does nothing.
    [<RequireQualifiedAccess>]
    type NodeOpenAction =
        | OpenFile
        | OpenLink
        | NoOp

    let nodeOpenAction (kind: NodeKind) : NodeOpenAction =
        match kind with
        | Special File -> NodeOpenAction.OpenFile
        | Special (Workspace | Directory) -> NodeOpenAction.NoOp
        | _ -> NodeOpenAction.OpenLink

    let fileOpenUrl (graph: Graph) (nodeId: NodeId) : string option =
        match Map.tryFind nodeId graph.nodes with
        | Some { kind = Special File } ->
            match enclosingWorkspaceName graph nodeId with
            | None -> None
            | Some label ->
                pathForNodeId graph nodeId
                |> Option.bind (relativeUnderLabel label)
                |> Option.map (fun relative -> davFileUrl label relative)
        | _ -> None

    let artifactRelativeForReference (nodeReference: string) : Result<string, string> =
        let path = nodeReference.Trim()

        if not (path.StartsWith(rootPrefix, StringComparison.Ordinal)) then
            Error ("invalid node reference: " + nodeReference)
        elif path = rootPrefix then
            Ok ".amb"
        elif path.EndsWith "/" then
            let inner = path.Substring(rootPrefix.Length).TrimEnd('/')

            if String.IsNullOrEmpty inner then
                Error ("invalid node reference: " + nodeReference)
            else
                directoryArtifactRelative inner
        else
            let inner = path.Substring rootPrefix.Length

            if String.IsNullOrEmpty inner then
                Error ("invalid node reference: " + nodeReference)
            // No Graph on this path. A single segment that contains '.' is a
            // root File on disk (`name.ext`). A dotted Workspace needs a Graph
            // before it can map to `label/.amb`.
            elif not (inner.Contains '/') && inner.Contains '.' then
                Ok inner
            elif not (inner.Contains '/') then
                Ok (inner + "/.amb")
            else
                match toDiskPath inner with
                | Some disk -> Ok disk
                | None -> Error ("invalid node reference: " + nodeReference)

    let desktopFileToDisk (path: string) : string option =
        if not (path.StartsWith(rootPrefix, StringComparison.Ordinal)) then
            None
        else
            let rest = path.Substring rootPrefix.Length

            if String.IsNullOrEmpty rest then
                None
            else
                toDiskPath (rest.TrimStart('/'))
