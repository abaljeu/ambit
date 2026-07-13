module Gambol.Server.Tests.DocumentLoaderTests

open System
open System.IO
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private testFile = "gambol"

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private writeLegacyFiles (dataDir: string) (graph: Graph) (revision: int) (logText: string) =
    let snapshotPath = Path.Combine(dataDir, testFile)
    File.WriteAllText(snapshotPath, Snapshot.write graph)
    File.WriteAllText(snapshotPath + ".meta", string revision)
    File.WriteAllText(snapshotPath + ".log", logText)

let private writeAmbFiles (dataDir: string) (state: State) =
    DocumentPersistence.writeAllDocuments dataDir state.graph |> requireOk "writeAllDocuments" |> ignore
    File.WriteAllText(Path.Combine(dataDir, testFile + ".meta"), string state.revision.Value)
    File.WriteAllText(Path.Combine(dataDir, testFile + ".log"), "")

let private stateWithRootChild (text: string) : State =
    let childId = NodeId.New()
    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, text)
              Op.Replace(Graph.rootId, 0, [], [ { ref = Ownership.Owner; id = childId } ]) ] }

    let initial =
        { graph = Graph.create ()
          history = History.empty
          revision = Revision 0 }

    match History.applyChange change initial with
    | ApplyResult.Changed st -> { st with revision = Revision 1 }
    | _ -> failwith "expected changed state"

[<Fact>]
let ``tryLoadState legacy gambol materializes amb artifacts`` () =
    let dataDir = newTempDir ()
    let state = stateWithRootChild "legacy-child"
    writeLegacyFiles dataDir state.graph state.revision.Value ""
    Assert.False(DocumentPersistence.hasArtifactSet dataDir)
    let loaded = DocumentLoader.tryLoadState dataDir testFile |> requireOk "load"
    Assert.True(DocumentPersistence.hasArtifactSet dataDir)
    Assert.True(File.Exists(Path.Combine(dataDir, ".amb")))
    Assert.Equal(state.revision.Value, loaded.revision.Value)
    let outline =
        Snapshot.normalizeOutlineForCompare (Snapshot.write state.graph)
    let loadedOutline =
        Snapshot.normalizeOutlineForCompare (Snapshot.write loaded.graph)
    Assert.Equal(outline, loadedOutline)

[<Fact>]
let ``tryLoadState reads amb network and replays log`` () =
    let dataDir = newTempDir ()
    let baseState =
        { graph = Graph.create ()
          history = History.empty
          revision = Revision 0 }
    writeAmbFiles dataDir baseState
    let childId = NodeId.New()
    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, "logged-child")
              Op.Replace(Graph.rootId, 0, [], [ { ref = Ownership.Owner; id = childId } ]) ] }

    let logPath = Path.Combine(dataDir, testFile + ".log")
    let line = sprintf "%08d%s%s" change.id (ChangeLog.encodeChange change) Environment.NewLine
    File.AppendAllText(logPath, line)

    let loaded = DocumentLoader.tryLoadState dataDir testFile |> requireOk "load"
    Assert.Equal(1, loaded.revision.Value)
    Assert.True(loaded.graph.nodes.Values |> Seq.exists (fun n -> n.text = "logged-child"))

[<Fact>]
let ``tryLoadState missing amb ref creates stub without legacy fallback`` () =
    let dataDir = newTempDir ()
    let state = stateWithRootChild "good"
    writeAmbFiles dataDir state
    writeLegacyFiles dataDir (Graph.create ()) 0 ""
    let missingId = NodeId.New()
    let ambPath = Path.Combine(dataDir, ".amb")
    let missingRef = "-> ^" + AmbDocument.formatStableId missingId + Environment.NewLine
    File.WriteAllText(ambPath, File.ReadAllText ambPath + missingRef)
    let loaded = DocumentLoader.tryLoadState dataDir testFile |> requireOk "load"
    Assert.Equal("Broken link.", loaded.graph.nodes.[missingId].text)
    Assert.True(loaded.graph.nodes.Values |> Seq.exists (fun n -> n.text = "good"))

