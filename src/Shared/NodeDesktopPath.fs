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
            Map.tryFind ownerId graph.nodes
            |> Option.bind (pathForNode graph visited)

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
            Map.tryFind ownerId graph.nodes
            |> Option.bind (expandedPathForNode graph visited)

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

    let private isRootFileSegment (segment: string) =
        segment.Contains '.' && not (segment.EndsWith "/")

    let private toDiskPath (rest: string) =
        if String.IsNullOrEmpty rest then
            None
        else
            let segment, tail = firstSegment rest

            if String.IsNullOrEmpty tail then
                Some segment
            else
                Some (segment + "/" + tail)

    let private canonicalDirectoryInner (inner: string) =
        let rec strip (rest: string) =
            if String.IsNullOrEmpty rest then
                rest
            elif not (rest.Contains '/') then
                rest
            else
                let segment, tail = firstSegment rest

                if isRootFileSegment segment then
                    strip (tail.TrimStart('/'))
                else
                    rest

        strip (inner.TrimEnd('/'))

    let canonicalDesktopPath (path: string) : string option =
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
                Some (rootPrefix + canonicalDirectoryInner inner + "/")
        else
            Some trimmed

    let private directoryArtifactRelative (inner: string) : Result<string, string> =
        let canonical = canonicalDirectoryInner inner

        if canonical.Contains '/' then
            match toDiskPath canonical with
            | Some disk -> Ok (disk + "/.amb")
            | None -> Error ("invalid node reference: //" + inner + "/")
        else
            Ok (canonical + "/.amb")

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

                if segment = "TRASH" || isRootFileSegment segment then
                    None
                else
                    Some (segment, tail)

    /// Named workspace label for git connect/clone/pull/push (not ROOT / empty).
    let tryWorkspaceGitLabel (graph: Graph) (nodeId: NodeId) : string option =
        pathForNodeId graph nodeId
        |> Option.bind tryParseWorkspacePath
        |> Option.bind (fun (label, _) ->
            if String.IsNullOrEmpty label then None else Some label)

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
            elif inner = "TRASH" then
                Ok "TRASH/.amb"
            else
                directoryArtifactRelative inner
        else
            let inner = path.Substring rootPrefix.Length

            if String.IsNullOrEmpty inner then
                Error ("invalid node reference: " + nodeReference)
            elif not (inner.Contains '/') && isRootFileSegment inner then
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
