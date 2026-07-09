module Gambol.Server.Tests.DocumentPathMoveExecutionTests

open System.IO
open Gambol.Server
open Gambol.Server.Tests.TestBackend
open Gambol.Shared
open Xunit

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private owned (ids: NodeId list) : ChildNode list =
    ids |> List.map (fun id -> { ref = Ownership.Owner; id = id })

let private specialNode (id: NodeId) (kind: SpecialKind) (name: string) (owner: NodeId) : Node =
    { Node.create id with
        text = name
        name = Filename.create name
        owner = owner
        kind = Special kind }

let private normalNode (id: NodeId) (text: string) (owner: NodeId) : Node =
    { Node.create id with
        text = text
        owner = owner }

let private graphWithNestedDocs () : Graph * NodeId * NodeId * NodeId * NodeId =
    let graph0 = Graph.create ()
    let wsId = NodeId.New()
    let dirId = NodeId.New()
    let fileId = NodeId.New()
    let normalId = NodeId.New()

    let graph1 =
        graph0.nodes
        |> Map.add wsId (specialNode wsId Workspace "home" Graph.workspacesId)
        |> Map.add dirId (specialNode dirId Directory "docs" wsId)
        |> Map.add fileId (specialNode fileId File "readme.txt" dirId)
        |> Map.add normalId (normalNode normalId "body" fileId)
        |> fun nodes -> Graph.fromNodes graph0.root nodes

    let graph2 =
        Graph.replace Graph.workspacesId 0 [] (owned [ wsId ]) graph1
        |> requireOk "workspaces->ws"

    let graph3 =
        Graph.replace wsId 0 [] (owned [ dirId ]) graph2
        |> requireOk "ws->dir"

    let graph4 =
        Graph.replace dirId 0 [] (owned [ fileId ]) graph3
        |> requireOk "dir->file"

    let graph5 =
        Graph.replace fileId 0 [] (owned [ normalId ]) graph4
        |> requireOk "file->normal"

    graph5, wsId, dirId, fileId, normalId

[<Fact>]
let ``persistGraphChange moves renamed file artifact before writing`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let postGraph = Graph.setName fileId "readme.txt" "notes.txt" graph |> requireOk "rename file"

    DocumentPersistence.persistGraphChange dataDir graph postGraph
    |> requireOk "persistGraphChange"
    |> ignore

    Assert.False(File.Exists(Path.Combine(dataDir, "home", "docs", "readme.txt")))
    Assert.True(File.Exists(Path.Combine(dataDir, "home", "docs", "notes.txt")))
    DocumentPersistence.readAllDocuments dataDir |> requireOk "read" |> ignore

[<Fact>]
let ``persistGraphChange moves renamed directory tree before writing`` () =
    let dataDir = newTempDir ()
    let graph, _, dirId, _, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let postGraph = Graph.setName dirId "docs" "archive" graph |> requireOk "rename dir"

    DocumentPersistence.persistGraphChange dataDir graph postGraph
    |> requireOk "persistGraphChange"
    |> ignore

    Assert.False(Directory.Exists(Path.Combine(dataDir, "home", "docs")))
    Assert.True(File.Exists(Path.Combine(dataDir, "home", "archive", ".amb")))
    Assert.True(File.Exists(Path.Combine(dataDir, "home", "archive", "readme.txt")))

[<Fact>]
let ``persistGraphChange rejects destination artifact conflict`` () =
    let dataDir = newTempDir ()
    let graph, _, _, fileId, _ = graphWithNestedDocs ()
    DocumentPersistence.writeAllDocuments dataDir graph |> requireOk "write" |> ignore
    let postGraph = Graph.setName fileId "readme.txt" "notes.txt" graph |> requireOk "rename file"
    File.WriteAllText(Path.Combine(dataDir, "home", "docs", "notes.txt"), "occupied")

    match DocumentPersistence.persistGraphChange dataDir graph postGraph with
    | Ok _ -> Assert.Fail("expected destination conflict")
    | Error msg -> Assert.Contains("already exists", msg)

    Assert.True(File.Exists(Path.Combine(dataDir, "home", "docs", "readme.txt")))
    Assert.True(File.Exists(Path.Combine(dataDir, "home", "docs", "notes.txt")))
