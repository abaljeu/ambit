namespace Gambol.Shared

/// Amb warm import via nested SpanNode + shared outline LCS.
[<RequireQualifiedAccess>]
module AmbReconcile =

    let private toNodesRead (r: AmbDocumentReadResult) =
        OutlineDocument.nodesRead r.documentRootId r.nodes

    let private toSpanTree text nodeIds =
        OutlineDocument.nestOutlineRows (AmbDocument.flattenText text) nodeIds

    let private readColdImpl text graph documentRootId =
        AmbDocument.read text documentRootId graph
        |> Result.map toNodesRead

    let private hooks: OutlineDocument.OutlineWarmHooks = {
        OutlineDocument.OutlineWarmHooks.previousNodeIds =
            AmbDocument.previousOutlineIds
        whenUnchanged = None
        fromAligned =
            fun _editedText graph documentRootId aligned ->
                AmbDocument.read
                    (AmbDocument.projectAligned aligned)
                    documentRootId
                    graph
                |> Result.map toNodesRead
    }

    let handler: DocumentHandler =
        OutlineDocument.makeOutlineHandler
            toSpanTree
            readColdImpl
            hooks
            (fun graph documentRootId _previousText ->
                AmbDocument.write graph documentRootId)

    let reconcile
        (previousText: string)
        (contextGraph: Graph)
        (documentRootId: NodeId)
        (editedText: string)
        : Result<AmbDocumentReadResult, string> =
        handler.readWarm editedText contextGraph documentRootId previousText
        |> Result.map (fun r -> {
            AmbDocumentReadResult.documentRootId = r.documentRootId
            AmbDocumentReadResult.nodes = r.nodes
        })
