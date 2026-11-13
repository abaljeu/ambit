module Gambol.Server.Tests.LoadSaveCommandTests

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.HttpResults
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend
open Thoth.Json.Newtonsoft

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private encodeRequest request =
    EventJson.encodeLoadSaveCommandRequest request
    |> Encode.toString 0

let private request operation prePick nodeId eventId =
    { operation = operation
      prePick = prePick
      holdStack = false
      start =
        { zoomId = nodeId
          focusId = nodeId
          runDefinitionId = nodeId
          graphIds = [ Graph.rootId; nodeId ]
          eventId = eventId } }

let private requireResponse (result: IResult) =
    match box result with
    | :? ContentHttpResult as content ->
        match
            Decode.fromString
                ApiResponseSerialization.decodeLoadSaveCommandResponseDecoder
                content.ResponseContent
        with
        | Result.Ok response -> response
        | Result.Error err ->
            Assert.Fail(err)
            Unchecked.defaultof<_>
    | other ->
        Assert.Fail($"expected JSON content, got {other.GetType().Name}")
        Unchecked.defaultof<_>

let private requireOk label =
    function
    | Result.Ok value -> value
    | Result.Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

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

type private Seed =
    { workspaceId: NodeId
      runDefinitionId: NodeId
      focusId: NodeId
      eventId: EventId }

type private GitHarness =
    { dataDir: string
      parent: string
      remote: string
      source: string
      workspace: string
      host: MailboxHost
      seed: Seed }

let private git root arguments =
    GitSave.runGit root arguments |> requireOk arguments

let private configureIdentity root =
    git root "config user.email test@gambol" |> ignore
    git root "config user.name test" |> ignore

let private commitFile
    (root: string)
    (path: string)
    (text: string)
    (message: string)
    =
    File.WriteAllText(Path.Combine(root, path), text)
    git root "add -A" |> ignore
    git root $"commit -m {message}" |> ignore

let private createTrackedWorkspace dataDir =
    let parent = newTempDir ()
    let remote = Path.Combine(parent, "remote.git")
    let source = Path.Combine(parent, "source")
    let workspace = Path.Combine(dataDir, "home")
    Directory.CreateDirectory(remote) |> ignore
    Directory.CreateDirectory(source) |> ignore
    git remote "init --bare -b main" |> ignore
    git source "init -b main" |> ignore
    configureIdentity source
    commitFile source "note.txt" "seed" "seed"
    git source $"remote add origin {remote}" |> ignore
    git source "push -u origin main" |> ignore
    git dataDir $"clone {remote} home" |> ignore
    configureIdentity workspace
    parent, remote, source, workspace

let private createProductionHost dataDir =
    let pool = CoreActorPool.create ()
    CoreMailbox.host
        pool
        (FileAgent.persist (FileAgent.create dataDir))
        admittedCredentials

let private postSeed host ops =
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    CoreMailboxApi.postGraphOnly host testCaller event
    |> Async.RunSynchronously
    |> requireOk "seed workspace"

let private markCurrent host nodeId =
    CoreMailboxApi.postNotice host (Notice.NodeParsed nodeId)
    let state =
        CoreMailboxApi.graphAtEvent host
        |> Async.RunSynchronously
        |> requireOk "mark current"
    match Map.tryFind nodeId state.graph.nodes with
    | Some node when Node.documentStateEnum node = Current ->
        state.graph
    | _ ->
        Assert.Fail("NodeParsed did not set Current")
        Unchecked.defaultof<_>

let private seedWorkspace host operationName =
    let workspaceId, workspaceOps =
        FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let runDefinitionId, focusId = NodeId.New(), NodeId.New()
    postSeed host workspaceOps |> ignore
    let graph = markCurrent host workspaceId
    let children = Graph.children graph workspaceId
    let accepted =
        postSeed
            host
            [ Op.NewNode(runDefinitionId, operationName)
              Op.NewNode(focusId, "focus")
              Op.Replace(
                  workspaceId,
                  children,
                  children
                  @ [ ChildNode.owner runDefinitionId
                      ChildNode.owner focusId ]) ]
    { workspaceId = workspaceId
      runDefinitionId = runDefinitionId
      focusId = focusId
      eventId = accepted.eventId }

