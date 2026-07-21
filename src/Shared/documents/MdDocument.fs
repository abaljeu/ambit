namespace Gambol.Shared

open System
open System.Text

type MdComplement = {
    cssClassesByNodeId: Map<NodeId, CssClasses>
}

/// RQA: keeps `nodes` off the unqualified field pool so it does not clash with `Graph.nodes`.
[<RequireQualifiedAccess>]
type MdReadResult = {
    documentRootId: NodeId
    nodes: Map<NodeId, Node>
    complement: MdComplement
}

[<RequireQualifiedAccess>]
module MdDocument =

    let private nl = Environment.NewLine
    let private structuralNames = set [ "md-head"; "md-list" ]

    type LineKind =
        | Blank
        | Head
        | ListItem
        | Plain

    type private RawLine = {
        raw: string
        content: string
        ending: string
    }

    type private OutlineLine = {
        depth: int
        text: string
        kind: LineKind
    }

    type private SerializedLine = {
        nodeId: NodeId option
        depth: int
        kind: LineKind
        content: string
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

    let private leadingWhitespace (line: string) : string =
        line |> Seq.takeWhile (fun c -> c = ' ' || c = '\t') |> String.Concat

    let private listIndentSteps (line: string) : int =
        let ws = leadingWhitespace line
        let tabs = ws |> Seq.filter ((=) '\t') |> Seq.length
        let spaces = ws |> Seq.filter ((=) ' ') |> Seq.length
        tabs + spaces / 2

    let private parseAtxHeading (content: string) : (int * string) option =
        let rec countHashes (i: int) (acc: int) =
            if i >= content.Length || acc >= 6 then acc
            elif content.[i] = '#' then countHashes (i + 1) (acc + 1)
            else acc

        let hashCount = countHashes 0 0

        if hashCount = 0 then
            None
        elif hashCount = 1 && (content.Length = 1 || content.[1] <> ' ') then
            None
        else
            let bodyStart =
                if content.Length > hashCount && content.[hashCount] = ' ' then
                    hashCount + 1
                else
                    hashCount

            Some(hashCount, content.Substring bodyStart)

    let private parseListItem (content: string) : (int * string) option =
        let wsLen = leadingWhitespace content |> String.length
        let rest = content.Substring wsLen

        if rest.StartsWith("- ") then
            Some(listIndentSteps content, rest.Substring 2)
        else
            None

    let private normalizeHeadingDepth (activeHeading: int) (depth: int) =
        if depth > activeHeading + 1 then activeHeading + 1 else depth

    let private classifyContent (activeHeading: int) (content: string) =
        if String.IsNullOrWhiteSpace content then
            activeHeading, {
                depth = 0
                text = ""
                kind = Blank
            }
        else
            match parseAtxHeading content with
            | Some(hashDepth, body) ->
                let depth = normalizeHeadingDepth activeHeading hashDepth

                depth,
                {
                    depth = depth
                    text = body
                    kind = Head
                }
            | None ->
                match parseListItem content with
                | Some(indentSteps, body) ->
                    activeHeading,
                    {
                        depth = activeHeading + 1 + indentSteps
                        text = body
                        kind = ListItem
                    }
                | None ->
                    activeHeading,
                    {
                        depth = activeHeading + 1
                        text = content
                        kind = Plain
                    }

    let private parseOutlineLines (text: string) : OutlineLine list =
        splitRawLines text
        |> List.map (fun line -> line.content)
        |> List.fold
            (fun (active, acc) content ->
                let active', line = classifyContent active content

                if line.kind = Blank then
                    active', acc
                else
                    active', line :: acc)
            (0, [])
        |> snd
        |> List.rev

    let private cssForKind kind =
        match kind with
        | Head -> CssClass.ofList [ "md-head" ]
        | ListItem -> CssClass.ofList [ "md-list" ]
        | Plain | Blank -> CssClass.empty

    let private withStructural (kind: LineKind) (existing: CssClasses) =
        let user =
            CssClass.toList existing
            |> List.filter (fun c -> not (Set.contains c structuralNames))

        let structural = CssClass.toList (cssForKind kind)
        CssClass.ofList (user @ structural)

    let private buildComplement (contextGraph: Graph) (documentRootId: NodeId) =
        let cssClassesByNodeId =
            DocumentPartition.memberNodeIds contextGraph documentRootId
            |> Set.toSeq
            |> Seq.choose (fun nodeId ->
                match Map.tryFind nodeId contextGraph.nodes with
                | Some node when hasCssClasses node.cssClasses -> Some(nodeId, node.cssClasses)
                | _ -> None)
            |> Map.ofSeq

        { cssClassesByNodeId = cssClassesByNodeId }

    let private applyCssClasses
        (complement: MdComplement)
        (nodes: Map<NodeId, Node>)
        (contextGraph: Graph)
        =
        nodes
        |> Map.map (fun nodeId node ->
            let structural =
                CssClass.toList node.cssClasses
                |> List.filter (fun c -> Set.contains c structuralNames)

            let userFrom =
                match Map.tryFind nodeId complement.cssClassesByNodeId with
                | Some classes -> classes
                | None ->
                    match Map.tryFind nodeId contextGraph.nodes with
                    | Some contextNode -> contextNode.cssClasses
                    | None -> CssClass.empty

            let user =
                CssClass.toList userFrom
                |> List.filter (fun c -> not (Set.contains c structuralNames))

            { node with cssClasses = CssClass.ofList (user @ structural) })

    let complementForWrite (graph: Graph) (documentRootId: NodeId) : MdComplement =
        buildComplement graph documentRootId

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

    let private mergeOwnerNode
        (nodeId: NodeId)
        (text: string)
        (kind: LineKind)
        (parentId: NodeId)
        (nodes: Map<NodeId, Node>)
        (contextGraph: Graph)
        =
        let baseNode =
            match Map.tryFind nodeId nodes, Map.tryFind nodeId contextGraph.nodes with
            | Some node, _ -> node
            | None, Some node -> node
            | None, None ->
                Node.Create(
                    nodeId,
                    text = text,
                    owner = parentId,
                    updateTime = NodeUpdateTime.now ())

        let merged =
            NodeUpdateTime.touch {
                baseNode with
                    text = text
                    owner = parentId
                    cssClasses = withStructural kind baseNode.cssClasses
            }

        Map.add nodeId merged nodes

    let private lineContent (graph: Graph) (child: ChildNode) =
        match Map.tryFind child.id graph.nodes with
        | None -> None
        | Some node -> Some node.text

    let private kindOfNode (graph: Graph) (nodeId: NodeId) : LineKind =
        match Map.tryFind nodeId graph.nodes with
        | None -> Plain
        | Some node ->
            let classes = CssClass.toList node.cssClasses

            if List.contains "md-head" classes then Head
            elif List.contains "md-list" classes then ListItem
            else Plain

    let private serializeLines (graph: Graph) (documentRootId: NodeId) =
        match Map.tryFind documentRootId graph.nodes with
        | None -> []
        | Some root ->
            let rec loop
                (activeHeading: int)
                (listDepth: int)
                (acc: SerializedLine list)
                (child: ChildNode)
                =
                match lineContent graph child with
                | None -> acc
                | Some content ->
                    let kind = kindOfNode graph child.id

                    let depth, activeHeading', listDepth' =
                        match kind with
                        | Head ->
                            let d = activeHeading + 1
                            d, d, 0
                        | ListItem ->
                            activeHeading + 1 + listDepth, activeHeading, listDepth
                        | Plain | Blank -> activeHeading + 1, activeHeading, 0

                    let acc' =
                        {
                            nodeId = Some child.id
                            depth = depth
                            kind = kind
                            content = content
                        }
                        :: acc

                    match child.ref, Map.tryFind child.id graph.nodes with
                    | Ownership.Owner, Some node ->
                        let nextListDepth =
                            match kind with
                            | ListItem -> listDepth' + 1
                            | _ -> 0

                        node.children
                        |> List.fold (loop activeHeading' nextListDepth) acc'
                    | _ -> acc'

            root.children |> List.fold (loop 0 0) [] |> List.rev

    let private mapPreviousLines (previousText: string) (graph: Graph) (documentRootId: NodeId) =
        let serialized = serializeLines graph documentRootId

        parseOutlineLines previousText
        |> List.mapi (fun i line ->
            let nodeId =
                match List.tryItem i serialized with
                | Some s -> s.nodeId
                | None -> None

            line, nodeId)

    /// Flatten file text to outline lines (blanks omitted).
    let flattenText (text: string) : (int * string * LineKind) list =
        parseOutlineLines text
        |> List.map (fun line -> line.depth, line.text, line.kind)

    let previousOutlineIds
        (previousText: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<NodeId option list, string> =
        mapPreviousLines previousText graph documentRootId
        |> List.map snd
        |> Ok

    /// Rebuild from aligned rows; kinds come from re-parsing editedText.
    let rebuildFromAligned
        (editedText: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        (aligned: (int * string * NodeId option) list)
        : Result<Map<NodeId, Node>, string> =
        match Map.tryFind documentRootId contextGraph.nodes with
        | None -> Error "document root not found in context graph"
        | Some _ ->
            let outline = parseOutlineLines editedText

            if List.length outline <> List.length aligned then
                Error "aligned row count does not match edited outline"
            else
                let rows =
                    List.map2
                        (fun (line: OutlineLine) (_, _, nodeIdOpt) ->
                            line.depth, line.text, line.kind, nodeIdOpt)
                        outline
                        aligned

                let folder
                    (nodes: Map<NodeId, Node>, stack: (int * NodeId) list)
                    (depth, text, kind, nodeIdOpt)
                    =
                    let stack' = popStack depth stack
                    let parentId = snd stack'.Head

                    let nodeId =
                        match nodeIdOpt with
                        | Some id -> id
                        | None -> NodeId.New()

                    let nodes' =
                        mergeOwnerNode nodeId text kind parentId nodes contextGraph

                    let edge = { ref = Ownership.Owner; id = nodeId }
                    let nodes'' = prependChild parentId edge nodes'
                    nodes'', (depth, nodeId) :: stack'

                let nodes, _ =
                    rows
                    |> List.fold
                        folder
                        (contextGraph.nodes |> Map.map (fun _ n -> { n with children = [] }),
                         [ (-1, documentRootId) ])

                Ok(finalizeDocument nodes)

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

    let finishRead
        (documentRootId: NodeId)
        (contextGraph: Graph)
        (nodes: Map<NodeId, Node>)
        : MdReadResult =
        let complement = buildComplement contextGraph documentRootId

        {
            MdReadResult.documentRootId = documentRootId
            MdReadResult.nodes = applyCssClasses complement nodes contextGraph
            MdReadResult.complement = complement
        }

    let private parseCold
        (text: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        : Result<Map<NodeId, Node>, string> =
        match Map.tryFind documentRootId contextGraph.nodes with
        | None -> Error "document root not found in context graph"
        | Some _ ->
            let outline = parseOutlineLines text

            let folder (nodes: Map<NodeId, Node>, stack: (int * NodeId) list) (line: OutlineLine) =
                let stack' = popStack line.depth stack
                let parentId = snd stack'.Head
                let nodeId = NodeId.New()

                let nodes' =
                    mergeOwnerNode nodeId line.text line.kind parentId nodes contextGraph

                let edge = { ref = Ownership.Owner; id = nodeId }
                let nodes'' = prependChild parentId edge nodes'
                nodes'', (line.depth, nodeId) :: stack'

            let nodes, _ =
                outline
                |> List.fold
                    folder
                    (contextGraph.nodes |> Map.map (fun _ n -> { n with children = [] }),
                     [ (-1, documentRootId) ])

            Ok(finalizeDocument nodes)

    let read
        (text: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        : Result<MdReadResult, string> =
        match parseCold text documentRootId contextGraph with
        | Error msg -> Error msg
        | Ok nodes -> Ok(finishRead documentRootId contextGraph nodes)

    let private activeHeadingBefore (lines: SerializedLine list) (index: int) =
        lines
        |> List.take index
        |> List.tryFindBack (fun line -> line.kind = Head)
        |> Option.map (fun line -> line.depth)
        |> Option.defaultValue 0

    let private formatLine (line: SerializedLine) (lines: SerializedLine list) (index: int) =
        match line.kind with
        | Head -> String.replicate line.depth "#" + " " + line.content
        | ListItem ->
            let active = activeHeadingBefore lines index
            let indentSteps = max 0 (line.depth - active - 1)
            String.replicate (indentSteps * 2) " " + "- " + line.content
        | Plain | Blank -> line.content

    /// Md blanks are not nodes; empty graph rows must not project as blank lines.
    let private isSubstantive (line: SerializedLine) =
        not (String.IsNullOrWhiteSpace line.content)

    /// One blank between block lines; keep heading→list and list→list tight.
    let private needsBlankSeparator (prev: SerializedLine) (curr: SerializedLine) =
        match prev.kind, curr.kind with
        | ListItem, ListItem -> false
        | Head, ListItem -> false
        | _ -> true

    let private writeFresh (graph: Graph) (documentRootId: NodeId) =
        let allLines = serializeLines graph documentRootId
        let lines = allLines |> List.filter isSubstantive
        let sb = StringBuilder()

        lines
        |> List.iteri (fun i line ->
            if i > 0 && needsBlankSeparator lines.[i - 1] line then
                sb.Append(nl) |> ignore

            let idx = List.findIndex (fun l -> l.nodeId = line.nodeId) allLines
            sb.Append(formatLine line allLines idx).Append(nl) |> ignore)

        Ok(sb.ToString())

    let private writeIncremental
        (graph: Graph)
        (documentRootId: NodeId)
        (previousText: string)
        =
        let rawLines = splitRawLines previousText
        let allExpected = serializeLines graph documentRootId
        let expected = allExpected |> List.filter isSubstantive

        let expectedById =
            expected
            |> List.choose (fun line -> line.nodeId |> Option.map (fun id -> id, line))
            |> Map.ofList

        let previousMapped = mapPreviousLines previousText graph documentRootId

        let substantiveRawIndices =
            rawLines
            |> List.mapi (fun i raw -> i, raw)
            |> List.choose (fun (i, raw) ->
                if String.IsNullOrWhiteSpace raw.content then None else Some i)

        let nodeIdAtRawIndex =
            List.zip substantiveRawIndices previousMapped
            |> List.choose (fun (rawIdx, (_, nodeId)) ->
                nodeId |> Option.map (fun id -> rawIdx, id))
            |> Map.ofList

        let expectedIndexById =
            allExpected
            |> List.mapi (fun i line -> line.nodeId |> Option.map (fun id -> id, i))
            |> List.choose id
            |> Map.ofList

        let folder (sb: StringBuilder, emitted: Set<NodeId>) (i: int) =
            let raw = rawLines.[i]

            match Map.tryFind i nodeIdAtRawIndex with
            | None -> sb.Append(raw.raw) |> ignore; sb, emitted
            | Some nodeId ->
                match Map.tryFind nodeId expectedById with
                | None -> sb, emitted
                | Some expectedLine ->
                    let idx = Map.find nodeId expectedIndexById
                    let newContent = formatLine expectedLine allExpected idx

                    if newContent = raw.content then
                        sb.Append(raw.raw) |> ignore
                    else
                        sb.Append(newContent).Append(raw.ending) |> ignore

                    sb, Set.add nodeId emitted

        let sb, emitted =
            [ 0 .. rawLines.Length - 1 ]
            |> List.fold folder (StringBuilder(), Set.empty)

        for line in expected do
            match line.nodeId with
            | Some nodeId when not (Set.contains nodeId emitted) ->
                let idx = Map.find nodeId expectedIndexById
                sb.Append(formatLine line allExpected idx).Append(nl) |> ignore
            | _ -> ()

        Ok(sb.ToString())

    /// When previousText is Some, untouched bytes from that artifact are preserved.
    let write
        (graph: Graph)
        (documentRootId: NodeId)
        (_complement: MdComplement)
        (previousText: string option)
        : Result<string, string> =
        match Map.tryFind documentRootId graph.nodes with
        | None -> Error "document root not found"
        | Some _ ->
            match previousText with
            | None -> writeFresh graph documentRootId
            | Some previous -> writeIncremental graph documentRootId previous

    let writeArtifact
        (graph: Graph)
        (documentRootId: NodeId)
        (previousText: string option)
        : Result<string, string> =
        write graph documentRootId (complementForWrite graph documentRootId) previousText
