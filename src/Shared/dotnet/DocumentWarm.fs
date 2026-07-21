namespace Gambol.Shared

/// DotNet warm artifact read (DiffPlex). Cold path delegates to DocumentFormat.readArtifactCold.
[<RequireQualifiedAccess>]
module DocumentWarm =

    let private handlerFor =
        function
        | DocumentCodec.Amb -> AmbReconcile.handler
        | DocumentCodec.Plain -> PlainTextReconcile.handler
        | DocumentCodec.Md -> MdReconcile.handler

    let readArtifact
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
                handlerFor codec
                |> fun h -> h.readWarm text context documentRootId prev
                |> Result.bind (
                    DocumentFormat.mergeReadResult allowContentUpdate context
                ))
