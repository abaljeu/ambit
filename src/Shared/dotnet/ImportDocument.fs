namespace Gambol.Shared

open System

/// Build DesktopImportPackage from document codecs (Md, Plain, Amb), not paste.
[<RequireQualifiedAccess>]
module ImportDocument =

    let private fileNameFromRelative (relativePath: string) =
        relativePath.Replace('\\', '/').Trim('/').Split('/')
        |> Array.last

    let private targetsDocumentRoot (documentRootId: NodeId) =
        function
        | Op.Replace(id, _, _, _) when id = documentRootId -> true
        | Op.SetText(id, _, _) when id = documentRootId -> true
        | Op.SetName(id, _, _) when id = documentRootId -> true
        | Op.SetClasses(id, _, _) when id = documentRootId -> true
        | Op.NewSpecialNode(id, _, _) when id = documentRootId -> true
        | Op.SetDocumentState(id, _, _) when id = documentRootId -> true
        | _ -> false

    let private topLevelIdsFromOps (documentRootId: NodeId) (ops: Op list) =
        ops
        |> List.tryPick (function
            | Op.Replace(id, _, _, children) when id = documentRootId ->
                Some (children |> List.map (fun c -> c.id))
            | _ -> None)
        |> Option.defaultValue []

    let private peelDocumentRootOps (documentRootId: NodeId) (ops: Op list) =
        let topLevelIds = topLevelIdsFromOps documentRootId ops
        let ops = ops |> List.filter (targetsDocumentRoot documentRootId >> not)
        topLevelIds, ops

    let private stubGraph (documentRootId: NodeId) (relativePath: string) : Graph =
        let graph0 = Graph.create ()
        let name = fileNameFromRelative relativePath

        let file =
            Node.Create(
                documentRootId,
                text = name,
                name = Filename.create name,
                owner = graph0.root,
                kind = Special File,
                documentState = Unparsed)

        graph0.nodes
        |> Map.add documentRootId file
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    /// Parse file text through DocumentFormat and return a paste-compatible package.
    let buildFilePackage (sourcePath: string) (text: string) : Result<DesktopImportPackage, string> =
        if String.IsNullOrWhiteSpace text then
            Error "import text is empty"
        else
            match NodeDesktopPath.artifactRelativeForReference sourcePath with
            | Error err -> Error err
            | Ok relativePath ->
                let documentRootId = NodeId.New()
                let graph = stubGraph documentRootId relativePath

                DocumentParseOps.planApplyArtifact
                    graph
                    documentRootId
                    relativePath
                    text
                    None
                |> Result.bind (fun ops ->
                    let topLevelIds, ops = peelDocumentRootOps documentRootId ops

                    if List.isEmpty topLevelIds && List.isEmpty ops then
                        Error "import text is empty"
                    else
                        Ok
                            { sourcePath = sourcePath
                              isDirectory = false
                              topLevelIds = topLevelIds
                              ops = ops })
