namespace Gambol.Shared

open System

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
        elif nodeId = Graph.trashId then
            "TRASH"
        else
            nodeId.Value.ToString()

    let tryParseStableId (token: string) : NodeId option =
        match token with
        | "WORKSPACES" -> Some Graph.workspacesId
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

    /// Serialize one document subtree. The document root is implicit; its children are depth 0.
    let write (graph: Graph) (documentRootId: NodeId) : Result<string, string> =
        match Map.tryFind documentRootId graph.nodes with
        | None -> Error "document root not found"
        | Some rootNode ->
            let documentMembers = DocumentPartition.memberNodeIds graph documentRootId
            let refTargets = refTargetIds graph
            let occurrenceCount =
                graph.nodes
                |> Map.toSeq
                |> Seq.collect (fun (_, node) -> node.children |> Seq.map (fun child -> child.id))
                |> Seq.groupBy id
                |> Seq.map (fun (nodeId, xs) -> nodeId, Seq.length xs)
                |> Map.ofSeq

            let sb = Text.StringBuilder()

            let rec writeChild (depth: int) (emittedOwners: Set<NodeId>) (child: ChildNode) : Set<NodeId> =
                let indent = String.replicate depth "\t"
                let nodeId = child.id
                let isShared =
                    (occurrenceCount |> Map.tryFind nodeId |> Option.defaultValue 0) > 1

                match child.ref with
                | Ownership.Ref ->
                    sb.Append(indent).Append(refLineContent graph documentRootId documentMembers nodeId).Append(nl)
                    |> ignore
                    emittedOwners
                | Ownership.Owner ->
                    let node = graph.nodes.[nodeId]
                    if DocumentPartition.isNestedDocumentRootBoundary graph documentRootId nodeId then
                        sb.Append(indent).Append(refLineContent graph documentRootId documentMembers nodeId).Append(nl)
                        |> ignore
                        emittedOwners
                    else
                        let body = node.text
                        if isShared && Set.contains nodeId emittedOwners then
                            sb.Append(indent).Append(refLineContent graph documentRootId documentMembers nodeId).Append(nl)
                            |> ignore
                            emittedOwners
                        else
                            let line =
                                if isShared || Set.contains nodeId refTargets then
                                    ownerLineContent nodeId node body
                                else
                                    plainLineContent node body

                            sb.Append(indent).Append(line).Append(nl)
                            |> ignore
                            let emitted' = Set.add nodeId emittedOwners
                            node.children
                            |> List.fold (fun emitted c -> writeChild (depth + 1) emitted c) emitted'

            rootNode.children
            |> List.fold (fun emitted child -> writeChild 0 emitted child) Set.empty
            |> ignore

            Ok (sb.ToString())

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

    let private stubNode (nodeId: NodeId) (contextGraph: Graph) : Node =
        match Map.tryFind nodeId contextGraph.nodes with
        | Some node -> { node with children = [] }
        | None ->
            Node.Create(nodeId)

    let private ensureNode
        (nodeId: NodeId)
        (nodes: Map<NodeId, Node>)
        (contextGraph: Graph)
        : Map<NodeId, Node> =
        if Map.containsKey nodeId nodes then
            nodes
        else
            match Map.tryFind nodeId contextGraph.nodes with
            | Some node -> Map.add nodeId { node with children = [] } nodes
            | None -> Map.add nodeId (stubNode nodeId contextGraph) nodes

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
            | Some crossPath ->
                match Map.tryFind nodeId contextGraph.nodes with
                | None -> Error ("cross-file ref target not found: " + stableToken)
                | Some _ ->
                    match NodeDesktopPath.pathForNodeId contextGraph nodeId with
                    | None -> Ok (nodeId, localNodes)
                    | Some actualPath when
                        String.Equals(actualPath, crossPath, StringComparison.OrdinalIgnoreCase)
                        ->
                        Ok (nodeId, localNodes)
                    | Some _ -> Ok (nodeId, localNodes)

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
            | None -> nodes, stack, idMap, claimed, Error ("invalid owner line: " + content)
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
                Ok { documentRootId = documentRootId; nodes = finalized }

    let normalizeForCompare (text: string) : string =
        text.Replace("\r\n", "\n").Replace("\r", "\n")
