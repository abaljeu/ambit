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
    CoreMailboxApi.postGraphOnly host testCaller event
    |> Async.RunSynchronously
    |> requireOk "seed"
    |> ignore

let private seedWorkspaceFile host label fileName =
    task {
        let! state0 =
            CoreMailboxApi.getState host |> Async.StartAsTask
        let state0 = requireOk "state0" state0
        let workspaceId, wsOps =
            FileNodeOps.planCreateWorkspace state0.graph label
        postOps host wsOps
        let! state1 =
            CoreMailboxApi.getState host |> Async.StartAsTask
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
            CoreMailboxApi.getState host |> Async.StartAsTask
        let state0 = requireOk "state0" state0
        let workspaceId, wsOps =
            FileNodeOps.planCreateWorkspace state0.graph "home"
        postOps host wsOps
        let! state1 =
            CoreMailboxApi.getState host |> Async.StartAsTask
        let state1 = requireOk "state1" state1
        let dirId, dirOps =
            FileNodeOps.planCreateOwnedDirectory
                state1.graph
                workspaceId
                "docs"
        postOps host dirOps
        let! state2 =
            CoreMailboxApi.getState host |> Async.StartAsTask
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
    match CoreMailboxApi.getState host |> Async.RunSynchronously with
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
    Assert.Equal(Some b, consumer ())
    Assert.Equal(Some a, consumer ())

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
                CoreMailboxApi.mbLoadNode host testCaller fileId
                |> Async.StartAsTask
            requireOk "Load" loaded |> ignore
            Assert.Equal(Some fileId, consumer ())
        finally
            CoreMailboxApi.dispose host
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
                CoreMailboxApi.mbLoadNode host testCaller workspaceId
                |> Async.StartAsTask
            requireOk "Load" loaded |> ignore
            Assert.Equal<NodeId list>([ workspaceId ], pushed |> Seq.toList)
        finally
            CoreMailboxApi.dispose host
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
                CoreMailboxApi.mbLoadNode host testCaller Graph.workspacesId
                |> Async.StartAsTask
            Assert.Equal(
                Error
                    "Load subject is not a Workspace, Directory, or File node",
                loaded)
            Assert.Empty(pushed)
        finally
            CoreMailboxApi.dispose host
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
                CoreMailboxApi.mbLoadNode host testCaller dirId
                |> Async.StartAsTask
            requireOk "Load" loaded |> ignore
            Assert.Equal<NodeId list>([ dirId ], pushed |> Seq.toList)
        finally
            CoreMailboxApi.dispose host
    }

let private deskLoadRequest (nodeId: NodeId) =
    { operation = LoadSaveOperation.Load
      prePick = LoadSavePrePick.Desk
      holdStack = false
      start =
        { zoomId = nodeId
          focusId = nodeId
          commandId = nodeId
          graphIds = [ nodeId ]
          eventId = EventId.zero } }

[<Fact>]
let ``held Desk Load does not push and the follow-up Load does`` () =
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
            let! fileId, _ = seedWorkspaceFile host "home" "note.txt"
            let held = { deskLoadRequest fileId with holdStack = true }
            let! waiting =
                CoreMailboxApi.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    held
                |> Async.StartAsTask
            requireOk "held" waiting
            Assert.Empty(pushed)
            let! loaded =
                CoreMailboxApi.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    (deskLoadRequest fileId)
                |> Async.StartAsTask
            requireOk "load" loaded
            Assert.Equal<NodeId list>([ fileId ], pushed |> Seq.toList)
        finally
            CoreMailboxApi.dispose host
    }

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
                CoreMailboxApi.startLoadSaveCommand
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
            CoreMailboxApi.dispose host
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
                CoreMailboxApi.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Desk
                    (PeerActorName "unregistered")
                    (deskLoadRequest dirId)
                |> Async.StartAsTask
            requireOk "directory" directory
            let! file =
                CoreMailboxApi.startLoadSaveCommand
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
            CoreMailboxApi.dispose host
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
                CoreMailboxApi.startLoadSaveCommand
                    host
                    testCaller
                    LoadSavePath.Git
                    (PeerActorName "unregistered")
                    (plainLoadRequest dirId LoadSavePrePick.Plain)
                |> Async.StartAsTask
            requireOk "directory" directory
            let! file =
                CoreMailboxApi.startLoadSaveCommand
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
            CoreMailboxApi.dispose host
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
                CoreMailboxApi.startLoadSaveCommand
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
            CoreMailboxApi.dispose host
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
                CoreMailboxApi.startLoadSaveCommand
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
            CoreMailboxApi.dispose host
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
            CoreDoor.create true host testCaller
        ParseThread.start
            { dataDir = dataDir
              consumer = consumer
              push = push
              getGraph = ParseThread.graphFromHost host
              postOps = ParseThread.postParseOps parseHandle
              markUnparsed = ignore
              beginParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsing nodeId)
              finishParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsed nodeId) }
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
                CoreMailboxApi.mbLoadNode host testCaller fileId
                |> Async.StartAsTask
            requireOk "Load" loaded |> ignore
            let! parsed =
                waitUntil
                    (fun () ->
                        match
                            CoreMailboxApi.getState host
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
            CoreMailboxApi.dispose host
    }

