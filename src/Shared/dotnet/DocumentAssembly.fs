// assemble a graph from files as source..
namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module DocumentAssembly =

    let private splitSegments (relativePath: string) =
        if String.IsNullOrEmpty relativePath then
            []
        else
            relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
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

    let private isDirectoryArtifact (relativePath: string) =
        // Only `.amb` / `*/.amb` are directory markers; any other path is File
        // (including names that happen to end in `.amb`, e.g. `d/bob/cea.amb`).
        DocumentArtifactPath.isMarker relativePath

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

    let private stubName (relativePath: string) : Filename =
        match splitSegments relativePath |> List.rev with
        | name :: parent :: _ when Filename.isAmbMarkerName name ->
            Filename.create parent
        | [ name ] when Filename.isAmbMarkerName name -> Filename.Empty
        | name :: _ -> Filename.create name
        | [] -> Filename.Empty

    let private stubKind
        (graph: Graph)
        (relativePath: string)
        (documentRootId: NodeId)
        : NodeKind =
        match Map.tryFind documentRootId graph.nodes with
        | Some node when node.kind = NodeKind.Special SpecialKind.Workspace ->
            node.kind
        | _ ->
            if isDirectoryArtifact relativePath then
                NodeKind.Special SpecialKind.Directory
            else
                NodeKind.Special SpecialKind.File

    let private stubNode
        (graph: Graph)
        (relativePath: string)
        (documentRootId: NodeId)
        : Node =
        let kind = stubKind graph relativePath documentRootId
        match Map.tryFind documentRootId graph.nodes with
        | Some node -> { node with kind = kind }
        | None -> Node.Create(documentRootId, name = stubName relativePath, kind = kind)

    let private seedStub (graph: Graph) (relativePath: string) (documentRootId: NodeId) : Graph =
        // Root `.amb` is already present via Graph.create / GraphBuild.ensure; do not replace it.
        if Filename.isAmbMarkerName relativePath then
            graph
        else
            let node = stubNode graph relativePath documentRootId
            let nodes = Map.add documentRootId node graph.nodes
            Graph.fromNodes graph.root nodes

    let private brokenLinkText = "Broken link."

    let private stubMissingRefTargets (graph: Graph) : Graph =
        let missingIds =
            graph.nodes
            |> Map.toSeq
            |> Seq.collect (fun (_, node) -> node.children)
            |> Seq.map (fun child -> child.id)
            |> Seq.distinct
            |> Seq.filter (fun id -> not (Map.containsKey id graph.nodes))
            |> Seq.toList

        match missingIds with
        | [] -> graph
        | ids ->
            let nodes =
                ids
                |> List.fold
                    (fun nodes id ->
                        Map.add id (Node.Create(id, text = brokenLinkText)) nodes)
                    graph.nodes
            Graph.fromNodes graph.root nodes

    let validateAssembledGraph (graph: Graph) : Result<Graph, string> =
        let graph = stubMissingRefTargets graph

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
        DocumentFormat.readArtifactCold relativePath text docId graph

    let private artifactDirectory (relativePath: string) =
        let slash = relativePath.LastIndexOf('/')
        if slash < 0 then "" else relativePath.Substring(0, slash + 1)

    let private tryChildArtifact
        (artifacts: Map<string, string>)
        (baseDirectory: string)
        (node: Node)
        : string option =
        match Filename.tryValue node.name with
        | None -> None
        | Some name ->
            [ baseDirectory + name + "/.amb"; baseDirectory + name ]
            |> List.tryFind (fun relativePath -> Map.containsKey relativePath artifacts)

    let private resolveChildArtifact
        (artifacts: Map<string, string>)
        (baseDirectory: string)
        (graph: Graph)
        (seen: Set<string>)
        (queue: (string * NodeId) list)
        (childId: NodeId)
        : Result<Graph * Set<string> * (string * NodeId) list, string> =
        match Map.tryFind childId graph.nodes with
        | None -> Ok (graph, seen, queue)
        | Some node ->
            match tryChildArtifact artifacts baseDirectory node with
            | None -> Ok (graph, seen, queue)
            | Some childRel when Set.contains childRel seen -> Ok (graph, seen, queue)
            | Some childRel ->
                let graph' = seedStub graph childRel childId
                Ok (graph', Set.add childRel seen, (childRel, childId) :: queue)

    let private resolveChildArtifacts
        (artifacts: Map<string, string>)
        (relativePath: string)
        (documentRootId: NodeId)
        (graph: Graph)
        (seen: Set<string>)
        (queue: (string * NodeId) list)
        =
        DocumentPartition.memberNodeIds graph documentRootId
        |> Set.remove documentRootId
        |> resultFold
            (fun (graph', seen', queue') childId ->
                resolveChildArtifact
                    artifacts
                    (artifactDirectory relativePath)
                    graph'
                    seen'
                    queue'
                    childId)
            (graph, seen, queue)

    let rec private assembleLoop
        (artifacts: Map<string, string>)
        (seen: Set<string>)
        (queue: (string * NodeId) list)
        (graph: Graph)
        : Result<Graph, string> =
        match queue with
        | [] -> validateAssembledGraph graph
        | (relativePath, docId) :: rest ->
            match Map.tryFind relativePath artifacts with
            | None ->
                let graph' = seedStub graph relativePath docId
                assembleLoop artifacts seen rest graph'
            | Some text ->
                let graph' = seedStub graph relativePath docId
                readArtifact graph' relativePath text docId
                |> Result.bind (fun refs ->
                    resolveChildArtifacts
                        artifacts relativePath docId refs seen rest
                    |> Result.bind (fun (graph'', seen', queue') ->
                        assembleLoop artifacts seen' queue' graph''))

    let assembleFromArtifacts (artifacts: Map<string, string>) : Result<Graph, string> =
        assembleLoop artifacts (Set.ofList [ ".amb" ]) [ ".amb", Graph.rootId ] (Graph.create ())
