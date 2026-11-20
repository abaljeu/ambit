module Gambol.Server.Tests.LoadSaveCommandTests

open System
open System.IO
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend
open Gambol.Server.Tests.LoadSaveCommandFixtures

[<Fact>]
let ``Desk request reaches pool without starting Peer Actor`` () = task {
    let host =
        CoreMailbox.host
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create (newTempDir ())))
            admittedCredentials
    try
        let nodeId = NodeId.New()
        let router =
            { resolvePath = fun _ _ -> Result.Ok LoadSavePath.Desk
              startCommand =
                fun path command ->
                    CoreMailboxApi.startLoadSaveCommand
                        host
                        testCaller
                        path
                        (PeerActorName "unregistered")
                        command }
        let body =
            request
                LoadSaveOperation.Load
                LoadSavePrePick.Desk
                nodeId
                EventId.zero
            |> encodeRequest

        let! result =
            Api.postLoadSaveCommand
                router
                (CoreDoor.create true host testCaller)
                body
            |> Async.StartAsTask

        let response = requireResponse result
        Assert.Equal(LoadSavePath.Desk, response.path)
        Assert.Equal(None, response.command)
    finally
        CoreMailboxApi.dispose host
}

let private routeMatrixDependencies (started: TaskCompletionSource<unit>) =
    let markStarted () = started.TrySetResult() |> ignore
    let tracked =
        { branch = "main"
          remote = "origin"
          upstream = "refs/heads/main" }
    { git =
        { remoteExists = fun _ -> Result.Ok true
          fetchTracked = fun _ _ -> Result.Ok "fetched"
          pullTracked =
            fun _ _ ->
                markStarted ()
                Result.Ok "pulled"
          commitAll =
            fun _ _ _ ->
                markStarted ()
                Result.Ok(tracked, "committed")
          pushTracked = fun _ _ _ -> Result.Ok "pushed" } }

let private routeMatrixHarness operationName =
    let dataDir = newTempDir ()
    let started =
        TaskCompletionSource<unit>(
            TaskCreationOptions.RunContinuationsAsynchronously)
    let dependencies = routeMatrixDependencies started
    let pool = CoreActorPool.create ()
    let host =
        CoreMailbox.host
            pool
            (FileAgent.persist (FileAgent.create dataDir))
            admittedCredentials
    host, seedWorkspace host operationName, started, dataDir, dependencies

let private routeMatrixRouter host dataDir dependencies operation =
    { resolvePath =
        fun _ command ->
            PathPick.resolveCommand
                { operation = command.operation
                  prePick = command.prePick
                  subject = Some LoadSubject.Directory }
      startCommand =
        fun path command ->
            if path = LoadSavePath.Desk then
                CoreMailboxApi.startLoadSaveCommand
                    host
                    testCaller
                    path
                    (GithubTransportActor.peerName
                        (operationFor operation))
                    command
            else
                GithubTransportActor.start
                    host
                    testCaller
                    dataDir
                    dependencies
                    (operationFor operation)
                    command.start }

let private operationFromName =
    function
    | "load" -> LoadSaveOperation.Load
    | "save" -> LoadSaveOperation.Save
    | name ->
        Assert.Fail($"unknown operation: {name}")
        Unchecked.defaultof<_>

let private prePickFromName =
    function
    | "plain" -> LoadSavePrePick.Plain
    | "git" -> LoadSavePrePick.Git
    | name ->
        Assert.Fail($"unknown pre-pick: {name}")
        Unchecked.defaultof<_>

