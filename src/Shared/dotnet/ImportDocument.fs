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
        if String.IsNullOrWhiteSpace text then
            Error "import text is empty"
        else
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
                          ops = ops })

    /// Parse file text through DocumentFormat and return a paste-compatible package.
    let buildFilePackage (sourcePath: string) (text: string) : Result<DesktopImportPackage, string> =
        if String.IsNullOrWhiteSpace text then
            Error "import text is empty"
        else
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
