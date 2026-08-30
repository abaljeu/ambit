namespace Gambol.Shared

/// Turn a DocumentFormat artifact read into history Ops for one document root.
[<RequireQualifiedAccess>]
module DocumentParseOps =

    /// Parse one `.amb` (or plain) artifact into ops. Document stays Current.
    /// When `previousText` is present, warm-reconcile via OutlineLcs (DiffPlex).
    /// Cold (`None`) delegates to DocumentColdParse.planApplyCold.
    let planApplyArtifact
        (graph: Graph)
        (documentRootId: NodeId)
        (relativePath: string)
        (text: string)
        (previousText: string option)
        : Result<Op list, string> =
        match previousText with
        | None ->
            DocumentColdParse.planApplyCold
                graph
                documentRootId
                relativePath
                text
        | Some _ ->
            DocumentWarm.readArtifact
                OutlineLcs.diffTexts
                relativePath
                text
                documentRootId
                graph
                previousText
            |> Result.map (
                DocumentColdParse.planOpsFromGraphs graph documentRootId
            )
