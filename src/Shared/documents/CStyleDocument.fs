namespace Gambol.Shared

open System
open System.Text

type CStyleComplement = {
    indentStyle: PlainTextIndentStyle
    cssClassesByNodeId: Map<NodeId, CssClasses>
}

[<RequireQualifiedAccess>]
type CStyleReadResult = {
    documentRootId: NodeId
    nodes: Map<NodeId, Node>
    complement: CStyleComplement
}

[<RequireQualifiedAccess>]
module CStyleDocument =

    let private nl = Environment.NewLine
    let private structuralName = "code-brace"
    let private structuralNames = set [ structuralName ]

    type private SerializedLine = {
        nodeId: NodeId option
        depth: int
        content: string
        braced: bool
    }

    let private hasCssClasses (classes: CssClasses) =
        not (List.isEmpty (CssClass.toList classes))

    let private withStructural braced (existing: CssClasses) =
        let user =
            CssClass.toList existing
            |> List.filter (fun c -> not (Set.contains c structuralNames))

        let structural =
            if braced then [ structuralName ] else []

        CssClass.ofList (user @ structural)

    let private indentForDepth (depth: int) (style: PlainTextIndentStyle) =
        match style with
        | PlainTextIndentStyle.Tabs -> String.replicate depth "\t"
        | PlainTextIndentStyle.Spaces n -> String(' ', depth * n)

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
                | Some node when hasCssClasses node.cssClasses ->
                    Some(nodeId, node.cssClasses)
                | _ -> None)
            |> Map.ofSeq

        { indentStyle = indentStyle; cssClassesByNodeId = cssClassesByNodeId }

    let private applyCssClasses
        (complement: CStyleComplement)
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

    let complementForWrite
        (graph: Graph)
        (documentRootId: NodeId)
        (previousText: string option)
        : CStyleComplement =
        let indentStyle =
            match previousText with
            | None -> PlainTextIndentStyle.Tabs
            | Some text -> fst (CStyleBrace.toOutlineRows text)

        buildComplement indentStyle graph documentRootId

    let private mergeOwnerNode
        (nodeId: NodeId)
        (text: string)
        (braced: bool)
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
                    cssClasses = withStructural braced baseNode.cssClasses
            }

        Map.add nodeId merged nodes

    let private isBracedNode (graph: Graph) (nodeId: NodeId) =
        match Map.tryFind nodeId graph.nodes with
        | Some node -> CssClass.contains structuralName node.cssClasses
        | None -> false

    let private serializeLines (graph: Graph) (documentRootId: NodeId) =
        match Map.tryFind documentRootId graph.nodes with
        | None -> []
        | Some root ->
            let rec loop depth acc (child: ChildNode) =
                match Map.tryFind child.id graph.nodes with
                | None -> acc
                | Some node ->
                    let acc' =
                        {
                            nodeId = Some child.id
                            depth = depth
                            content = node.text
                            braced = isBracedNode graph child.id
                        }
                        :: acc

                    match child.ref with
                    | Ownership.Owner ->
                        node.children |> List.fold (loop (depth + 1)) acc'
                    | Ownership.Ref -> acc'

            root.children |> List.fold (loop 0) [] |> List.rev

    let flattenText (text: string) : PlainTextIndentStyle * (int * string * bool) list =
        let style, rows = CStyleBrace.toOutlineRows text
        style, rows |> List.map (fun r -> r.depth, r.text, r.braced)

    let previousOutlineIds
        (previousText: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<NodeId option list, string> =
        let serialized = serializeLines graph documentRootId
        let _, rows = CStyleBrace.toOutlineRows previousText

        rows
        |> List.mapi (fun i _ ->
            match List.tryItem i serialized with
            | Some s -> s.nodeId
            | None -> None)
        |> Ok

    let rebuildFromAligned
        (editedText: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        (aligned: (int * string * NodeId option) list)
        : Result<Map<NodeId, Node>, string> =
        match Map.tryFind documentRootId contextGraph.nodes with
        | None -> Error "document root not found in context graph"
        | Some _ ->
            let _, outline = CStyleBrace.toOutlineRows editedText

            if List.length outline <> List.length aligned then
                Error "aligned row count does not match edited outline"
            else
                let rows =
                    List.map2
                        (fun (row: CStyleBrace.OutlineRow) (_, _, idOpt) ->
                            row.depth, row.text, row.braced, idOpt)
                        outline
                        aligned

                Ok(
                    DocumentOutlineOps.foldRowsIntoTree
                        documentRootId
                        contextGraph
                        rows
                        (fun (depth, _, _, _) -> depth)
                        (fun (_, _, _, nodeIdOpt) ->
                            match nodeIdOpt with
                            | Some id -> id
                            | None -> NodeId.New())
                        (fun nodeId (_, text, braced, _) parentId nodes ctx ->
                            mergeOwnerNode
                                nodeId text braced parentId nodes ctx))

    let copyDocumentFromGraph = DocumentOutlineOps.copyDocumentFromGraph

    let finishRead
        (documentRootId: NodeId)
        (contextGraph: Graph)
        (nodes: Map<NodeId, Node>)
        (indentStyle: PlainTextIndentStyle)
        : CStyleReadResult =
        let complement = buildComplement indentStyle contextGraph documentRootId

        {
            CStyleReadResult.documentRootId = documentRootId
            CStyleReadResult.nodes = applyCssClasses complement nodes contextGraph
            CStyleReadResult.complement = complement
        }

    let private parseCold
        (text: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        : Result<Map<NodeId, Node> * PlainTextIndentStyle, string> =
        match Map.tryFind documentRootId contextGraph.nodes with
        | None -> Error "document root not found in context graph"
        | Some _ ->
            let indentStyle, outline = CStyleBrace.toOutlineRows text

            Ok(
                DocumentOutlineOps.foldRowsIntoTree
                    documentRootId
                    contextGraph
                    outline
                    (fun (row: CStyleBrace.OutlineRow) -> row.depth)
                    (fun _ -> NodeId.New())
                    (fun nodeId row parentId nodes ctx ->
                        mergeOwnerNode
                            nodeId
                            row.text
                            row.braced
                            parentId
                            nodes
                            ctx),
                indentStyle)

    let read
        (text: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        : Result<CStyleReadResult, string> =
        match parseCold text documentRootId contextGraph with
        | Error msg -> Error msg
        | Ok(nodes, indentStyle) ->
            Ok(finishRead documentRootId contextGraph nodes indentStyle)

    let private writeFresh
        (graph: Graph)
        (documentRootId: NodeId)
        (complement: CStyleComplement)
        =
        let lines = serializeLines graph documentRootId
        let sb = StringBuilder()

        let emit (stack: (int * bool) list) (line: SerializedLine) =
            let rec close acc =
                function
                | [] -> acc
                | (d, braced) :: rest when d >= line.depth ->
                    if braced then
                        sb.Append(indentForDepth d complement.indentStyle)
                            .Append("}")
                            .Append(nl)
                        |> ignore

                    close acc rest
                | rest -> rest

            let stack' = close stack stack

            sb.Append(indentForDepth line.depth complement.indentStyle)
                .Append(line.content)
                .Append(nl)
            |> ignore

            if line.braced then
                sb.Append(indentForDepth line.depth complement.indentStyle)
                    .Append("{")
                    .Append(nl)
                |> ignore

            (line.depth, line.braced) :: stack'

        let finalStack = lines |> List.fold emit []

        finalStack
        |> List.iter (fun (d, braced) ->
            if braced then
                sb.Append(indentForDepth d complement.indentStyle)
                    .Append("}")
                    .Append(nl)
                |> ignore)

        Ok(sb.ToString())

    let private formatFresh
        (complement: CStyleComplement)
        (edit: OutlineReconcile.OutlineLine)
        (braced: bool)
        =
        let openPart =
            indentForDepth edit.depth complement.indentStyle
            + edit.text
            + nl
            + if braced then
                  indentForDepth edit.depth complement.indentStyle + "{" + nl
              else
                  ""

        let closePart =
            if braced then
                indentForDepth edit.depth complement.indentStyle + "}" + nl
            else
                ""

        openPart, closePart

    let private writeWarmImpl
        (diffTexts: OutlineDiffTexts)
        (graph: Graph)
        (documentRootId: NodeId)
        (complement: CStyleComplement)
        (previousText: string)
        =
        let expected = serializeLines graph documentRootId

        let edited =
            expected
            |> List.map (fun line ->
                OutlineReconcile.writeLine line.depth line.content line.nodeId)

        let warmUnits = CStyleBrace.toWarmUnits previousText

        let prevEntities =
            let keyed =
                OutlineReconcile.assignPrevHardKeys
                    edited
                    (warmUnits
                     |> List.map (fun u ->
                         OutlineReconcile.writeLine u.depth u.text None))

            List.map2
                (fun (u: CStyleBrace.WarmUnit) line -> u, line)
                warmUnits
                keyed

        let previous = prevEntities |> List.map snd
        let plan = OutlineDocumentWarm.writePlan diffTexts previous edited

        let bracedOf (edit: OutlineReconcile.OutlineLine) =
            match edit.nodeId with
            | Some id -> isBracedNode graph id
            | None ->
                expected
                |> List.tryFind (fun l ->
                    l.content = edit.text && l.depth = edit.depth)
                |> Option.map (fun l -> l.braced)
                |> Option.defaultValue false

        let emitStep (stack: (int * string) list) step =
            let flushTo depth accStack =
                let rec loop acc =
                    function
                    | [] -> acc, []
                    | (d, closeRaw) :: rest when d >= depth ->
                        loop (acc + closeRaw) rest
                    | rest -> acc, rest

                loop "" accStack

            match step with
            | OutlineDocumentWarm.EmitKeep(pi, edit) ->
                let u, prevLine = prevEntities.[pi]
                let flushed, stack' = flushTo edit.depth stack

                let openChunk, closeChunk =
                    if edit.text = prevLine.text then
                        u.openRaw, u.closeRaw
                    else
                        formatFresh complement edit (bracedOf edit)

                (edit.depth, closeChunk) :: stack', flushed + openChunk
            | OutlineDocumentWarm.EmitInsert edit ->
                let braced = bracedOf edit
                let flushed, stack' = flushTo edit.depth stack
                let openChunk, closeChunk = formatFresh complement edit braced
                (edit.depth, closeChunk) :: stack', flushed + openChunk

        let body, finalStack =
            plan
            |> List.fold
                (fun (acc, stack) step ->
                    let stack', chunk = emitStep stack step
                    acc + chunk, stack')
                ("", [])

        Ok(body + (finalStack |> List.map snd |> String.concat ""))

    let write
        (graph: Graph)
        (documentRootId: NodeId)
        (complement: CStyleComplement)
        (previousText: string option)
        : Result<string, string> =
        match Map.tryFind documentRootId graph.nodes with
        | None -> Error "document root not found"
        | Some _ ->
            match previousText with
            | None -> writeFresh graph documentRootId complement
            | Some _ ->
                Error "warm artifact write requires CStyleDocument.writeWarm"

    let writeWarm
        (diffTexts: OutlineDiffTexts)
        (graph: Graph)
        (documentRootId: NodeId)
        (complement: CStyleComplement)
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
