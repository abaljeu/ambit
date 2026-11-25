module Gambol.Server.Tests.DatabaseSetupTests

open System
open System.IO
open Xunit
open Gambol.Server
open Gambol.Server.Tests.TestBackend
open Gambol.Shared
open DocumentPersistenceTests.Fixtures

let private withNodeText (nodeId: NodeId) (text: string) (graph: Graph) : Graph =
    let node = graph.nodes.[nodeId]
    Graph.fromNodes graph.root (Map.add nodeId { node with text = text } graph.nodes) graph.childMap

/// A workspace note is a plain outline node (not a `File`), so its text is
/// part of the persisted outline and a text edit shows up as an outline diff.
let private graphWithWorkspaceNote (text: string) : Graph * NodeId =
    let wsId = NodeId.New()
    let noteId = NodeId.New()
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode (
            Node.Create(
                wsId,
                text = "home",
                name = Filename.create "home",
                owner = Graph.workspacesId,
                kind = Special Workspace))
        |> Graph.addDetachedNode (Node.Create(noteId, text = text, owner = wsId))
    let graph =
        Graph.replace Graph.workspacesId 0 [] (ChildNode.owners [ wsId ]) graph
        |> requireOk "workspaces->ws"
    let graph =
        Graph.replace wsId 0 [] (ChildNode.owners [ noteId ]) graph
        |> requireOk "ws->note"
    graph, noteId

[<Fact>]
let ``statusFromMatches returns Ok when initial state matches`` () =
    let status = DatabaseSetup.statusFromMatches true false
    Assert.Equal(DatabaseSetup.DbStatus.Ok, status)

[<Fact>]
let ``statusFromMatches returns Mismatch1 when rebuild fixes mismatch`` () =
    let status = DatabaseSetup.statusFromMatches false true
    Assert.Equal(DatabaseSetup.DbStatus.Mismatch1, status)

[<Fact>]
let ``statusFromMatches returns Mismatch2 when rebuild does not fix mismatch`` () =
    let status = DatabaseSetup.statusFromMatches false false
    Assert.Equal(DatabaseSetup.DbStatus.Mismatch2, status)

[<Fact>]
let ``documentStatesMatch treats two outline reads as same when revision matches`` () =
    let outline = "\tSame text each load\n"
    let g1 = Snapshot.read outline
    let g2 = Snapshot.read outline
    Assert.False(GraphProjection.graphEquals g1 g2)

    let st1 =
        { graph = g1
          eventId = EventId.zero }

    let st2 =
        { graph = g2
          eventId = EventId.zero }

    Assert.True(DatabaseSetup.documentStatesMatch st1 st2)

[<Fact>]
let ``resolveConnectionString prefers TEST_DB_CONNECTION_STRING`` () =
    let priorTest = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING")
    let priorMain = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")

    try
        Environment.SetEnvironmentVariable("TEST_DB_CONNECTION_STRING", "test-conn")
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", "main-conn")

        let result = DatabaseSetup.resolveConnectionString ()
        Assert.Equal("test-conn", result)
    finally
        Environment.SetEnvironmentVariable("TEST_DB_CONNECTION_STRING", priorTest)
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", priorMain)

[<Fact>]
let ``resolveConnectionString falls back to DB_CONNECTION_STRING`` () =
    let priorTest = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING")
    let priorMain = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")

    try
        Environment.SetEnvironmentVariable("TEST_DB_CONNECTION_STRING", null)
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", "main-conn")

        let result = DatabaseSetup.resolveConnectionString ()
        Assert.Equal("main-conn", result)
    finally
        Environment.SetEnvironmentVariable("TEST_DB_CONNECTION_STRING", priorTest)
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", priorMain)

[<Fact>]
let ``resolveDbConnection in File mode logs an outline mismatch when disk and DB diverge`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let dataDir = newTempDir ()

    let graph1, noteId = graphWithWorkspaceNote "original note"
    DocumentPersistWrite.writeAllDocuments dataDir graph1 |> requireOk "write1" |> ignore
    let bootstrapStatus =
        DatabaseSetup.resolveDbConnection DatabaseSetup.PersistenceMode.File connStr dataDir
    Assert.Equal(DatabaseSetup.DbStatus.Ok, bootstrapStatus)

    // Rewrite disk with different note text at the same path/ids; the DB
    // still holds the bootstrapped state, so the next resolve sees a mismatch.
    let changedGraph = withNodeText noteId "changed note" graph1
    DocumentPersistWrite.writeAllDocuments dataDir changedGraph |> requireOk "write2" |> ignore

    let status =
        DatabaseSetup.resolveDbConnection DatabaseSetup.PersistenceMode.File connStr dataDir
    Assert.Equal(DatabaseSetup.DbStatus.Mismatch1, status)

    let text = File.ReadAllText(ErrorLog.logPath dataDir)
    Assert.Contains("STARTUP-FALLBACK", text)
    Assert.Contains("DatabaseSetup.validateAmbNetworkAgainstDb", text)
    Assert.Contains("outline mismatch", text)
}

[<Fact>]
let ``resolveConnectionString returns empty when neither env var is set`` () =
    let priorTest = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING")
    let priorMain = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")

    try
        Environment.SetEnvironmentVariable("TEST_DB_CONNECTION_STRING", null)
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", null)

        let result = DatabaseSetup.resolveConnectionString ()
        Assert.Equal("", result)
    finally
        Environment.SetEnvironmentVariable("TEST_DB_CONNECTION_STRING", priorTest)
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", priorMain)
