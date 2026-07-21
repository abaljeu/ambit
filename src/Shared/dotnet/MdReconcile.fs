namespace Gambol.Shared

open System

/// Md warm import via nested SpanNode + shared outline LCS.
/// Blank lines absorb into neighboring bound node spans (no blank nodes).
[<RequireQualifiedAccess>]
module MdReconcile =

    let private toNodesRead (r: MdReadResult) =
        OutlineDocument.nodesRead r.documentRootId r.nodes

    let private lineEndInArtifact
        (spanned: (TextSpan * string) list)
        (normalized: string)
        (lineIndex: int)
        =
        if lineIndex + 1 < spanned.Length then
            (fst spanned.[lineIndex + 1]).start
        else
            normalized.Length

    let private absorbedSpan
        (spanned: (TextSpan * string) list)
        (normalized: string)
        (substantiveFileIndices: int list)
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

    let private toSpanTree (text: string) (nodeIds: NodeId option list) : SpanNode =
        let normalized = text.Replace("\r\n", "\n").Replace("\r", "\n")
        let spanned = OutlineDocument.lineSpans normalized
        let flats: (int * string * MdDocument.LineKind) list =
            MdDocument.flattenText normalized

        let substantiveFileIndices =
            spanned
            |> List.indexed
            |> List.choose (fun (i, (_, content)) ->
                if String.IsNullOrWhiteSpace content then None else Some i)

        let lines =
            flats
            |> List.mapi (fun i (depth, body, _) ->
                let span =
                    absorbedSpan spanned normalized substantiveFileIndices i

                let nodeId =
                    match List.tryItem i nodeIds with
                    | Some id -> id
                    | None -> None

                OutlineDocument.flatLine depth span body None nodeId)

        let rootSpan: TextSpan = {
            TextSpan.start = 0
            end_ = normalized.Length
        }

        OutlineDocument.nestByDepth rootSpan lines

    let private finishNodes documentRootId contextGraph nodes =
        toNodesRead (MdDocument.finishRead documentRootId contextGraph nodes)

    let private readColdImpl text graph documentRootId =
        MdDocument.read text documentRootId graph
        |> Result.map toNodesRead

    let private hooks: OutlineDocumentWarm.OutlineWarmHooks = {
        OutlineDocumentWarm.OutlineWarmHooks.previousNodeIds =
            MdDocument.previousOutlineIds
        whenUnchanged =
            Some(fun _previousText contextGraph documentRootId ->
                MdDocument.copyDocumentFromGraph contextGraph documentRootId
                |> finishNodes documentRootId contextGraph
                |> Ok)
        fromAligned =
            fun editedText contextGraph documentRootId aligned ->
                MdDocument.rebuildFromAligned
                    editedText
                    documentRootId
                    contextGraph
                    aligned
                |> Result.map (finishNodes documentRootId contextGraph)
    }

    let handler: DocumentHandler =
        OutlineDocumentWarm.makeOutlineHandler
            toSpanTree
            readColdImpl
            hooks
            MdDocument.writeArtifact

    let reconcile
        (previousText: string)
        (contextGraph: Graph)
        (documentRootId: NodeId)
        (editedText: string)
        : Result<MdReadResult, string> =
        handler.readWarm editedText contextGraph documentRootId previousText
        |> Result.map (fun r ->
            MdDocument.finishRead documentRootId contextGraph r.nodes)
