// picks the codec for a single persisted artifact and routes read/write through that.
namespace Gambol.Shared

open System

type DocumentCodec =
    | Amb
    | Plain
    | Md

[<RequireQualifiedAccess>]
module DocumentFormat =

    let private normalizeRelative (relativePath: string) =
        relativePath.Replace('\\', '/').TrimStart('/')

    let classifyCodec (relativePath: string) : Result<DocumentCodec, string> =
        let path = normalizeRelative relativePath

        if path.EndsWith(".amb") then
            Ok DocumentCodec.Amb
        elif path.EndsWith(".md") then
            Ok DocumentCodec.Md
        else
            Ok DocumentCodec.Plain

    let private toNodesRead (documentRootId: NodeId) (nodes: Map<NodeId, Node>) =
        OutlineDocument.nodesRead documentRootId nodes

    let private warmUnavailable _ _ _ _ =
        Error "warm reconcile requires DocumentWarm with Diff"

    /// Fable-safe cold handlers (no Diff). Warm handlers need injected Diff.
    let private coldHandlerFor =
        function
        | DocumentCodec.Amb ->
            {
                DocumentHandler.parse =
                    fun text _graph _documentRootId ->
                        Ok(
                            OutlineDocument.nestOutlineRows
                                (AmbDocument.flattenText text)
                                [])
                DocumentHandler.readCold =
                    fun text graph documentRootId ->
                        AmbDocument.read text documentRootId graph
                        |> Result.map (fun r ->
                            toNodesRead r.documentRootId r.nodes)
                DocumentHandler.readWarm = warmUnavailable
                DocumentHandler.write =
                    fun graph documentRootId _previousText ->
                        AmbDocument.write graph documentRootId
            }
        | DocumentCodec.Plain ->
            {
                DocumentHandler.parse =
                    fun text _graph _documentRootId ->
                        let _, flat = PlainTextDocument.flattenText text

                        Ok(
                            OutlineDocument.nestOutlineRows
                                (flat
                                 |> List.map (fun (depth, body) ->
                                     depth, body, None))
                                [])
                DocumentHandler.readCold =
                    fun text graph documentRootId ->
                        PlainTextDocument.read text documentRootId graph
                        |> Result.map (fun r ->
                            toNodesRead r.documentRootId r.nodes)
                DocumentHandler.readWarm = warmUnavailable
                DocumentHandler.write = PlainTextDocument.writeArtifact
            }
        | DocumentCodec.Md ->
            {
                DocumentHandler.parse =
                    fun text _graph _documentRootId ->
                        let normalized =
                            text.Replace("\r\n", "\n").Replace("\r", "\n")

                        let flats = MdDocument.flattenText normalized

                        Ok(
                            OutlineDocument.nestOutlineRows
                                (flats
                                 |> List.map (fun (depth, body, _) ->
                                     depth, body, None))
                                [])
                DocumentHandler.readCold =
                    fun text graph documentRootId ->
                        MdDocument.read text documentRootId graph
                        |> Result.map (fun r ->
                            toNodesRead r.documentRootId r.nodes)
                DocumentHandler.readWarm = warmUnavailable
                DocumentHandler.write = MdDocument.writeArtifact
            }

    let mergeReadResult
        (allowContentUpdate: bool)
        (context: Graph)
        (readResult: DocumentNodesRead)
        : Result<Graph, string> =
        let graphWithRead = { context with nodes = readResult.nodes }
        let overlayIds =
            DocumentPartition.memberNodeIds graphWithRead readResult.documentRootId
            |> Set.filter (fun nodeId ->
                nodeId = readResult.documentRootId
                || not (
                    DocumentPartition.isNestedDocumentRootBoundary
                        graphWithRead
                        readResult.documentRootId
                        nodeId))

        let conflict =
            if allowContentUpdate then
                None
            else
                overlayIds
                |> Seq.tryPick (fun nodeId ->
                    match Map.tryFind nodeId context.nodes, Map.tryFind nodeId readResult.nodes with
                    | Some existing, Some incoming when
                        existing.text <> incoming.text
                        || existing.name <> incoming.name
                        || existing.kind <> incoming.kind
                        ->
                        Some ("conflicting node definition: " + AmbDocument.formatStableId nodeId)
                    | _ -> None)

        match conflict with
        | Some msg -> Error msg
        | None ->
            let mergedNodes =
                overlayIds
                |> Set.fold
                    (fun nodes nodeId ->
                        match Map.tryFind nodeId readResult.nodes with
                        | Some node -> Map.add nodeId node nodes
                        | None -> nodes)
                    context.nodes

            Ok (Graph.fromNodes context.root mergedNodes)

    let private hasNestedDocumentRootChild (graph: Graph) (documentRootId: NodeId) =
        match Map.tryFind documentRootId graph.nodes with
        | None -> false
        | Some root ->
            root.children
            |> List.exists (fun child ->
                child.ref = Ownership.Owner
                && DocumentPartition.isNestedDocumentRootBoundary graph documentRootId child.id)

    let private looksLikeAmbContent (text: string) =
        if String.IsNullOrEmpty text then
            false
        else
            text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n')
            |> Array.exists (fun line ->
                let trimmed = line.TrimStart('\t')
                trimmed.StartsWith("-> ") || trimmed.StartsWith("^"))

    let classifyCodecForWrite
        (graph: Graph)
        (documentRootId: NodeId)
        (relativePath: string)
        : Result<DocumentCodec, string> =
        classifyCodec relativePath
        |> Result.bind (function
            | DocumentCodec.Plain when hasNestedDocumentRootChild graph documentRootId ->
                Ok DocumentCodec.Amb
            | codec -> Ok codec)

    let classifyCodecForRead
        (graph: Graph)
        (documentRootId: NodeId)
        (relativePath: string)
        (text: string)
        : Result<DocumentCodec, string> =
        classifyCodec relativePath
        |> Result.bind (function
            | DocumentCodec.Plain when hasNestedDocumentRootChild graph documentRootId -> Ok DocumentCodec.Amb
            | DocumentCodec.Plain when looksLikeAmbContent text -> Ok DocumentCodec.Amb
            | codec -> Ok codec)

    /// Cold codec read only — never DiffPlex / readWarm.
    let readArtifactCold
        (relativePath: string)
        (text: string)
        (documentRootId: NodeId)
        (context: Graph)
        : Result<Graph, string> =
        classifyCodecForRead context documentRootId relativePath text
        |> Result.bind (fun codec ->
            coldHandlerFor codec
            |> fun h -> h.readCold text context documentRootId
            |> Result.bind (mergeReadResult false context))

    /// Cold path when previousText is None. Warm (Some _) → DocumentWarm.readArtifact.
    let readArtifact
        (relativePath: string)
        (text: string)
        (documentRootId: NodeId)
        (context: Graph)
        (previousText: string option)
        : Result<Graph, string> =
        match previousText with
        | None -> readArtifactCold relativePath text documentRootId context
        | Some _ ->
            Error "warm artifact read requires DocumentWarm.readArtifact"

    let writeArtifact
        (graph: Graph)
        (documentRootId: NodeId)
        (relativePath: string)
        (previousText: string option)
        : Result<string, string> =
        match previousText with
        | Some _ ->
            Error "warm artifact write requires DocumentWarm.writeArtifact"
        | None ->
            classifyCodecForWrite graph documentRootId relativePath
            |> Result.bind (fun codec ->
                coldHandlerFor codec
                |> fun h -> h.write graph documentRootId None)
