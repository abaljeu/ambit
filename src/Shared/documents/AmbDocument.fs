namespace Gambol.Shared

open System

/// RQA: keeps `nodes` off the unqualified field pool so it does not clash with `Graph.nodes` across assemblies.
[<RequireQualifiedAccess>]
type AmbDocumentReadResult = {
    documentRootId: NodeId
    nodes: Map<NodeId, Node>
}

[<RequireQualifiedAccess>]
module AmbDocument =

    let private nl = Environment.NewLine

    let formatStableId (nodeId: NodeId) : string =
        if nodeId = Graph.workspacesId then
            "WORKSPACES"
        elif nodeId = Graph.systemId then
            "SYSTEM"
        elif nodeId = Graph.trashId then
            "TRASH"
        else
            nodeId.Value.ToString()

    let tryParseStableId (token: string) : NodeId option =
        match token with
        | "WORKSPACES" -> Some Graph.workspacesId
        | "SYSTEM" -> Some Graph.systemId
        | "TRASH" -> Some Graph.trashId
        | _ ->
            match Guid.TryParse token with
            | true, guid -> Some (NodeId guid)
            | false, _ -> None

    let private parseOutlineMeta (raw: string) : CssClasses * string =
        if not (raw.StartsWith("{")) then
            CssClass.empty, raw
        else
            let closeIdx = raw.IndexOf('}')
            if closeIdx < 0 then
                CssClass.empty, raw
            else
                let metaContent = raw.Substring(1, closeIdx - 1)
                let nodeText = raw.Substring(closeIdx + 1)
                let classList =
                    metaContent.Split(' ')
                    |> Array.toList
                    |> List.choose (fun tok ->
                        let t = tok.Trim()
                        if t.StartsWith(".") && t.Length > 1 then Some (t.Substring(1))
                        else None)
                CssClass.ofList classList, nodeText

    let private splitStableIdPrefix (text: string) : (string * string) option =
        if text.StartsWith("WORKSPACES") then
            Some ("WORKSPACES", text.Substring("WORKSPACES".Length).TrimStart())
        elif text.StartsWith("SYSTEM") then
            Some ("SYSTEM", text.Substring("SYSTEM".Length).TrimStart())
        elif text.StartsWith("TRASH") then
            Some ("TRASH", text.Substring("TRASH".Length).TrimStart())
        else
            let spaceIdx = text.IndexOf(' ')
            let token =
                if spaceIdx < 0 then text
                else text.Substring(0, spaceIdx)
            match Guid.TryParse token with
            | true, _ ->
                let rest =
                    if spaceIdx < 0 then ""
                    else text.Substring(spaceIdx + 1).TrimStart()
                Some (token, rest)
            | false, _ -> None

    let private parseOwnerRest (rest: string) : Filename * string =
        let tabIdx = rest.IndexOf('\t')
        if tabIdx >= 0 then
            let nameToken = rest.Substring(0, tabIdx)
            let body = rest.Substring(tabIdx + 1)
            match Filename.create nameToken with
            | Filename.Ok validName -> Filename.Ok validName, body
            | _ -> Filename.Empty, rest
        else
            Filename.Empty, rest

    let private parseRefTarget (target: string) : (string option * string) option =
        let caretIdx = target.LastIndexOf('^')
        if caretIdx < 0 then
            None
        elif caretIdx = 0 then
            Some (None, target.Substring(1))
        else
            let path = target.Substring(0, caretIdx)
            Some (Some path, target.Substring(caretIdx + 1))

    let private outlineSourceLines (text: string) : string array =
        if String.IsNullOrEmpty text then
            Array.empty
        else
            text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n')

    let private lineBodyFor (node: Node) (bodyText: string) =
        let needsMeta =
            not (CssClass.toList node.cssClasses).IsEmpty || bodyText.StartsWith("{")
        if needsMeta then
            "{" + CssClass.toMetaString node.cssClasses + "}" + bodyText
        else
            bodyText

    let private plainLineContent (node: Node) (bodyText: string) =
        lineBodyFor node bodyText

    let private refTargetIds (graph: Graph) : Set<NodeId> =
        graph.nodes
        |> Map.toSeq
        |> Seq.collect (fun (_, node) -> node.children)
        |> Seq.filter (fun child -> child.ref = Ownership.Ref)
        |> Seq.map (fun child -> child.id)
        |> Set.ofSeq

    let private ownerLineContent (nodeId: NodeId) (node: Node) (bodyText: string) : string =
        let sid = formatStableId nodeId
        let body = lineBodyFor node bodyText
        match Filename.tryValue node.name with
        | None -> "^" + sid + " " + body
        | Some name -> "^" + sid + " " + name + "\t" + body

    let private refLineContent
        (graph: Graph)
        (documentRootId: NodeId)
        (documentMembers: Set<NodeId>)
        (nodeId: NodeId)
        : string =
        let sid = formatStableId nodeId
        let usePath =
            DocumentPartition.isNestedDocumentRootBoundary graph documentRootId nodeId
            || not (Set.contains nodeId documentMembers)
        if usePath then
            match NodeDesktopPath.pathForNodeId graph nodeId with
            | None -> "-> ^" + sid
            | Some path -> "-> " + path + "^" + sid
        else
            "-> ^" + sid

    type SerializedLine = {
        depth: int
        content: string
        nodeId: NodeId option
    }

    let tryHardKey (content: string) : string option =
        if content.StartsWith("-> ") then
            match parseRefTarget (content.Substring(3).Trim()) with
            | Some(_, token) -> Some("->^" + token)
            | None -> None
        elif content.StartsWith("^") then
            match splitStableIdPrefix (content.Substring(1)) with
            | Some(token, _) -> Some("^" + token)
            | None -> None
        else
            None

    let flattenText (text: string) : (int * string * string option) list =
        outlineSourceLines text
        |> Array.toList
        |> List.map (fun line ->
            let depth = line |> Seq.takeWhile ((=) '\t') |> Seq.length
            let content = line.Substring depth
            depth, content, tryHardKey content)

    let serializeLines
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<SerializedLine list, string> =
        match Map.tryFind documentRootId graph.nodes with
        | None -> Error "document root not found"
        | Some rootNode ->
            let documentMembers =
                DocumentPartition.memberNodeIds graph documentRootId
            let refTargets = refTargetIds graph
            let occurrenceCount =
                graph.nodes
                |> Map.toSeq
                |> Seq.collect (fun (_, node) ->
                    node.children |> Seq.map (fun child -> child.id))
                |> Seq.groupBy id
                |> Seq.map (fun (nodeId, xs) -> nodeId, Seq.length xs)
                |> Map.ofSeq

            let rec writeChild
                (depth: int)
                (emittedOwners: Set<NodeId>)
                (acc: SerializedLine list)
                (child: ChildNode)
                : Set<NodeId> * SerializedLine list =
                let nodeId = child.id
                let isShared =
                    (occurrenceCount
                     |> Map.tryFind nodeId
                     |> Option.defaultValue 0) > 1

                match child.ref with
                | Ownership.Ref ->
                    let content =
                        refLineContent
                            graph documentRootId documentMembers nodeId
                    let line = {
                        depth = depth
                        content = content
                        nodeId = Some nodeId
                    }
                    emittedOwners, line :: acc
                | Ownership.Owner ->
                    let node = graph.nodes.[nodeId]
                    if DocumentPartition.isNestedDocumentRootBoundary
                        graph documentRootId nodeId then
                        let content =
                            refLineContent
                                graph documentRootId documentMembers nodeId
                        let line = {
                            depth = depth
                            content = content
                            nodeId = Some nodeId
                        }
                        emittedOwners, line :: acc
                    else
                        let body = node.text
                        if isShared && Set.contains nodeId emittedOwners then
                            let content =
                                refLineContent
                                    graph documentRootId documentMembers nodeId
                            let line = {
                                depth = depth
                                content = content
                                nodeId = Some nodeId
                            }
                            emittedOwners, line :: acc
                        else
                            let plain = plainLineContent node body
                            let ambiguousPlain =
                                plain.StartsWith("^")
                                || plain.StartsWith("-> ")
                            let content =
                                if isShared
                                   || Set.contains nodeId refTargets
                                   || ambiguousPlain then
                                    ownerLineContent nodeId node body
                                else
                                    plain
                            let line = {
                                depth = depth
                                content = content
                                nodeId = Some nodeId
                            }
                            let emitted' = Set.add nodeId emittedOwners
                            let emitted'', acc' =
                                node.children
                                |> List.fold
                                    (fun (em, a) c ->
                                        writeChild (depth + 1) em a c)
                                    (emitted', line :: acc)
                            emitted'', acc'

            let _, lines =
                rootNode.children
                |> List.fold
                    (fun (emitted, acc) child ->
                        writeChild 0 emitted acc child)
                    (Set.empty, [])

            Ok(List.rev lines)

    /// Serialize one document subtree. The document root is implicit; its children are depth 0.
    let write (graph: Graph) (documentRootId: NodeId) : Result<string, string> =
        serializeLines graph documentRootId
        |> Result.map (fun lines ->
            let sb = Text.StringBuilder()
            for line in lines do
                sb.Append(String.replicate line.depth "\t")
                    .Append(line.content)
                    .Append(nl)
                |> ignore
            sb.ToString())

    /// Previous file lines paired with node ids from the current graph projection.
    let mappedPrevious
        (previousText: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<(int * string * NodeId option * string option) list, string> =
        serializeLines graph documentRootId
        |> Result.map (fun serialized ->
            flattenText previousText
            |> List.mapi (fun i (depth, content, hardKey) ->
                let nodeId =
                    match List.tryItem i serialized with
                    | Some s -> s.nodeId
                    | None -> None
                depth, content, nodeId, hardKey))

    let previousOutlineIds
        (previousText: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<NodeId option list, string> =
        mappedPrevious previousText graph documentRootId
        |> Result.map (List.map (fun (_, _, nodeId, _) -> nodeId))

    /// Inject owner stable ids so cold read recovers LCS-kept plain rows.
    let projectAligned
        (aligned: (int * string * NodeId option) list)
        : string =
        let sb = Text.StringBuilder()
        for depth, content, nodeIdOpt in aligned do
            let body =
                match nodeIdOpt, tryHardKey content with
                | Some id, None when not (content.StartsWith("-> ")) ->
                    "^" + formatStableId id + " " + content
                | _ -> content
            sb.Append(String.replicate depth "\t")
                .Append(body)
                .Append(nl)
            |> ignore
        sb.ToString()

    let private prependChild
        (parentId: NodeId)
        (edge: ChildNode)
        (nodes: Map<NodeId, Node>)
        =
        let parent = nodes |> Map.find parentId
        nodes |> Map.add parentId { parent with children = edge :: parent.children }

    let rec private popStack depth stack =
        match stack with
        | (d, _) :: tail when d >= depth -> popStack depth tail
        | _ -> stack

    let private brokenLinkText = "Broken link."

    let private stubBrokenLink (nodeId: NodeId) : Node =
        Node.Create(nodeId, text = brokenLinkText)

    let private ensureNode
        (nodeId: NodeId)
        (nodes: Map<NodeId, Node>)
        (contextGraph: Graph)
        : Map<NodeId, Node> =
        if Map.containsKey nodeId nodes then
            nodes
        else
            match Map.tryFind nodeId contextGraph.nodes with
            | Some node when not (String.IsNullOrWhiteSpace node.text) ->
                Map.add nodeId { node with children = [] } nodes
            | Some node ->
                Map.add nodeId { node with children = []; text = brokenLinkText } nodes
            | None ->
                Map.add nodeId (stubBrokenLink nodeId) nodes

    let private resolveRefTarget
        (path: string option)
        (stableToken: string)
        (contextGraph: Graph)
        (localNodes: Map<NodeId, Node>)
        : Result<NodeId * Map<NodeId, Node>, string> =
        match tryParseStableId stableToken with
        | None -> Error ("invalid stable id in ref: " + stableToken)
        | Some nodeId ->
            match path with
            | None ->
                Ok (nodeId, ensureNode nodeId localNodes contextGraph)
            | Some _crossPath ->
                match Map.tryFind nodeId contextGraph.nodes with
                | None ->
                    Ok (nodeId, ensureNode nodeId localNodes contextGraph)
                | Some _ ->
                    Ok (nodeId, localNodes)

    let private resolveOwnerLine
        (stableToken: string)
        (name: Filename)
        (classes: CssClasses)
        (nodeText: string)
        (nodes: Map<NodeId, Node>)
        (contextGraph: Graph)
        : Result<NodeId * Map<NodeId, Node>, string> =
        match tryParseStableId stableToken with
        | None -> Error ("invalid stable id in owner line: " + stableToken)
        | Some nodeId ->
            let baseNode =
                match Map.tryFind nodeId nodes, Map.tryFind nodeId contextGraph.nodes with
                | Some node, _ -> node
                | None, Some node -> node
                | None, None ->
                    Node.Create(
                        nodeId,
                        text = nodeText,
                        name = name,
                        cssClasses = classes,
                        updateTime = NodeUpdateTime.now ())

            let merged =
                NodeUpdateTime.touch
                    { baseNode with
                        text = nodeText
                        name = name
                        cssClasses = classes }

            Ok (nodeId, Map.add nodeId merged nodes)

    let private tryMatchPlainOwnerChild
        (parentId: NodeId)
        (classes: CssClasses)
        (nodeText: string)
        (nodes: Map<NodeId, Node>)
        (contextGraph: Graph)
        (claimed: Set<NodeId>)
        : NodeId option =
        let matchesNode (nodeId: NodeId) (node: Node) =
            not (Set.contains nodeId claimed)
            && node.text = nodeText
            && node.cssClasses = classes

        let fromOwnerChild (nodeSources: Map<NodeId, Node>) (nodeId: NodeId) =
            match Map.tryFind nodeId nodeSources with
            | Some node when matchesNode nodeId node -> Some nodeId
            | _ -> None

        let fromParentChildren (nodeSources: Map<NodeId, Node>) (parent: Node) =
            parent.children
            |> List.tryPick (fun child ->
                if child.ref <> Ownership.Owner then None
                else fromOwnerChild nodeSources child.id)

        let fromOwnerLinks (nodeSources: Map<NodeId, Node>) =
            nodeSources
            |> Map.toSeq
            |> Seq.tryPick (fun (nodeId, node) ->
                if node.owner = parentId then fromOwnerChild nodeSources nodeId
                else None)

        let trySources nodeSources =
            match Map.tryFind parentId nodeSources with
            | None -> None
            | Some parent ->
                match fromParentChildren nodeSources parent with
                | Some nodeId -> Some nodeId
                | None -> fromOwnerLinks nodeSources

        match trySources contextGraph.nodes with
        | Some nodeId -> Some nodeId
        | None -> trySources nodes

    let private resolvePlainLine
        (parentId: NodeId)
        (classes: CssClasses)
        (nodeText: string)
        (nodes: Map<NodeId, Node>)
        (contextGraph: Graph)
        (claimed: Set<NodeId>)
        : NodeId * Map<NodeId, Node> * Set<NodeId> =
        match tryMatchPlainOwnerChild parentId classes nodeText nodes contextGraph claimed with
        | Some nodeId ->
            let baseNode =
                match Map.tryFind nodeId nodes, Map.tryFind nodeId contextGraph.nodes with
                | Some node, _ -> node
                | None, Some node -> node
                | None, None ->
                    Node.Create(
                        nodeId,
                        text = nodeText,
                        cssClasses = classes,
                        owner = parentId,
                        updateTime = NodeUpdateTime.now ())

            let merged =
                NodeUpdateTime.touch { baseNode with text = nodeText; cssClasses = classes }

            nodeId, Map.add nodeId merged nodes, Set.add nodeId claimed
        | None ->
            let nodeId = NodeId.New()
            let node =
                Node.Create(
                    nodeId,
                    text = nodeText,
                    cssClasses = classes,
                    owner = parentId,
                    updateTime = NodeUpdateTime.now ())

            nodeId, Map.add nodeId node nodes, claimed

    let private foldOutlineLine
        (documentRootId: NodeId)
        (contextGraph: Graph)
        (nodes, stack, idMap: Map<string, NodeId>, claimed: Set<NodeId>)
        (line: string)
        =
        let depth = line |> Seq.takeWhile ((=) '\t') |> Seq.length
        let content = line.Substring depth
        let stack = popStack depth stack
        let parentId = snd stack.Head

        if content.StartsWith("-> ") then
            let target = content.Substring(3).Trim()
            match parseRefTarget target with
            | None -> nodes, stack, idMap, claimed, Error ("invalid ref line: " + content)
            | Some (path, stableToken) ->
                match resolveRefTarget path stableToken contextGraph nodes with
                | Error msg -> nodes, stack, idMap, claimed, Error msg
                | Ok (nodeId, nodes') ->
                    let edge = { ref = Ownership.Ref; id = nodeId }
                    let nodes'' = prependChild parentId edge nodes'
                    nodes'', stack, idMap, claimed, Ok ()

        elif content.StartsWith("^") then
            match splitStableIdPrefix (content.Substring(1)) with
            | Some (stableToken, rest) ->
                let name, bodyRest = parseOwnerRest rest
                let classes, nodeText = parseOutlineMeta bodyRest
                match resolveOwnerLine stableToken name classes nodeText nodes contextGraph with
                | Error msg -> nodes, stack, idMap, claimed, Error msg
                | Ok (nodeId, nodes') ->
                    let idMap' = idMap |> Map.add stableToken nodeId
                    let claimed' = Set.add nodeId claimed
                    let edge = { ref = Ownership.Owner; id = nodeId }
                    let nodes'' = prependChild parentId edge nodes'
                    nodes'', (depth, nodeId) :: stack, idMap', claimed', Ok ()
            | None ->
                // Body text may start with '^' without a stable id (legacy plain write).
                let classes, nodeText = parseOutlineMeta content
                let nodeId, nodes', claimed' =
                    resolvePlainLine parentId classes nodeText nodes contextGraph claimed
                let edge = { ref = Ownership.Owner; id = nodeId }
                let nodes'' = prependChild parentId edge nodes'
                nodes'', (depth, nodeId) :: stack, idMap, claimed', Ok ()

        else
            let classes, nodeText = parseOutlineMeta content
            let nodeId, nodes', claimed' =
                resolvePlainLine parentId classes nodeText nodes contextGraph claimed

            let edge = { ref = Ownership.Owner; id = nodeId }
            let nodes'' = prependChild parentId edge nodes'
            nodes'', (depth, nodeId) :: stack, idMap, claimed', Ok ()

    let private finalizeDocument (nodemap: Map<NodeId, Node>) : Map<NodeId, Node> =
        nodemap
        |> Map.map (fun _ node -> { node with children = List.rev node.children })

    /// Parse one document artifact. `contextGraph` resolves cross-file refs and seeds known nodes.
    let read
        (text: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        : Result<AmbDocumentReadResult, string> =
        match Map.tryFind documentRootId contextGraph.nodes with
        | None -> Error "document root not found in context graph"
        | Some rootNode ->
            let seedNodes =
                contextGraph.nodes
                |> Map.map (fun _ node -> { node with children = [] })

            let initial =
                ( seedNodes
                  , [ (-1, documentRootId) ]
                  , Map.empty<string, NodeId>
                  , Set.empty )

            let folder acc line =
                match acc with
                | Error msg -> Error msg
                | Ok (nodes, stack, idMap, claimed) ->
                    let nodes', stack', idMap', claimed', result =
                        foldOutlineLine documentRootId contextGraph (nodes, stack, idMap, claimed) line
                    match result with
                    | Ok () -> Ok (nodes', stack', idMap', claimed')
                    | Error msg -> Error msg

            match outlineSourceLines text |> Array.fold folder (Ok initial) with
            | Error msg -> Error msg
            | Ok (nodes, _, _, _) ->
                let finalized = finalizeDocument nodes
                Ok {
                    AmbDocumentReadResult.documentRootId = documentRootId
                    AmbDocumentReadResult.nodes = finalized
                }

    let normalizeForCompare (text: string) : string =
        text.Replace("\r\n", "\n").Replace("\r", "\n")