[<Theory>]
[<InlineData("load", "git")>]
[<InlineData("save", "git")>]
[<InlineData("save", "plain")>]
let ``every Git choice starts the Server Peer Actor``
    operationName
    prePickName
    = task {
    let operation = operationFromName operationName
    let prePick = prePickFromName prePickName
    let host, seed, started, dataDir, dependencies =
        routeMatrixHarness operationName
    let locksBefore = NodeLockPool.entryCount ()
    try
        let focusId =
            if operationName = "save" then
                seed.workspaceId
            else
                seed.focusId
        let command = requestForFocus operation prePick seed focusId
        let! result =
            Api.postLoadSaveCommand
                (routeMatrixRouter host dataDir dependencies operation)
                (CoreDoor.create true host testCaller)
                (encodeRequest command)
            |> Async.StartAsTask
        let response = requireResponse result
        Assert.Equal(LoadSavePath.Git, response.path)
        Assert.True(Option.isSome response.command)
        do! started.Task.WaitAsync(TimeSpan.FromSeconds 1.0)
        let! history = CoreMailboxApi.eventHistory host |> Async.StartAsTask
        Assert.Contains(history.events, hasStartedActor focusId)
        let! stopped = waitUntilActorStops host focusId 2000
        Assert.True(stopped, "git actor still holds the workspace lock")
    finally
        CoreMailboxApi.dispose host
    Assert.Equal(locksBefore, NodeLockPool.entryCount ())
}

[<Fact>]
let ``plain Load of a non-Workspace returns Desk when a remote exists`` () =
    task {
        let operation = LoadSaveOperation.Load
        let host, seed, started, dataDir, dependencies =
            routeMatrixHarness "load"
        try
            let command =
                requestFor operation LoadSavePrePick.Plain seed
            let! result =
                Api.postLoadSaveCommand
                    (routeMatrixRouter
                        host
                        dataDir
                        dependencies
                        operation)
                    (CoreDoor.create true host testCaller)
                    (encodeRequest command)
                |> Async.StartAsTask
            let response = requireResponse result
            Assert.Equal(LoadSavePath.Desk, response.path)
            Assert.True(response.command.IsNone)
            Assert.False(started.Task.IsCompleted)
        finally
            CoreMailboxApi.dispose host
    }

