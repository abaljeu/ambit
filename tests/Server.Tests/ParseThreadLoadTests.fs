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
                CoreMailbox.mbLoadNode host testCaller fileId
                |> Async.StartAsTask
            requireOk "Load" loaded
            Assert.Equal(fileId, consumer ())
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``mailbox Load of a Workspace pushes onto the Parse stack`` () =
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
            let! _, workspaceId =
                seedWorkspaceFile host "home" "note.txt"
            let! loaded =
                CoreMailbox.mbLoadNode host testCaller workspaceId
                |> Async.StartAsTask
            requireOk "Load" loaded
            Assert.Equal<NodeId list>([ workspaceId ], pushed |> Seq.toList)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``mailbox Load refuses a node that is not a Workspace, Directory, or File`` () =
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
            let! loaded =
                CoreMailbox.mbLoadNode host testCaller Graph.workspacesId
                |> Async.StartAsTask
            Assert.Equal(
                Error
                    "Load subject is not a Workspace, Directory, or File node",
                loaded)
            Assert.Empty(pushed)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``mailbox Load of a Directory pushes onto the Parse stack`` () =
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
            let! _, dirId = seedDocsFile host
            let! loaded =
                CoreMailbox.mbLoadNode host testCaller dirId
                |> Async.StartAsTask
            requireOk "Load" loaded
            Assert.Equal<NodeId list>([ dirId ], pushed |> Seq.toList)
        finally
            CoreMailbox.dispose host
    }

let private deskLoadRequest (nodeId: NodeId) =
    { operation = LoadSaveOperation.Load
      prePick = LoadSavePrePick.Desk
      start =
        { zoomId = nodeId
          focusId = nodeId
          commandId = nodeId
          graphIds = [ nodeId ]
          eventId = EventId.zero } }

[<Fact>]
let ``Desk Load pushes a Workspace`` () =
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
            let! fileId, workspaceId =
                seedWorkspaceFile host "home" "note.txt"
            let! loaded =
                CoreMailbox.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    (deskLoadRequest workspaceId)
                |> Async.StartAsTask
            requireOk "workspace" loaded
            Assert.Equal<NodeId list>(
                [ workspaceId ],
                pushed |> Seq.toList)
            Assert.DoesNotContain(fileId, pushed)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``Desk Load command pushes a Directory and a File`` () =
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
            let! fileId, dirId = seedDocsFile host
            let! directory =
                CoreMailbox.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    (deskLoadRequest dirId)
                |> Async.StartAsTask
            requireOk "directory" directory
            let! file =
                CoreMailbox.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    (deskLoadRequest fileId)
                |> Async.StartAsTask
            requireOk "file" file
            Assert.Equal<NodeId list>(
                [ dirId; fileId ],
                pushed |> Seq.toList)
        finally
            CoreMailbox.dispose host
    }

let private plainLoadRequest (nodeId: NodeId) (prePick: LoadSavePrePick) =
    { deskLoadRequest nodeId with prePick = prePick }

