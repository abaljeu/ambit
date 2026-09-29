namespace Gambol.Shared

open System

/// Md warm import via nested SpanNode + shared outline LCS.
/// Blank lines absorb into neighboring bound node spans (no blank nodes).
[<RequireQualifiedAccess>]
module MdReconcile =

    let private toNodesRead (r: MdReadResult) =
        OutlineDocument.nodesRead r.documentRootId r.nodes r.childMap

    let private lineEndInArtifact
        (spanned: (TextSpan * string) array)
        (normalized: string)
        (lineIndex: int)
        =
        if lineIndex + 1 < spanned.Length then
            (fst spanned.[lineIndex + 1]).start
        else
            normalized.Length

    let private absorbedSpan
        (spanned: (TextSpan * string) array)
        (normalized: string)
        (substantiveFileIndices: int array)
        (flatIndex: int)
        : TextSpan =
        let fileIndex = substantiveFileIndices.[flatIndex]
        let isBlank i = String.IsNullOrWhiteSpace(snd spanned.[i])

        let start =
            if flatIndex = 0 then
                (fst spanned.[0]).start
            else
                (fst spanned.[fileIndex]).start

        let rec forward j end_ =
            if j < spanned.Length && isBlank j then
                forward (j + 1) (lineEndInArtifact spanned normalized j)
            else
                end_

        let end_ =
            forward
                (fileIndex + 1)
                (lineEndInArtifact spanned normalized fileIndex)

        { TextSpan.start = start; end_ = end_ }

    let private sentenceSpanLines
        (expand: bool)
        (fileIndex: int)
        (spanned: (TextSpan * string) array)
        (depth: int)
        (span: TextSpan)
        (body: string)
        (kind: MdDocument.LineKind)
        (nodeId: NodeId option)
        =
        let parts = MdDocument.splitLineSentences kind body

        match parts with
        | (first, _) :: (_ :: _ as tails) when expand ->
            let parent =
                OutlineDocument.flatLine depth span first None nodeId

            let contentStart = (fst spanned.[fileIndex]).start

            let kids =
                tails
                |> List.map (fun (text, offset) ->
                    let childSpan: TextSpan = {
                        TextSpan.start = contentStart + offset
                        end_ = contentStart + offset + text.Length
                    }

                    OutlineDocument.flatLine (depth + 1) childSpan text None None)

            parent :: kids
        | _ ->
            [ OutlineDocument.flatLine depth span body None nodeId ]

    let private fileSpanLines
        (expand: bool)
        (normalized: string)
        (spanned: (TextSpan * string) array)
        (flats: (int * string * MdDocument.LineKind) list)
        (nodeIds: NodeId option list)
        =
        let substantive =
            spanned
            |> Array.indexed
            |> Array.choose (fun (i, (_, content)) ->
                if String.IsNullOrWhiteSpace content then None else Some i)

        let ids = nodeIds |> List.toArray

        flats
        |> List.mapi (fun i (depth, body, kind) ->
            let fileIndex = substantive.[i]
            let span = absorbedSpan spanned normalized substantive i

            let nodeId =
                match Array.tryItem i ids with
                | Some id -> id
                | None -> None

            sentenceSpanLines
                expand
                fileIndex
                spanned
                depth
                span
                body
                kind
                nodeId)
        |> List.collect id

    let private spanTree
        (expandSentences: bool)
        (text: string)
        (nodeIds: NodeId option list)
        : SpanNode =
        let normalized = text.Replace("\r\n", "\n").Replace("\r", "\n")
        let spanned = OutlineDocument.lineSpans normalized |> List.toArray
        let flats = MdDocument.flattenText normalized
        let lines = fileSpanLines expandSentences normalized spanned flats nodeIds

        let rootSpan: TextSpan = {
            TextSpan.start = 0
            end_ = normalized.Length
        }

        OutlineDocument.nestByDepth rootSpan lines

    let private toSpanTree text nodeIds =
        spanTree true text nodeIds

    let private toAlignTree text nodeIds =
        spanTree false text nodeIds

    let private finishNodes documentRootId contextGraph (nodes, childMap) =
        toNodesRead (
            MdDocument.finishRead documentRootId contextGraph nodes childMap)

    let private readColdImpl text graph documentRootId =
        MdDocument.read text documentRootId graph
        |> Result.map toNodesRead

    let private fromAligned
        editedText
        contextGraph
        documentRootId
        aligned
        =
        MdDocument.rebuildFromAligned
            editedText
            documentRootId
            contextGraph
            aligned
        |> Result.map (finishNodes documentRootId contextGraph)

    /// Positional id reuse when outline matches. Always re-parse — never
    /// copyDocumentFromGraph (ParseFile no-op when graph drifted from file).
    let private rebuildUnchanged previousText contextGraph documentRootId =
        MdDocument.previousOutlineIds previousText contextGraph documentRootId
        |> Result.bind (fun prevIds ->
            let flats = MdDocument.flattenText previousText

            if List.length flats <> List.length prevIds then
                Error "previous outline id count does not match flatten"
            else
                let aligned =
                    List.map2
                        (fun (depth, text, _) idOpt -> depth, text, idOpt)
                        flats
                        prevIds

                fromAligned previousText contextGraph documentRootId aligned)

    let private readWarm
        (diffTexts: OutlineDiffTexts)
        (editedText: string)
        (contextGraph: Graph)
        (documentRootId: NodeId)
        (previousText: string)
        =
        // Compare outlines (blanks omitted), not raw bytes. writeFresh may insert
        // blank separators so export≠disk even when the outline matches. Outline
        // inequality always takes LCS so a disk reorder cannot no-op.
        let prevFlat = MdDocument.flattenText previousText
        let editFlat = MdDocument.flattenText editedText

        if prevFlat = editFlat then
            rebuildUnchanged previousText contextGraph documentRootId
        else
            MdDocument.previousOutlineIds previousText contextGraph documentRootId
            |> Result.bind (fun prevIds ->
                let aligned =
                    OutlineDocumentWarm.alignWarmEdit
                        diffTexts
                        toAlignTree
                        previousText
                        editedText
                        prevIds

                fromAligned editedText contextGraph documentRootId aligned)

    let handler (diffTexts: OutlineDiffTexts) : DocumentHandler = {
        DocumentHandler.parse =
            fun text _graph _documentRootId -> Ok(toSpanTree text [])
        DocumentHandler.readCold = readColdImpl
        DocumentHandler.readWarm = readWarm diffTexts
        DocumentHandler.write =
            fun graph documentRootId previousText ->
                match previousText with
                | None -> MdDocument.writeArtifact graph documentRootId None
                | Some prev ->
                    MdDocument.writeArtifactWarm diffTexts graph documentRootId prev
    }

    let reconcile
        (diffTexts: OutlineDiffTexts)
        (previousText: string)
        (contextGraph: Graph)
        (documentRootId: NodeId)
        (editedText: string)
        : Result<MdReadResult, string> =
        readWarm diffTexts editedText contextGraph documentRootId previousText
        |> Result.map (fun r ->
            MdDocument.finishRead
                documentRootId
                contextGraph
                r.nodes
                r.childMap)
