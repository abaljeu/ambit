module Gambol.Server.Tests.DocumentLoaderTests

open System
open System.IO
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend
open SpecialNodeTestHelpers

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private writeAmbFiles (dataDir: string) (state: State) =
    DocumentPersistence.writeAllDocuments dataDir state.graph
    |> requireOk "writeAllDocuments"
    |> ignore

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
let ``tryLoadState empty dataDir returns empty graph`` () =
    let dataDir = newTempDir ()
    let loaded = DocumentLoader.tryLoadState dataDir |> requireOk "load"
    Assert.Equal(0, loaded.revision.Value)
    Assert.Equal(0, userNodeCount loaded.graph)

[<Fact>]
let ``tryLoadState reads amb network`` () =
    let dataDir = newTempDir ()
    let state = stateWithRootChild "from-amb"
    writeAmbFiles dataDir state
    let loaded = DocumentLoader.tryLoadState dataDir |> requireOk "load"
    Assert.Equal(0, loaded.revision.Value)
    Assert.True(loaded.graph.nodes.Values |> Seq.exists (fun n -> n.text = "from-amb"))

[<Fact>]
let ``tryLoadState missing amb ref creates stub`` () =
    let dataDir = newTempDir ()
    let state = stateWithRootChild "good"
    writeAmbFiles dataDir state
    let missingId = NodeId.New()
    let ambPath = Path.Combine(dataDir, ".amb")
    let missingRef = "-> ^" + AmbDocument.formatStableId missingId + Environment.NewLine
    File.WriteAllText(ambPath, File.ReadAllText ambPath + missingRef)
    let loaded = DocumentLoader.tryLoadState dataDir |> requireOk "load"
    Assert.Equal("Broken link.", loaded.graph.nodes.[missingId].text)
    Assert.True(loaded.graph.nodes.Values |> Seq.exists (fun n -> n.text = "good"))

[<Fact>]
let ``resolveDbConnection file mode matching amb network returns Ok`` () = task {
    let connStr = requireDbConnStr ()
    let dataDir = newTempDir ()
    do! resetTestDatabase connStr
    DatabaseSetup.resetAgentCacheForTest ()
    let state = stateWithRootChild "aligned"
    writeAmbFiles dataDir state
    do! Database.rebuildFromDocumentFiles connStr { state with revision = Revision 0 }
        |> Async.AwaitTask
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
