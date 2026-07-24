namespace Gambol.Shared

open System

/// Build DesktopImportPackage from document codecs (Md, Plain, Amb), not paste.
[<RequireQualifiedAccess>]
module ImportDocument =

    let private fileNameFromRelative (relativePath: string) =
        relativePath.Replace('\\', '/').Trim('/').Split('/')
        |> Array.last

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

    let private packageFromCold
        (sourcePath: string)
        (relativePath: string)
        (text: string)
        : Result<DesktopImportPackage, string> =
        DocumentParseLimits.refuseText text
        |> Result.bind (fun () ->
            if String.IsNullOrWhiteSpace text then
                Error "import text is empty"
            else
                DocumentBinary.refuseParse relativePath text
                |> Result.bind (fun () ->
                    let documentRootId = NodeId.New()
                    let graph = stubGraph documentRootId relativePath

                    DocumentColdParse.planApplyCold
                        graph
                        documentRootId
                        relativePath
                        text
                    |> Result.bind (fun ops ->
                        let topLevelIds, ops =
                            DocumentColdParse.peelDocumentRootOps documentRootId ops

                        if List.isEmpty topLevelIds && List.isEmpty ops then
                            Error "import text is empty"
                        else
                            Ok
                                { sourcePath = sourcePath
                                  isDirectory = false
                                  topLevelIds = topLevelIds
                                  ops = ops })))

    /// Parse file text through DocumentFormat and return a paste-compatible package.
    let buildFilePackage (sourcePath: string) (text: string) : Result<DesktopImportPackage, string> =
        match NodeDesktopPath.artifactRelativeForReference sourcePath with
        | Error err -> Error err
        | Ok relativePath ->
            packageFromCold sourcePath relativePath text

    /// Non-directory text import via cold Plain (default `__paste__.txt`).
    let buildTextPackage
        (sourcePath: string)
        (text: string)
        (relativePath: string option)
        : Result<DesktopImportPackage, string> =
        let relativePath =
            defaultArg relativePath DocumentColdParse.PasteRelativePath

        packageFromCold sourcePath relativePath text

    /// Prior artifact bytes for warm reconcile (graph export, not disk).
    let private previousArtifactText
        (graph: Graph)
        (fileId: NodeId)
        (relativePath: string)
        =
        match DocumentFormat.writeArtifact graph fileId relativePath None with
        | Ok prev when prev.Length > 0 -> Some prev
        | _ -> None

    /// Live-graph parse ops for ParseFile (server apply). Unparsed → cold +
    /// SetDocumentState; Current → warm with previousText from graph export.
    let planParseFile
        (graph: Graph)
        (fileId: NodeId)
        (text: string)
        : Result<Op list, string> =
        DocumentParseLimits.refuseText text
        |> Result.bind (fun () ->
            if String.IsNullOrWhiteSpace text then
                Error "import text is empty"
            else
                match Map.tryFind fileId graph.nodes with
                | Some { kind = Special File; documentState = state } ->
                    match DocumentPartition.artifactFileRelative graph fileId with
                    | None -> Error "no artifact path for file"
                    | Some relativePath ->
                        DocumentBinary.refuseParse relativePath text
                        |> Result.bind (fun () ->
                            let previousText =
                                match state with
                                | Current ->
                                    previousArtifactText graph fileId relativePath
                                | Unparsed -> None

                            DocumentParseOps.planApplyArtifact
                                graph
                                fileId
                                relativePath
                                text
                                previousText
                            |> Result.map (fun parseOps ->
                                let markCurrent =
                                    match state with
                                    | Unparsed ->
                                        [ Op.SetDocumentState(
                                            fileId,
                                            Unparsed,
                                            Current) ]
                                    | Current -> []

                                markCurrent @ parseOps))
                | _ -> Error "file not found or not a File document")

    /// Warm reconcile package (legacy shape). Prefer planParseFile for apply.
    let buildReconcilePackage
        (graph: Graph)
        (fileId: NodeId)
        (sourcePath: string)
        (text: string)
        : Result<DesktopImportPackage, string> =
        match Map.tryFind fileId graph.nodes with
        | Some { kind = Special File; documentState = Unparsed } ->
            Error "file is unparsed; use cold import"
        | Some { kind = Special File; documentState = Current } ->
            planParseFile graph fileId text
            |> Result.map (fun ops ->
                { sourcePath = sourcePath
                  isDirectory = false
                  topLevelIds = []
                  ops = ops })
        | _ -> Error "file not found or not a File document"