[<Fact>]
let ``plain Load of a File or Directory ignores a Git path`` () =
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
            let! fileId, dirId = seedDocsFile host
            let! directory =
                CoreMailbox.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Git
                    (PeerActorName "unregistered")
                    (plainLoadRequest dirId LoadSavePrePick.Plain)
                |> Async.StartAsTask
            requireOk "directory" directory
            let! file =
                CoreMailbox.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Git
                    (PeerActorName "unregistered")
                    (plainLoadRequest fileId LoadSavePrePick.Plain)
                |> Async.StartAsTask
            requireOk "file" file
            Assert.Equal<NodeId list>(
                [ dirId; fileId ],
                pushed |> Seq.toList)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``plain Load of a Workspace still requires the git actor`` () =
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
            let! _, workspaceId =
                seedWorkspaceFile host "home" "note.txt"
            let! loaded =
                CoreMailbox.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Git
                    (PeerActorName "unregistered")
                    (plainLoadRequest
                        workspaceId
                        LoadSavePrePick.Plain)
                |> Async.StartAsTask
            Assert.Equal(
                Error "Peer Actor 'unregistered' not registered",
                loaded)
            Assert.Empty(pushed)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``explicit git Load of a File still requires the git actor`` () =
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
            let! fileId, _ =
                seedWorkspaceFile host "home" "note.txt"
            let! loaded =
                CoreMailbox.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Git
                    (PeerActorName "unregistered")
                    (plainLoadRequest fileId LoadSavePrePick.Git)
                |> Async.StartAsTask
            Assert.Equal(
                Error "Peer Actor 'unregistered' not registered",
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
                CoreMailbox.mbLoadNode host testCaller fileId
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

// requeueOnUnparsed is false in ParseThread. A disk-newer file is still
// marked Unparsed. It is not added to the Parse queue, so its text does
// not land. Queue processing is currently wrong. It caused a
// whole-workspace reconcile that exceeded the Azure Free plan CPU quota:
// 60% short-window, 5% daily average. This is not a decision to drop the
// requeue step. Re-enable once queue processing is fixed (pending
// github-transport Load ticket: move Load onto a paced Parse thread).
[<Fact>]
let ``disk-newer file stays Unparsed and is not requeued`` () =
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
            let! marked =
                waitUntil
                    (fun () ->
                        match
                            CoreMailbox.getState host
                            |> Async.RunSynchronously
                        with
                        | Error _ -> false
                        | Ok state ->
                            let node = state.graph.nodes.[fileId]
                            node.parseState = ParseState.Unparsed)
                    2000
            Assert.True(marked, "disk-newer file should be Unparsed")
            do! Task.Delay 400
            let stillUnparsed =
                match
                    CoreMailbox.getState host
                    |> Async.RunSynchronously
                with
                | Error _ -> false
                | Ok state ->
                    let node = state.graph.nodes.[fileId]
                    node.parseState = ParseState.Unparsed
            Assert.True(
                stillUnparsed,
                "requeue is disabled; file stays Unparsed")
            Assert.False(
                graphHasText host fileId "NEWER",
                "requeue is disabled; disk text is not parsed")
        finally
            CoreMailbox.dispose host
    }

let private ownedNames (host: MailboxHost) (parentId: NodeId) =
    match CoreMailbox.getState host |> Async.RunSynchronously with
    | Error _ -> []
    | Ok state ->
        Graph.children state.graph parentId
        |> List.choose (fun child ->
            Filename.tryValue state.graph.nodes.[child.id].name)

