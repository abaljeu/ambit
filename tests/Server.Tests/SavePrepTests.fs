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
let ``Git DB flush writes artifacts without touching locked log or meta`` () =
    let dataDir = newTempDir ()
    let state = stateWithRootChild "git-artifact"
    let logPath = Path.Combine(dataDir, "gambol.log")
    let metaPath = Path.Combine(dataDir, "gambol.meta")
    File.WriteAllText(logPath, "pending")
    File.WriteAllText(metaPath, "sentinel")

    use lockedLog =
        new FileStream(logPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)

    SavePrep.syncGitArtifacts
        DatabaseSetup.PersistenceMode.Db
        DatabaseSetup.DbStatus.Ok
        (fun () -> async { return encodeState state })
        (fun () -> async { return failwith "file flush should not run" })
        (fun () -> async { return failwith "file revision should not be read" })
        dataDir
    |> Async.RunSynchronously
    |> requireOk "Git flush"
    |> ignore

    let artifactPath = Path.Combine(dataDir, ".amb")
    Assert.True(File.Exists(artifactPath))
    Assert.Contains("git-artifact", File.ReadAllText(artifactPath))
    Assert.Equal("sentinel", File.ReadAllText(metaPath))
    Assert.Equal(int64 "pending".Length, lockedLog.Length)

[<Fact>]
let ``Full DB sync still updates meta and clears log`` () =
    let dataDir = newTempDir ()
    let state = stateWithRootChild "full-backup"
    let logPath = Path.Combine(dataDir, "gambol.log")
    let metaPath = Path.Combine(dataDir, "gambol.meta")
    File.WriteAllText(logPath, "pending")
    File.WriteAllText(metaPath, "stale")

    let revision =
        SavePrep.syncDataDir
            DatabaseSetup.PersistenceMode.Db
            DatabaseSetup.DbStatus.Ok
            (fun () -> async { return encodeState state })
            (fun () -> async { return failwith "file flush should not run" })
            (fun () -> async { return failwith "file revision should not be read" })
            dataDir
            "gambol"
        |> Async.RunSynchronously
        |> requireOk "full sync"

    Assert.Equal(state.revision.Value, revision)
    Assert.Equal(string state.revision.Value, File.ReadAllText(metaPath))
    Assert.Equal("", File.ReadAllText(logPath))
