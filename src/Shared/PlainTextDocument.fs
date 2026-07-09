namespace Gambol.Shared

open System
open System.Text

type PlainTextIndentStyle =
    | Tabs
    | Spaces of spacesPerLevel: int

type PlainTextComplement = {
    indentStyle: PlainTextIndentStyle
    cssClassesByNodeId: Map<NodeId, CssClasses>
}

type PlainTextReadResult = {
    documentRootId: NodeId
    nodes: Map<NodeId, Node>
    complement: PlainTextComplement
}

[<RequireQualifiedAccess>]
module PlainTextDocument =

    let private nl = Environment.NewLine

    type private RawLine = {
        raw: string
        content: string
        ending: string
    }

    type private ParsedLine = {
        depth: int
        body: string
        isBlank: bool
    }

    type private OutlineLine = {
        depth: int
        text: string
    }

    type private MappedLine = {
        depth: int
        text: string
        nodeId: NodeId option
    }

    let private hasCssClasses (classes: CssClasses) =
        not (List.isEmpty (CssClass.toList classes))

    let private splitRawLines (text: string) : RawLine list =
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

    let private gcd (a: int) (b: int) : int =
        let rec loop x y = if y = 0 then abs x else loop y (x % y)
        loop a b

    let private leadingWhitespace (line: string) : string =
        line |> Seq.takeWhile (fun c -> c = ' ' || c = '\t') |> String.Concat

    let private inferIndentStyle (lines: ParsedLine list) : PlainTextIndentStyle =
        let nonBlank = lines |> List.filter (fun line -> not line.isBlank)

        if
            nonBlank
            |> List.exists (fun line -> leadingWhitespace line.body |> Seq.exists ((=) '\t'))
        then
            PlainTextIndentStyle.Tabs
        else
            let counts =
                nonBlank
                |> List.map (fun line ->
                    leadingWhitespace line.body |> Seq.filter ((=) ' ') |> Seq.length)
                |> List.filter ((<) 0)

            match counts with
            | [] -> PlainTextIndentStyle.Spaces 1
            | xs -> PlainTextIndentStyle.Spaces(max 1 (List.reduce gcd xs))

    let private depthOf (line: string) (style: PlainTextIndentStyle) : int =
        let ws = leadingWhitespace line

        match style with
        | PlainTextIndentStyle.Tabs -> ws |> Seq.filter ((=) '\t') |> Seq.length
        | PlainTextIndentStyle.Spaces n ->
            let spaces = ws |> Seq.filter ((=) ' ') |> Seq.length
            spaces / n

    let private popStack depth stack =
        let rec loop acc = function
            | (d, _) :: tail when d >= depth -> loop acc tail
            | rest -> List.rev acc @ rest

        loop [] stack

    let private prependChild (parentId: NodeId) (edge: ChildNode) (nodes: Map<NodeId, Node>) =
        let parent = nodes.[parentId]
        nodes |> Map.add parentId { parent with children = edge :: parent.children }

    let private finalizeDocument (nodes: Map<NodeId, Node>) =
        nodes |> Map.map (fun _ node -> { node with children = List.rev node.children })

    let private strippedBody (line: string) : string =
        let wsLen = leadingWhitespace line |> String.length
        line.Substring wsLen

    let private parseOutlineLines (text: string) : PlainTextIndentStyle * OutlineLine list =
        let rawLines = splitRawLines text

        let parsed =
            rawLines
            |> List.map (fun line ->
                { depth = 0
                  body = line.content
                  isBlank = String.IsNullOrWhiteSpace line.content })

        let indentStyle = inferIndentStyle parsed

        let outline =
            parsed
            |> List.filter (fun line -> not line.isBlank)
            |> List.map (fun line ->
                { depth = depthOf line.body indentStyle
                  text = strippedBody line.body })

        indentStyle, outline

    let private buildComplement
        (indentStyle: PlainTextIndentStyle)
        (contextGraph: Graph)
        (documentRootId: NodeId)
        =
        let cssClassesByNodeId =
            DocumentPartition.memberNodeIds contextGraph documentRootId
            |> Set.toSeq
            |> Seq.choose (fun nodeId ->
                match Map.tryFind nodeId contextGraph.nodes with
                | Some node when hasCssClasses node.cssClasses -> Some(nodeId, node.cssClasses)
                | _ -> None)
            |> Map.ofSeq

        { indentStyle = indentStyle; cssClassesByNodeId = cssClassesByNodeId }

    let private applyCssClasses
        (complement: PlainTextComplement)
        (nodes: Map<NodeId, Node>)
        (contextGraph: Graph)
        =
        nodes
        |> Map.map (fun nodeId node ->
            match Map.tryFind nodeId complement.cssClassesByNodeId with
            | Some classes -> { node with cssClasses = classes }
            | None ->
                match Map.tryFind nodeId contextGraph.nodes with
                | Some contextNode when hasCssClasses contextNode.cssClasses ->
                    { node with cssClasses = contextNode.cssClasses }
                | _ -> node)

    let complementForWrite
        (graph: Graph)
        (documentRootId: NodeId)
        (previousText: string option)
        : PlainTextComplement =
        let indentStyle =
            match previousText with
            | None -> PlainTextIndentStyle.Tabs
            | Some text ->
                let style, _ = parseOutlineLines text
                style

        buildComplement indentStyle graph documentRootId

    let private mergeOwnerNode
        (nodeId: NodeId)
        (text: string)
        (parentId: NodeId)
        (nodes: Map<NodeId, Node>)
        (contextGraph: Graph)
        =
        let baseNode =
            match Map.tryFind nodeId nodes, Map.tryFind nodeId contextGraph.nodes with
            | Some node, _ -> node
            | None, Some node -> node
            | None, None ->
                { id = nodeId
                  text = text
                  name = Filename.Empty
                  children = []
                  cssClasses = CssClass.empty
                  owner = parentId
                  kind = Normal
                  fileState = FileState.defaultValue
                  updateTime = NodeUpdateTime.now () }

        let merged = NodeUpdateTime.touch { baseNode with text = text; owner = parentId }
        Map.add nodeId merged nodes

    type private SerializedLine = {
        nodeId: NodeId option
        depth: int
        content: string
    }

    let private lineContent (graph: Graph) (child: ChildNode) =
        match child.ref with
        | Ownership.Ref ->
            match Map.tryFind child.id graph.nodes with
            | None -> None
            | Some node -> Some node.text
        | Ownership.Owner ->
            match Map.tryFind child.id graph.nodes with
            | None -> None
            | Some node -> Some node.text

    let private serializeLines (graph: Graph) (documentRootId: NodeId) =
        match Map.tryFind documentRootId graph.nodes with
        | None -> []
        | Some root ->
            let rec loop depth (acc: SerializedLine list) (child: ChildNode) =
                match lineContent graph child with
                | None -> acc
                | Some content ->
                    let acc' =
                        { nodeId = Some child.id; depth = depth; content = content } :: acc

                    match child.ref, Map.tryFind child.id graph.nodes with
                    | Ownership.Owner, Some node ->
                        node.children |> List.fold (loop (depth + 1)) acc'
                    | _ -> acc'

            root.children |> List.fold (loop 0) [] |> List.rev

    let private parseCold
        (text: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        : Result<Map<NodeId, Node> * PlainTextIndentStyle, string> =
        match Map.tryFind documentRootId contextGraph.nodes with
        | None -> Error "document root not found in context graph"
        | Some _ ->
            let indentStyle, outline = parseOutlineLines text

            let folder (nodes: Map<NodeId, Node>, stack: (int * NodeId) list) (line: OutlineLine) =
                let stack' = popStack line.depth stack
                let parentId = snd stack'.Head
                let nodeId = NodeId.New()
                let nodes' = mergeOwnerNode nodeId line.text parentId nodes contextGraph
                let edge = { ref = Ownership.Owner; id = nodeId }
                let nodes'' = prependChild parentId edge nodes'
                nodes'', (line.depth, nodeId) :: stack'

            let nodes, _ =
                outline
                |> List.fold folder (contextGraph.nodes |> Map.map (fun _ n -> { n with children = [] }), [ (-1, documentRootId) ])

            Ok(finalizeDocument nodes, indentStyle)

    let private mapPreviousLines (previousText: string) (graph: Graph) (documentRootId: NodeId) =
        let serialized = serializeLines graph documentRootId
        let _, prevOutline = parseOutlineLines previousText

        prevOutline
        |> List.mapi (fun i line ->
            let nodeId =
                match List.tryItem i serialized with
                | Some s -> s.nodeId
                | None -> None

            { depth = line.depth; text = line.text; nodeId = nodeId })

    let private isRelocated (previous: MappedLine list) (index: int) (line: OutlineLine) =
        previous
        |> List.indexed
        |> List.exists (fun (j, prev) -> j <> index && prev.text = line.text)

    let private resolveNodeId
        (previous: MappedLine list)
        (index: int)
        (line: OutlineLine)
        =
        if isRelocated previous index line then
            None
        else
            match List.tryItem index previous with
            | Some prev when prev.nodeId.IsSome && prev.text = line.text -> prev.nodeId
            | Some prev when prev.nodeId.IsSome && prev.depth = line.depth -> prev.nodeId
            | _ -> None

    let private parseReconcile
        (editedText: string)
        (previousText: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        : Result<Map<NodeId, Node> * PlainTextIndentStyle, string> =
        match Map.tryFind documentRootId contextGraph.nodes with
        | None -> Error "document root not found in context graph"
        | Some _ ->
            let indentStyle, outline = parseOutlineLines editedText
            let previous = mapPreviousLines previousText contextGraph documentRootId

            let folder
                (nodes: Map<NodeId, Node>, stack: (int * NodeId) list, lineIndex: int)
                (line: OutlineLine)
                =
                let stack' = popStack line.depth stack
                let parentId = snd stack'.Head

                let nodeId =
                    match resolveNodeId previous lineIndex line with
                    | Some id -> id
                    | None -> NodeId.New()

                let nodes' = mergeOwnerNode nodeId line.text parentId nodes contextGraph
                let edge = { ref = Ownership.Owner; id = nodeId }
                let nodes'' = prependChild parentId edge nodes'
                nodes'', (line.depth, nodeId) :: stack', lineIndex + 1

            let nodes, _, _ =
                outline
                |> List.fold folder (contextGraph.nodes |> Map.map (fun _ n -> { n with children = [] }), [ (-1, documentRootId) ], 0)

            Ok(finalizeDocument nodes, indentStyle)

    let private documentFromGraph (contextGraph: Graph) (documentRootId: NodeId) =
        let rec copySubtree nodeId acc =
            match Map.tryFind nodeId contextGraph.nodes with
            | None -> acc
            | Some node ->
                let acc' = Map.add nodeId { node with children = [] } acc

                node.children
                |> List.fold (fun a child ->
                    let a' = copySubtree child.id a

                    match Map.tryFind child.id contextGraph.nodes with
                    | None -> a'
                    | Some _ ->
                        let parent = a'.[nodeId]
                        Map.add nodeId { parent with children = child :: parent.children } a')
                    acc'

        copySubtree documentRootId Map.empty |> finalizeDocument

    let read
        (text: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        : Result<PlainTextReadResult, string> =
        match parseCold text documentRootId contextGraph with
        | Error msg -> Error msg
        | Ok(nodes, indentStyle) ->
            let complement = buildComplement indentStyle contextGraph documentRootId

            Ok
                { documentRootId = documentRootId
                  nodes = applyCssClasses complement nodes contextGraph
                  complement = complement }

    let private indentForDepth (depth: int) (style: PlainTextIndentStyle) : string =
        match style with
        | PlainTextIndentStyle.Tabs -> String.replicate depth "\t"
        | PlainTextIndentStyle.Spaces n -> String(' ', depth * n)

    let private writeFresh (graph: Graph) (documentRootId: NodeId) (complement: PlainTextComplement) =
        let lines = serializeLines graph documentRootId
        let sb = StringBuilder()

        for line in lines do
            sb.Append(indentForDepth line.depth complement.indentStyle).Append(line.content).Append(nl)
            |> ignore

        Ok(sb.ToString())

    let private writeIncremental
        (graph: Graph)
        (documentRootId: NodeId)
        (complement: PlainTextComplement)
        (previousText: string)
        =
        let rawLines = splitRawLines previousText
        let expected = serializeLines graph documentRootId
        let expectedById =
            expected
            |> List.choose (fun line -> line.nodeId |> Option.map (fun id -> id, line))
            |> Map.ofList

        let previousMapped = mapPreviousLines previousText graph documentRootId

        let nodeIdAtLineIndex =
            previousMapped
            |> List.mapi (fun i mapped -> i, mapped.nodeId)
            |> List.choose (fun (i, nodeId) -> nodeId |> Option.map (fun id -> i, id))
            |> Map.ofList

        let nonBlankIndexByRawIndex =
            rawLines
            |> List.indexed
            |> List.fold
                (fun (nonBlankIdx, acc) (rawIdx, raw) ->
                    if String.IsNullOrWhiteSpace raw.content then
                        nonBlankIdx, acc
                    else
                        nonBlankIdx + 1, Map.add rawIdx nonBlankIdx acc)
                (0, Map.empty)
            |> snd

        let sb = StringBuilder()
        let mutable emitted = Set.empty<NodeId>

        for i in 0 .. rawLines.Length - 1 do
            let raw = rawLines.[i]

            if String.IsNullOrWhiteSpace raw.content then
                sb.Append(raw.raw) |> ignore
            else
                match Map.tryFind i nonBlankIndexByRawIndex with
                | None -> sb.Append(raw.raw) |> ignore
                | Some nonBlankIdx ->
                    match Map.tryFind nonBlankIdx nodeIdAtLineIndex with
                    | None -> sb.Append(raw.raw) |> ignore
                    | Some nodeId ->
                        match Map.tryFind nodeId expectedById with
                        | None -> ()
                        | Some expectedLine ->
                            let newContent =
                                indentForDepth expectedLine.depth complement.indentStyle
                                + expectedLine.content

                            if newContent = raw.content then
                                sb.Append(raw.raw) |> ignore
                            else
                                sb.Append(newContent).Append(raw.ending) |> ignore

                            emitted <- Set.add nodeId emitted

        for line in expected do
            match line.nodeId with
            | Some nodeId when not (Set.contains nodeId emitted) ->
                sb.Append(indentForDepth line.depth complement.indentStyle).Append(line.content).Append(nl)
                |> ignore
            | _ -> ()

        Ok(sb.ToString())

    /// When previousText is Some, untouched bytes from that artifact are preserved.
    let write
        (graph: Graph)
        (documentRootId: NodeId)
        (complement: PlainTextComplement)
        (previousText: string option)
        : Result<string, string> =
        match Map.tryFind documentRootId graph.nodes with
        | None -> Error "document root not found"
        | Some _ ->
            match previousText with
            | None -> writeFresh graph documentRootId complement
            | Some previous -> writeIncremental graph documentRootId complement previous

    let reconcile
        (previousText: string)
        (contextGraph: Graph)
        (documentRootId: NodeId)
        (editedText: string)
        : Result<PlainTextReadResult, string> =
        if editedText = previousText then
            let indentStyle, _ = parseOutlineLines previousText
            let complement = buildComplement indentStyle contextGraph documentRootId
            let nodes = documentFromGraph contextGraph documentRootId

            Ok
                { documentRootId = documentRootId
                  nodes = applyCssClasses complement nodes contextGraph
                  complement = complement }
        else
            match parseReconcile editedText previousText documentRootId contextGraph with
            | Error msg -> Error msg
            | Ok(nodes, indentStyle) ->
                let complement = buildComplement indentStyle contextGraph documentRootId

                Ok
                    { documentRootId = documentRootId
                      nodes = applyCssClasses complement nodes contextGraph
                      complement = complement }
