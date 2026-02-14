namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module Snapshot =

    /// Serialize a graph to tab-indented text outline format.
    /// The root node is implicit; its children become top-level lines.
    let write (graph: Graph) : string =
        let sb = Text.StringBuilder()

        let rec writeNode (depth: int) (nodeId: NodeId) =
            let node = graph.nodes.[nodeId]
            sb.Append(String.replicate depth "\t").Append(node.text).Append('\n')
            |> ignore

            for childId in node.children do
                writeNode (depth + 1) childId

        let root = graph.nodes.[graph.root]

        for childId in root.children do
            writeNode 0 childId

        sb.ToString()

    /// Parse tab-indented text outline into a new Graph.
    /// Creates new NodeIds; original IDs are not preserved.
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

        let processLine (nodes, stack) (line: string) =
            let depth =
                line |> Seq.takeWhile ((=) '\t') |> Seq.length

            let nodeText = line.Substring(depth)
            let nodeId = NodeId.New()

            let node: Node =
                { id = nodeId
                  text = nodeText
                  name = None
                  children = [] }

            let nodes = nodes |> Map.add nodeId node
            let stack = popStack depth stack
            let parentId = snd stack.Head
            let parent = nodes.[parentId]

            let nodes =
                nodes
                |> Map.add parentId { parent with children = parent.children @ [ nodeId ] }

            (nodes, (depth, nodeId) :: stack)

        let nodes, _ =
            lines
            |> Array.fold processLine (Map.ofList [ rootId, rootNode ], [ (-1, rootId) ])

        { root = rootId; nodes = nodes }
