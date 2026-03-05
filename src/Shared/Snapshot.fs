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
            |> Seq.collect (fun (_, node) -> node.children)
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
                if isShared then
                    let sid = assignShortId nodeId
                    sb.Append(indent).Append("#").Append(sid).Append(" ").Append(node.text).Append(nl) |> ignore
                else
                    sb.Append(indent).Append(node.text).Append(nl) |> ignore
                for childId in node.children do
                    writeNode (depth + 1) childId

        let root = graph.nodes.[graph.root]

        for childId in root.children do
            writeNode 0 childId

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
              text = ""
              name = None
              children = [] }

        let rec popStack depth stack =
            match stack with
            | (d, _) :: tail when d >= depth -> popStack depth tail
            | _ -> stack

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
                let nodes = nodes |> Map.add parentId { parent with children = parent.children @ [ nodeId ] }
                // Do not push onto stack — no children are expected below a reference line.
                (nodes, stack, idMap)

            elif content.StartsWith("#") then
                // First occurrence of a shared node: "#sid text"
                let spaceIdx = content.IndexOf(' ')
                let sid, nodeText =
                    if spaceIdx < 0 then content.Substring(1), ""
                    else content.Substring(1, spaceIdx - 1), content.Substring(spaceIdx + 1)
                let nodeId = NodeId.New()
                let node: Node = { id = nodeId; text = nodeText; name = None; children = [] }
                let nodes = nodes |> Map.add nodeId node
                let nodes = nodes |> Map.add parentId { parent with children = parent.children @ [ nodeId ] }
                let idMap = idMap |> Map.add sid nodeId
                (nodes, (depth, nodeId) :: stack, idMap)

            else
                // Plain line — fresh ID (backward compatible).
                let nodeId = NodeId.New()
                let node: Node = { id = nodeId; text = content; name = None; children = [] }
                let nodes = nodes |> Map.add nodeId node
                let nodes = nodes |> Map.add parentId { parent with children = parent.children @ [ nodeId ] }
                (nodes, (depth, nodeId) :: stack, idMap)

        let nodes, _, _ =
            lines
            |> Array.fold processLine (Map.ofList [ rootId, rootNode ], [ (-1, rootId) ], Map.empty)

        { root = rootId; nodes = nodes }