let private createGitHarness operationName =
    let dataDir = newTempDir ()
    let parent, remote, source, workspace =
        createTrackedWorkspace dataDir
    let host = createProductionHost dataDir
    { dataDir = dataDir
      parent = parent
      remote = remote
      source = source
      workspace = workspace
      host = host
      seed = seedWorkspace host operationName }

let private operationFor =
    function
    | LoadSaveOperation.Load -> GithubTransportOperation.Load
    | LoadSaveOperation.Save -> GithubTransportOperation.Save

let private productionRouter harness =
    { resolvePath = LoadSaveRouting.resolvePath
      startCommand =
        LoadSaveStart.startCommand
            (CoreDoor.create true harness.host testCaller)
            harness.dataDir }

let private requestFor operation prePick seed =
    { operation = operation
      prePick = prePick
      holdStack = false
      start =
        { zoomId = seed.workspaceId
          focusId = seed.focusId
          runDefinitionId = seed.runDefinitionId
          graphIds =
            [ Graph.rootId
              Graph.workspacesId
              seed.workspaceId
              seed.runDefinitionId
              seed.focusId ]
          eventId = seed.eventId } }

let private requestForFocus operation prePick seed focusId =
    let command = requestFor operation prePick seed
    { command with
        start =
            { command.start with
                zoomId = focusId
                focusId = focusId } }

let private postRoutedFocus operation prePick focusId harness = task {
    let command = requestForFocus operation prePick harness.seed focusId
    let! result =
        Api.postLoadSaveCommand
            (productionRouter harness)
            (CoreDoor.create true harness.host testCaller)
            (encodeRequest command)
        |> Async.StartAsTask
    let response = requireResponse result
    Assert.Equal(LoadSavePath.Git, response.path)
    Assert.True(Option.isSome response.command)
}

let private postRouted operation prePick harness =
    postRoutedFocus operation prePick harness.seed.focusId harness

let rec private waitForStopUntil
    host
    focusId
    (deadline: DateTime)
    (delayMs: int)
    =
    task {
        let! history =
            CoreMailboxApi.eventHistory host |> Async.StartAsTask
        let stopped =
            history.events
            |> List.tryPick (fun event ->
                match event.body with
                | EventBody.ActorStop(id, result) when id = focusId ->
                    Some result
                | _ -> None)
        match stopped with
        | Some result -> return Some result
        | None when DateTime.UtcNow >= deadline -> return None
        | None ->
            do! Task.Delay delayMs
            return!
                waitForStopUntil
                    host focusId deadline (min 250 (delayMs * 2))
    }

let private waitForStop host focusId =
    waitForStopUntil
        host focusId (DateTime.UtcNow.AddSeconds 30.0) 10

let private actorStops host focusId = task {
    let! history =
        CoreMailboxApi.eventHistory host |> Async.StartAsTask
    return
        history.events
        |> List.filter (fun event ->
            match event.body with
            | EventBody.ActorStop(id, _) -> id = focusId
            | _ -> false)
        |> List.length
}

let rec private waitForStopCount host focusId count deadline = task {
    let! found = actorStops host focusId
    if found >= count || DateTime.UtcNow >= deadline then
        return found
    else
        do! Task.Delay 10
        return! waitForStopCount host focusId count deadline
}

let private decodePoll result =
    match box result with
    | :? ContentHttpResult as content ->
        Decode.fromString
            ApiResponseSerialization.decodeChangeSuccessResponseDecoder
            content.ResponseContent
        |> requireOk "decode Poll"
    | other ->
        failwith $"expected Poll JSON, got {other.GetType().Name}"

