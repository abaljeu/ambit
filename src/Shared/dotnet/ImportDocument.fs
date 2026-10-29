namespace Gambol.Shared

open System

/// Server ParseFile ops from document codecs (Md, Plain, Amb).
[<RequireQualifiedAccess>]
module ImportDocument =

    /// Prior artifact bytes for warm reconcile (graph export, not disk).
    let private previousArtifactText
        (graph: Graph)
        (fileId: NodeId)
        (relativePath: string)
        =
        match DocumentFormat.writeArtifact graph fileId relativePath None with
        | Ok prev when prev.Length > 0 -> Some prev
        | _ -> None

    /// Live-graph parse ops for ParseFile (server apply). Reuse the existing
    /// File owner id. When graph export yields prior artifact text (Current, or
    /// Unparsed after M/upload with structure retained), warm-reconcile; cold
    /// only when there is no prior content. The result is outline ops.
    let planParseFile
        (graph: Graph)
        (fileId: NodeId)
        (text: string)
        : Result<Op list, string> =
        DocumentParseLimits.refuseText text
        |> Result.bind (fun () ->
            match Map.tryFind fileId graph.nodes with
            | Some { kind = Special File
                     documentState = ds } when
                DocumentState.toLegacy ds = PullingFile ->
                Error "no file on server"
            | Some { kind = Special File
                     documentState = ds } ->
                let state = DocumentState.toLegacy ds
                match DocumentPartition.artifactFileRelative graph fileId with
                | None -> Error "no artifact path for file"
                | Some relativePath ->
                    DocumentBinary.refuseParse relativePath text
                    |> Result.bind (fun () ->
                        let previousText =
                            match state with
                            | Current
                            | Unparsed ->
                                previousArtifactText graph fileId relativePath
                            | PullingFile -> None

                        DocumentParseOps.planApplyArtifact
                            graph
                            fileId
                            relativePath
                            text
                            previousText)
            | _ -> Error "file not found or not a File document")