// A directory reconcile does not mark an existing disk-newer file Unparsed.
[<Fact>]
let ``disk-newer file stays Parsed when its directory reconciles`` () =
    task {
        let dataDir = newTempDir ()
        let push, consumer = ParseStack.create ()
        let host = CoreMailbox.createFile dataDir admittedCredentials
        let parseHandle = CoreDoor.create true host testCaller
        ParseThread.start
            { dataDir = dataDir
              consumer = consumer
              push = push
              getGraph = ParseThread.graphFromHost host
              postOps = ParseThread.postParseOps parseHandle
              markUnparsed =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeUnparsed nodeId)
              beginParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsing nodeId)
              finishParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsed nodeId) }
        try
            let! fileId, dirId = seedDocsFile host
            CoreMailboxApi.postNotice host (Notice.NodeParsed fileId)
            let! parsed =
                waitUntil
                    (fun () ->
                        match
                            CoreMailboxApi.getState host
                            |> Async.RunSynchronously
                        with
                        | Error _ -> false
                        | Ok state ->
                            let node = state.graph.nodes.[fileId]
                            node.documentState.parseState = ParseState.Parsed)
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
            let! finished =
                waitUntil
                    (fun () ->
                        match
                            CoreMailboxApi.getState host
                            |> Async.RunSynchronously
                        with
                        | Error _ -> false
                        | Ok state ->
                            let dir = state.graph.nodes.[dirId]
                            dir.documentState.parseState = ParseState.Parsed)
                    2000
            Assert.True(finished, "directory reconcile should finish")
            let fileStaysParsed =
                match
                    CoreMailboxApi.getState host
                    |> Async.RunSynchronously
                with
                | Error _ -> false
                | Ok state ->
                    let node = state.graph.nodes.[fileId]
                    node.documentState.parseState = ParseState.Parsed
            Assert.True(
                fileStaysParsed,
                "existing disk-newer file is not marked Unparsed")
            Assert.False(
                graphHasText host fileId "NEWER",
                "the file text is not parsed by the directory reconcile")
        finally
            CoreMailboxApi.dispose host
    }

let private ownedNames (host: MailboxHost) (parentId: NodeId) =
    match CoreMailboxApi.getState host |> Async.RunSynchronously with
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
        let parseHandle = CoreDoor.create true host testCaller
        ParseThread.start
            { dataDir = dataDir
              consumer = consumer
              push = push
              getGraph = ParseThread.graphFromHost host
              postOps = ParseThread.postParseOps parseHandle
              markUnparsed = ignore
              beginParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsing nodeId)
              finishParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsed nodeId) }
        try
            let! _, dirId = seedDocsFile host
            let extra =
                Path.Combine(dataDir, "home", "docs", "extra.txt")
            Directory.CreateDirectory(Path.GetDirectoryName extra)
            |> ignore
            File.WriteAllText(extra, "EXTRA\n")
            let! started =
                CoreMailboxApi.startLoadSaveCommand
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
            CoreMailboxApi.dispose host
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
        let parseHandle = CoreDoor.create true host testCaller
        ParseThread.start
            { dataDir = dataDir
              consumer = consumer
              push = push
              getGraph = ParseThread.graphFromHost host
              postOps = ParseThread.postParseOps parseHandle
              markUnparsed = ignore
              beginParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsing nodeId)
              finishParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsed nodeId) }
        try
            let! fileId, _ = seedWorkspaceFile host "home" "note.txt"
            let diskPath = Path.Combine(dataDir, "home", "note.txt")
            Directory.CreateDirectory(Path.GetDirectoryName diskPath)
            |> ignore
            File.WriteAllText(diskPath, "HELLO\n")
            let! started =
                CoreMailboxApi.startLoadSaveCommand
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
            CoreMailboxApi.dispose host
    }