let private pollSince harness = task {
    let body =
        ApiResponseSerialization.encodePollRequest
            { eventId = harness.seed.eventId; want = [] }
        |> Encode.toString 0
    let! result =
        Api.postPoll
            (CoreDoor.create true harness.host testCaller)
            10
            20
            body
        |> Async.StartAsTask
    return decodePoll result
}

let private hasActorStart focusId event =
    match event.body with
    | EventBody.ActorStart start -> start.focusId = focusId
    | _ -> false

let private hasStartedActor focusId event =
    match event.body with
    | EventBody.Order (Order.StartActor order) ->
        order.actorNodeId = focusId
    | _ -> false

let private hasActorStop focusId event =
    match event.body with
    | EventBody.ActorStop(id, ActorSucceeded) -> id = focusId
    | _ -> false

let private actorLeft focusId event =
    match event.body with
    | EventBody.ActorStop(id, _) -> id = focusId
    | _ -> false

/// Dispose joins the persist thread, not the Git actor. After start,
/// that actor can still hold the Workspace lock, so wait until it stops.
let rec private waitUntilActorStops host focusId remainingMs = task {
    let! history =
        CoreMailboxApi.eventHistory host |> Async.StartAsTask
    if List.exists (actorLeft focusId) history.events then
        return true
    elif remainingMs <= 0 then
        return false
    else
        do! Task.Delay 10
        return! waitUntilActorStops host focusId (remainingMs - 10)
}

let private isParseChange event =
    event.commandName = "Parse"
    && match event.body with
       | EventBody.Change _ -> true
       | _ -> false

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

let private routeMatrixHarness operation operationName =
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
        routeMatrixHarness operation operationName
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
let ``plain Load starts the Load actor when a remote is reported`` () =
    task {
        let operation = LoadSaveOperation.Load
        let host, seed, started, dataDir, dependencies =
            routeMatrixHarness operation "load"
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
            Assert.Equal(LoadSavePath.Git, response.path)
            Assert.True(response.command.IsSome)
            do! started.Task.WaitAsync(TimeSpan.FromSeconds 2.0)
            let! history =
                CoreMailboxApi.eventHistory host |> Async.StartAsTask
            Assert.Contains(
                history.events,
                hasStartedActor seed.focusId)
        finally
            CoreMailboxApi.dispose host
    }