[<Fact>]
let ``tryLoadState amb takes precedence over stale monolithic gambol`` () =
    let dataDir = newTempDir ()
    let ambState = stateWithRootChild "from-amb"
    writeAmbFiles dataDir ambState
    let stale = Graph.create ()
    File.WriteAllText(Path.Combine(dataDir, testFile), Snapshot.write stale)
    let loaded = DocumentLoader.tryLoadState dataDir testFile |> requireOk "load"
    Assert.Equal(ambState.revision.Value, loaded.revision.Value)
    let expected =
        Snapshot.normalizeOutlineForCompare (Snapshot.write ambState.graph)
    let actual =
        Snapshot.normalizeOutlineForCompare (Snapshot.write loaded.graph)
    Assert.Equal(expected, actual)

[<Fact>]
let ``writeStateBackup emits amb meta and empty log`` () =
    let dataDir = newTempDir ()
    let state = stateWithRootChild "db-backup"
    DocumentLoader.writeStateBackup dataDir testFile state
    Assert.True(DocumentPersistence.hasArtifactSet dataDir)
    Assert.True(File.Exists(Path.Combine(dataDir, ".amb")))
    Assert.Equal(string state.revision.Value, File.ReadAllText(Path.Combine(dataDir, testFile + ".meta")).Trim())
    Assert.Equal("", File.ReadAllText(Path.Combine(dataDir, testFile + ".log")))

[<Fact>]
let ``resolveDbConnection file mode matching amb network returns Ok`` () = task {
    let connStr = requireDbConnStr ()
    let dataDir = newTempDir ()
    do! resetTestDatabase connStr
    DatabaseSetup.resetAgentCacheForTest ()
    let state = stateWithRootChild "aligned"
    writeAmbFiles dataDir state
    do! Database.rebuildFromDocumentFiles connStr state |> Async.AwaitTask
    let status = DatabaseSetup.resolveDbConnection DatabaseSetup.PersistenceMode.File connStr dataDir
    Assert.Equal(DatabaseSetup.DbStatus.Ok, status)
}

[<Fact>]
let ``resolveDbConnection file mode divergent amb rebuild returns Mismatch1`` () = task {
    let connStr = requireDbConnStr ()
    let dataDir = newTempDir ()
    do! resetTestDatabase connStr
    DatabaseSetup.resetAgentCacheForTest ()
    let dbState = stateWithRootChild "db-only"
    do! Database.rebuildFromDocumentFiles connStr dbState |> Async.AwaitTask
    let fileState = stateWithRootChild "disk-only"
    writeAmbFiles dataDir fileState
    let status = DatabaseSetup.resolveDbConnection DatabaseSetup.PersistenceMode.File connStr dataDir
    Assert.Equal(DatabaseSetup.DbStatus.Mismatch1, status)
}

let private decodeChange (s: string) =
    Thoth.Json.Newtonsoft.Decode.fromString Serialization.decodeChange s

[<Fact>]
let ``resolveDbConnection legacy gambol only skips compare when db nonempty`` () = task {
    let connStr = requireDbConnStr ()
    let dataDir = newTempDir ()
    do! resetTestDatabase connStr
    DatabaseSetup.resetAgentCacheForTest ()
    let dbState = stateWithRootChild "db-only"
    do! Database.rebuildFromDocumentFiles connStr dbState |> Async.AwaitTask
    writeLegacyFiles dataDir (Graph.create ()) 0 ""
    let status = DatabaseSetup.resolveDbConnection DatabaseSetup.PersistenceMode.File connStr dataDir
    Assert.Equal(DatabaseSetup.DbStatus.Ok, status)
    let! dbAfter = Database.loadPersistedState connStr decodeChange |> Async.AwaitTask
    Assert.Equal("db-only", dbAfter.graph.nodes.Values |> Seq.find (fun n -> n.text = "db-only") |> fun n -> n.text)
}
