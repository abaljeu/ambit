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

let private owned ids =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

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
    let nodes =
        graph0.nodes
        |> Map.add wsId (specialNode wsId Workspace "home" Graph.workspacesId)
        |> Map.add fileAId (specialNode fileAId File "a.txt" wsId)
        |> Map.add fileBId (specialNode fileBId File "b.txt" wsId)
        |> Map.add bodyAId (Node.Create(bodyAId, text = "alpha", owner = fileAId))
        |> Map.add bodyBId (Node.Create(bodyBId, text = "beta", owner = fileBId))
    let graph1 = Graph.fromNodes graph0.root nodes
    let graph =
        graph1
        |> attach Graph.workspacesId [ wsId ]
        |> attach wsId [ fileAId; fileBId ]
        |> attach fileAId [ bodyAId ]
        |> attach fileBId [ bodyBId ]
    graph, fileAId, fileBId, bodyAId, bodyBId

let private artifactPath dataDir graph rootId =
    DocumentPersistence.resolveArtifactPath dataDir graph rootId
    |> requireOk "resolve artifact path"

[<Fact>]
let ``persistGraphOps writes only roots represented by accepted operations`` () =
    let dataDir = newTempDir ()
    let graph, fileAId, fileBId, bodyAId, bodyBId = graphWithTwoFiles ()
    DocumentPersistence.writeAllDocuments dataDir graph
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

    DocumentPersistence.persistGraphOps dataDir graph post acceptedOps
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
    let nodes =
        graph0.nodes
        |> Map.add fileId fileNode
        |> Map.add bodyId bodyNode
    let graph1 = Graph.fromNodes graph0.root nodes
    let graph =
        graph1
        |> attach Graph.systemId [ fileId ]
        |> attach fileId [ bodyId ]
    let post =
        Graph.setText bodyId "body" "BODY" graph
        |> requireOk "edit"
    let result =
        DocumentPersistence.persistGraphOps
            dataDir
            graph
            post
            [ Op.SetText(bodyId, "body", "BODY") ]
        |> requireOk "persistGraphOps"
    Assert.Equal(
        Some(DocumentPersistence.fileCouldNotSave "SYSTEM/secret.txt"),
        result.message)
    Assert.Equal("BODY", result.graph.nodes.[bodyId].text)
