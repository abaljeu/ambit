namespace Gambol.Server

open System
open System.IO
open Gambol.Shared

/// Path written plus optional status for the succeeded graph-change ack.
type DocumentWriteOk = {
    path: string
    message: string option
}

[<RequireQualifiedAccess>]
module DocumentPersistWrite =

    let private writeArtifactTextCore
        (dataDir: string)
        (graph: Graph)
        (fileId: NodeId)
        (text: string)
        : Result<unit, string> =
        match DocumentPartition.artifactFileRelative graph fileId with
        | None -> Error (DocumentPersistPath.noArtifactPathError graph fileId)
        | Some relativePath ->
            match SystemDirectoryPersist.refuseWrite relativePath with
            | Error msg -> Error msg
            | Ok () ->
                match DocumentPersistPath.resolveUnderDataDir dataDir relativePath with
                | Error msg -> Error msg
                | Ok fullPath ->
                    try
                        let parent = Path.GetDirectoryName fullPath

                        if not (String.IsNullOrEmpty parent) then
                            Directory.CreateDirectory parent |> ignore

                        let preservedMtime =
                            if File.Exists fullPath then
                                Some(File.GetLastWriteTimeUtc fullPath)
                            else
                                None

                        let tmpPath = fullPath + ".tmp"
                        File.WriteAllText(tmpPath, text)
                        File.Move(tmpPath, fullPath, true)

                        match preservedMtime with
                        | Some utc -> File.SetLastWriteTimeUtc(fullPath, utc)
                        | None -> ()

                        Ok ()
                    with ex ->
                        Error ex.Message

    let private writeArtifactText
        (dataDir: string)
        (graph: Graph)
        (fileId: NodeId)
        (text: string)
        : Result<unit, string> =
        match DocumentPersistPath.workspaceRootFor dataDir graph fileId with
        | None -> writeArtifactTextCore dataDir graph fileId text
        | Some root ->
            WorkspaceGit.withWorkTreeGate root (fun () ->
                writeArtifactTextCore dataDir graph fileId text)

    let private preparedParseText
        (dataDir: string)
        (graph: Graph)
        (fileId: NodeId)
        (relativePath: string)
        (textOpt: string option)
        : Result<string, string> =
        match textOpt with
        | Some text ->
            DocumentParseLimits.refuseText text
            |> Result.bind (fun () ->
                DocumentBinary.refuseParse relativePath text)
            |> Result.bind (fun () ->
                writeArtifactText dataDir graph fileId text
                |> Result.map (fun () -> text))
        | None ->
            if DocumentBinary.isBinaryExtension relativePath then
                Error DocumentBinary.parseError
            else
                DocumentPersistPath.readFileTextAtRelative dataDir relativePath
                |> Result.bind (fun text ->
                    DocumentParseLimits.refuseText text
                    |> Result.bind (fun () ->
                        DocumentBinary.refuseParse relativePath text)
                    |> Result.map (fun () -> text))

    let private parseOpsWithMtimeStamp
        (dataDir: string)
        (graph: Graph)
        (fileId: NodeId)
        (text: string)
        : Result<Op list, string> =
        ImportDocument.planParseFile graph fileId text
        |> Result.map (fun parseOps ->
            match DocumentPersistPath.resolveArtifactPath dataDir graph fileId with
            | Ok fullPath when File.Exists fullPath ->
                let mtime =
                    NodeUpdateTime.toDbPrecision(
                        File.GetLastWriteTimeUtc fullPath)
                let node = graph.nodes.[fileId]

                if node.updateTime = mtime then
                    parseOps
                else
                    parseOps
                    @ [ Op.SetUpdateTime(
                            fileId,
                            node.updateTime,
                            mtime) ]
            | _ -> parseOps)

    /// Plan ParseFile ops on the live graph. `textOpt` from desktop upload
    /// (writes artifact to DataDir first); otherwise read artifact text from DataDir.
    let planParseFile
        (dataDir: string)
        (graph: Graph)
        (fileId: NodeId)
        (textOpt: string option)
        : Result<Op list, string> =
        match Map.tryFind fileId graph.nodes with
        | Some { kind = Special File } ->
            match DocumentPartition.artifactFileRelative graph fileId with
            | None -> Error "selected File has no occurrence on the server"
            | Some relativePath ->
                preparedParseText dataDir graph fileId relativePath textOpt
                |> Result.bind (parseOpsWithMtimeStamp dataDir graph fileId)
        | _ -> Error "file not found or not a File document"

    /// Refuse persist when the graph node *name* is `.amb` (not path basename).
    /// Legitimate Workspace/Directory nodes write Directory Files as `xx/.amb`.
    let refuseDirectoryFileNamedDocument (node: Node) : Result<unit, string> =
        if Filename.isDirectoryFileFilename node.name then
            Error "directory file basename must not drive artifact writes"
        else
            Ok ()

    /// Live-save write outcomes returned with a succeeded graph change.
    let fileCouldNotSave (path: string) = $"file couldn't save: {path}"

    let stableFileUpdateFailed (path: string) =
        $"partial file update failed.  full file rewrite completed: {path}"

    let private refuseWriteDocumentRoot
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<unit, string> =
        match Map.tryFind documentRootId graph.nodes with
        | Some node -> refuseDirectoryFileNamedDocument node
        | None -> Ok ()

    let private artifactRelativeForWrite
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<string, string> =
        match DocumentPartition.artifactFileRelative graph documentRootId with
        | None ->
            Error "no file relative artifact path for document root"
        | Some rel -> Ok rel

    let private previousFileText (fullPath: string) : string option =
        if File.Exists fullPath then
            Some(File.ReadAllText fullPath)
        else
            None

    let private ensureWorkspaceRepo (dirFull: string) (node: Node) =
        match node.kind with
        | Special Workspace ->
            if WorkspaceGit.isRepo dirFull then
                Ok ()
            else
                WorkspaceGit.ensureInit dirFull
        | _ -> Ok ()

    let private ensureArtifactDirectory
        (dataDir: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<unit, string> =
        match DocumentPartition.artifactDirectoryRelative graph documentRootId with
        | None -> Ok ()
        | Some dirRel ->
            match DocumentPersistPath.resolveUnderDataDir dataDir dirRel with
            | Error msg -> Error msg
            | Ok dirFull ->
                try
                    Directory.CreateDirectory dirFull |> ignore
                    match Map.tryFind documentRootId graph.nodes with
                    | Some node -> ensureWorkspaceRepo dirFull node
                    | None -> Ok ()
                with ex ->
                    Error ex.Message

    let private writeArtifactFile
        (fullPath: string)
        (text: string)
        (message: string option)
        : Result<DocumentWriteOk, string> =
        let parent = Path.GetDirectoryName fullPath

        if not (String.IsNullOrEmpty parent) then
            Directory.CreateDirectory parent |> ignore

        try
            let tmpPath = fullPath + ".tmp"
            File.WriteAllText(tmpPath, text)
            File.Move(tmpPath, fullPath, true)
            Ok {
                path = fullPath
                message = message
            }
        with ex ->
            Error ex.Message

    let private persistArtifactText
        (dataDir: string)
        (graph: Graph)
        (documentRootId: NodeId)
        (fullPath: string)
        (previousText: string option)
        (rel: string)
        (artifact: DocumentWarm.ArtifactWrite)
        : Result<DocumentWriteOk, string> =
        let message =
            if artifact.stableUpdateFailed then
                Some(stableFileUpdateFailed rel)
            else
                None

        if previousText = Some artifact.text then
            Ok {
                path = fullPath
                message = message
            }
        else
            match ensureArtifactDirectory dataDir graph documentRootId with
            | Error msg -> Error msg
            | Ok () -> writeArtifactFile fullPath artifact.text message

    let private writeDocumentCore
        (dataDir: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<DocumentWriteOk, string> =
        match refuseWriteDocumentRoot graph documentRootId with
        | Error msg -> Error msg
        | Ok () ->
            match DocumentPersistPath.resolveArtifactPath dataDir graph documentRootId with
            | Error msg -> Error msg
            | Ok fullPath ->
                match artifactRelativeForWrite graph documentRootId with
                | Error msg -> Error msg
                | Ok rel ->
                    match SystemDirectoryPersist.refuseWrite rel with
                    | Error msg -> Error msg
                    | Ok () ->
                        let previousText = previousFileText fullPath
                        match
                            DocumentWarm.writeArtifact
                                OutlineLcs.diffTexts
                                graph
                                documentRootId
                                rel
                                previousText
                        with
                        | Error msg -> Error msg
                        | Ok artifact ->
                            persistArtifactText
                                dataDir graph documentRootId fullPath
                                previousText rel artifact

    let writeDocument
        (dataDir: string)
        (graph: Graph)
        (documentRootId: NodeId)
        : Result<DocumentWriteOk, string> =
        match DocumentPersistPath.workspaceRootFor dataDir graph documentRootId with
        | None -> writeDocumentCore dataDir graph documentRootId
        | Some root ->
            WorkspaceGit.withWorkTreeGate root (fun () ->
                writeDocumentCore dataDir graph documentRootId)

    let private joinWriteMessages (messages: string list) : string option =
        match messages |> List.distinct with
        | [] -> None
        | msgs -> Some(String.concat "; " msgs)

    /// Strict writes for bootstrap/tests: any writeDocument Error fails the fold.
    let private writeDocuments
        (dataDir: string)
        (graph: Graph)
        (rootIds: NodeId list)
        : Result<Graph, string> =
        let baseDir = DocumentPersistPath.dataDirBase dataDir
        Directory.CreateDirectory baseDir |> ignore

        rootIds
        |> List.fold
            (fun acc documentRootId ->
                match acc with
                | Error msg -> Error msg
                | Ok stamps ->
                    match writeDocumentCore dataDir graph documentRootId with
                    | Error msg -> Error msg
                    | Ok written ->
                        let mtime = File.GetLastWriteTimeUtc written.path
                        Ok(Map.add documentRootId mtime stamps))
            (Ok Map.empty)
        |> Result.map (fun stamps ->
            DocumentPersistPath.stampNodes stamps graph)

    let private softWritePathHint (graph: Graph) (documentRootId: NodeId) =
        match DocumentPartition.artifactFileRelative graph documentRootId with
        | Some rel -> rel
        | None -> $"id={documentRootId.Value}"

    /// Live-save writes: compute/IO failures never fail the fold; they become messages.
    let writeDocumentsSoft
        (dataDir: string)
        (graph: Graph)
        (rootIds: NodeId list)
        : Graph * string option =
        let baseDir = DocumentPersistPath.dataDirBase dataDir
        Directory.CreateDirectory baseDir |> ignore

        let stamps, messages =
            rootIds
            |> List.fold
                (fun (stamps, messages) documentRootId ->
                    match writeDocumentCore dataDir graph documentRootId with
                    | Error _ ->
                        let msg =
                            softWritePathHint graph documentRootId
                            |> fileCouldNotSave
                        stamps, msg :: messages
                    | Ok written ->
                        let mtime = File.GetLastWriteTimeUtc written.path
                        let stamps' = Map.add documentRootId mtime stamps
                        let messages' =
                            match written.message with
                            | Some msg -> msg :: messages
                            | None -> messages
                        stamps', messages')
                (Map.empty, [])

        DocumentPersistPath.stampNodes stamps graph,
        joinWriteMessages (List.rev messages)

    /// Test/bootstrap helper that materializes a complete file layout from a generated graph.
    /// Normal accepted graph changes use persistGraphOps/JIT live-save; this intentionally
    /// bypasses normal production persistence.
    let writeAllDocuments (dataDir: string) (graph: Graph) : Result<Graph, string> =
        let roots =
            DocumentPersistPath.enumerateDocumentRoots graph
            |> List.filter (fun documentRootId ->
            DocumentPartition.shouldWriteDocumentRoot graph.nodes.[documentRootId])
        let workTreeRoots =
            roots
            |> List.choose (DocumentPersistPath.workspaceRootFor dataDir graph)
            |> DocumentPersistPath.normalizedWorkTreeRoots
        DocumentPersistPath.withWorkTreeGates workTreeRoots (fun () ->
            writeDocuments dataDir graph roots)
