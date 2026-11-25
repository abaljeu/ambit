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
                    LoadSaveStart.startDesk
                        (CoreDoor.create true host testCaller)
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

[<Fact>]
let ``Desk Load of a Workspace places Order.Load and Desk Save places nothing`` () = task {
    let host =
        CoreMailbox.host
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create (newTempDir ())))
            admittedCredentials
    try
        let seed = seedWorkspace host "load"
        let door = CoreDoor.create true host testCaller
        let desk operation =
            request
                operation
                LoadSavePrePick.Desk
                seed.workspaceId
                EventId.zero
        let loadedCount (history: EventLog) =
            history.events
            |> List.filter (fun event ->
                match event.body with
                | EventBody.Load id -> id = seed.workspaceId
                | _ -> false)
            |> List.length
        let! saved =
            LoadSaveStart.startDesk door (desk LoadSaveOperation.Save)
            |> Async.StartAsTask
        Assert.Equal(Result.Ok (), saved)
        let! afterSave = CoreMailboxApi.eventHistory host |> Async.StartAsTask
        Assert.Equal(0, loadedCount afterSave)
        let! loaded =
            LoadSaveStart.startDesk door (desk LoadSaveOperation.Load)
            |> Async.StartAsTask
        Assert.Equal(Result.Ok (), loaded)
        let! afterLoad = CoreMailboxApi.eventHistory host |> Async.StartAsTask
        Assert.Equal(1, loadedCount afterLoad)
        Assert.DoesNotContain(
            afterLoad.events,
            hasStartedActor seed.workspaceId)
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
    actorRouter host dataDir dependencies operation

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
        let focusId = seed.workspaceId
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