let private parseStateOf host nodeId =
    match CoreMailboxApi.getState host |> Async.RunSynchronously with
    | Error _ -> None
    | Ok state ->
        Map.tryFind nodeId state.graph.nodes
        |> Option.map (fun node -> node.documentState.parseState)

let private childIdNamed host parentId name =
    match CoreMailboxApi.getState host |> Async.RunSynchronously with
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
        let parseHandle = CoreDoor.create true host testCaller
        ParseThread.start
            { dataDir = dataDir
              consumer = consumer
              push = push
              getGraph = ParseThread.graphFromHost host
              postOps = ParseThread.postParseOps parseHandle
              markUnparsed = ignore
              beginParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsing nodeId)
              finishParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsed nodeId) }
        try
            let! fileId, _ = seedWorkspaceFile host "home" "note.txt"
            let diskPath = Path.Combine(dataDir, "home", "note.txt")
            Directory.CreateDirectory(Path.GetDirectoryName diskPath)
            |> ignore
            File.WriteAllText(diskPath, "HELLO\n")
            let! first =
                CoreMailboxApi.startLoadSaveCommand
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
                CoreMailboxApi.startLoadSaveCommand
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
            CoreMailboxApi.dispose host
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
        let parseHandle = CoreDoor.create true host testCaller
        ParseThread.start
            { dataDir = dataDir
              consumer = consumer
              push = push
              getGraph = ParseThread.graphFromHost host
              postOps = ParseThread.postParseOps parseHandle
              markUnparsed = ignore
              beginParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsing nodeId)
              finishParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsed nodeId) }
        try
            let! fileId, _ = seedWorkspaceFile host "home" "note.txt"
            let diskPath = Path.Combine(dataDir, "home", "note.txt")
            Directory.CreateDirectory(Path.GetDirectoryName diskPath)
            |> ignore
            File.WriteAllText(diskPath, "HELLO\n")
            let! first =
                CoreMailboxApi.startLoadSaveCommand
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
                CoreMailboxApi.getState host |> Async.StartAsTask
            let held = requireOk "held" heldResult
            Assert.Equal(Current, held.graph.nodes.[fileId].documentState.AsEnum)
            File.WriteAllText(diskPath, "AGAIN\n")
            let! second =
                CoreMailboxApi.startLoadSaveCommand
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
                CoreMailboxApi.getEventsSince host held.eventId
                |> Async.StartAsTask
            let landed = applyEvents held events
            Assert.True(childText landed.graph fileId "AGAIN")
            Assert.Equal(Current, landed.graph.nodes.[fileId].documentState.AsEnum)
        finally
            CoreMailboxApi.dispose host
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
        let parseHandle = CoreDoor.create true host testCaller
        ParseThread.start
            { dataDir = dataDir
              consumer = consumer
              push = push
              getGraph = ParseThread.graphFromHost host
              postOps = ParseThread.postParseOps parseHandle
              markUnparsed =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeUnparsed nodeId)
              beginParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsing nodeId)
              finishParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsed nodeId) }
        try
            let! _, dirId = seedDocsFile host
            let! first =
                CoreMailboxApi.startLoadSaveCommand
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
                CoreMailboxApi.startLoadSaveCommand
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
            CoreMailboxApi.dispose host
    }

let private openParse (dataDir: string) =
    let push, consumer = ParseStack.create ()
    let host =
        CoreMailbox.hostWithParsePush
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create dataDir))
            admittedCredentials
            push
    let parseHandle = CoreDoor.create true host testCaller
    ParseThread.start
        { dataDir = dataDir
          consumer = consumer
          push = push
          getGraph = ParseThread.graphFromHost host
          postOps = ParseThread.postParseOps parseHandle
          markUnparsed =
            fun nodeId ->
                CoreMailboxApi.postNotice host (Notice.NodeUnparsed nodeId)
          beginParse =
            fun nodeId ->
                CoreMailboxApi.postNotice
                    host
                    (Notice.NodeParsing nodeId)
          finishParse =
            fun nodeId ->
                CoreMailboxApi.postNotice
                    host
                    (Notice.NodeParsed nodeId) }
    host

