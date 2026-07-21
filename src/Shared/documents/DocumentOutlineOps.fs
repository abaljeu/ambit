namespace Gambol.Shared

open System

/// Shared outline tree / raw-line helpers for Plain and Md documents.
[<RequireQualifiedAccess>]
module DocumentOutlineOps =

    type RawLine = {
        raw: string
        content: string
        ending: string
    }

    let splitRawLines (text: string) : RawLine list =
        if String.IsNullOrEmpty text then
            []
        else
            let rec findEnding (idx: int) =
                if idx >= text.Length then None
                elif idx + 1 < text.Length && text.[idx] = '\r' && text.[idx + 1] = '\n' then
                    Some("\r\n", idx + 2)
                elif text.[idx] = '\n' then Some("\n", idx + 1)
                elif text.[idx] = '\r' then Some("\r", idx + 1)
                else findEnding (idx + 1)

            let rec loop (idx: int) (acc: RawLine list) =
                if idx >= text.Length then
                    List.rev acc
                else
                    match findEnding idx with
                    | None ->
                        let content = text.Substring idx
                        List.rev ({ raw = content; content = content; ending = "" } :: acc)
                    | Some(ending, next) ->
                        let content = text.Substring(idx, next - idx - ending.Length)

                        loop next (
                            { raw = content + ending
                              content = content
                              ending = ending }
                            :: acc
                        )

            loop 0 []

    let leadingWhitespace (line: string) : string =
        line |> Seq.takeWhile (fun c -> c = ' ' || c = '\t') |> String.Concat

    let popStack depth stack =
        let rec loop acc = function
            | (d, _) :: tail when d >= depth -> loop acc tail
            | rest -> List.rev acc @ rest

        loop [] stack

    let prependChild (parentId: NodeId) (edge: ChildNode) (nodes: Map<NodeId, Node>) =
        let parent = nodes.[parentId]
        nodes |> Map.add parentId { parent with children = edge :: parent.children }

    let finalizeDocument (nodes: Map<NodeId, Node>) =
        nodes |> Map.map (fun _ node -> { node with children = List.rev node.children })

    let copyDocumentFromGraph (contextGraph: Graph) (documentRootId: NodeId) =
        let rec copySubtree nodeId acc =
            match Map.tryFind nodeId contextGraph.nodes with
            | None -> acc
            | Some node ->
                let acc' = Map.add nodeId { node with children = [] } acc

                node.children
                |> List.fold
                    (fun a child ->
                        let a' = copySubtree child.id a

                        match Map.tryFind child.id contextGraph.nodes with
                        | None -> a'
                        | Some _ ->
                            let parent = a'.[nodeId]

                            Map.add
                                nodeId
                                { parent with children = child :: parent.children }
                                a')
                    acc'

        copySubtree documentRootId Map.empty |> finalizeDocument

    /// Fold depth-ordered rows into an owner-child tree under documentRootId.
    let foldRowsIntoTree
        (documentRootId: NodeId)
        (contextGraph: Graph)
        (rows: 'Row list)
        (depthOf: 'Row -> int)
        (nodeIdFor: 'Row -> NodeId)
        (mergeNode:
            NodeId
                -> 'Row
                -> NodeId
                -> Map<NodeId, Node>
                -> Graph
                -> Map<NodeId, Node>)
        : Map<NodeId, Node> =
        let cleared =
            contextGraph.nodes |> Map.map (fun _ n -> { n with children = [] })

        let folder (nodes: Map<NodeId, Node>, stack: (int * NodeId) list) (row: 'Row) =
            let depth = depthOf row
            let stack' = popStack depth stack
            let parentId = snd stack'.Head
            let nodeId = nodeIdFor row
            let nodes' = mergeNode nodeId row parentId nodes contextGraph
            let edge = { ref = Ownership.Owner; id = nodeId }
            let nodes'' = prependChild parentId edge nodes'
            nodes'', (depth, nodeId) :: stack'

        let nodes, _ = List.fold folder (cleared, [ (-1, documentRootId) ]) rows
        finalizeDocument nodes
