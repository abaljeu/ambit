module DocumentOpPersistenceTests

open System.IO
open Gambol.Server
open Gambol.Server.Tests.TestBackend
open Gambol.Shared
open Xunit

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error error -> failwith $"{label}: {error}"

let private owned = ChildNode.owners

let private specialNode id kind name owner =
    Node.Create(
        id,
        text = name,
        name = Filename.create name,
        owner = owner,
        kind = Special kind)

let private attach parent ids graph =
    Graph.replace parent 0 [] (owned ids) graph |> requireOk "attach"

let private graphWithTwoFiles () =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let fileAId = NodeId.New()
    let fileBId = NodeId.New()
    let bodyAId = NodeId.New()
    let bodyBId = NodeId.New()
    let graph1 =
        graph0
        |> Graph.addDetachedNode (specialNode wsId Workspace "home" Graph.workspacesId)
        |> Graph.addDetachedNode (specialNode fileAId File "a.txt" wsId)
        |> Graph.addDetachedNode (specialNode fileBId File "b.txt" wsId)
        |> Graph.addDetachedNode (Node.Create(bodyAId, text = "alpha", owner = fileAId))
        |> Graph.addDetachedNode (Node.Create(bodyBId, text = "beta", owner = fileBId))
    let graph =
        graph1
        |> attach Graph.workspacesId [ wsId ]
        |> attach wsId [ fileAId; fileBId ]
        |> attach fileAId [ bodyAId ]
        |> attach fileBId [ bodyBId ]
    graph, fileAId, fileBId, bodyAId, bodyBId

let private artifactPath dataDir graph rootId =
    DocumentPersistPath.resolveArtifactPath dataDir graph rootId
    |> requireOk "resolve artifact path"

[<Fact>]
let ``persistGraphOps writes only roots represented by accepted operations`` () =
    let dataDir = newTempDir ()
    let graph, fileAId, fileBId, bodyAId, bodyBId = graphWithTwoFiles ()
    DocumentPersistWrite.writeAllDocuments dataDir graph
    |> requireOk "initial write"
    |> ignore
    let pathA = artifactPath dataDir graph fileAId
    let pathB = artifactPath dataDir graph fileBId
    let planted = "PLANTED-NOT-IN-OPS"
    File.WriteAllText(pathB, planted)
    let afterA =
        Graph.setText bodyAId "alpha" "ALPHA" graph
        |> requireOk "edit a"
    let post =
        Graph.setText bodyBId "beta" "BETA" afterA
        |> requireOk "edit b"
    let acceptedOps = [ Op.SetText(bodyAId, "alpha", "ALPHA") ]

    DocumentPersistChange.persistGraphOps dataDir graph post acceptedOps
    |> requireOk "persistGraphOps"
    |> ignore

    Assert.Contains("ALPHA", File.ReadAllText pathA)
    Assert.Equal(planted, File.ReadAllText pathB)

[<Fact>]
let ``persistGraphOps soft-fails illicit write and returns could-not-save message`` () =
    let dataDir = newTempDir ()
    let graph0 = Graph.create ()
    let fileId = NodeId.New()
    let bodyId = NodeId.New()
    // Non-allowlisted SYSTEM file: still a writable document root for impact,
    // but writeDocument refuses via SystemDirectoryPersist.
    let fileNode =
        Node.Create(
            fileId,
            text = "secret.txt",
            name = Filename.Ok "secret.txt",
            owner = Graph.systemId,
            kind = Special File)
    let bodyNode = Node.Create(bodyId, text = "body", owner = fileId)
    let graph1 =
        graph0
        |> Graph.addDetachedNode fileNode
        |> Graph.addDetachedNode bodyNode
    let graph =
        graph1
        |> attach Graph.systemId [ fileId ]
        |> attach fileId [ bodyId ]
    let post =
        Graph.setText bodyId "body" "BODY" graph
        |> requireOk "edit"
    let result =
        DocumentPersistChange.persistGraphOps
            dataDir
            graph
            post
            [ Op.SetText(bodyId, "body", "BODY") ]
        |> requireOk "persistGraphOps"
    Assert.Equal(
        Some(DocumentPersistWrite.fileCouldNotSave "SYSTEM/secret.txt"),
        result.message)
    Assert.Equal("BODY", result.graph.nodes.[bodyId].text)
    Assert.Equal(PersistState.Unpersisted, result.graph.nodes.[fileId].persistState)
    let stampOps = PersistStamp.opsBetween post result.graph
    Assert.DoesNotContain(
        stampOps,
        fun op ->
            match op with
            | Op.SetPersistState(id, _, _) when id = fileId -> true
            | _ -> false)

let private workspaceIdOf (graph: Graph) =
    graph.nodes
    |> Map.pick (fun id node ->
        if node.kind = Special Workspace then Some id else None)

[<Fact>]
let ``successful persistGraphOps marks the written special Persisted`` () =
    let dataDir = newTempDir ()
    let built, fileAId, fileBId, bodyAId, _ = graphWithTwoFiles ()
    let wsId = workspaceIdOf built
    let clean id graph =
        Graph.setPersistState id PersistState.Persisted graph |> requireOk "clean"
    let graph = built |> clean fileAId |> clean fileBId |> clean wsId
    DocumentPersistWrite.writeAllDocuments dataDir graph
    |> requireOk "initial write"
    |> ignore
    let edited =
        Graph.setText bodyAId "alpha" "ALPHA" graph
        |> requireOk "edit"
    Assert.Equal(PersistState.Unpersisted, edited.nodes.[fileAId].persistState)
    let dirty =
        Graph.setPersistState wsId PersistState.Unpersisted edited
        |> requireOk "dirty workspace"
    let result =
        DocumentPersistChange.persistGraphOps
            dataDir
            graph
            dirty
            [ Op.SetText(bodyAId, "alpha", "ALPHA") ]
        |> requireOk "persistGraphOps"
    Assert.Equal(PersistState.Persisted, result.graph.nodes.[fileAId].persistState)
    Assert.Equal(ParseState.Parsed, result.graph.nodes.[fileAId].parseState)
    Assert.Equal(PersistState.Persisted, result.graph.nodes.[fileBId].persistState)
    Assert.Equal(PersistState.Unpersisted, result.graph.nodes.[wsId].persistState)
    let stampOps = PersistStamp.opsBetween dirty result.graph
    Assert.Contains(
        stampOps,
        fun op ->
            match op with
            | Op.SetPersistState(
                id, PersistState.Unpersisted, PersistState.Persisted)
                when id = fileAId -> true
            | _ -> false)
    match Op.applyAll stampOps { graph = dirty; eventId = EventId.zero } with
    | ApplyResult.Changed state ->
        Assert.Equal(PersistState.Persisted, state.graph.nodes.[fileAId].persistState)
        Assert.Equal(PersistState.Unpersisted, state.graph.nodes.[wsId].persistState)
    | other -> failwith $"expected stamp ops to apply, got {other}"