let private stateOf host =
    CoreMailboxApi.getState host
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
            CoreMailboxApi.getState host |> Async.StartAsTask
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
        CoreMailboxApi.postNotice host (Notice.NodeParsed keptId)
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
        Assert.Equal(Unparsed, docsNode.documentState.AsEnum)
        Assert.Empty(Graph.children (stateOf host).graph docsNode.id)
    Assert.Equal(
        Some Current,
        childNamed host workspaceId "kept"
        |> Option.map (fun node -> node.documentState.AsEnum))
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
                CoreMailboxApi.mbLoadNode host testCaller workspaceId
                |> Async.StartAsTask
            requireOk "load" loaded |> ignore
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
            CoreMailboxApi.dispose host
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
                CoreMailboxApi.mbLoadNode host testCaller dirId
                |> Async.StartAsTask
            requireOk "load" loaded |> ignore
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
                Assert.Equal(Unparsed, side.documentState.AsEnum)
                Assert.Empty(Graph.children (stateOf host).graph side.id)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``Load of a newly Unparsed child Directory reconciles one level`` () =
    task {
        let dataDir = newTempDir ()
        let host = openParse dataDir
        try
            let! _, dirId = seedDocsFile host
            let docs = Path.Combine(dataDir, "home", "docs")
            Directory.CreateDirectory(docs) |> ignore
            File.WriteAllText(Path.Combine(docs, "note.txt"), "keep")
            let sideDir = Path.Combine(docs, "side")
            Directory.CreateDirectory(sideDir) |> ignore
            File.WriteAllText(Path.Combine(sideDir, "inner.txt"), "inner")
            let! parentLoad =
                CoreMailboxApi.mbLoadNode host testCaller dirId
                |> Async.StartAsTask
            requireOk "parent" parentLoad |> ignore
            let! childReady =
                waitUntil
                    (fun () ->
                        match childNamed host dirId "side" with
                        | Some node ->
                            Node.documentStateEnum node = Unparsed
                            && node.kind = Special SpecialKind.Directory
                        | None -> false)
                    2000
            Assert.True(childReady, "parent Load adds the child Unparsed")
            let side = childNamed host dirId "side" |> Option.get
            Assert.Empty(Graph.children (stateOf host).graph side.id)
            let! heldResult =
                CoreMailboxApi.getState host |> Async.StartAsTask
            let held = requireOk "held" heldResult
            let! childLoad =
                CoreMailboxApi.mbLoadNode host testCaller side.id
                |> Async.StartAsTask
            requireOk "child" childLoad |> ignore
            let! memberReady =
                waitUntil
                    (fun () ->
                        childNamed host side.id "inner.txt" |> Option.isSome
                        && parseStateOf host side.id = Some ParseState.Parsed)
                    2000
            Assert.True(memberReady, "child Load adds its member")
            let! events =
                CoreMailboxApi.getEventsSince host held.eventId
                |> Async.StartAsTask
            let landed = applyEvents held events
            Assert.Equal(Current, landed.graph.nodes.[side.id].documentState.AsEnum)
            let replayedMember =
                Graph.children landed.graph side.id
                |> List.exists (fun child ->
                    Filename.tryValue landed.graph.nodes.[child.id].name
                        = Some "inner.txt")
            Assert.True(replayedMember, "replay shows the child member")
        finally
            CoreMailboxApi.dispose host
    }

type private LoadRig =
    { host: MailboxHost
      pushed: ResizeArray<NodeId>
      startThread: unit -> unit }

let private pushedIds (pushed: ResizeArray<NodeId>) =
    lock pushed (fun () -> pushed |> Seq.toList)

