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

    let prependChild
        (parentId: NodeId)
        (edge: ChildNode)
        (childMap: Map<NodeId, ChildNode list>)
        =
        let kids = Map.tryFind parentId childMap |> Option.defaultValue []
        Map.add parentId (edge :: kids) childMap

    let finalizeChildMap (childMap: Map<NodeId, ChildNode list>) =
        childMap |> Map.map (fun _ kids -> List.rev kids)

    let copyDocumentFromGraph (contextGraph: Graph) (documentRootId: NodeId) =
        let rec copySubtree nodeId (nodes, childMap) =
            match Map.tryFind nodeId contextGraph.nodes with
            | None -> nodes, childMap
            | Some node ->
                let nodes = Map.add nodeId node nodes
                let childMap = Map.add nodeId [] childMap
                GraphChildren.get contextGraph nodeId
                |> List.fold
                    (fun (nodes, childMap) child ->
                        let nodes, childMap = copySubtree child.id (nodes, childMap)
                        match Map.tryFind child.id contextGraph.nodes with
                        | None -> nodes, childMap
                        | Some _ ->
                            nodes, prependChild nodeId child childMap)
                    (nodes, childMap)

        let nodes, childMap =
            copySubtree documentRootId (Map.empty, Map.empty)
        nodes, finalizeChildMap childMap

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
        : Map<NodeId, Node> * Map<NodeId, ChildNode list> =
        let clearedNodes = contextGraph.nodes
        let clearedChildMap =
            contextGraph.nodes |> Map.map (fun _ _ -> [])

        let folder
            (nodes, childMap, stack: (int * NodeId) list)
            (row: 'Row)
            =
            let depth = depthOf row
            let stack' = popStack depth stack
            let parentId = snd stack'.Head
            let nodeId = nodeIdFor row
            let nodes' = mergeNode nodeId row parentId nodes contextGraph
            let edge = ChildNode.owner nodeId
            let childMap' = prependChild parentId edge childMap
            nodes', childMap', (depth, nodeId) :: stack'

        let nodes, childMap, _ =
            List.fold
                folder
                (clearedNodes, clearedChildMap, [ (-1, documentRootId) ])
                rows
        nodes, finalizeChildMap childMap