[<SkippableFact>]
let ``routed Git Load pulls the tracked branch`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let harness = createGitHarness "load"
    try
        commitFile
            harness.source
            "from-remote.txt"
            "pulled"
            "remote-change"
        git harness.source "push origin main" |> ignore
        do!
            postRoutedFocus
                LoadSaveOperation.Load
                LoadSavePrePick.Plain
                harness.seed.workspaceId
                harness
        let! stopped =
            waitForStop harness.host harness.seed.workspaceId
        Assert.Equal(Some ActorSucceeded, stopped)
        Assert.Equal(
            "pulled",
            File.ReadAllText(
                Path.Combine(harness.workspace, "from-remote.txt")))
        let! poll = pollSince harness
        Assert.Contains(
            poll.events,
            hasStartedActor harness.seed.workspaceId)
        Assert.Contains(
            poll.events,
            hasActorStop harness.seed.workspaceId)
        Assert.Contains(poll.events, isParseChange)
    finally
        CoreMailboxApi.dispose harness.host
}

/// Git Load of a Directory still posts github-load. The client must drop Cancel.
[<Fact>]
let ``git Load of a Directory clears Cancel after the actor stops`` () = task {
    let operation = LoadSaveOperation.Load
    let host, seed, _started, dataDir, dependencies =
        routeMatrixHarness "load"
    try
        let dirId, dirOps =
            FileNodeOps.planCreateOwnedDirectory
                (readGraph host)
                seed.workspaceId
                "docs"
        postChange host dirOps
        let! before =
            CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
        let before = requireOk "before" before
        let command =
            requestForFocus operation LoadSavePrePick.Git seed dirId
        let command =
            { command with
                start = { command.start with eventId = before.eventId } }
        let! result =
            Api.postLoadSaveCommand
                (routeMatrixRouter host dataDir dependencies operation)
                (CoreDoor.create true host testCaller)
                (encodeRequest command)
            |> Async.StartAsTask
        let response = requireResponse result
        Assert.Equal(LoadSavePath.Git, response.path)
        let commandEvents =
            match response.command with
            | Some loaded -> loaded.events
            | None -> []
        let baseline =
            ClientSyncState.create
                before.graph
                before.eventId
                (ClientHistory.clear ())
        let afterCommand = applyTail "command response" commandEvents baseline
        let! stopped = waitForStop host dirId
        Assert.True(
            stopped.IsSome,
            "github-load posted no stop for the Directory")
        let stopInCommand =
            commandEvents
            |> List.exists (fun event ->
                match event.body with
                | EventBody.ActorStop(id, _) -> id = dirId
                | _ -> false)
        if stopInCommand then
            Assert.False(
                ActorLive.offersCancel dirId afterCommand.actorLiveFocusIds,
                "the command response included the stop and left Cancel up")
        else
            Assert.True(
                ActorLive.offersCancel dirId afterCommand.actorLiveFocusIds,
                "command response did not show Cancel")
        let! poll = pollFrom host afterCommand.eventId
        let afterPoll = applyTail "poll" poll.events afterCommand
        Assert.False(
            ActorLive.offersCancel dirId afterPoll.actorLiveFocusIds,
            "Cancel stays: poll did not clear the Directory actor")
        let! history =
            CoreMailboxApi.eventHistory host |> Async.StartAsTask
        let started =
            history.events |> List.exists (githubLoadStarted dirId)
        Assert.True(started, "github-load StartActor was not for the Directory")
        Assert.Equal(Some ActorSucceeded, stopped)
    finally
        CoreMailboxApi.dispose host
}

[<SkippableFact>]
let ``git Load pulls a staging checkout and does not reconcile`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let dataDir = newTempDir ()
    let workspace = Path.Combine(dataDir, "home")
    Directory.CreateDirectory(workspace) |> ignore
    let remote = publishStaging (newTempDir ())
    let host = createProductionHost dataDir
    try
        let seed = seedWorkspace host "load"
        let graph = readGraph host
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile graph seed.workspaceId "note.txt"
        postChange host fileOps
        CoreMailboxApi.postNotice host (Notice.NodeUnparsed fileId)
        readGraph host |> ignore
        fetchSwitchStaging workspace remote
        let harness =
            { dataDir = dataDir
              parent = dataDir
              remote = remote
              source = remote
              workspace = workspace
              host = host
              seed = seed }
        do!
            postRouted
                LoadSaveOperation.Load
                LoadSavePrePick.Git
                harness
        let! stopped = waitForStop host seed.focusId
        Assert.Equal(Some ActorSucceeded, stopped)
        let after = readGraph host
        let names =
            Graph.children after seed.workspaceId
            |> List.choose (fun child ->
                Filename.tryValue after.nodes.[child.id].name)
        Assert.Equal(
            "arrived",
            File.ReadAllText(Path.Combine(workspace, "arrived.txt")))
        Assert.DoesNotContain("arrived.txt", names)
        Assert.Equal(
            Unparsed,
            (childNamed after seed.workspaceId "note.txt").documentState.AsEnum)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``plain Load of a File parses that file from disk`` () = task {
    let dataDir = newTempDir ()
    let host = createParsingHost dataDir
    try
        let seed = seedWorkspace host "load"
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                (readGraph host) seed.workspaceId "note.txt"
        postChange host fileOps
        let disk = Path.Combine(dataDir, "home", "note.txt")
        Directory.CreateDirectory(Path.GetDirectoryName disk) |> ignore
        File.WriteAllText(disk, "seed")
        do! postPlainLoad host dataDir seed fileId
        do! assertParsed host fileId "seed"
        assertNoLoadActor host [ fileId ]
    finally
        CoreMailboxApi.dispose host
}

