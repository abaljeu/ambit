module Gambol.Server.Tests.ParseThreadLoadTests

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

let private seedDocsFile host =
    task {
        let! state0 =
            CoreMailbox.getState host |> Async.StartAsTask
        let state0 = requireOk "state0" state0
        let workspaceId, wsOps =
            FileNodeOps.planCreateWorkspace state0.graph "home"
        postOps host wsOps
        let! state1 =
            CoreMailbox.getState host |> Async.StartAsTask
        let state1 = requireOk "state1" state1
        let dirId, dirOps =
            FileNodeOps.planCreateOwnedDirectory
                state1.graph
                workspaceId
                "docs"
        postOps host dirOps
        let! state2 =
            CoreMailbox.getState host |> Async.StartAsTask
        let state2 = requireOk "state2" state2
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                state2.graph
                dirId
                "note.txt"
        postOps host fileOps
        return fileId, dirId
    }

let private graphHasText host fileId text =
    match CoreMailbox.getState host |> Async.RunSynchronously with
    | Error _ -> false
    | Ok state ->
        Graph.children state.graph fileId
        |> List.exists (fun child ->
            state.graph.nodes.[child.id].text = text)

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
let ``ParseThread loop runs planParseFile for stacked File`` () =
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
        ParseThread.start
            { dataDir = dataDir
              consumer = consumer
              push = push
              getGraph = ParseThread.graphFromHost host
              postOps = ParseThread.postParseOps parseHandle
              markUnparsed = ignore
              finishParse =
                fun nodeId ->
                    CoreMailbox.addInMsg
                        host
                        (InMsg.ParseFinished nodeId) }
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

[<Fact>]
let ``disk-newer file text lands after directory reconcile`` () =
    task {
        let dataDir = newTempDir ()
        let push, consumer = ParseStack.create ()
        let host = CoreMailbox.createFile dataDir admittedCredentials
        let parseHandle = CoreMailbox.coreChanges host testCaller
        ParseThread.start
            { dataDir = dataDir
              consumer = consumer
              push = push
              getGraph = ParseThread.graphFromHost host
              postOps = ParseThread.postParseOps parseHandle
              markUnparsed =
                fun nodeId ->
                    CoreMailbox.addInMsg
                        host
                        (InMsg.MarkUnparsed nodeId)
              finishParse =
                fun nodeId ->
                    CoreMailbox.addInMsg
                        host
                        (InMsg.ParseFinished nodeId) }
        try
            let! fileId, dirId = seedDocsFile host
            CoreMailbox.addInMsg host (InMsg.ParseFinished fileId)
            let! parsed =
                waitUntil
                    (fun () ->
                        match
                            CoreMailbox.getState host
                            |> Async.RunSynchronously
                        with
                        | Error _ -> false
                        | Ok state ->
                            let node = state.graph.nodes.[fileId]
                            node.parseState = ParseState.Parsed)
                    2000
            Assert.True(parsed, "file should start Parsed")
            let old =
                DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            postOps
                host
                [ Op.SetUpdateTime(fileId, DateTime.MinValue, old) ]
            let diskPath =
                Path.Combine(dataDir, "home", "docs", "note.txt")
            Directory.CreateDirectory(
                Path.GetDirectoryName diskPath)
            |> ignore
            File.WriteAllText(diskPath, "NEWER\n")
            File.SetLastWriteTimeUtc(diskPath, DateTime.UtcNow)
            push dirId
            let! landed =
                waitUntil
                    (fun () -> graphHasText host fileId "NEWER")
                    2000
            Assert.True(
                landed,
                "disk-newer file should parse into the graph")
        finally
            CoreMailbox.dispose host
    }
