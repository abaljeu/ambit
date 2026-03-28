namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module Snapshot =

    /// Serialize a graph to tab-indented text outline format.
    /// The root node is implicit; its children become top-level lines.
    /// Shared NodeIds (multiple child slots) use Ownership: the Owner edge emits
    ///   #n1 text
    /// and indented children; each Ref edge emits only
    ///   -> #n1
    /// Single-slot nodes are plain text (backward compatible).
    let write (graph: Graph) : string =
        let sb = Text.StringBuilder()
        let nl = Environment.NewLine

        // Count how many children-list slots each NodeId occupies.
        let occurrenceCount =
            graph.nodes
            |> Map.toSeq
            |> Seq.collect (fun (_, node) -> node.children |> Seq.map (fun child -> child.id))
            |> Seq.groupBy id
            |> Seq.map (fun (nodeId, xs) -> nodeId, Seq.length xs)
            |> Map.ofSeq

        let mutable shortIdCounter = 0
        let mutable shortIds = Map.empty<NodeId, string>

        let assignShortId (nodeId: NodeId) =
            shortIdCounter <- shortIdCounter + 1
            let sid = $"n{shortIdCounter}"
            shortIds <- Map.add nodeId sid shortIds
            sid

        let ensureShortId (nodeId: NodeId) =
            match shortIds |> Map.tryFind nodeId with
            | Some sid -> sid
            | None -> assignShortId nodeId

        let lineBodyFor (node: Node) =
            let needsMeta =
                not (CssClass.toList node.cssClasses).IsEmpty || node.text.StartsWith("{")
            if needsMeta then
                "{" + CssClass.toMetaString node.cssClasses + "}" + node.text
            else
                node.text

        let rec writeChild (depth: int) (child: ChildNode) =
            let indent = String.replicate depth "\t"
            let nodeId = child.id
            let isShared =
                (occurrenceCount |> Map.tryFind nodeId |> Option.defaultValue 0) > 1

            match child.ref with
            | Ownership.Ref ->
                let sid = ensureShortId nodeId
                sb.Append(indent).Append("-> #").Append(sid).Append(nl) |> ignore
            | Ownership.Owner ->
                let node = graph.nodes.[nodeId]
                let body = lineBodyFor node
                if isShared then
                    let sid = ensureShortId nodeId
                    sb.Append(indent).Append("#").Append(sid).Append(" ").Append(body).Append(nl)
                    |> ignore
                else
                    sb.Append(indent).Append(body).Append(nl) |> ignore
                for c in node.children do
                    writeChild (depth + 1) c

        let root = graph.nodes.[graph.root]

        for child in root.children do
            writeChild 0 child

        sb.ToString()

    /// Optional leading metadata "{...}rest" → (cssClasses, nodeText).
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

    let private outlineTextNode (id: NodeId) (nodeText: string) (classes: CssClasses) : Node =
        { id = id
          text = nodeText
          name = None
          children = []
          cssClasses = classes }

    let private outlineStubNode (id: NodeId) : Node =
        outlineTextNode id "" CssClass.empty

    /// Prepends edge to parent's children (fold uses reverse; see read).
    let private prependOutlineChild
        (parentId: NodeId)
        (edge: ChildNode)
        (nodes: Map<NodeId, Node>)
        =
        let p = nodes |> Map.find parentId
        nodes |> Map.add parentId { p with children = edge :: p.children }

    let private parseHashDefLine (content: string) : string * string =
        let spaceIdx = content.IndexOf(' ')
        if spaceIdx < 0 then content.Substring(1), ""
        else content.Substring(1, spaceIdx - 1), content.Substring(spaceIdx + 1)

    let private resolveRefSid
        (sid: string)
        (nodes: Map<NodeId, Node>)
        (idMap: Map<string, NodeId>)
        =
        match idMap |> Map.tryFind sid with
        | Some nid -> nid, nodes, idMap
        | None ->
            let nid = NodeId.New()
            nid, nodes |> Map.add nid (outlineStubNode nid), idMap |> Map.add sid nid

    let private resolveOwnerSid
        (sid: string)
        (classes: CssClasses)
        (nodeText: string)
        (nodes: Map<NodeId, Node>)
        (idMap: Map<string, NodeId>)
        =
        match idMap |> Map.tryFind sid with
        | Some nid ->
            let prior = nodes |> Map.find nid
            let n = { prior with text = nodeText; cssClasses = classes }
            nid, nodes |> Map.add nid n, idMap
        | None ->
            let nid = NodeId.New()
            let n = outlineTextNode nid nodeText classes
            nid, nodes |> Map.add nid n, idMap |> Map.add sid nid

    let rec private popOutlineStack depth stack =
        match stack with
        | (d, _) :: tail when d >= depth -> popOutlineStack depth tail
        | _ -> stack

    let private outlineSourceLines (text: string) =
        if String.IsNullOrEmpty(text) then
            Array.empty
        else
            text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n')

    let private newOutlineRoot () : NodeId * Node =
        let rootId = NodeId.New()
        let rootNode: Node =
            { id = rootId
              text = "ROOT"
              name = None
              children = []
              cssClasses = CssClass.empty }
        rootId, rootNode

    let private foldOutlineLine (nodes, stack, idMap: Map<string, NodeId>) (line: string) =
        let depth = line |> Seq.takeWhile ((=) '\t') |> Seq.length
        let content = line.Substring(depth)
        let stack = popOutlineStack depth stack
        let parentId = snd stack.Head

        if content.StartsWith("-> #") then
            let sid = content.Substring(4).Trim()
            let nid, nodes, idMap = resolveRefSid sid nodes idMap
            let edge = { ref = Ownership.Ref; id = nid }
            (prependOutlineChild parentId edge nodes, stack, idMap)

        elif content.StartsWith("#") then
            let sid, body = parseHashDefLine content
            let classes, nodeText = parseOutlineMeta body
            let nid, nodes, idMap = resolveOwnerSid sid classes nodeText nodes idMap
            let edge = { ref = Ownership.Owner; id = nid }
            (prependOutlineChild parentId edge nodes, (depth, nid) :: stack, idMap)

        else
            let classes, nodeText = parseOutlineMeta content
            let nid = NodeId.New()
            let nodes = nodes |> Map.add nid (outlineTextNode nid nodeText classes)
            let edge = { ref = Ownership.Owner; id = nid }
            (prependOutlineChild parentId edge nodes, (depth, nid) :: stack, idMap)

    let private finalizeOutlineGraph (rootId: NodeId) (nodemap: Map<NodeId, Node>) : Graph =
        let nodes =
            nodemap
            |> Map.map (fun _ (n: Node) -> { n with children = List.rev n.children })
        { root = rootId; nodes = nodes }

    /// Parse tab-indented text outline into a new Graph.
    /// Creates new NodeIds; original IDs are not preserved.
    /// Formats: #n1 text (Owner), -> #n1 (Ref), or plain line. Owner may update a ref stub.
    let read (text: string) : Graph =
        let rootId, rootNode = newOutlineRoot ()
        let initial = (Map.ofList [ rootId, rootNode ], [ (-1, rootId) ], Map.empty)
        let nodemap, _, _ = outlineSourceLines text |> Array.fold foldOutlineLine initial
        finalizeOutlineGraph rootId nodemap