[<SkippableFact>]
let ``routed Git Save commits then pushes through Peer Actor`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let harness = createGitHarness "save"
    try
        File.WriteAllText(
            Path.Combine(harness.workspace, "saved.txt"),
            "saved")
        do!
            postRoutedFocus
                LoadSaveOperation.Save
                LoadSavePrePick.Git
                harness.seed.workspaceId
                harness
        let! stopped =
            waitForStop harness.host harness.seed.workspaceId
        Assert.Equal(Some ActorSucceeded, stopped)
        let verify = Path.Combine(harness.parent, "verify")
        git harness.parent $"clone {harness.remote} verify" |> ignore
        Assert.True(File.Exists(Path.Combine(verify, "saved.txt")))
        Assert.Equal(
            git verify "rev-parse refs/heads/main",
            git harness.workspace "rev-parse refs/heads/main")
    finally
        CoreMailboxApi.dispose harness.host
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

let private postChange host ops =
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    CoreMailboxApi.postGraphOnly host testCaller event
    |> Async.RunSynchronously
    |> requireOk "post change"
    |> ignore

let private readGraph host =
    CoreMailboxApi.graphAtEvent host
    |> Async.RunSynchronously
    |> requireOk "state"
    |> fun state -> state.graph

let private childNamed graph parentId name =
    Graph.children graph parentId
    |> List.pick (fun child ->
        match Filename.tryValue graph.nodes.[child.id].name with
        | Some candidate when candidate = name -> Some graph.nodes.[child.id]
        | _ -> None)

let private publishStaging parent =
    let remote = Path.Combine(parent, "remote.git")
    let source = Path.Combine(parent, "source")
    Directory.CreateDirectory(remote) |> ignore
    Directory.CreateDirectory(source) |> ignore
    git remote "init --bare -b staging" |> ignore
    git source "init -b staging" |> ignore
    configureIdentity source
    File.WriteAllText(Path.Combine(source, "note.txt"), "from-staging")
    File.WriteAllText(Path.Combine(source, "arrived.txt"), "arrived")
    git source "add -A" |> ignore
    git source "commit -m seed" |> ignore
    git source $"remote add origin {remote}" |> ignore
    git source "push -u origin staging" |> ignore
    remote

let private fetchSwitchStaging workspace remote =
    git workspace "init -b master" |> ignore
    git workspace $"remote add origin {remote}" |> ignore
    git workspace "fetch" |> ignore
    git workspace "switch staging" |> ignore

let private applyTail label events state =
    match SyncLogic.applyServerTail events state with
    | Ok next -> next
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private pollFrom host after = task {
    let body =
        ApiResponseSerialization.encodePollRequest
            { eventId = after; want = [] }
        |> Encode.toString 0
    let! result =
        Api.postPoll
            (CoreDoor.create true host testCaller)
            10
            20
            body
        |> Async.StartAsTask
    return decodePoll result
}

/// Git Load of a Directory still posts github-load. The client must drop Cancel.
[<Fact>]
let ``git Load of a Directory clears Cancel after the actor stops`` () = task {
    let operation = LoadSaveOperation.Load
    let host, seed, _started, dataDir, dependencies =
        routeMatrixHarness operation "load"
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

[<SkippableFact>]
let ``plain Directory Load with a remote pulls through the Load actor`` () =
    task {
        Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
        let harness = createGitHarness "load"
        try
            let dirId, dirOps =
                FileNodeOps.planCreateOwnedDirectory
                    (readGraph harness.host)
                    harness.seed.workspaceId
                    "docs"
            postChange harness.host dirOps
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
                    dirId
                    harness
            let! stopped = waitForStop harness.host dirId
            Assert.Equal(Some ActorSucceeded, stopped)
            Assert.Equal(
                "pulled",
                File.ReadAllText(
                    Path.Combine(harness.workspace, "from-remote.txt")))
            let! history =
                CoreMailboxApi.eventHistory harness.host
                |> Async.StartAsTask
            let started =
                history.events |> List.exists (githubLoadStarted dirId)
            Assert.True(started, "github-load did not start for the Directory")
        finally
            CoreMailboxApi.dispose harness.host
    }

let private countingGit () =
    let pulls = ResizeArray<string>()
    let pushes = ResizeArray<string>()
    let tracked = {
        branch = "main"
        remote = "origin"
        upstream = "refs/heads/main"
    }
    let dependencies = {
        git = {
            remoteExists = WorkspaceGit.remoteExists
            fetchTracked = fun _ _ -> Result.Ok "fetched"
            pullTracked =
                fun root _ ->
                    pulls.Add root
                    Result.Ok "pulled"
            commitAll =
                fun _ _ _ -> Result.Ok(tracked, "committed")
            pushTracked =
                fun root _ _ ->
                    pushes.Add root
                    Result.Ok "pushed"
        }
    }
    pulls, pushes, dependencies

let private actorRouter host dataDir dependencies operation =
    { resolvePath = LoadSaveRouting.resolvePath
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

[<Fact>]
let ``plain File Load with no Git is Neither through the Load actor`` () =
    task {
        let dataDir = newTempDir ()
        let host = createProductionHost dataDir
        let pulls, pushes, dependencies = countingGit ()
        try
            let seed = seedWorkspace host "load"
            let fileId, fileOps =
                FileNodeOps.planCreateOwnedFile
                    (readGraph host)
                    seed.workspaceId
                    "note.txt"
            postChange host fileOps
            let command =
                requestForFocus
                    LoadSaveOperation.Load
                    LoadSavePrePick.Plain
                    seed
                    fileId
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
            let response = requireResponse result
            Assert.Equal(LoadSavePath.Git, response.path)
            let! stopped = waitForStop host fileId
            Assert.Equal(Some ActorSucceeded, stopped)
            Assert.Empty(pulls)
            Assert.Empty(pushes)
            let! history =
                CoreMailboxApi.eventHistory host |> Async.StartAsTask
            let started =
                history.events |> List.exists (githubLoadStarted fileId)
            Assert.True(started, "Neither File Load posted no StartActor")
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

let private withRemote (remoteExists: string -> Result<bool, string>) =
    let pulls, pushes, dependencies = countingGit ()
    pulls,
    pushes,
    { git = { dependencies.git with remoteExists = remoteExists } }

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
            let dirId, dirOps =
                FileNodeOps.planCreateOwnedDirectory
                    (readGraph host)
                    seed.workspaceId
                    "docs"
            postChange host dirOps
            let command =
                requestForFocus
                    LoadSaveOperation.Load
                    LoadSavePrePick.Plain
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
            Assert.Equal(Some (ActorFailed gitError), stopped)
            Assert.Empty(pulls)
            let! history =
                CoreMailboxApi.eventHistory host |> Async.StartAsTask
            Assert.True(
                history.events |> List.exists (githubLoadStarted dirId),
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
            Assert.Equal(LoadSavePath.Git, (requireResponse loaded).path)
            let! loadStopped = waitForStop host fileId
            Assert.Equal(Some ActorSucceeded, loadStopped)
            Assert.Empty(pulls)
            Assert.Empty(pushes)
            let! beforeSave =
                CoreMailboxApi.eventHistory host |> Async.StartAsTask
            let stopsBefore =
                beforeSave.events
                |> List.filter (fun event ->
                    match event.body with
                    | EventBody.ActorStop(id, _) -> id = fileId
                    | _ -> false)
                |> List.length
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
                    (stopsBefore + 1)
                    (DateTime.UtcNow.AddSeconds 5.0)
            Assert.True(stopsAfter > stopsBefore, "Save posted no stop")
            let dataRoot =
                Path.GetFullPath(DocumentPersistPath.dataDirBase dataDir)
            let recorded roots =
                roots
                |> Seq.map Path.GetFullPath
                |> Seq.toList
            Assert.Equal<string list>([ dataRoot ], recorded pulls)
            Assert.Equal<string list>([ dataRoot ], recorded pushes)
            let! history =
                CoreMailboxApi.eventHistory host |> Async.StartAsTask
            let started =
                history.events |> List.exists (githubLoadStarted fileId)
            Assert.True(started, "outside-workspace Load posted no StartActor")
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

let private assertDeskResponse (result: IResult) =
    let response = requireResponse result
    Assert.Equal(LoadSavePath.Desk, response.path)
    Assert.True(response.command.IsNone)

let private postCommand operation prePick harness = task {
    let command = requestFor operation prePick harness.seed
    let! result =
        Api.postLoadSaveCommand
            (productionRouter harness)
            (CoreDoor.create true harness.host testCaller)
            (encodeRequest command)
        |> Async.StartAsTask
    return result
}

/// Plain Save of a file in a workspace that has a git remote pushes that work tree.
[<SkippableFact>]
let ``plain Save with a GitHub remote pushes from a file focus`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let harness = createGitHarness "save"
    try
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                (readGraph harness.host)
                harness.seed.workspaceId
                "note.txt"
        postChange harness.host fileOps
        File.WriteAllText(
            Path.Combine(harness.workspace, "saved.txt"),
            "saved")
        do!
            postRoutedFocus
                LoadSaveOperation.Save
                LoadSavePrePick.Plain
                fileId
                harness
        let! stopped = waitForStop harness.host fileId
        Assert.Equal(Some ActorSucceeded, stopped)
        let verify = Path.Combine(harness.parent, "verify-plain")
        git harness.parent $"clone {harness.remote} verify-plain" |> ignore
        Assert.Equal(
            "saved",
            File.ReadAllText(Path.Combine(verify, "saved.txt")))
        match GitSave.runGit verify "log --oneline --grep=gambol:" with
        | Ok text ->
            let commits =
                text.Split(
                    [| '\r'; '\n' |],
                    StringSplitOptions.RemoveEmptyEntries)
            Assert.Equal(1, commits.Length)
        | Error err -> Assert.Fail(err)
    finally
        CoreMailboxApi.dispose harness.host
}

