namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module DocumentAssembly =

    type DocumentArtifactKind =
        | Directory
        | File

    type ArtifactDescriptor = {
        relativePath: string
        kind: DocumentArtifactKind
    }

    let private normalizeRelative (relativePath: string) =
        relativePath.Replace('\\', '/').TrimStart('/')

    let private splitSegments (relativePath: string) =
        normalizeRelative relativePath
        |> fun path ->
            if String.IsNullOrEmpty path then
                []
            else
                path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                |> Array.toList

    let private resultFold folder state items =
        items
        |> Seq.fold
            (fun acc item ->
                match acc with
                | Error msg -> Error msg
                | Ok state -> folder state item)
            (Ok state)

    let private tryParseRefTarget (target: string) : (string option * string) option =
        let caretIdx = target.LastIndexOf('^')
        if caretIdx < 0 then
            None
        elif caretIdx = 0 then
            Some (None, target.Substring(1))
        else
            let path = target.Substring(0, caretIdx)
            Some (Some path, target.Substring(caretIdx + 1))

    let classifyArtifactRelative (relativePath: string) : Result<ArtifactDescriptor, string> =
        let path = normalizeRelative relativePath

        if path = ".amb" || path.EndsWith("/.amb") then
            Ok { relativePath = path; kind = DocumentArtifactKind.Directory }
        elif path.EndsWith(".amb") then
            Error ("unrecognized artifact path: " + path)
        else
            Ok { relativePath = path; kind = DocumentArtifactKind.File }

    let artifactRelativeForNodeReference (nodeReference: string) : Result<string, string> =
        NodeDesktopPath.artifactRelativeForReference nodeReference

    let private sourceLines (text: string) =
        if String.IsNullOrEmpty text then
            Seq.empty
        else
            text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n')
            |> Array.toSeq

    let private parseRefLine (line: string) : Result<(string option * NodeId) option, string> =
        let trimmed = line.TrimStart('\t')
        if trimmed.StartsWith("-> ") then
            let target = trimmed.Substring(3).Trim()
            match tryParseRefTarget target with
            | None -> Error ("invalid ref line: " + trimmed)
            | Some (pathOpt, stableToken) ->
                match AmbDocument.tryParseStableId stableToken with
                | None -> Error ("invalid stable id in ref: " + stableToken)
                | Some nodeId -> Ok (Some (pathOpt, nodeId))
        else
            Ok None

    let private parseNestedDocRefs (text: string) : Result<(string * NodeId) list, string> =
        sourceLines text
        |> resultFold
            (fun refs line ->
                parseRefLine line
                |> Result.map (function
                    | None -> refs
                    | Some (Some path, nodeId) -> (path, nodeId) :: refs
                    | Some (None, _) -> refs))
            []
        |> Result.map List.rev

    let scanRefIndex (texts: string seq) : Result<Map<string, NodeId>, string> =
        let addLine index (line: string) =
            parseRefLine line
            |> Result.bind (function
                | None -> Ok index
                | Some (None, _) -> Ok index
                | Some (Some path, nodeId) -> Ok (index |> Map.add path nodeId))

        texts
        |> Seq.collect sourceLines
        |> resultFold addLine Map.empty<string, NodeId>


    let private isWorkspaceArtifact (nodeReference: string option) (descriptor: ArtifactDescriptor) =
        descriptor.kind = DocumentArtifactKind.Directory
        && match nodeReference with
           | Some ref when ref.EndsWith("/") -> false
           | Some ref ->
               NodeDesktopPath.tryParseWorkspacePath ref
               |> Option.exists (fun (_, tail) -> String.IsNullOrEmpty(tail.TrimEnd('/')))
           | None -> false

    let private stubName (nodeReference: string option) (descriptor: ArtifactDescriptor) : Filename =
        match splitSegments descriptor.relativePath |> List.rev with
        | ".amb" :: name :: _ when isWorkspaceArtifact nodeReference descriptor ->
            Filename.create name
        | ".amb" :: name :: _ -> Filename.create name
        | name :: _ -> Filename.create name
        | [] -> Filename.Empty

    let private stubKind (nodeReference: string option) (descriptor: ArtifactDescriptor) : NodeKind =
        match descriptor.kind with
        | DocumentArtifactKind.Directory when isWorkspaceArtifact nodeReference descriptor ->
            NodeKind.Special SpecialKind.Workspace
        | DocumentArtifactKind.Directory -> NodeKind.Special SpecialKind.Directory
        | DocumentArtifactKind.File -> NodeKind.Special SpecialKind.File

    let private stubNode (nodeReference: string option) (descriptor: ArtifactDescriptor) (documentRootId: NodeId) : Node =
        { Node.create documentRootId with
            name = stubName nodeReference descriptor
            kind = stubKind nodeReference descriptor }

    let private seedStub
        (graph: Graph)
        (descriptor: ArtifactDescriptor)
        (documentRootId: NodeId)
        (nodeReference: string option)
        : Graph =
        if documentRootId = Graph.rootId || documentRootId = Graph.trashId then
            graph
        else
            let nodes = Map.add documentRootId (stubNode nodeReference descriptor documentRootId) graph.nodes
            Graph.fromNodes graph.root nodes

    let validateAssembledGraph (graph: Graph) : Result<Graph, string> =
        let missingChild =
            graph.nodes
            |> Map.toSeq
            |> Seq.collect (fun (_, node) -> node.children)
            |> Seq.tryFind (fun child -> not (Map.containsKey child.id graph.nodes))

        let documentRoots =
            graph.nodes
            |> Map.toSeq
            |> Seq.choose (fun (nodeId, _) ->
                if DocumentPartition.isDocumentRootNode graph nodeId then Some nodeId else None)

        let addMembership index (memberId, docId) =
            match Map.tryFind memberId index with
            | None -> Ok (Map.add memberId docId index)
            | Some other when other <> docId ->
                if DocumentPartition.isDocumentRootNode graph memberId then
                    Ok index
                else
                    Error (
                        "member "
                        + AmbDocument.formatStableId memberId
                        + " belongs to multiple documents")
            | Some _ -> Ok index

        match missingChild with
        | Some child -> Error ("missing ref target: " + AmbDocument.formatStableId child.id)
        | None ->
            documentRoots
            |> Seq.collect (fun docId ->
                DocumentPartition.memberNodeIds graph docId
                |> Set.toSeq
                |> Seq.map (fun memberId -> memberId, docId))
            |> resultFold addMembership Map.empty<NodeId, NodeId>
            |> Result.map (fun _ -> graph)

    let private readArtifact
        (graph: Graph)
        (relativePath: string)
        (text: string)
        (docId: NodeId)
        : Result<Graph, string> =
        DocumentFormat.readArtifact relativePath text docId graph

    let private seedNestedRefStubs
        (graph: Graph)
        (seen: Set<string>)
        (queue: (string * NodeId * string option) list)
        (path: string, childId: NodeId)
        : Result<Graph * Set<string> * (string * NodeId * string option) list, string> =
        artifactRelativeForNodeReference path
        |> Result.bind (fun childRel ->
            classifyArtifactRelative childRel
            |> Result.map (fun childDescriptor ->
                let graph' = seedStub graph childDescriptor childId (Some path)
                if Set.contains childRel seen then
                    graph', seen, queue
                else
                    graph', Set.add childRel seen, queue @ [ childRel, childId, Some path ]))

    let rec private assembleLoop
        (artifacts: Map<string, string>)
        (seen: Set<string>)
        (queue: (string * NodeId * string option) list)
        (graph: Graph)
        : Result<Graph, string> =
        match queue with
        | [] -> validateAssembledGraph graph
        | (relativePath, docId, nodeReference) :: rest ->
            match classifyArtifactRelative relativePath with
            | Error msg -> Error msg
            | Ok descriptor ->
                match Map.tryFind relativePath artifacts with
                | None ->
                    let graph' = seedStub graph descriptor docId nodeReference
                    assembleLoop artifacts seen rest graph'
                | Some text ->
                    parseNestedDocRefs text
                    |> Result.bind (fun refs ->
                        refs
                        |> resultFold
                            (fun (graph', seen', queue') ref ->
                                seedNestedRefStubs graph' seen' queue' ref)
                            (graph, seen, rest)
                        |> Result.bind (fun (graph', seen', queue') ->
                            let graph'' = seedStub graph' descriptor docId nodeReference
                            readArtifact graph'' relativePath text docId
                            |> Result.bind (fun graphAfterRead ->
                                assembleLoop artifacts seen' queue' graphAfterRead)))

    let assembleFromArtifacts (artifacts: Map<string, string>) : Result<Graph, string> =
        assembleLoop artifacts (Set.ofList [ ".amb" ]) [ ".amb", Graph.rootId, None ] (Graph.create ())
