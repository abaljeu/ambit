// picks the codec for a single persisted artifact and routes read/write through that.
namespace Gambol.Shared

open System

type DocumentCodec =
    | Amb
    | Plain

[<RequireQualifiedAccess>]
module DocumentFormat =

    let private normalizeRelative (relativePath: string) =
        relativePath.Replace('\\', '/').TrimStart('/')

    let classifyCodec (relativePath: string) : Result<DocumentCodec, string> =
        let path = normalizeRelative relativePath

        if path.EndsWith(".amb") then
            Ok DocumentCodec.Amb
        else
            Ok DocumentCodec.Plain

    let mergeReadResult (context: Graph) (readResult: AmbDocumentReadResult) : Result<Graph, string> =
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

    let private classifyCodecForWrite
        (graph: Graph)
        (documentRootId: NodeId)
        (relativePath: string)
        : Result<DocumentCodec, string> =
        classifyCodec relativePath
        |> Result.bind (function
            | DocumentCodec.Plain when hasNestedDocumentRootChild graph documentRootId ->
                Ok DocumentCodec.Amb
            | codec -> Ok codec)

    let private classifyCodecForRead
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

    let readArtifact
        (relativePath: string)
        (text: string)
        (documentRootId: NodeId)
        (context: Graph)
        : Result<Graph, string> =
        classifyCodecForRead context documentRootId relativePath text
        |> Result.bind (function
            | DocumentCodec.Amb ->
                AmbDocument.read text documentRootId context
                |> Result.bind (mergeReadResult context)
            | DocumentCodec.Plain ->
                PlainTextDocument.read text documentRootId context
                |> Result.bind (fun readResult ->
                    mergeReadResult
                        context
                        { documentRootId = readResult.documentRootId
                          nodes = readResult.nodes }))

    let writeArtifact
        (graph: Graph)
        (documentRootId: NodeId)
        (relativePath: string)
        (previousText: string option)
        : Result<string, string> =
        classifyCodecForWrite graph documentRootId relativePath
        |> Result.bind (function
            | DocumentCodec.Amb -> AmbDocument.write graph documentRootId
            | DocumentCodec.Plain ->
                let complement =
                    PlainTextDocument.complementForWrite graph documentRootId previousText

                PlainTextDocument.write graph documentRootId complement previousText)
