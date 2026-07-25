module Gambol.Server.Tests.SavePrepTests

open System
open System.IO
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Encode = Thoth.Json.Newtonsoft.Encode

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
    | ApplyResult.Changed state -> { state with revision = Revision 1 }
    | _ -> failwith "expected changed state"

let private encodeState (state: State) =
    Encode.toString 0 (
        Thoth.Json.Core.Encode.object
            [ "revision", Serialization.encodeRevision state.revision
              "graph", Serialization.encodeGraph state.graph ])

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

[<Fact>]
let ``Git DB flush returns revision without rewriting disk`` () =
    let dataDir = newTempDir ()
    let state = stateWithRootChild "git-artifact"
    Directory.CreateDirectory(Bookkeeping.systemDir dataDir) |> ignore
    File.WriteAllText(Bookkeeping.logPath dataDir, "pending")
    File.WriteAllText(Bookkeeping.metaPath dataDir, "sentinel")

    use lockedLog =
        new FileStream(
            Bookkeeping.logPath dataDir,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None)

    let revision =
        SavePrep.syncGitArtifacts
            DatabaseSetup.PersistenceMode.Db
            DatabaseSetup.DbStatus.Ok
            (fun () -> async { return encodeState state })
            (fun () -> async { return failwith "file flush should not run" })
            (fun () -> async { return failwith "file revision should not be read" })
            dataDir
        |> Async.RunSynchronously
        |> requireOk "Git flush"

    Assert.Equal(state.revision.Value, revision)
    Assert.False(File.Exists(Path.Combine(dataDir, ".amb")))
    Assert.Equal("sentinel", File.ReadAllText(Bookkeeping.metaPath dataDir))
    Assert.Equal(int64 "pending".Length, lockedLog.Length)

[<Fact>]
let ``Full DB sync returns revision without rewriting disk`` () =
    let dataDir = newTempDir ()
    let state = stateWithRootChild "full-backup"
    Directory.CreateDirectory(Bookkeeping.systemDir dataDir) |> ignore
    File.WriteAllText(Bookkeeping.logPath dataDir, "pending")
    File.WriteAllText(Bookkeeping.metaPath dataDir, "sentinel")

    use lockedLog =
        new FileStream(
            Bookkeeping.logPath dataDir,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None)

    let revision =
        SavePrep.syncDataDir
            DatabaseSetup.PersistenceMode.Db
            DatabaseSetup.DbStatus.Ok
            (fun () -> async { return encodeState state })
            (fun () -> async { return failwith "file flush should not run" })
            (fun () -> async { return failwith "file revision should not be read" })
            dataDir
        |> Async.RunSynchronously
        |> requireOk "full sync"

    Assert.Equal(state.revision.Value, revision)
    Assert.False(File.Exists(Path.Combine(dataDir, ".amb")))
    Assert.Equal("sentinel", File.ReadAllText(Bookkeeping.metaPath dataDir))
    Assert.Equal(int64 "pending".Length, lockedLog.Length)
