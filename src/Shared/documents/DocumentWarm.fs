namespace Gambol.Shared

/// Warm artifact read/write. Pass Diff (DotNet: OutlineLcs.diffTexts). Cold path uses
/// DocumentFormat.readArtifactCold / writeArtifact.
[<RequireQualifiedAccess>]
module DocumentWarm =

    let private handlerFor (diffTexts: OutlineDiffTexts) =
        function
        | DocumentCodec.Amb -> AmbReconcile.handler diffTexts
        | DocumentCodec.Plain -> PlainTextReconcile.handler diffTexts
        | DocumentCodec.Md -> MdReconcile.handler diffTexts
        | DocumentCodec.CStyle -> CStyleReconcile.handler diffTexts

    let readArtifact
        (diffTexts: OutlineDiffTexts)
        (relativePath: string)
        (text: string)
        (documentRootId: NodeId)
        (context: Graph)
        (previousText: string option)
        : Result<Graph, string> =
        match previousText with
        | None ->
            DocumentFormat.readArtifactCold
                relativePath
                text
                documentRootId
                context
        | Some prev ->
            let allowContentUpdate = true

            DocumentFormat.classifyCodecForRead
                context
                documentRootId
                relativePath
                text
            |> Result.bind (fun codec ->
                handlerFor diffTexts codec
                |> fun h -> h.readWarm text context documentRootId prev
                |> Result.bind (
                    DocumentFormat.mergeReadResult allowContentUpdate context
                ))

    let writeArtifact
        (diffTexts: OutlineDiffTexts)
        (graph: Graph)
        (documentRootId: NodeId)
        (relativePath: string)
        (previousText: string option)
        : Result<string, string> =
        match previousText with
        | None ->
            DocumentFormat.writeArtifact graph documentRootId relativePath None
        | Some _ ->
            DocumentFormat.classifyCodecForWrite graph documentRootId relativePath
            |> Result.bind (fun codec ->
                handlerFor diffTexts codec
                |> fun h -> h.write graph documentRootId previousText)