/// Git Load of a Workspace posts github-load. The client must drop Cancel.
[<Fact>]
let ``git Load of a Workspace clears Cancel after the actor stops`` () = task {
    let operation = LoadSaveOperation.Load
    let host, seed, _started, dataDir, dependencies =
        routeMatrixHarness "load"
    try
        let wsId = seed.workspaceId
        let! before =
            CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
        let before = requireOk "before" before
        let command =
            requestForFocus operation LoadSavePrePick.Git seed wsId
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
        let! stopped = waitForStop host wsId
        Assert.True(
            stopped.IsSome,
            "github-load posted no stop for the Workspace")
        let stopInCommand =
            commandEvents
            |> List.exists (fun event ->
                match event.body with
                | EventBody.ActorStop(id, _) -> id = wsId
                | _ -> false)
        if stopInCommand then
            Assert.False(
                ActorLive.offersCancel wsId afterCommand.actorLiveFocusIds,
                "the command response included the stop and left Cancel up")
        else
            Assert.True(
                ActorLive.offersCancel wsId afterCommand.actorLiveFocusIds,
                "command response did not show Cancel")
        let! poll = pollFrom host afterCommand.eventId
        let afterPoll = applyTail "poll" poll.events afterCommand
        Assert.False(
            ActorLive.offersCancel wsId afterPoll.actorLiveFocusIds,
            "Cancel stays: poll did not clear the Workspace actor")
        let! history =
            CoreMailboxApi.eventHistory host |> Async.StartAsTask
        let started =
            history.events |> List.exists (githubLoadStarted wsId)
        Assert.True(started, "github-load StartActor was not for the Workspace")
        Assert.Equal(Some ActorSucceeded, stopped)
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
let ``explicit git Load of a File parses disk and does not pull`` () = task {
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
        commitFile harness.source "from-remote.txt" "pulled" "add-remote"
        git harness.source "push origin main" |> ignore
        do!
            postDeskLoad
                LoadSavePrePick.Git
                harness.host
                harness.dataDir
                harness.seed
                fileId
        do! assertParsed harness.host fileId "seed"
        Assert.Equal(
            "seed",
            File.ReadAllText(Path.Combine(harness.workspace, "note.txt")))
        Assert.False(
            File.Exists(Path.Combine(harness.workspace, "from-remote.txt")),
            "explicit git File Load pulled the remote")
        assertNoLoadActor harness.host [ fileId; harness.seed.workspaceId ]
    finally
        CoreMailboxApi.dispose harness.host
}

[<SkippableFact>]
let ``explicit git Load of a Directory reconciles disk and does not pull`` () =
    task {
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
            let local = Path.Combine(harness.workspace, "docs", "local.txt")
            Directory.CreateDirectory(Path.GetDirectoryName local) |> ignore
            File.WriteAllText(local, "local")
            commitFile harness.source "docs/remote.txt" "remote" "add-remote"
            git harness.source "push origin main" |> ignore
            do!
                postDeskLoad
                    LoadSavePrePick.Git
                    harness.host
                    harness.dataDir
                    harness.seed
                    dirId
            do! assertDirectoryParsed harness.host dirId "local.txt"
            Assert.False(
                File.Exists(
                    Path.Combine(harness.workspace, "docs", "remote.txt")),
                "explicit git Directory Load pulled the remote")
            assertNoLoadActor harness.host [ dirId; harness.seed.workspaceId ]
        finally
            CoreMailboxApi.dispose harness.host
    }

[<SkippableFact>]
let ``explicit git Load of a Normal node in a Workspace does not pull`` () =
    task {
        Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
        let harness = createGitHarness "load"
        try
            commitFile harness.source "from-remote.txt" "pulled" "add-remote"
            git harness.source "push origin main" |> ignore
            do!
                postDeskLoad
                    LoadSavePrePick.Git
                    harness.host
                    harness.dataDir
                    harness.seed
                    harness.seed.focusId
            Assert.False(
                File.Exists(Path.Combine(harness.workspace, "from-remote.txt")),
                "explicit git Load of a Normal node pulled the remote")
            assertNoLoadActor
                harness.host
                [ harness.seed.focusId; harness.seed.workspaceId ]
        finally
            CoreMailboxApi.dispose harness.host
    }

[<SkippableFact>]
let ``explicit git Load of a Workspace pulls and reconciles`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let harness = createGitHarnessOn createParsingHost "load"
    try
        commitFile harness.source "from-remote.txt" "pulled" "remote-change"
        git harness.source "push origin main" |> ignore
        do!
            postRoutedFocus
                LoadSaveOperation.Load
                LoadSavePrePick.Git
                harness.seed.workspaceId
                harness
        let! stopped = waitForStop harness.host harness.seed.workspaceId
        Assert.Equal(Some ActorSucceeded, stopped)
        Assert.Equal(
            "pulled",
            File.ReadAllText(
                Path.Combine(harness.workspace, "from-remote.txt")))
        do!
            assertDirectoryParsed
                harness.host
                harness.seed.workspaceId
                "from-remote.txt"
    finally
        CoreMailboxApi.dispose harness.host
}

[<Fact>]
let ``failed git Load of a Workspace does not reconcile`` () = task {
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
        CoreMailboxApi.postNotice host (Notice.NodeUnparsed seed.workspaceId)
        let disk = Path.Combine(dataDir, "home", "extra.txt")
        Directory.CreateDirectory(Path.GetDirectoryName disk) |> ignore
        File.WriteAllText(disk, "extra")
        let command =
            requestForFocus
                LoadSaveOperation.Load
                LoadSavePrePick.Git
                seed
                seed.workspaceId
        let! result =
            Api.postLoadSaveCommand
                (actorRouter host dataDir dependencies LoadSaveOperation.Load)
                (CoreDoor.create true host testCaller)
                (encodeRequest command)
            |> Async.StartAsTask
        Assert.Equal(LoadSavePath.Git, (requireResponse result).path)
        let! stopped = waitForStop host seed.workspaceId
        Assert.Equal(Some (ActorFailed conflict), stopped)
        let after = readGraph host
        Assert.Equal(
            Unparsed,
            Node.documentStateEnum after.nodes.[seed.workspaceId])
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
