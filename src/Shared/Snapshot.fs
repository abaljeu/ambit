namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module Snapshot =

    /// Serialize a graph to tab-indented text outline format.
    /// The root node is implicit; its children become top-level lines.
    /// Nodes that appear under more than one parent are emitted once as
    ///   #n1 text
    /// and every subsequent appearance as
    ///   -> #n1
    /// Nodes that appear only once are emitted as plain text (backward compatible).
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

        let mutable visited = Set.empty<NodeId>

        let rec writeNode (depth: int) (nodeId: NodeId) =
            let indent = String.replicate depth "\t"
            let isShared = (occurrenceCount |> Map.tryFind nodeId |> Option.defaultValue 0) > 1
            if Set.contains nodeId visited then
                let sid = shortIds.[nodeId]
                sb.Append(indent).Append("-> #").Append(sid).Append(nl) |> ignore
            else
                visited <- Set.add nodeId visited
                let node = graph.nodes.[nodeId]
                // Build the encoded line body: metadata prefix + text.
                // A metadata block is emitted when there are classes, or when the text
                // itself starts with '{' (to disambiguate from the metadata sigil).
                let lineBody =
                    let needsMeta = not (CssClass.toList node.cssClasses).IsEmpty || node.text.StartsWith("{")
                    if needsMeta then
                        "{" + CssClass.toMetaString node.cssClasses + "}" + node.text
                    else
                        node.text
                if isShared then
                    let sid = assignShortId nodeId
                    sb.Append(indent).Append("#").Append(sid).Append(" ").Append(lineBody).Append(nl) |> ignore
                else
                    sb.Append(indent).Append(lineBody).Append(nl) |> ignore
                for child in node.children do
                    writeNode (depth + 1) child.id

        let root = graph.nodes.[graph.root]

        for child in root.children do
            writeNode 0 child.id

        sb.ToString()

    /// Parse tab-indented text outline into a new Graph.
    /// Creates new NodeIds; original IDs are not preserved.
    /// Handles three line formats:
    ///   #n1 text   — first occurrence of a shared node; registers "n1" in ID map
    ///   -> #n1     — subsequent occurrence; reuses the registered NodeId
    ///   text       — plain line; fresh NodeId (backward compatible with old snapshots)
    let read (text: string) : Graph =
        let lines =
            if String.IsNullOrEmpty(text) then
                Array.empty
            else
                text.Replace("\r\n", "\n").TrimEnd('\n').Split('\n')

        let rootId = NodeId.New()

        let rootNode: Node =
            { id = rootId
              text = "ROOT"
              name = None
              children = []
              cssClasses = CssClass.empty }

        let rec popStack depth stack =
            match stack with
            | (d, _) :: tail when d >= depth -> popStack depth tail
            | _ -> stack

        // Parse an optional leading metadata block: "{...}rest".
        // Returns (cssClasses, nodeText). If no '{' prefix, returns ([], raw).
        let parseMeta (raw: string) : CssClasses * string =
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

        // !! creates reversed children !!
        let processLine (nodes, stack, idMap: Map<string, NodeId>) (line: string) =
            let depth =
                line |> Seq.takeWhile ((=) '\t') |> Seq.length

            let content = line.Substring(depth)
            let stack = popStack depth stack
            let parentId = snd stack.Head
            let parent: Node = nodes |> Map.find parentId

            if content.StartsWith("-> #") then
                // Reference to an already-introduced shared node.
                let sid = content.Substring(4).Trim()
                let nodeId = idMap.[sid]
                let child = { ref = Ownership.Ref; id = nodeId }
                let nodes = nodes |> Map.add parentId { parent with children = child :: parent.children }
                // Do not push onto stack — no children are expected below a reference line.
                (nodes, stack, idMap)

            elif content.StartsWith("#") then
                // First occurrence of a shared node: "#sid lineBody"
                let spaceIdx = content.IndexOf(' ')
                let sid, lineBody =
                    if spaceIdx < 0 then content.Substring(1), ""
                    else content.Substring(1, spaceIdx - 1), content.Substring(spaceIdx + 1)
                let classes, nodeText = parseMeta lineBody
                let nodeId = NodeId.New()
                let node: Node = { id = nodeId; text = nodeText; name = None; children = []; cssClasses = classes }
                let nodes = nodes |> Map.add nodeId node
                let child = { ref = Ownership.Owner; id = nodeId }
                let nodes = nodes |> Map.add parentId { parent with children = child :: parent.children }
                let idMap = idMap |> Map.add sid nodeId
                (nodes, (depth, nodeId) :: stack, idMap)

            else
                // Plain or metadata line — fresh ID (backward compatible).
                let classes, nodeText = parseMeta content
                let nodeId = NodeId.New()
                let node: Node = { id = nodeId; text = nodeText; name = None; children = []; cssClasses = classes }
                let nodes = nodes |> Map.add nodeId node
                let child = { ref = Ownership.Owner; id = nodeId }
                let nodes = nodes |> Map.add parentId { parent with children = child :: parent.children }
                (nodes, (depth, nodeId) :: stack, idMap)

        let (nodemap, _, _) =
            lines
            |> Array.fold processLine (Map.ofList [ rootId, rootNode ], [ (-1, rootId) ], Map.empty)

        let nodes =
            nodemap
            |> Map.map (fun _ (node: Node) -> { node with children = List.rev node.children })

        { root = rootId; nodes = nodes }