[<Fact>]
let ``Directory Load push reconciles when the parse thread pops it`` () =
    task {
        let dataDir = newTempDir ()
        let push, consumer = ParseStack.create ()
        let host =
            CoreMailbox.hostWithParsePush
                (CoreActorPool.create ())
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
                push
        let parseHandle = CoreMailbox.coreChanges host testCaller
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
            let! _, dirId = seedDocsFile host
            let extra =
                Path.Combine(dataDir, "home", "docs", "extra.txt")
            Directory.CreateDirectory(Path.GetDirectoryName extra)
            |> ignore
            File.WriteAllText(extra, "EXTRA\n")
            let! started =
                CoreMailbox.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    (deskLoadRequest dirId)
                |> Async.StartAsTask
            requireOk "directory" started
            let! created =
                waitUntil
                    (fun () ->
                        List.contains "extra.txt" (ownedNames host dirId))
                    2000
            Assert.True(
                created,
                "popped directory should reconcile extra.txt")
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``File Load push parses when the parse thread pops it`` () =
    task {
        let dataDir = newTempDir ()
        let push, consumer = ParseStack.create ()
        let host =
            CoreMailbox.hostWithParsePush
                (CoreActorPool.create ())
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
                push
        let parseHandle = CoreMailbox.coreChanges host testCaller
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
            let! fileId, _ = seedWorkspaceFile host "home" "note.txt"
            let diskPath = Path.Combine(dataDir, "home", "note.txt")
            Directory.CreateDirectory(Path.GetDirectoryName diskPath)
            |> ignore
            File.WriteAllText(diskPath, "HELLO\n")
            let! started =
                CoreMailbox.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    (deskLoadRequest fileId)
                |> Async.StartAsTask
            requireOk "file" started
            let! parsed =
                waitUntil
                    (fun () -> graphHasText host fileId "HELLO")
                    2000
            Assert.True(parsed, "popped file should parse HELLO")
        finally
            CoreMailbox.dispose host
    }

let private parseStateOf host nodeId =
    match CoreMailbox.getState host |> Async.RunSynchronously with
    | Error _ -> None
    | Ok state ->
        Map.tryFind nodeId state.graph.nodes
        |> Option.map (fun node -> node.parseState)

let private childIdNamed host parentId name =
    match CoreMailbox.getState host |> Async.RunSynchronously with
    | Error _ -> None
    | Ok state ->
        Graph.children state.graph parentId
        |> List.tryFind (fun child ->
            Filename.tryValue state.graph.nodes.[child.id].name = Some name)
        |> Option.map (fun child -> child.id)

[<Fact>]
let ``second Load of a Parsed File is parsed again`` () =
    task {
        let dataDir = newTempDir ()
        let push, consumer = ParseStack.create ()
        let host =
            CoreMailbox.hostWithParsePush
                (CoreActorPool.create ())
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
                push
        let parseHandle = CoreMailbox.coreChanges host testCaller
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
            let! fileId, _ = seedWorkspaceFile host "home" "note.txt"
            let diskPath = Path.Combine(dataDir, "home", "note.txt")
            Directory.CreateDirectory(Path.GetDirectoryName diskPath)
            |> ignore
            File.WriteAllText(diskPath, "HELLO\n")
            let! first =
                CoreMailbox.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    (deskLoadRequest fileId)
                |> Async.StartAsTask
            requireOk "first" first
            let! parsed =
                waitUntil
                    (fun () ->
                        graphHasText host fileId "HELLO"
                        && parseStateOf host fileId = Some ParseState.Parsed)
                    2000
            Assert.True(parsed, "first Load should parse HELLO")
            File.WriteAllText(diskPath, "AGAIN\n")
            let! second =
                CoreMailbox.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    (deskLoadRequest fileId)
                |> Async.StartAsTask
            requireOk "second" second
            let! again =
                waitUntil
                    (fun () -> graphHasText host fileId "AGAIN")
                    2000
            Assert.True(again, "second Load should parse AGAIN")
        finally
            CoreMailbox.dispose host
    }

let private applyEvents (state: State) (events: Ev list) : State =
    List.fold
        (fun current event ->
            match Ev.apply event current with
            | ApplyResult.Changed next -> next
            | ApplyResult.Unchanged next -> next
            | ApplyResult.Invalid (_, message) ->
                failwith message)
        state
        events

let private childText (graph: Graph) (fileId: NodeId) (text: string) =
    Graph.children graph fileId
    |> List.exists (fun child -> graph.nodes.[child.id].text = text)

[<Fact>]
let ``second Load of a Current File lands its parse Event on a client that still holds Current`` () =
    task {
        let dataDir = newTempDir ()
        let push, consumer = ParseStack.create ()
        let host =
            CoreMailbox.hostWithParsePush
                (CoreActorPool.create ())
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
                push
        let parseHandle = CoreMailbox.coreChanges host testCaller
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
            let! fileId, _ = seedWorkspaceFile host "home" "note.txt"
            let diskPath = Path.Combine(dataDir, "home", "note.txt")
            Directory.CreateDirectory(Path.GetDirectoryName diskPath)
            |> ignore
            File.WriteAllText(diskPath, "HELLO\n")
            let! first =
                CoreMailbox.startLoadSaveCommand
                    host testCaller LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    (deskLoadRequest fileId)
                |> Async.StartAsTask
            requireOk "first" first
            let! ready =
                waitUntil
                    (fun () ->
                        graphHasText host fileId "HELLO"
                        && parseStateOf host fileId = Some ParseState.Parsed)
                    2000
            Assert.True(ready, "first Load should parse HELLO")
            let! heldResult =
                CoreMailbox.getState host |> Async.StartAsTask
            let held = requireOk "held" heldResult
            Assert.Equal(Current, held.graph.nodes.[fileId].documentState)
            File.WriteAllText(diskPath, "AGAIN\n")
            let! second =
                CoreMailbox.startLoadSaveCommand
                    host testCaller LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    (deskLoadRequest fileId)
                |> Async.StartAsTask
            requireOk "second" second
            let! parsed =
                waitUntil
                    (fun () -> graphHasText host fileId "AGAIN")
                    2000
            Assert.True(parsed, "second Load should parse AGAIN")
            let! events =
                CoreMailbox.getEventsSince host held.eventId
                |> Async.StartAsTask
            let landed = applyEvents held events
            Assert.True(childText landed.graph fileId "AGAIN")
            Assert.Equal(Current, landed.graph.nodes.[fileId].documentState)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``second Load of a Parsed Directory reconciles and does not requeue`` () =
    task {
        let dataDir = newTempDir ()
        let push, consumer = ParseStack.create ()
        let host =
            CoreMailbox.hostWithParsePush
                (CoreActorPool.create ())
                (FileAgent.persist (FileAgent.create dataDir))
                admittedCredentials
                push
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
            let! _, dirId = seedDocsFile host
            let! first =
                CoreMailbox.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    (deskLoadRequest dirId)
                |> Async.StartAsTask
            requireOk "first" first
            let! parsed =
                waitUntil
                    (fun () ->
                        parseStateOf host dirId = Some ParseState.Parsed)
                    2000
            Assert.True(parsed, "first Load should mark the directory Parsed")
            let extra = Path.Combine(dataDir, "home", "docs", "extra.txt")
            Directory.CreateDirectory(Path.GetDirectoryName extra)
            |> ignore
            File.WriteAllText(extra, "EXTRA\n")
            let! second =
                CoreMailbox.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    (deskLoadRequest dirId)
                |> Async.StartAsTask
            requireOk "second" second
            let! created =
                waitUntil
                    (fun () -> childIdNamed host dirId "extra.txt" |> Option.isSome)
                    2000
            Assert.True(created, "second Load should reconcile extra.txt")
            do! Task.Delay 400
            match childIdNamed host dirId "extra.txt" with
            | None -> Assert.Fail("extra.txt missing")
            | Some extraId ->
                Assert.Equal(Some ParseState.Unparsed, parseStateOf host extraId)
                Assert.False(graphHasText host extraId "EXTRA")
        finally
            CoreMailbox.dispose host
    }

let private openParse (dataDir: string) =
    let push, consumer = ParseStack.create ()
    let host =
        CoreMailbox.hostWithParsePush
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create dataDir))
            admittedCredentials
            push
    let parseHandle = CoreMailbox.coreChanges host testCaller
    ParseThread.start
        { dataDir = dataDir
          consumer = consumer
          push = push
          getGraph = ParseThread.graphFromHost host
          postOps = ParseThread.postParseOps parseHandle
          markUnparsed =
            fun nodeId ->
                CoreMailbox.addInMsg host (InMsg.MarkUnparsed nodeId)
          finishParse =
            fun nodeId ->
                CoreMailbox.addInMsg
                    host
                    (InMsg.ParseFinished nodeId) }
    host

