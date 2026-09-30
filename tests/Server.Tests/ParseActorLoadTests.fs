module Gambol.Server.Tests.ParseActorLoadTests

open System
open System.IO
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private waitUntil pred timeoutMs =
    task {
        let start = DateTime.UtcNow
        while
            not (pred ())
            && (DateTime.UtcNow - start).TotalMilliseconds
               < float timeoutMs do
            do! Task.Delay 10
        return pred ()
    }

let private postOps host ops =
    let event = GraphOnlyChangePost.mint "seed" ops
    CoreMailbox.postGraphOnly host testCaller event
    |> Async.RunSynchronously
    |> requireOk "seed"
    |> ignore

let private seedWorkspaceFile host label fileName =
    task {
        let! state0 =
            CoreMailbox.getState host |> Async.StartAsTask
        let state0 = requireOk "state0" state0
        let workspaceId, wsOps =
            FileNodeOps.planCreateWorkspace state0.graph label
        postOps host wsOps
        let! state1 =
            CoreMailbox.getState host |> Async.StartAsTask
        let state1 = requireOk "state1" state1
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                state1.graph
                workspaceId
                fileName
        postOps host fileOps
        return fileId, workspaceId
    }

[<Fact>]
let ``ParseStack push then consumer is LIFO`` () =
    let push, consumer = ParseStack.create ()
    let a = NodeId.New()
    let b = NodeId.New()
    push a
    push b
    Assert.Equal(b, consumer ())
    Assert.Equal(a, consumer ())

[<Fact>]
let ``mailbox Load of File node pushes onto Parse stack`` () =
    task {
        let dataDir = newTempDir ()
        let push, consumer = ParseStack.create ()
        let host =
            CoreMailbox.hostWithParsePush
                (CoreActorPool.create ())
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
                push
        try
            let! fileId, _ =
                seedWorkspaceFile host "home" "note.txt"
            let! loaded =
                CoreMailbox.load host testCaller fileId
                |> Async.StartAsTask
            requireOk "Load" loaded
            Assert.Equal(fileId, consumer ())
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``mailbox Load refuses non-File subject`` () =
    task {
        let dataDir = newTempDir ()
        let pushed = ResizeArray<NodeId>()
        let host =
            CoreMailbox.hostWithParsePush
                (CoreActorPool.create ())
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
                pushed.Add
        try
            let! state0 =
                CoreMailbox.getState host |> Async.StartAsTask
            let state0 = requireOk "state0" state0
            let workspaceId, wsOps =
                FileNodeOps.planCreateWorkspace state0.graph "home"
            postOps host wsOps
            let! loaded =
                CoreMailbox.load host testCaller workspaceId
                |> Async.StartAsTask
            Assert.Equal(
                Error "Load subject is not a File node",
                loaded)
            Assert.Empty(pushed)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``Parse actor loop runs planParseFile for stacked File`` () =
    task {
        let dataDir = newTempDir ()
        let push, consumer = ParseStack.create ()
        let host =
            CoreMailbox.hostWithParsePush
                (CoreActorPool.create ())
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
                push
        let parseHandle =
            CoreMailbox.coreChanges host testCaller
        ParseActor.start
            { dataDir = dataDir
              consumer = consumer
              getGraph = ParseActor.graphFromHost host
              postOps = ParseActor.postParseOps parseHandle }
        try
            let! fileId, _ =
                seedWorkspaceFile host "home" "note.txt"
            let diskPath =
                Path.Combine(dataDir, "home", "note.txt")
            Directory.CreateDirectory(
                Path.GetDirectoryName diskPath)
            |> ignore
            File.WriteAllText(diskPath, "HELLO\n")
            let! loaded =
                CoreMailbox.load host testCaller fileId
                |> Async.StartAsTask
            requireOk "Load" loaded
            let! parsed =
                waitUntil
                    (fun () ->
                        match
                            CoreMailbox.getState host
                            |> Async.RunSynchronously
                        with
                        | Error _ -> false
                        | Ok state ->
                            Graph.children state.graph fileId
                            |> List.exists (fun c ->
                                let text =
                                    state.graph.nodes.[c.id].text
                                text = "HELLO"))
                    2000
            Assert.True(parsed, "parse loop should apply HELLO")
        finally
            CoreMailbox.dispose host
    }
