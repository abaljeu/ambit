namespace Gambol.Shared

/// Plain warm import: outline LCS reconcile (Shared/DotNet only).
[<RequireQualifiedAccess>]
module PlainTextReconcile =

    let private toOutline (depth, text, nodeId) : OutlineReconcile.OutlineLine = {
        depth = depth
        text = text
        nodeId = nodeId
    }

    let private fromDispositions
        (disps: OutlineReconcile.LineDisposition list)
        : (int * string * NodeId option) list =
        disps
        |> List.choose (function
            | OutlineReconcile.Keep(id, depth, text) -> Some(depth, text, Some id)
            | OutlineReconcile.Insert(depth, text) -> Some(depth, text, None)
            | OutlineReconcile.Delete _ -> None)

    let reconcile
        (previousText: string)
        (contextGraph: Graph)
        (documentRootId: NodeId)
        (editedText: string)
        : Result<PlainTextReadResult, string> =
        if editedText = previousText then
            let indentStyle, _ = PlainTextDocument.flattenText previousText
            let nodes =
                PlainTextDocument.copyDocumentFromGraph contextGraph documentRootId

            Ok(PlainTextDocument.finishRead documentRootId contextGraph nodes indentStyle)
        else
            let indentStyle, editedFlat = PlainTextDocument.flattenText editedText

            let previous =
                PlainTextDocument.mappedPrevious previousText contextGraph documentRootId

            let prevLines = previous |> List.map toOutline

            let editLines =
                editedFlat
                |> List.map (fun (depth, text) -> toOutline (depth, text, None))

            let aligned =
                OutlineReconcile.align prevLines editLines
                |> fromDispositions

            match PlainTextDocument.rebuildFromAligned documentRootId contextGraph aligned with
            | Error msg -> Error msg
            | Ok nodes ->
                Ok(PlainTextDocument.finishRead documentRootId contextGraph nodes indentStyle)