/// A repository with no remote is Save Neither: persist, one commit, no push.
[<SkippableFact>]
let ``plain Save with no GitHub remote commits once and does not push`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let dataDir = newTempDir ()
    let workspace = Path.Combine(dataDir, "home")
    Directory.CreateDirectory(workspace) |> ignore
    git workspace "init -b master" |> ignore
    configureIdentity workspace
    let host = createProductionHost dataDir
    try
        let seed = seedWorkspace host "save"
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                (readGraph host)
                seed.workspaceId
                "note.txt"
        postChange host fileOps
        let state =
            CoreMailboxApi.graphAtEvent host
            |> Async.RunSynchronously
            |> requireOk "file event"
        let harness =
            { dataDir = dataDir
              parent = dataDir
              remote = ""
              source = ""
              workspace = workspace
              host = host
              seed =
                { seed with
                    focusId = fileId
                    eventId = state.eventId } }
        let! result =
            postCommand
                LoadSaveOperation.Save
                LoadSavePrePick.Plain
                harness
        let response = requireResponse result
        Assert.Equal(LoadSavePath.Git, response.path)
        let! stopped = waitForStop host fileId
        Assert.Equal(Some ActorSucceeded, stopped)
        let saved = readGraph host
        Assert.Equal(ParseState.Parsed, saved.nodes.[fileId].parseState)
        Assert.True(File.Exists(Path.Combine(workspace, "note.txt")))
        let saveCommits () =
            match GitSave.runGit workspace "log --oneline --grep=gambol:" with
            | Ok text ->
                text.Split(
                    [| '\r'; '\n' |],
                    StringSplitOptions.RemoveEmptyEntries)
                |> Array.length
            | Error err ->
                Assert.Fail(err)
                0
        Assert.Equal(1, saveCommits ())
        let! stopsBefore = actorStops host fileId
        let again =
            CoreMailboxApi.graphAtEvent host
            |> Async.RunSynchronously
            |> requireOk "after save"
        let! second =
            postCommand
                LoadSaveOperation.Save
                LoadSavePrePick.Plain
                { harness with
                    seed =
                        { harness.seed with
                            eventId = again.eventId } }
        Assert.Equal(LoadSavePath.Git, (requireResponse second).path)
        let! stopsAfter =
            waitForStopCount
                host
                fileId
                (stopsBefore + 1)
                (DateTime.UtcNow.AddSeconds 30.0)
        Assert.True(stopsAfter > stopsBefore, "second Save posted no stop")
        Assert.Equal(1, saveCommits ())
    finally
        CoreMailboxApi.dispose host
}

/// Explicit desk Save stays Desk when the work tree has a remote.
[<SkippableFact>]
let ``explicit Desk Save with a GitHub remote takes the Desk path`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let harness = createGitHarness "save"
    try
        let! result =
            postCommand
                LoadSaveOperation.Save
                LoadSavePrePick.Desk
                harness
        assertDeskResponse result
        let! history =
            CoreMailboxApi.eventHistory harness.host |> Async.StartAsTask
        Assert.DoesNotContain(
            history.events,
            hasActorStart harness.seed.focusId)
    finally
        CoreMailboxApi.dispose harness.host
}

