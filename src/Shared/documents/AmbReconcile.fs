namespace Gambol.Shared

/// Amb warm import: hard-match stable ids, then outline LCS (Shared/DotNet).
[<RequireQualifiedAccess>]
module AmbReconcile =

    let private toOutline
        (depth, text, nodeId, hardKey)
        : OutlineReconcile.OutlineLine =
        {
            depth = depth
            text = text
            nodeId = nodeId
            hardKey = hardKey
        }

    let private fromDispositions
        (disps: OutlineReconcile.LineDisposition list)
        : (int * string * NodeId option) list =
        disps
        |> List.choose (function
            | OutlineReconcile.Keep(id, depth, text) ->
                Some(depth, text, Some id)
            | OutlineReconcile.Insert(depth, text) ->
                Some(depth, text, None)
            | OutlineReconcile.Delete _ -> None)

    let reconcile
        (previousText: string)
        (contextGraph: Graph)
        (documentRootId: NodeId)
        (editedText: string)
        : Result<AmbDocumentReadResult, string> =
        if editedText = previousText then
            AmbDocument.read previousText documentRootId contextGraph
        else
            AmbDocument.mappedPrevious
                previousText contextGraph documentRootId
            |> Result.bind (fun previous ->
                let prevLines = previous |> List.map toOutline
                let editLines =
                    AmbDocument.flattenText editedText
                    |> List.map (fun (depth, text, hardKey) ->
                        toOutline (depth, text, None, hardKey))
                let aligned =
                    OutlineReconcile.align prevLines editLines
                    |> fromDispositions
                let projected = AmbDocument.projectAligned aligned
                AmbDocument.read projected documentRootId contextGraph)