let private stateOf host =
    CoreMailbox.getState host
    |> Async.RunSynchronously
    |> requireOk "state"

let private childNamed host parentId name =
    let graph = (stateOf host).graph
    Graph.children graph parentId
    |> List.tryPick (fun child ->
        if Filename.tryValue graph.nodes.[child.id].name = Some name then
            Some graph.nodes.[child.id]
        else
            None)

let private holdCurrentChild host dataDir =
    task {
        let! state0 =
            CoreMailbox.getState host |> Async.StartAsTask
        let state0 = requireOk "state0" state0
        let workspaceId, wsOps =
            FileNodeOps.planCreateWorkspace state0.graph "home"
        postOps host wsOps
        let graph1 = (stateOf host).graph
        let keptId, keptOps =
            FileNodeOps.planCreateOwnedDirectory graph1 workspaceId "kept"
        postOps host keptOps
        let diskTime = DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc)
        let keptDir = Path.Combine(dataDir, "home", "kept")
        Directory.CreateDirectory(keptDir) |> ignore
        File.WriteAllText(Path.Combine(keptDir, "secret.txt"), "secret")
        Directory.SetLastWriteTimeUtc(keptDir, diskTime)
        let keptNode = (stateOf host).graph.nodes.[keptId]
        postOps
            host
            [ Op.SetDocumentState(keptId, Unparsed, Current)
              Op.SetUpdateTime(keptId, keptNode.updateTime, diskTime) ]
        CoreMailbox.addInMsg host (InMsg.ParseFinished keptId)
        let! parsed =
            waitUntil
                (fun () -> parseStateOf host keptId = Some ParseState.Parsed)
                2000
        Assert.True(parsed, "held directory should be Parsed")
        return workspaceId, keptId
    }

