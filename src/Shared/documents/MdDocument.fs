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
    childMap: Map<NodeId, ChildNode list>
    complement: MdComplement
}

[<RequireQualifiedAccess>]
module MdDocument =

    let private nl = Environment.NewLine

    let private structuralNames =
        set [
            "md-head"
            "md-list"
            "md-list-star"
            "md-number"
            "md-table"
        ]

    type LineKind =
        | Blank
        | Head
        | ListItem
        | StarItem
        | NumberItem
        | Table
        | Plain

    type private OutlineLine = {
        depth: int
        text: string
        kind: LineKind
        tableHeader: bool
    }

    type private SerializedLine = {
        nodeId: NodeId option
        depth: int
        kind: LineKind
        content: string
        parentKind: LineKind
        numberIndex: int
    }

    let private hasCssClasses (classes: CssClasses) =
        not (List.isEmpty (CssClass.toList classes))

    let private listIndentSteps (line: string) : int =
        let ws = DocumentOutlineOps.leadingWhitespace line
        let tabs = ws |> Seq.filter ((=) '\t') |> Seq.length
        let spaces = ws |> Seq.filter ((=) ' ') |> Seq.length
        tabs + spaces / 2

    let parseAtxHeading (content: string) : (int * string) option =
        let rec countHashes (i: int) (acc: int) =
            if i >= content.Length then acc
            elif content.[i] = '#' then countHashes (i + 1) (acc + 1)
            else acc

        let hashCount = countHashes 0 0

        if hashCount = 0 || hashCount > 6 then
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

    let private numberedBody (rest: string) : string option =
        let rec digits i =
            if i < rest.Length && System.Char.IsDigit rest.[i] then
                digits (i + 1)
            else
                i

        let n = digits 0

        if
            n > 0
            && n + 1 < rest.Length
            && rest.[n] = '.'
            && rest.[n + 1] = ' '
        then
            Some(rest.Substring(n + 2))
        else
            None

    let parseMarker (content: string) : (LineKind * int * string) option =
        let wsLen = DocumentOutlineOps.leadingWhitespace content |> String.length
        let rest = content.Substring wsLen
        let steps = listIndentSteps content

        if rest.StartsWith "- " then
            Some(ListItem, steps, rest.Substring 2)
        elif rest.StartsWith "* " then
            Some(StarItem, steps, rest.Substring 2)
        else
            match numberedBody rest with
            | Some body -> Some(NumberItem, steps, body)
            | None -> None

    let private normalizeHeadingDepth (activeHeading: int) (depth: int) =
        if depth > activeHeading + 1 then activeHeading + 1 else depth

    let private isSentenceStop (text: string) (i: int) =
        let ch = text.[i]

        (ch = '.' || ch = '?' || ch = '!')
        && i + 1 < text.Length
        && text.[i + 1] = ' '
        && i + 2 < text.Length

    let private nextSentenceCut (text: string) (start: int) =
        let rec find i =
            if i >= text.Length then None
            elif isSentenceStop text i then Some(i + 2)
            else find (i + 1)

        find start

    /// First piece stays on the file-line node. Later pieces are sentence tails.
    let splitLineSentences (kind: LineKind) (text: string) : (string * int) list =
        let splits =
            match kind with
            | Head | ListItem | StarItem | NumberItem | Plain -> true
            | Table | Blank -> false

        if not splits || text = "" then
            [ text, 0 ]
        else
            let rec cuts start acc =
                match nextSentenceCut text start with
                | None -> List.rev acc
                | Some next -> cuts next (next :: acc)

            let rec parts prev (cs: int list) acc =
                match cs with
                | [] -> List.rev ((text.Substring prev, prev) :: acc)
                | c :: rest ->
                    let sentence = text.Substring(prev, c - 1 - prev)
                    parts c rest ((sentence, prev) :: acc)

            parts 0 (cuts 0 []) []

    let private outlineOf
        (depth: int)
        (text: string)
        (kind: LineKind)
        (tableHeader: bool)
        =
        {
            depth = depth
            text = text
            kind = kind
            tableHeader = tableHeader
        }

    let private classifyContent
        (active: int)
        (afterBlank: bool)
        (openTable: int option)
        (content: string)
        =
        match parseAtxHeading content with
        | Some(hashDepth, body) ->
            let depth = normalizeHeadingDepth active hashDepth
            outlineOf depth body Head false, depth, None
        | None ->
            match parseMarker content with
            | Some(kind, steps, body) ->
                outlineOf (active + 1 + steps) body kind false, active, None
            | None ->
                let trimmed = content.TrimEnd()

                if trimmed.StartsWith "|" then
                    match openTable with
                    | Some headerDepth when not afterBlank ->
                        outlineOf (headerDepth + 1) trimmed Table false,
                        active,
                        openTable
                    | _ ->
                        let depth = active + 2
                        outlineOf depth trimmed Table true, active, Some depth
                else
                    outlineOf (active + 1) content Plain false, active, None

    let private parseOutlineLines (text: string) : OutlineLine list =
        let contents =
            DocumentOutlineOps.splitRawLines text
            |> List.map (fun line -> line.content)

        let rec loop active afterBlank openTable acc lines =
            match lines with
            | [] -> List.rev acc
            | content :: rest when String.IsNullOrWhiteSpace content ->
                loop active true openTable acc rest
            | content :: rest ->
                let line, active', open' =
                    classifyContent active afterBlank openTable content

                loop active' false open' (line :: acc) rest

        loop 0 false None [] contents

    let private cssForKind kind =
        match kind with
        | Head -> CssClass.ofList [ "md-head" ]
        | ListItem -> CssClass.ofList [ "md-list" ]
        | StarItem -> CssClass.ofList [ "md-list-star" ]
        | NumberItem -> CssClass.ofList [ "md-number" ]
        | Table -> CssClass.ofList [ "md-table" ]
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
            elif List.contains "md-list-star" classes then StarItem
            elif List.contains "md-list" classes then ListItem
            elif List.contains "md-number" classes then NumberItem
            elif List.contains "md-table" classes then Table
            else Plain

    let private isListRun kind =
        match kind with
        | ListItem | StarItem | NumberItem -> true
        | _ -> false

    let private endsSentence (text: string) =
        if text.Length = 0 then
            false
        else
            let ch = text.[text.Length - 1]
            ch = '.' || ch = '?' || ch = '!'

    /// Childless plain children join only while the line already ends a sentence.
    /// A following paragraph under a heading stays its own file line.
    let private plainTails
        (graph: Graph)
        (parentText: string)
        (children: ChildNode list)
        =
        let rec take (acc: ChildNode list) (joined: string) (rest: ChildNode list) =
            match rest with
            | child :: more when endsSentence joined ->
                let kind = kindOfNode graph child.id
                let kids = GraphChildren.get graph child.id

                match lineContent graph child with
                | Some text when kind = Plain && kids = [] ->
                    take (child :: acc) (joined + " " + text) more
                | _ -> List.rev acc, rest
            | _ -> List.rev acc, rest

        take [] parentText children

    let private joinTailText (graph: Graph) (baseText: string) (tails: ChildNode list) =
        let extra =
            tails
            |> List.choose (fun child -> lineContent graph child)
            |> String.concat " "

        if List.isEmpty tails || extra = "" then baseText
        elif baseText = "" then extra
        else baseText + " " + extra

    let private numberAmong (graph: Graph) (parentId: NodeId) (nodeId: NodeId) =
        GraphChildren.get graph parentId
        |> List.filter (fun child -> kindOfNode graph child.id = NumberItem)
        |> List.tryFindIndex (fun child -> child.id = nodeId)
        |> Option.map (fun index -> index + 1)
        |> Option.defaultValue 1

    let private lineDepth (kind: LineKind) (active: int) (listDepth: int) =
        if kind = Head then
            let depth = active + 1
            depth, depth, 0
        elif isListRun kind then
            active + 1 + listDepth, active, listDepth
        else
            active + 1, active, 0

    let private serializeLines (graph: Graph) (documentRootId: NodeId) =
        let rec visit parentId active listDepth acc child =
            match lineContent graph child with
            | None -> acc
            | Some text ->
                let kind = kindOfNode graph child.id
                let tails, rest =
                    plainTails graph text (GraphChildren.get graph child.id)
                let depth, active', listDepth' = lineDepth kind active listDepth

                let line = {
                    nodeId = Some child.id
                    depth = depth
                    kind = kind
                    content = joinTailText graph text tails
                    parentKind = kindOfNode graph parentId
                    numberIndex =
                        if kind = NumberItem then
                            numberAmong graph parentId child.id
                        else
                            0
                }

                let nextList = if isListRun kind then listDepth' + 1 else 0

                rest
                |> List.fold (visit child.id active' nextList) (line :: acc)

        match Map.tryFind documentRootId graph.nodes with
        | None -> []
        | Some _ ->
            GraphChildren.get graph documentRootId
            |> List.fold (visit documentRootId 0 0) []
            |> List.rev

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

    let private lacksStructural (node: Node) =
        CssClass.toList node.cssClasses
        |> List.forall (fun name -> not (Set.contains name structuralNames))

    let private tryReusePlain
        (contextGraph: Graph)
        (used: Set<NodeId>)
        (parentId: NodeId)
        (text: string)
        =
        GraphChildren.get contextGraph parentId
        |> List.tryFind (fun child ->
            not (Set.contains child.id used)
            && match Map.tryFind child.id contextGraph.nodes with
               | Some node -> node.text = text && lacksStructural node
               | None -> false)
        |> Option.map (fun child -> child.id)

    type private FoldState = {
        nodes: Map<NodeId, Node>
        childMap: Map<NodeId, ChildNode list>
        stack: (int * NodeId) list
        used: Set<NodeId>
    }

    let private pushNode
        (contextGraph: Graph)
        (state: FoldState)
        (depth: int)
        (kind: LineKind)
        (text: string)
        (nodeId: NodeId)
        =
        let stack' = DocumentOutlineOps.popStack depth state.stack
        let parentId = snd stack'.Head

        let nodes =
            mergeOwnerNode nodeId text kind parentId state.nodes contextGraph

        let childMap =
            DocumentOutlineOps.prependChild
                parentId
                (ChildNode.owner nodeId)
                state.childMap

        {
            nodes = nodes
            childMap = childMap
            stack = (depth, nodeId) :: stack'
            used = Set.add nodeId state.used
        }

    let private addSentenceTails
        (contextGraph: Graph)
        (state: FoldState)
        (depth: int)
        (tails: string list)
        =
        let parentId = snd state.stack.Head

        let rec loop state tails =
            match tails with
            | [] -> state
            | text :: rest ->
                let id =
                    match tryReusePlain contextGraph state.used parentId text with
                    | Some id -> id
                    | None -> NodeId.New()

                let state' =
                    pushNode contextGraph state (depth + 1) Plain text id

                loop state' rest

        loop state tails

    let private placeFileLine
        (contextGraph: Graph)
        (state: FoldState)
        (line: OutlineLine)
        (nodeId: NodeId)
        =
        let parts = splitLineSentences line.kind line.text |> List.map fst

        let first, tails =
            match parts with
            | head :: rest -> head, rest
            | [] -> line.text, []

        let placed =
            pushNode contextGraph state line.depth line.kind first nodeId

        addSentenceTails contextGraph placed line.depth tails

    let private placeTableHeader
        (contextGraph: Graph)
        (state: FoldState)
        (line: OutlineLine)
        (nodeId: NodeId)
        =
        let carrierDepth = line.depth - 1
        let opened = DocumentOutlineOps.popStack (carrierDepth + 1) state.stack

        match opened with
        | (depth, _) :: _ when depth = carrierDepth ->
            placeFileLine contextGraph { state with stack = opened } line nodeId
        | _ ->
            let parentId = snd opened.Head

            let carrierId =
                match tryReusePlain contextGraph state.used parentId "" with
                | Some id -> id
                | None -> NodeId.New()

            let withCarrier =
                pushNode
                    contextGraph
                    { state with stack = opened }
                    carrierDepth
                    Plain
                    ""
                    carrierId

            placeFileLine contextGraph withCarrier line nodeId

    let private foldOutline
        (contextGraph: Graph)
        (documentRootId: NodeId)
        (rows: (OutlineLine * NodeId) list)
        =
        let state0 = {
            nodes = contextGraph.nodes
            childMap = contextGraph.nodes |> Map.map (fun _ _ -> [])
            stack = [ (-1, documentRootId) ]
            used = Set.empty
        }

        let rec loop state rows =
            match rows with
            | [] -> state
            | (line: OutlineLine, nodeId) :: rest ->
                let state' =
                    if line.kind = Table && line.tableHeader then
                        placeTableHeader contextGraph state line nodeId
                    else
                        placeFileLine contextGraph state line nodeId

                loop state' rest

        let state = loop state0 rows
        state.nodes, DocumentOutlineOps.finalizeChildMap state.childMap

    /// Rebuild from aligned rows; kinds come from re-parsing editedText.
    let rebuildFromAligned
        (editedText: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        (aligned: (int * string * NodeId option) list)
        : Result<Map<NodeId, Node> * Map<NodeId, ChildNode list>, string> =
        match Map.tryFind documentRootId contextGraph.nodes with
        | None -> Error "document root not found in context graph"
        | Some _ ->
            let outline = parseOutlineLines editedText

            if List.length outline <> List.length aligned then
                Error "aligned row count does not match edited outline"
            else
                let rows =
                    List.map2
                        (fun line (_, _, nodeIdOpt) ->
                            let nodeId =
                                match nodeIdOpt with
                                | Some id -> id
                                | None -> NodeId.New()

                            line, nodeId)
                        outline
                        aligned

                Ok(foldOutline contextGraph documentRootId rows)

    let finishRead
        (documentRootId: NodeId)
        (contextGraph: Graph)
        (nodes: Map<NodeId, Node>)
        (childMap: Map<NodeId, ChildNode list>)
        : MdReadResult =
        let complement = buildComplement contextGraph documentRootId

        {
            MdReadResult.documentRootId = documentRootId
            MdReadResult.nodes = applyCssClasses complement nodes contextGraph
            MdReadResult.childMap = childMap
            MdReadResult.complement = complement
        }

    let private parseCold
        (text: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        : Result<Map<NodeId, Node> * Map<NodeId, ChildNode list>, string> =
        match Map.tryFind documentRootId contextGraph.nodes with
        | None -> Error "document root not found in context graph"
        | Some _ ->
            let rows =
                parseOutlineLines text
                |> List.map (fun line -> line, NodeId.New())

            Ok(foldOutline contextGraph documentRootId rows)

    let read
        (text: string)
        (documentRootId: NodeId)
        (contextGraph: Graph)
        : Result<MdReadResult, string> =
        match parseCold text documentRootId contextGraph with
        | Error msg -> Error msg
        | Ok (nodes, childMap) ->
            Ok(finishRead documentRootId contextGraph nodes childMap)

    let private activeHeadingBefore (lines: SerializedLine list) (index: int) =
        lines
        |> List.take index
        |> List.tryFindBack (fun line -> line.kind = Head)
        |> Option.map (fun line -> line.depth)
        |> Option.defaultValue 0

    let private indentPrefix (line: SerializedLine) (lines: SerializedLine list) (index: int) =
        let active = activeHeadingBefore lines index
        let steps = max 0 (line.depth - active - 1)
        String.replicate (steps * 2) " "

    let private formatLine (line: SerializedLine) (lines: SerializedLine list) (index: int) =
        match line.kind with
        | Head -> String.replicate line.depth "#" + " " + line.content
        | ListItem -> indentPrefix line lines index + "- " + line.content
        | StarItem -> indentPrefix line lines index + "* " + line.content
        | NumberItem ->
            indentPrefix line lines index
            + string line.numberIndex
            + ". "
            + line.content
        | Table -> line.content
        | Plain | Blank -> line.content

    /// Md blanks are not nodes; empty graph rows must not project as blank lines.
    let private isSubstantive (line: SerializedLine) =
        not (String.IsNullOrWhiteSpace line.content)

    let private needsPreBlank
        (prevKind: LineKind option)
        (kind: LineKind)
        (parentKind: LineKind)
        =
        match kind with
        | Head -> prevKind.IsSome
        | kind when isListRun kind ->
            match prevKind with
            | Some prev when isListRun prev -> false
            | Some _ -> true
            | None -> false
        | Table ->
            match prevKind with
            | Some Table when parentKind <> Table -> true
            | _ -> false
        | Plain | Blank | ListItem | StarItem | NumberItem -> false

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
                if needsPreBlank prevKind line.kind line.parentKind then
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
                | Some(line, _) -> line.kind, line.parentKind
                | None -> Plain, Plain
            | None ->
                expected
                |> List.tryFind (fun l ->
                    l.content = edit.text && l.depth = edit.depth)
                |> Option.map (fun l -> l.kind, l.parentKind)
                |> Option.defaultValue (Plain, Plain)

        let plan = OutlineDocumentWarm.writePlan diffTexts previous edited

        let emitStep (prevKind: LineKind option) step =
            match step with
            | OutlineDocumentWarm.EmitKeep(pi, edit) ->
                let ent = prevEntities.[pi]
                let formatted = formatEdit edit
                let kind, _ = kindOfEdit edit

                let chunk =
                    if formatted = ent.substantive.content then
                        ent.substantive.raw
                    else
                        formatted + ent.substantive.ending

                let chunk' =
                    chunk
                    + (ent.trailing |> List.map (fun b -> b.raw) |> String.concat "")

                Some kind, chunk'
            | OutlineDocumentWarm.EmitInsert edit ->
                let kind, parentKind = kindOfEdit edit
                let prefix =
                    if needsPreBlank prevKind kind parentKind then nl else ""

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