let private loadRig (dataDir: string) =
    let stackPush, consumer = ParseStack.create ()
    let pushed = ResizeArray<NodeId>()
    let push nodeId =
        lock pushed (fun () -> pushed.Add nodeId)
        stackPush nodeId
    let host =
        CoreMailbox.hostWithParsePush
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create dataDir))
            admittedCredentials
            push
    let startThread () =
        let parseHandle = CoreDoor.create true host testCaller
        ParseThread.start
            { dataDir = dataDir
              consumer = consumer
              push = push
              getGraph = ParseThread.graphFromHost host
              postOps = ParseThread.postParseOps parseHandle
              markUnparsed =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeUnparsed nodeId)
              beginParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsing nodeId)
              finishParse =
                fun nodeId ->
                    CoreMailboxApi.postNotice
                        host
                        (Notice.NodeParsed nodeId) }
    { host = host
      pushed = pushed
      startThread = startThread }

let private userLoad host nodeId =
    CoreMailboxApi.mbLoadNode host testCaller nodeId
    |> Async.StartAsTask

let private ensureDocsDisk dataDir =
    let docs = Path.Combine(dataDir, "home", "docs")
    Directory.CreateDirectory(docs) |> ignore
    docs

[<Fact>]
let ``user Load on a Parsed Directory marks it Unparsed then reconciles`` () =
    task {
        let dataDir = newTempDir ()
        let rig = loadRig dataDir
        try
            let! _, dirId = seedDocsFile rig.host
            let docs = ensureDocsDisk dataDir
            File.WriteAllText(Path.Combine(docs, "note.txt"), "keep")
            File.WriteAllText(Path.Combine(docs, "extra.txt"), "EXTRA\n")
            CoreMailboxApi.postNotice rig.host (Notice.NodeParsed dirId)
            let! parsed =
                waitUntil
                    (fun () ->
                        parseStateOf rig.host dirId = Some ParseState.Parsed)
                    2000
            Assert.True(parsed, "directory should be Parsed before Load")
            let! loaded = userLoad rig.host dirId
            requireOk "Load" loaded |> ignore
            Assert.Equal(
                Some ParseState.Unparsed,
                parseStateOf rig.host dirId)
            Assert.Equal<NodeId list>([ dirId ], pushedIds rig.pushed)
            rig.startThread ()
            let! created =
                waitUntil
                    (fun () ->
                        parseStateOf rig.host dirId = Some ParseState.Parsed
                        && (childIdNamed rig.host dirId "extra.txt"
                            |> Option.isSome))
                    2000
            Assert.True(created, "Load should reconcile extra.txt")
        finally
            CoreMailboxApi.dispose rig.host
    }

[<Fact>]
let ``Directory reconcile leaves existing member parse state unchanged`` () =
    task {
        let dataDir = newTempDir ()
        let rig = loadRig dataDir
        rig.startThread ()
        try
            let! fileId, dirId = seedDocsFile rig.host
            let graph = (stateOf rig.host).graph
            let keptId, keptOps =
                FileNodeOps.planCreateOwnedDirectory graph dirId "kept"
            postOps rig.host keptOps
            let docs = ensureDocsDisk dataDir
            File.WriteAllText(Path.Combine(docs, "note.txt"), "DISK-NOTE\n")
            Directory.CreateDirectory(Path.Combine(docs, "kept")) |> ignore
            File.WriteAllText(
                Path.Combine(docs, "kept", "inner.txt"),
                "INNER\n")
            File.WriteAllText(Path.Combine(docs, "extra.txt"), "EXTRA\n")
            CoreMailboxApi.postNotice rig.host (Notice.NodeParsed fileId)
            CoreMailboxApi.postNotice rig.host (Notice.NodeParsed keptId)
            let! held =
                waitUntil
                    (fun () ->
                        parseStateOf rig.host fileId = Some ParseState.Parsed
                        && parseStateOf rig.host keptId
                           = Some ParseState.Parsed)
                    2000
            Assert.True(held, "existing members should be Parsed before reconcile")
            let! loaded = userLoad rig.host dirId
            requireOk "Load" loaded |> ignore
            let! reconciled =
                waitUntil
                    (fun () ->
                        parseStateOf rig.host dirId = Some ParseState.Parsed
                        && (childIdNamed rig.host dirId "extra.txt"
                            |> Option.isSome))
                    2000
            Assert.True(reconciled, "parent Load should reconcile")
            Assert.Equal(Some fileId, childIdNamed rig.host dirId "note.txt")
            Assert.Equal(Some ParseState.Parsed, parseStateOf rig.host fileId)
            Assert.Equal(Some ParseState.Parsed, parseStateOf rig.host keptId)
            Assert.False(graphHasText rig.host fileId "DISK-NOTE")
            Assert.True(
                childNamed rig.host keptId "inner.txt" |> Option.isNone)
            match childIdNamed rig.host dirId "extra.txt" with
            | None -> Assert.Fail("extra.txt missing")
            | Some extraId ->
                Assert.Equal(
                    Some ParseState.Unparsed,
                    parseStateOf rig.host extraId)
        finally
            CoreMailboxApi.dispose rig.host
    }

