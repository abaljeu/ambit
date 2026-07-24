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

    let private listIndentSteps (line: string) : int =
        let ws = DocumentOutlineOps.leadingWhitespace line
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
        let wsLen = DocumentOutlineOps.leadingWhitespace content |> String.length
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
        DocumentOutlineOps.splitRawLines text
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
        let serialized = serializeLines graph documentRootId |> List.toArray

        parseOutlineLines previousText
        |> List.mapi (fun i line ->
            let nodeId =
                match Array.tryItem i serialized with
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
                        (fun nodeId (_, text, kind, _) parentId nodes ctx ->
                            mergeOwnerNode nodeId text kind parentId nodes ctx))

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

            Ok(
                DocumentOutlineOps.foldRowsIntoTree
                    documentRootId
                    contextGraph
                    outline
                    (fun line -> line.depth)
                    (fun _ -> NodeId.New())
                    (fun nodeId line parentId nodes ctx ->
                        mergeOwnerNode nodeId line.text line.kind parentId nodes ctx))

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

    let private needsPreBlank (prevKind: LineKind option) (kind: LineKind) =
        match kind with
        | Head -> prevKind.IsSome
        | ListItem ->
            match prevKind with
            | Some ListItem -> false
            | Some _ -> true
            | None -> false
        | Plain | Blank -> false

    type private PrevEntity = {
        line: OutlineReconcile.OutlineLine
        substantive: DocumentOutlineOps.RawLine
        trailing: DocumentOutlineOps.RawLine list
        kind: LineKind
    }

    let private buildPrevEntities (previousText: string) : PrevEntity list =
        let rawLines = DocumentOutlineOps.splitRawLines previousText
        let outline = parseOutlineLines previousText

        let substantiveRaw =
            rawLines
            |> List.mapi (fun i raw -> i, raw)
            |> List.choose (fun (i, raw) ->
                if String.IsNullOrWhiteSpace raw.content then None
                else Some i)

        substantiveRaw
        |> List.mapi (fun flatIdx rawIdx ->
            let line = outline.[flatIdx]
            let trailing =
                let nextSub =
                    substantiveRaw
                    |> List.tryItem (flatIdx + 1)
                    |> Option.defaultValue rawLines.Length

                [ rawIdx + 1 .. nextSub - 1 ]
                |> List.map (fun i -> rawLines.[i])

            {
                line = OutlineReconcile.writeLine line.depth line.text None
                substantive = rawLines.[rawIdx]
                trailing = trailing
                kind = line.kind
            })

    let private writeFresh (graph: Graph) (documentRootId: NodeId) =
        let allLines = serializeLines graph documentRootId
        let lines = allLines |> List.filter isSubstantive
        let sb = StringBuilder()

        lines
        |> List.fold
            (fun prevKind line ->
                if needsPreBlank prevKind line.kind then
                    sb.Append(nl) |> ignore

                let idx =
                    List.findIndex (fun l -> l.nodeId = line.nodeId) allLines

                sb.Append(formatLine line allLines idx).Append(nl) |> ignore
                Some line.kind)
            None
        |> ignore

        Ok(sb.ToString())

    let private writeWarmImpl
        (diffTexts: OutlineDiffTexts)
        (graph: Graph)
        (documentRootId: NodeId)
        (previousText: string)
        =
        let allLines = serializeLines graph documentRootId
        let expected = allLines |> List.filter isSubstantive

        let edited =
            expected
            |> List.map (fun line ->
                OutlineReconcile.writeLine line.depth line.content line.nodeId)

        let prevEntities =
            buildPrevEntities previousText
            |> fun entities ->
                let prevLines = entities |> List.map (fun e -> e.line)

                let keyed =
                    OutlineReconcile.assignPrevHardKeys edited prevLines

                List.zip entities keyed
                |> List.map (fun (e, line) -> { e with line = line })

        let previous = prevEntities |> List.map (fun e -> e.line)

        let serializedById =
            expected
            |> List.choose (fun line ->
                line.nodeId
                |> Option.map (fun id ->
                    let idx =
                        List.findIndex
                            (fun l -> l.nodeId = line.nodeId)
                            allLines

                    id, (line, idx)))
            |> Map.ofList

        let formatEdit (edit: OutlineReconcile.OutlineLine) =
            match edit.nodeId with
            | Some id ->
                match Map.tryFind id serializedById with
                | Some(line, idx) -> formatLine line allLines idx
                | None -> edit.text
            | None ->
                match
                    expected
                    |> List.tryFind (fun l ->
                        l.content = edit.text && l.depth = edit.depth)
                with
                | Some line ->
                    let idx =
                        List.findIndex
                            (fun l -> l.nodeId = line.nodeId)
                            allLines

                    formatLine line allLines idx
                | None -> edit.text

        let kindOfEdit (edit: OutlineReconcile.OutlineLine) =
            match edit.nodeId with
            | Some id ->
                match Map.tryFind id serializedById with
                | Some(line, _) -> line.kind
                | None -> Plain
            | None ->
                expected
                |> List.tryFind (fun l ->
                    l.content = edit.text && l.depth = edit.depth)
                |> Option.map (fun l -> l.kind)
                |> Option.defaultValue Plain

        let plan = OutlineDocumentWarm.writePlan diffTexts previous edited

        let emitStep (prevKind: LineKind option) step =
            match step with
            | OutlineDocumentWarm.EmitKeep(pi, edit) ->
                let ent = prevEntities.[pi]
                let formatted = formatEdit edit

                let chunk =
                    if formatted = ent.substantive.content then
                        ent.substantive.raw
                    else
                        formatted + ent.substantive.ending

                let chunk' =
                    chunk
                    + (ent.trailing |> List.map (fun b -> b.raw) |> String.concat "")

                Some(kindOfEdit edit), chunk'
            | OutlineDocumentWarm.EmitInsert edit ->
                let kind = kindOfEdit edit
                let prefix = if needsPreBlank prevKind kind then nl else ""
                Some kind, prefix + formatEdit edit + nl

        Ok(OutlineDocumentWarm.executeWritePlan plan emitStep None)

    /// When previousText is Some, LCS warm write requires writeWarm.
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
            | Some _ ->
                Error "warm artifact write requires MdDocument.writeWarm"

    let writeWarm
        (diffTexts: OutlineDiffTexts)
        (graph: Graph)
        (documentRootId: NodeId)
        (_complement: MdComplement)
        (previousText: string)
        : Result<string, string> =
        match Map.tryFind documentRootId graph.nodes with
        | None -> Error "document root not found"
        | Some _ -> writeWarmImpl diffTexts graph documentRootId previousText

    let writeArtifact
        (graph: Graph)
        (documentRootId: NodeId)
        (previousText: string option)
        : Result<string, string> =
        write
            graph
            documentRootId
            (complementForWrite graph documentRootId)
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
            (complementForWrite graph documentRootId)
            previousText
