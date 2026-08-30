namespace Gambol.Shared

/// Warm artifact read/write. Pass Diff (DotNet: OutlineLcs.diffTexts). Cold path uses
/// DocumentFormat.readArtifactCold / writeArtifact.
[<RequireQualifiedAccess>]
module DocumentWarm =

    /// Result of computing artifact text. `stableUpdateFailed` means warm merge of
    /// graph+file failed and cold graph-only text was used instead.
    type ArtifactWrite = {
        text: string
        stableUpdateFailed: bool
    }

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
            DocumentParseLimits.refuseText text
            |> Result.bind (fun () ->
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
                    )))

    let private writeWarm
        (diffTexts: OutlineDiffTexts)
        (graph: Graph)
        (documentRootId: NodeId)
        (relativePath: string)
        (previousText: string option)
        : Result<string, string> =
        DocumentFormat.classifyCodecForWrite graph documentRootId relativePath
        |> Result.bind (fun codec ->
            handlerFor diffTexts codec
            |> fun h -> h.write graph documentRootId previousText)

    /// Compute artifact text: warm merge when previous disk text exists; on warm
    /// failure (Error or exception) fall back to cold graph-only write.
    let writeArtifact
        (diffTexts: OutlineDiffTexts)
        (graph: Graph)
        (documentRootId: NodeId)
        (relativePath: string)
        (previousText: string option)
        : Result<ArtifactWrite, string> =
        match previousText with
        | None ->
            DocumentFormat.writeArtifact graph documentRootId relativePath None
            |> Result.map (fun text -> {
                text = text
                stableUpdateFailed = false
            })
        | Some _ ->
            let warm =
                try
                    writeWarm
                        diffTexts
                        graph
                        documentRootId
                        relativePath
                        previousText
                with ex ->
                    Error ex.Message

            match warm with
            | Ok text ->
                Ok {
                    text = text
                    stableUpdateFailed = false
                }
            | Error _ ->
                DocumentFormat.writeArtifact graph documentRootId relativePath None
                |> Result.map (fun text -> {
                    text = text
                    stableUpdateFailed = true
                })