[<Fact>]
let ``File Load parses its file regardless of Parsed state or disk date`` () =
    task {
        let dataDir = newTempDir ()
        let rig = loadRig dataDir
        try
            let! fileId, _ = seedWorkspaceFile rig.host "home" "note.txt"
            let diskPath = Path.Combine(dataDir, "home", "note.txt")
            Directory.CreateDirectory(Path.GetDirectoryName diskPath)
            |> ignore
            let old = DateTime(1999, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            File.WriteAllText(diskPath, "OLD\n")
            File.SetLastWriteTimeUtc(diskPath, old)
            CoreMailboxApi.postNotice rig.host (Notice.NodeParsed fileId)
            let! wasParsed =
                waitUntil
                    (fun () ->
                        parseStateOf rig.host fileId = Some ParseState.Parsed)
                    2000
            Assert.True(wasParsed, "file should be Parsed before Load")
            let! first = userLoad rig.host fileId
            requireOk "first" first |> ignore
            Assert.Equal(Some ParseState.Unparsed, parseStateOf rig.host fileId)
            Assert.Equal<NodeId list>([ fileId ], pushedIds rig.pushed)
            rig.startThread ()
            let! parsed =
                waitUntil
                    (fun () ->
                        graphHasText rig.host fileId "OLD"
                        && parseStateOf rig.host fileId
                           = Some ParseState.Parsed)
                    2000
            Assert.True(parsed, "Load should parse OLD when the disk date is older")
            File.WriteAllText(diskPath, "NEW\n")
            File.SetLastWriteTimeUtc(diskPath, old)
            let! second = userLoad rig.host fileId
            requireOk "second" second |> ignore
            let! again =
                waitUntil
                    (fun () -> graphHasText rig.host fileId "NEW")
                    2000
            Assert.True(
                again,
                "a later Load of that Parsed file should parse NEW")
        finally
            CoreMailboxApi.dispose rig.host
    }

[<Fact>]
let ``Directory reconcile pushes nothing onto the parse stack`` () =
    task {
        let dataDir = newTempDir ()
        let rig = loadRig dataDir
        rig.startThread ()
        try
            let! fileId, dirId = seedDocsFile rig.host
            let docs = ensureDocsDisk dataDir
            File.WriteAllText(Path.Combine(docs, "note.txt"), "keep")
            Directory.CreateDirectory(Path.Combine(docs, "side")) |> ignore
            File.WriteAllText(Path.Combine(docs, "extra.txt"), "EXTRA\n")
            let! loaded = userLoad rig.host dirId
            requireOk "Load" loaded |> ignore
            let! reconciled =
                waitUntil
                    (fun () ->
                        parseStateOf rig.host dirId = Some ParseState.Parsed
                        && (childIdNamed rig.host dirId "extra.txt"
                            |> Option.isSome)
                        && (childNamed rig.host dirId "side" |> Option.isSome))
                    2000
            Assert.True(reconciled, "directory Load should reconcile members")
            do! Task.Delay 400
            Assert.Equal<NodeId list>([ dirId ], pushedIds rig.pushed)
            Assert.DoesNotContain(fileId, pushedIds rig.pushed)
            match childIdNamed rig.host dirId "extra.txt" with
            | None -> Assert.Fail("extra.txt missing")
            | Some extraId ->
                Assert.DoesNotContain(extraId, pushedIds rig.pushed)
                Assert.Equal(
                    Some ParseState.Unparsed,
                    parseStateOf rig.host extraId)
            match childNamed rig.host dirId "side" with
            | None -> Assert.Fail("side missing")
            | Some side ->
                Assert.DoesNotContain(side.id, pushedIds rig.pushed)
                Assert.Equal(ParseState.Unparsed, side.documentState.parseState)
        finally
            CoreMailboxApi.dispose rig.host
    }