[<SkippableFact>]
let ``plain Load of a File or Directory with a remote parses and does not pull`` () =
    task {
        Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
        let harness = createGitHarnessOn createParsingHost "load"
        try
            let fileId, fileOps =
                FileNodeOps.planCreateOwnedFile
                    (readGraph harness.host)
                    harness.seed.workspaceId
                    "note.txt"
            postChange harness.host fileOps
            let dirId, dirOps =
                FileNodeOps.planCreateOwnedDirectory
                    (readGraph harness.host)
                    harness.seed.workspaceId
                    "docs"
            postChange harness.host dirOps
            commitFile harness.source "from-remote.txt" "pulled" "remote-change"
            git harness.source "push origin main" |> ignore
            for focusId in [ dirId; fileId ] do
                do! postPlainLoad harness.host harness.dataDir harness.seed focusId
            do! assertParsed harness.host fileId "seed"
            Assert.False(
                File.Exists(Path.Combine(harness.workspace, "from-remote.txt")),
                "plain File or Directory Load pulled the remote")
            assertNoLoadActor harness.host [ fileId; dirId ]
        finally
            CoreMailboxApi.dispose harness.host
    }

[<SkippableFact>]
let ``explicit git Load of a File pulls and parses that file`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let harness = createGitHarnessOn createParsingHost "load"
    try
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                (readGraph harness.host)
                harness.seed.workspaceId
                "note.txt"
        postChange harness.host fileOps
        CoreMailboxApi.postNotice harness.host (Notice.NodeUnparsed fileId)
        commitFile harness.source "note.txt" "from-remote" "update-note"
        git harness.source "push origin main" |> ignore
        do!
            postRoutedFocus
                LoadSaveOperation.Load
                LoadSavePrePick.Git
                fileId
                harness
        let! stopped = waitForStop harness.host fileId
        Assert.Equal(Some ActorSucceeded, stopped)
        do! assertParsed harness.host fileId "from-remote"
    finally
        CoreMailboxApi.dispose harness.host
}

[<SkippableFact>]
let ``explicit git Load of a Directory pulls and reconciles it`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let harness = createGitHarnessOn createParsingHost "load"
    try
        let dirId, dirOps =
            FileNodeOps.planCreateOwnedDirectory
                (readGraph harness.host)
                harness.seed.workspaceId
                "docs"
        postChange harness.host dirOps
        CoreMailboxApi.postNotice harness.host (Notice.NodeUnparsed dirId)
        commitFile harness.source "docs/guide.txt" "guide" "add-guide"
        git harness.source "push origin main" |> ignore
        do!
            postRoutedFocus
                LoadSaveOperation.Load
                LoadSavePrePick.Git
                dirId
                harness
        let! stopped = waitForStop harness.host dirId
        Assert.Equal(Some ActorSucceeded, stopped)
        do! assertDirectoryParsed harness.host dirId "guide.txt"
    finally
        CoreMailboxApi.dispose harness.host
}