let private writeNestedWorkspaceFiles dataDir =
    File.WriteAllText(Path.Combine(dataDir, "home", "readme.txt"), "top")
    let inner = Path.Combine(dataDir, "home", "docs", "inner")
    Directory.CreateDirectory(inner) |> ignore
    File.WriteAllText(
        Path.Combine(dataDir, "home", "docs", "inside.txt"),
        "inside")
    File.WriteAllText(Path.Combine(inner, "deep.txt"), "deep")

let private assertOneLevelWorkspace host workspaceId keptId =
    match childNamed host workspaceId "docs" with
    | None -> Assert.Fail("child directory docs missing")
    | Some docsNode ->
        Assert.Equal(Special SpecialKind.Directory, docsNode.kind)
        Assert.Equal(Unparsed, docsNode.documentState)
        Assert.Empty(Graph.children (stateOf host).graph docsNode.id)
    Assert.Equal(
        Some Current,
        childNamed host workspaceId "kept"
        |> Option.map (fun node -> node.documentState))
    Assert.Equal(Some ParseState.Parsed, parseStateOf host keptId)
    Assert.True(childNamed host keptId "secret.txt" |> Option.isNone)

[<Fact>]
let ``git Load and Workspace Load reconcile walks one level`` () =
    task {
        let dataDir = newTempDir ()
        let host = openParse dataDir
        try
            let! workspaceId, keptId = holdCurrentChild host dataDir
            writeNestedWorkspaceFiles dataDir
            let! loaded =
                CoreMailbox.mbLoadNode host testCaller workspaceId
                |> Async.StartAsTask
            requireOk "load" loaded
            let! found =
                waitUntil
                    (fun () ->
                        parseStateOf host workspaceId = Some ParseState.Parsed
                        && (childNamed host workspaceId "readme.txt"
                            |> Option.isSome))
                    2000
            Assert.True(found, "direct file should be discovered")
            assertOneLevelWorkspace host workspaceId keptId
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``directory Load reconcile walks one level`` () =
    task {
        let dataDir = newTempDir ()
        let host = openParse dataDir
        try
            let! _, dirId = seedDocsFile host
            let docs = Path.Combine(dataDir, "home", "docs")
            Directory.CreateDirectory(Path.Combine(docs, "side", "inner"))
            |> ignore
            File.WriteAllText(Path.Combine(docs, "added.txt"), "note")
            File.WriteAllText(Path.Combine(docs, "side", "deep.txt"), "deep")
            let! loaded =
                CoreMailbox.mbLoadNode host testCaller dirId
                |> Async.StartAsTask
            requireOk "load" loaded
            let! found =
                waitUntil
                    (fun () ->
                        parseStateOf host dirId = Some ParseState.Parsed
                        && (childNamed host dirId "added.txt" |> Option.isSome))
                    2000
            Assert.True(found, "direct file should be discovered")
            match childNamed host dirId "side" with
            | None -> Assert.Fail("child directory side missing")
            | Some side ->
                Assert.Equal(Special SpecialKind.Directory, side.kind)
                Assert.Equal(Unparsed, side.documentState)
                Assert.Empty(Graph.children (stateOf host).graph side.id)
        finally
            CoreMailbox.dispose host
    }