[<Fact>]
let ``Desk load-save runs no Actor and posts no StartActor`` () = task {
    let pool = CoreActorPool.create ()
    let host =
        CoreMailbox.host
            pool
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
        Assert.Empty(pool.liveActorNodeIds ())
        let! history =
            CoreMailboxApi.eventHistory host |> Async.StartAsTask
        let live = ActorLive.applyEvents history.events Set.empty
        Assert.False(Set.contains nodeId live)
        let started =
            history.events
            |> List.exists (fun event ->
                match event.body with
                | EventBody.Order (Order.StartActor _)
                | EventBody.ActorStop _ -> true
                | _ -> false)
        Assert.False(started, "Desk Load posted an actor lifecycle row")
    finally
        CoreMailboxApi.dispose host
}

/// A workspace folder with no repository is Save Neither, and it still persists.
[<SkippableFact>]
let ``plain Save of a non-git workspace persists and does not push`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let dataDir = newTempDir ()
    let host = createProductionHost dataDir
    try
        let seed = seedWorkspace host "save"
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                (readGraph host)
                seed.workspaceId
                "note.txt"
        postChange host fileOps
        let state =
            CoreMailboxApi.graphAtEvent host
            |> Async.RunSynchronously
            |> requireOk "file event"
        let harness =
            { dataDir = dataDir
              parent = dataDir
              remote = ""
              source = ""
              workspace = Path.Combine(dataDir, "home")
              host = host
              seed =
                { seed with
                    focusId = fileId
                    eventId = state.eventId } }
        let! result =
            postCommand
                LoadSaveOperation.Save
                LoadSavePrePick.Plain
                harness
        let response = requireResponse result
        Assert.Equal(LoadSavePath.Git, response.path)
        let! stopped = waitForStop host fileId
        Assert.Equal(Some ActorSucceeded, stopped)
        let saved = readGraph host
        Assert.Equal(ParseState.Parsed, saved.nodes.[fileId].parseState)
        Assert.True(File.Exists(Path.Combine(dataDir, "home", "note.txt")))
    finally
        CoreMailboxApi.dispose host
}

/// A parent repository is not the workspace. No `.git` in the workspace
/// is Neither: persist, and no commit, pull, or push.
[<SkippableFact>]
let ``plain Save of a non-git workspace ignores a parent repository`` () =
    task {
        Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
        let parent = newTempDir ()
        let remote = Path.Combine(parent, "remote.git")
        let dataDir = Path.Combine(parent, "data")
        Directory.CreateDirectory(remote) |> ignore
        Directory.CreateDirectory(dataDir) |> ignore
        git remote "init --bare -b master" |> ignore
        git dataDir "init -b master" |> ignore
        configureIdentity dataDir
        commitFile dataDir "seed.txt" "seed" "seed"
        git dataDir $"remote add origin {remote}" |> ignore
        git dataDir "push -u origin master" |> ignore
        let host = createProductionHost dataDir
        try
            let seed = seedWorkspace host "save"
            let fileId, fileOps =
                FileNodeOps.planCreateOwnedFile
                    (readGraph host)
                    seed.workspaceId
                    "note.txt"
            postChange host fileOps
            let state =
                CoreMailboxApi.graphAtEvent host
                |> Async.RunSynchronously
                |> requireOk "file event"
            let harness =
                { dataDir = dataDir
                  parent = parent
                  remote = remote
                  source = ""
                  workspace = Path.Combine(dataDir, "home")
                  host = host
                  seed =
                    { seed with
                        focusId = fileId
                        eventId = state.eventId } }
            let! result =
                postCommand
                    LoadSaveOperation.Save
                    LoadSavePrePick.Plain
                    harness
            Assert.Equal(LoadSavePath.Git, (requireResponse result).path)
            let! stopped = waitForStop host fileId
            Assert.Equal(Some ActorSucceeded, stopped)
            let note = Path.Combine(dataDir, "home", "note.txt")
            Assert.True(File.Exists note)
            Assert.False(
                Directory.Exists(Path.Combine(dataDir, "home", ".git")))
            match GitSave.runGit dataDir "log --oneline --grep=gambol:" with
            | Ok text -> Assert.True(String.IsNullOrWhiteSpace text)
            | Error err -> Assert.Fail(err)
            let verify = Path.Combine(parent, "verify-parent")
            git parent $"clone {remote} verify-parent" |> ignore
            Assert.False(
                File.Exists(Path.Combine(verify, "home", "note.txt")))
        finally
            CoreMailboxApi.dispose host
    }

/// No work tree is the same Neither arm: persist, and no commit, pull, or push.
[<Fact>]
let ``plain Save with no work tree persists and does not commit pull or push`` () =
    task {
        let dataDir = newTempDir ()
        let host = createProductionHost dataDir
        let pulls, pushes, dependencies =
            withRemote (fun _ -> Result.Ok true)
        try
            let fileId = NodeId.New()
            postChange
                host
                [ Op.NewSpecialNode(fileId, SpecialKind.File, "loose.txt") ]
            let state =
                CoreMailboxApi.graphAtEvent host
                |> Async.RunSynchronously
                |> requireOk "detached file"
            let seed =
                { workspaceId = Graph.rootId
                  runDefinitionId = fileId
                  focusId = fileId
                  eventId = state.eventId }
            let! result =
                Api.postLoadSaveCommand
                    (actorRouter
                        host
                        dataDir
                        dependencies
                        LoadSaveOperation.Save)
                    (CoreDoor.create true host testCaller)
                    (encodeRequest
                        (requestForFocus
                            LoadSaveOperation.Save
                            LoadSavePrePick.Plain
                            seed
                            fileId))
                |> Async.StartAsTask
            Assert.Equal(LoadSavePath.Git, (requireResponse result).path)
            let! stopped = waitForStop host fileId
            Assert.Equal(Some ActorSucceeded, stopped)
            Assert.Empty(pulls)
            Assert.Empty(pushes)
            let saved = readGraph host
            Assert.Equal(ParseState.Parsed, saved.nodes.[fileId].parseState)
        finally
            CoreMailboxApi.dispose host
    }

/// ROOT's repository is the data directory. A remote there is git Save.
[<SkippableFact>]
let ``plain Save of ROOT with a data-directory remote commits that repository`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let parent = newTempDir ()
    let remote = Path.Combine(parent, "remote.git")
    let source = Path.Combine(parent, "source")
    let dataDir = Path.Combine(parent, "data")
    Directory.CreateDirectory(remote) |> ignore
    Directory.CreateDirectory(source) |> ignore
    git remote "init --bare -b main" |> ignore
    git source "init -b main" |> ignore
    configureIdentity source
    commitFile source "seed.txt" "seed" "seed"
    git source $"remote add origin {remote}" |> ignore
    git source "push -u origin main" |> ignore
    git parent $"clone {remote} data" |> ignore
    configureIdentity dataDir
    let host = createProductionHost dataDir
    try
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                (readGraph host)
                Graph.rootId
                "root.txt"
        postChange host fileOps
        let state =
            CoreMailboxApi.graphAtEvent host
            |> Async.RunSynchronously
            |> requireOk "root file"
        let harness =
            { dataDir = dataDir
              parent = parent
              remote = remote
              source = source
              workspace = dataDir
              host = host
              seed =
                { workspaceId = Graph.rootId
                  runDefinitionId = fileId
                  focusId = fileId
                  eventId = state.eventId } }
        do!
            postRoutedFocus
                LoadSaveOperation.Save
                LoadSavePrePick.Plain
                fileId
                harness
        let! stopped = waitForStop host fileId
        Assert.Equal(Some ActorSucceeded, stopped)
        let verify = Path.Combine(parent, "verify-root")
        git parent $"clone {remote} verify-root" |> ignore
        Assert.True(File.Exists(Path.Combine(verify, "root.txt")))
    finally
        CoreMailboxApi.dispose host
}
