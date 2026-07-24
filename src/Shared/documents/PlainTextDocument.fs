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

/// RQA: keeps `nodes` off the unqualified field pool so it does not clash with `Graph.nodes` across assemblies.
[<RequireQualifiedAccess>]
type PlainTextReadResult = {
    documentRootId: NodeId
    nodes: Map<NodeId, Node>
    complement: PlainTextComplement
}

[<RequireQualifiedAccess>]
module PlainTextDocument =

    let private nl = Environment.NewLine

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

    let private gcd (a: int) (b: int) : int =
        let rec loop x y = if y = 0 then abs x else loop y (x % y)
        loop a b

    let private inferIndentStyle (lines: ParsedLine list) : PlainTextIndentStyle =
        let nonBlank = lines |> List.filter (fun line -> not line.isBlank)

        if
            nonBlank
            |> List.exists (fun line ->
                DocumentOutlineOps.leadingWhitespace line.body
                |> Seq.exists ((=) '\t'))
        then
            PlainTextIndentStyle.Tabs
        else
            let counts =
                nonBlank
                |> List.map (fun line ->
                    DocumentOutlineOps.leadingWhitespace line.body
                    |> Seq.filter ((=) ' ')
                    |> Seq.length)
                |> List.filter ((<) 0)

            match counts with
            | [] -> PlainTextIndentStyle.Spaces 1
            | xs -> PlainTextIndentStyle.Spaces(max 1 (List.reduce gcd xs))

    let private depthOf (line: string) (style: PlainTextIndentStyle) : int =
        let ws = DocumentOutlineOps.leadingWhitespace line

        match style with
        | PlainTextIndentStyle.Tabs -> ws |> Seq.filter ((=) '\t') |> Seq.length
        | PlainTextIndentStyle.Spaces n ->
            let spaces = ws |> Seq.filter ((=) ' ') |> Seq.length
            spaces / n

    let private strippedBody (line: string) : string =
        let wsLen = DocumentOutlineOps.leadingWhitespace line |> String.length
        line.Substring wsLen

    let private parseOutlineLines (text: string) : PlainTextIndentStyle * OutlineLine list =
        let rawLines = DocumentOutlineOps.splitRawLines text

        let parsed =
            rawLines
            |> List.map (fun line ->
                { depth = 0
                  body = line.content
                  isBlank = String.IsNullOrWhiteSpace line.content })

        let indentStyle = inferIndentStyle parsed

        // Whitespace-only lines inherit predecessor depth so they stay indented
        // with neighboring content lines.
        let outline, _ =
            (0, parsed)
            ||> List.mapFold (fun prevDepth line ->
                let depth =
                    if line.isBlank then
                        prevDepth
                    else
                        depthOf line.body indentStyle

                let outlineLine =
                    { depth = depth
                      text =
                        if line.isBlank then
                            ""
                        else
                            strippedBody line.body }

                outlineLine, depth)

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
                Node.Create(
                    nodeId,
                    text = text,
                    owner = parentId,
                    updateTime = NodeUpdateTime.now ())

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
                        { nodeId = Some child.id; depth = depth; content = content }
                        :: acc

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

            Ok(
                DocumentOutlineOps.foldRowsIntoTree
                    documentRootId
                    contextGraph
                    outline
                    (fun line -> line.depth)
                    (fun _ -> NodeId.New())
                    (fun nodeId line parentId nodes ctx ->
                        mergeOwnerNode nodeId line.text parentId nodes ctx),
                indentStyle)

    let private mapPreviousLines (previousText: string) (graph: Graph) (documentRootId: NodeId) =
        let serialized = serializeLines graph documentRootId |> List.toArray
        let _, prevOutline = parseOutlineLines previousText

        prevOutline
        |> List.mapi (fun i line ->
            let nodeId =
                match Array.tryItem i serialized with
                | Some s -> s.nodeId
                | None -> None

            { depth = line.depth; text = line.text; nodeId = nodeId })

    /// Flatten file text to outline lines (blanks → text "").
    let flattenText (text: string) : PlainTextIndentStyle * (int * string) list =
        let style, outline = parseOutlineLines text
        style, outline |> List.map (fun line -> line.depth, line.text)

    /// Previous file lines paired with node ids from the current graph projection.
    let mappedPrevious
        (previousText: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : (int * string * NodeId option) list =
        mapPreviousLines previousText graph documentRootId
        |> List.map (fun line -> line.depth, line.text, line.nodeId)

    let previousOutlineIds
        (previousText: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<NodeId option list, string> =
        mappedPrevious previousText graph documentRootId
        |> List.map (fun (_, _, id) -> id)
        |> Ok

    /// Rebuild document members from aligned (depth, text, nodeId) rows.
    let rebuildFromAligned
        (documentRootId: NodeId)
        (contextGraph: Graph)
        (aligned: (int * string * NodeId option) list)
        : Result<Map<NodeId, Node>, string> =
        match Map.tryFind documentRootId contextGraph.nodes with
        | None -> Error "document root not found in context graph"
        | Some _ ->
            Ok(
                DocumentOutlineOps.foldRowsIntoTree
                    documentRootId
                    contextGraph
                    aligned
                    (fun (depth, _, _) -> depth)
                    (fun (_, _, nodeIdOpt) ->
                        match nodeIdOpt with
                        | Some id -> id
                        | None -> NodeId.New())
                    (fun nodeId (_, text, _) parentId nodes ctx ->
                        mergeOwnerNode nodeId text parentId nodes ctx))

    let copyDocumentFromGraph =
        DocumentOutlineOps.copyDocumentFromGraph

    let finishRead
        (documentRootId: NodeId)
        (contextGraph: Graph)
        (nodes: Map<NodeId, Node>)
        (indentStyle: PlainTextIndentStyle)
        : PlainTextReadResult =
        let complement = buildComplement indentStyle contextGraph documentRootId

        {
            PlainTextReadResult.documentRootId = documentRootId
            PlainTextReadResult.nodes = applyCssClasses complement nodes contextGraph
            PlainTextReadResult.complement = complement
        }

    let read
        (text: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        : Result<PlainTextReadResult, string> =
        match parseCold text documentRootId contextGraph with
        | Error msg -> Error msg
        | Ok(nodes, indentStyle) ->
            Ok(finishRead documentRootId contextGraph nodes indentStyle)

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

    let private writeWarmImpl
        (diffTexts: OutlineDiffTexts)
        (graph: Graph)
        (documentRootId: NodeId)
        (complement: PlainTextComplement)
        (previousText: string)
        =
        let rawLines = DocumentOutlineOps.splitRawLines previousText
        let expected = serializeLines graph documentRootId

        let edited =
            expected
            |> List.map (fun line ->
                OutlineReconcile.writeLine line.depth line.content line.nodeId)

        let _, prevOutline = parseOutlineLines previousText

        let previous =
            prevOutline
            |> List.map (fun line ->
                OutlineReconcile.writeLine line.depth line.text None)
            |> OutlineReconcile.assignPrevHardKeys edited

        let formatEdit (edit: OutlineReconcile.OutlineLine) =
            indentForDepth edit.depth complement.indentStyle + edit.text

        Ok(
            OutlineDocumentWarm.writeByLcs
                diffTexts
                previous
                edited
                (fun pi edit ->
                    let formatted = formatEdit edit
                    let raw = rawLines.[pi]

                    if formatted = raw.content then raw.raw
                    else formatted + raw.ending)
                (fun edit -> formatEdit edit + nl)
        )

    /// When previousText is Some, LCS warm write requires writeWarm.
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
            | Some _ ->
                Error "warm artifact write requires PlainTextDocument.writeWarm"

    let writeWarm
        (diffTexts: OutlineDiffTexts)
        (graph: Graph)
        (documentRootId: NodeId)
        (complement: PlainTextComplement)
        (previousText: string)
        : Result<string, string> =
        match Map.tryFind documentRootId graph.nodes with
        | None -> Error "document root not found"
        | Some _ ->
            writeWarmImpl
                diffTexts
                graph
                documentRootId
                complement
                previousText

    let writeArtifact
        (graph: Graph)
        (documentRootId: NodeId)
        (previousText: string option)
        : Result<string, string> =
        write
            graph
            documentRootId
            (complementForWrite graph documentRootId previousText)
            previousText

    let writeArtifactWarm
        (diffTexts: OutlineDiffTexts)
        (graph: Graph)
        (documentRootId: NodeId)
        (previousText: string)
        : Result<string, string> =
        writeWarm
            diffTexts
            graph
            documentRootId
            (complementForWrite graph documentRootId (Some previousText))
            previousText