[<Fact>]
let ``failed git Load of a File does not reconcile`` () = task {
    let dataDir = newTempDir ()
    let host = createParsingHost dataDir
    let conflict = "merge conflict"
    let _, _, baseDeps = countingGit ()
    let dependencies =
        { git =
            { baseDeps.git with
                remoteExists = fun _ -> Result.Ok true
                pullTracked = fun _ _ -> Result.Error conflict } }
    try
        let seed = seedWorkspace host "load"
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                (readGraph host)
                seed.workspaceId
                "note.txt"
        postChange host fileOps
        CoreMailboxApi.postNotice host (Notice.NodeUnparsed fileId)
        let disk = Path.Combine(dataDir, "home", "note.txt")
        Directory.CreateDirectory(Path.GetDirectoryName disk) |> ignore
        File.WriteAllText(disk, "seed")
        let command =
            requestForFocus LoadSaveOperation.Load LoadSavePrePick.Git seed fileId
        let! result =
            Api.postLoadSaveCommand
                (actorRouter host dataDir dependencies LoadSaveOperation.Load)
                (CoreDoor.create true host testCaller)
                (encodeRequest command)
            |> Async.StartAsTask
        Assert.Equal(LoadSavePath.Git, (requireResponse result).path)
        let! stopped = waitForStop host fileId
        Assert.Equal(Some (ActorFailed conflict), stopped)
        let after = readGraph host
        Assert.Equal(Unparsed, Node.documentStateEnum after.nodes.[fileId])
        Assert.Empty(Graph.children after fileId)
        let! history = CoreMailboxApi.eventHistory host |> Async.StartAsTask
        let loaded =
            history.events
            |> List.exists (fun event ->
                match event.body with
                | EventBody.Load _ -> true
                | _ -> false)
        Assert.False(loaded, "failed git Load posted a reconcile Load")
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``explicit Git Load of a non-repo Directory is Neither`` () = task {
    let dataDir = newTempDir ()
    let host = createProductionHost dataDir
    let pulls, _, dependencies = countingGit ()
    try
        let seed = seedWorkspace host "load"
        let dirId, dirOps =
            FileNodeOps.planCreateOwnedDirectory
                (readGraph host)
                seed.workspaceId
                "docs"
        postChange host dirOps
        let command =
            requestForFocus
                LoadSaveOperation.Load
                LoadSavePrePick.Git
                seed
                dirId
        let! result =
            Api.postLoadSaveCommand
                (actorRouter
                    host
                    dataDir
                    dependencies
                    LoadSaveOperation.Load)
                (CoreDoor.create true host testCaller)
                (encodeRequest command)
            |> Async.StartAsTask
        Assert.Equal(LoadSavePath.Git, (requireResponse result).path)
        let! stopped = waitForStop host dirId
        Assert.Equal(Some ActorSucceeded, stopped)
        Assert.Empty(pulls)
        let! history =
            CoreMailboxApi.eventHistory host |> Async.StartAsTask
        let started =
            history.events |> List.exists (githubLoadStarted dirId)
        Assert.True(started, "non-repo Directory Load posted no StartActor")
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``plain Workspace Load with no Git remote is Neither`` () = task {
    let dataDir = newTempDir ()
    let host = createProductionHost dataDir
    let pulls, pushes, dependencies =
        withRemote (fun _ -> Result.Ok false)
    try
        let seed = seedWorkspace host "load"
        let command =
            requestForFocus
                LoadSaveOperation.Load
                LoadSavePrePick.Plain
                seed
                seed.workspaceId
        let! result =
            Api.postLoadSaveCommand
                (actorRouter
                    host
                    dataDir
                    dependencies
                    LoadSaveOperation.Load)
                (CoreDoor.create true host testCaller)
                (encodeRequest command)
            |> Async.StartAsTask
        Assert.Equal(LoadSavePath.Git, (requireResponse result).path)
        let! stopped = waitForStop host seed.workspaceId
        Assert.Equal(Some ActorSucceeded, stopped)
        Assert.Empty(pulls)
        Assert.Empty(pushes)
        let! history =
            CoreMailboxApi.eventHistory host |> Async.StartAsTask
        Assert.True(
            history.events
            |> List.exists (githubLoadStarted seed.workspaceId),
            "Neither Workspace Load posted no StartActor")
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``Load actor fails when git remote fails besides no repository`` () =
    task {
        let dataDir = newTempDir ()
        let host = createProductionHost dataDir
        let gitError = "fatal: bad config line 1 in file .git/config"
        let pulls, _, dependencies =
            withRemote (fun _ -> Result.Error gitError)
        try
            Directory.CreateDirectory(Path.Combine(dataDir, "home")) |> ignore
            let seed = seedWorkspace host "load"
            let command =
                requestForFocus
                    LoadSaveOperation.Load
                    LoadSavePrePick.Plain
                    seed
                    seed.workspaceId
            let! result =
                Api.postLoadSaveCommand
                    (actorRouter
                        host
                        dataDir
                        dependencies
                        LoadSaveOperation.Load)
                    (CoreDoor.create true host testCaller)
                    (encodeRequest command)
                |> Async.StartAsTask
            Assert.Equal(LoadSavePath.Git, (requireResponse result).path)
            let! stopped = waitForStop host seed.workspaceId
            Assert.Equal(Some (ActorFailed gitError), stopped)
            Assert.Empty(pulls)
            let! history =
                CoreMailboxApi.eventHistory host |> Async.StartAsTask
            Assert.True(
                history.events |> List.exists (githubLoadStarted seed.workspaceId),
                "failed Load posted no StartActor")
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``Load of a ROOT file skips git and git Save uses the data directory`` () =
    task {
        let dataDir = newTempDir ()
        let host = createProductionHost dataDir
        let pulls, pushes, dependencies = countingGit ()
        try
            let seed = seedWorkspace host "load"
            let graph = readGraph host
            let fileId = NodeId.New()
            let children = Graph.children graph Graph.rootId
            postChange
                host
                [ Op.NewSpecialNode(fileId, SpecialKind.File, "loose.txt")
                  Op.Replace(
                      Graph.rootId,
                      children,
                      children @ [ ChildNode.owner fileId ]) ]
            let load =
                requestForFocus
                    LoadSaveOperation.Load
                    LoadSavePrePick.Plain
                    seed
                    fileId
            let! loaded =
                Api.postLoadSaveCommand
                    (actorRouter
                        host
                        dataDir
                        dependencies
                        LoadSaveOperation.Load)
                    (CoreDoor.create true host testCaller)
                    (encodeRequest load)
                |> Async.StartAsTask
            Assert.Equal(LoadSavePath.Desk, (requireResponse loaded).path)
            Assert.Empty(pulls)
            Assert.Empty(pushes)
            assertNoLoadActor host [ fileId ]
            let save =
                requestForFocus
                    LoadSaveOperation.Save
                    LoadSavePrePick.Git
                    seed
                    fileId
            let saveDependencies =
                { dependencies with
                    git =
                        { dependencies.git with
                            remoteExists = fun _ -> Result.Ok true } }
            let! saved =
                Api.postLoadSaveCommand
                    (actorRouter
                        host
                        dataDir
                        saveDependencies
                        LoadSaveOperation.Save)
                    (CoreDoor.create true host testCaller)
                    (encodeRequest save)
                |> Async.StartAsTask
            Assert.Equal(LoadSavePath.Git, (requireResponse saved).path)
            let! stopsAfter =
                waitForStopCount
                    host
                    fileId
                    1
                    (DateTime.UtcNow.AddSeconds 5.0)
            Assert.True(stopsAfter > 0, "Save posted no stop")
            let dataRoot =
                Path.GetFullPath(DocumentPersistPath.dataDirBase dataDir)
            let recorded roots =
                roots
                |> Seq.map Path.GetFullPath
                |> Seq.toList
            Assert.Equal<string list>([ dataRoot ], recorded pulls)
            Assert.Equal<string list>([ dataRoot ], recorded pushes)
        finally
            CoreMailboxApi.dispose host
    }

[<SkippableFact>]
let ``git Load does not reconcile a disk file into the graph`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let harness = createGitHarness "load"
    try
        File.WriteAllText(
            Path.Combine(harness.workspace, "leftover.txt"),
            "already on disk")
        do!
            postRouted
                LoadSaveOperation.Load
                LoadSavePrePick.Git
                harness
        let! stopped =
            waitForStop harness.host harness.seed.focusId
        Assert.Equal(Some ActorSucceeded, stopped)
        let after = readGraph harness.host
        let names =
            Graph.children after harness.seed.workspaceId
            |> List.choose (fun child ->
                Filename.tryValue after.nodes.[child.id].name)
        Assert.Equal(
            "already on disk",
            File.ReadAllText(
                Path.Combine(harness.workspace, "leftover.txt")))
        Assert.DoesNotContain("leftover.txt", names)
        Assert.DoesNotContain("note.txt", names)
    finally
        CoreMailboxApi.dispose harness.host
}
