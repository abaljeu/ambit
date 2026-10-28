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
          commandId = nodeId
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
      commandId: NodeId
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

let private registerPeer dataDir pool operation dependencies =
    pool.registerPeer
        (GithubTransportActor.peerName operation)
        (GithubTransportActor.actorFn
            dataDir operation dependencies)

let private createProductionHost dataDir =
    let pool = CoreActorPool.create ()
    let dependencies =
        GithubTransportActor.productionDependencies
    registerPeer dataDir pool GithubTransportOperation.Load dependencies
    registerPeer dataDir pool GithubTransportOperation.Save dependencies
    CoreMailbox.host
        pool
        (FileAgent.persist (FileAgent.create dataDir))
        admittedCredentials

let private seedWorkspace host operationName =
    let workspaceId, workspaceOps =
        FileNodeOps.planCreateWorkspace (Graph.create ()) "home"
    let commandId, focusId = NodeId.New(), NodeId.New()
    let ops =
        workspaceOps
        @ [ Op.SetDocumentState(workspaceId, Unparsed, Current)
            Op.NewNode(commandId, operationName)
            Op.NewNode(focusId, "focus")
            Op.Replace(
                workspaceId,
                [],
                [ ChildNode.owner commandId
                  ChildNode.owner focusId ]) ]
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    let accepted =
        CoreMailboxApi.postGraphOnly host testCaller event
        |> Async.RunSynchronously
        |> requireOk "seed workspace"
    { workspaceId = workspaceId
      commandId = commandId
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
    { resolvePath = LoadSaveRouting.resolvePath harness.dataDir
      startCommand =
        fun path command ->
            CoreMailboxApi.startLoadSaveCommand
                harness.host
                testCaller
                path
                (GithubTransportActor.peerName
                    (operationFor command.operation))
                command }

let private requestFor operation prePick seed =
    { operation = operation
      prePick = prePick
      holdStack = false
      start =
        { zoomId = seed.workspaceId
          focusId = seed.focusId
          commandId = seed.commandId
          graphIds =
            [ Graph.rootId
              Graph.workspacesId
              seed.workspaceId
              seed.commandId
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

let private hasActorStop focusId event =
    match event.body with
    | EventBody.ActorStop(id, ActorSucceeded) -> id = focusId
    | _ -> false

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
        { withWorkTreeGate =
            { new GithubTransportWorkTreeGate with
                member _.applyAction _ action = action () }
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
    let pool = CoreActorPool.create ()
    registerPeer
        dataDir pool (operationFor operation)
        (routeMatrixDependencies started)
    let host =
        CoreMailbox.host
            pool
            (FileAgent.persist (FileAgent.create dataDir))
            admittedCredentials
    host, seedWorkspace host operationName, started

let private routeMatrixRouter host operation =
    { resolvePath =
        fun _ command ->
            PathPick.resolveCommand
                { operation = command.operation
                  prePick = command.prePick
                  subject = Some LoadSubject.Directory }
                (fun () -> Result.Ok true)
      startCommand =
        fun path command ->
            CoreMailboxApi.startLoadSaveCommand
                host testCaller path
                (GithubTransportActor.peerName
                    (operationFor operation))
                command }

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
    let host, seed, started =
        routeMatrixHarness operation operationName
    try
        let command = requestFor operation prePick seed
        let! result =
            Api.postLoadSaveCommand
                (routeMatrixRouter host operation)
                (CoreDoor.create true host testCaller)
                (encodeRequest command)
            |> Async.StartAsTask
        let response = requireResponse result
        Assert.Equal(LoadSavePath.Git, response.path)
        Assert.True(Option.isSome response.command)
        do! started.Task.WaitAsync(TimeSpan.FromSeconds 1.0)
        let! history = CoreMailboxApi.eventHistory host |> Async.StartAsTask
        Assert.Contains(history.events, hasActorStart seed.focusId)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``plain Load of a non-Workspace returns Desk when a remote exists`` () =
    task {
        let operation = LoadSaveOperation.Load
        let host, seed, started =
            routeMatrixHarness operation "load"
        try
            let command =
                requestFor operation LoadSavePrePick.Plain seed
            let! result =
                Api.postLoadSaveCommand
                    (routeMatrixRouter host operation)
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
let ``routed Git Save commits then pushes through Peer Actor`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let harness = createGitHarness "save"
    try
        File.WriteAllText(
            Path.Combine(harness.workspace, "saved.txt"),
            "saved")
        do!
            postRouted
                LoadSaveOperation.Save
                LoadSavePrePick.Git
                harness
        let! stopped =
            waitForStop harness.host harness.seed.focusId
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
            hasActorStart harness.seed.workspaceId)
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
    CoreMailboxApi.getState host
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

[<SkippableFact(Skip = "pending documentState/Load spec")>]
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
        let _, fileOps =
            FileNodeOps.planCreateOwnedFile graph seed.workspaceId "note.txt"
        postChange host fileOps
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
            Current,
            (childNamed after seed.workspaceId "note.txt").documentState.AsEnum)
    finally
        CoreMailboxApi.dispose host
}

[<SkippableFact>]
let ``plain Load of a File or Directory stays Desk when a remote exists`` () =
    task {
        Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
        let harness = createGitHarness "load"
        try
            let graph = readGraph harness.host
            let fileId, fileOps =
                FileNodeOps.planCreateOwnedFile
                    graph
                    harness.seed.workspaceId
                    "note.txt"
            postChange harness.host fileOps
            let graph = readGraph harness.host
            let dirId, dirOps =
                FileNodeOps.planCreateOwnedDirectory
                    graph
                    harness.seed.workspaceId
                    "docs"
            postChange harness.host dirOps
            for focusId in [ fileId; dirId ] do
                let command =
                    requestForFocus
                        LoadSaveOperation.Load
                        LoadSavePrePick.Plain
                        harness.seed
                        focusId
                let! result =
                    Api.postLoadSaveCommand
                        (productionRouter harness)
                        (CoreDoor.create true
                            harness.host
                            testCaller)
                        (encodeRequest command)
                    |> Async.StartAsTask
                let response = requireResponse result
                Assert.Equal(LoadSavePath.Desk, response.path)
                Assert.True(response.command.IsNone)
            let! history =
                CoreMailboxApi.eventHistory harness.host
                |> Async.StartAsTask
            Assert.DoesNotContain(history.events, hasActorStart fileId)
            Assert.DoesNotContain(history.events, hasActorStart dirId)
        finally
            CoreMailboxApi.dispose harness.host
    }

let rec private waitForParsedFile host fileId text deadline = task {
    let after = readGraph host
    let parsed =
        match Map.tryFind fileId after.nodes with
        | Some node when Node.documentStateEnum node = Current ->
            Graph.children after fileId
            |> List.exists (fun kid ->
                after.nodes.[kid.id].text = text)
        | _ -> false
    if parsed then
        return Some after
    elif DateTime.UtcNow >= deadline then
        return None
    else
        do! Task.Delay 10
        return! waitForParsedFile host fileId text deadline
}

let private fileLoadHost (dataDir: string) =
    let push, consumer = ParseStack.create ()
    let host =
        CoreMailbox.hostWithParsePush
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create dataDir))
            admittedCredentials
            push
    let handle = CoreDoor.create true host testCaller
    ParseThread.start
        { dataDir = dataDir
          consumer = consumer
          push = push
          getGraph = ParseThread.graphFromHost host
          postOps = ParseThread.postParseOps handle
          markUnparsed =
            fun nodeId ->
                CoreMailboxApi.postNotice host (Notice.NodeUnparsed nodeId)
          finishParse =
            fun nodeId ->
                CoreMailboxApi.postNotice
                    host
                    (Notice.NodeParsed nodeId) }
    let router =
        { resolvePath = LoadSaveRouting.resolvePath dataDir
          startCommand =
            fun path request ->
                CoreMailboxApi.startLoadSaveCommand
                    host
                    testCaller
                    path
                    (GithubTransportActor.peerName
                        GithubTransportOperation.Load)
                    request }
    host, handle, router

[<Fact>]
let ``Load on a file parses that file`` () = task {
    let dataDir = newTempDir ()
    let host, handle, router = fileLoadHost dataDir
    try
        let seed = seedWorkspace host "load"
        let fileId, fileOps =
            FileNodeOps.planCreateOwnedFile
                (readGraph host) seed.workspaceId "note.txt"
        postChange host fileOps
        let disk = Path.Combine(dataDir, "home", "note.txt")
        Directory.CreateDirectory(Path.GetDirectoryName disk) |> ignore
        File.WriteAllText(disk, "seed")
        let command =
            requestForFocus
                LoadSaveOperation.Load
                LoadSavePrePick.Plain
                seed
                fileId
        let! result =
            Api.postLoadSaveCommand
                router
                handle
                (encodeRequest command)
            |> Async.StartAsTask
        Assert.Equal(LoadSavePath.Desk, (requireResponse result).path)
        let! parsed =
            waitForParsedFile host fileId "seed" (DateTime.UtcNow.AddSeconds 2.0)
        match parsed with
        | Some after ->
            Assert.Equal(Current, after.nodes.[fileId].documentState.AsEnum)
            Assert.Equal(
                "seed",
                after.nodes.[(Graph.children after fileId).Head.id].text)
        | None -> Assert.Fail("Load did not parse the file")
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
    finally
        CoreMailboxApi.dispose harness.host
}

/// A work tree with no remote stays on the desk Save path.
[<SkippableFact>]
let ``plain Save with no GitHub remote takes the Desk path`` () = task {
    Skip.IfNot(DesktopGit.isAvailable(), "git not on PATH")
    let dataDir = newTempDir ()
    let workspace = Path.Combine(dataDir, "home")
    Directory.CreateDirectory(workspace) |> ignore
    git workspace "init -b master" |> ignore
    configureIdentity workspace
    let host = createProductionHost dataDir
    try
        let seed = seedWorkspace host "save"
        let harness =
            { dataDir = dataDir
              parent = dataDir
              remote = ""
              source = ""
              workspace = workspace
              host = host
              seed = seed }
        let! result =
            postCommand
                LoadSaveOperation.Save
                LoadSavePrePick.Plain
                harness
        assertDeskResponse result
        let! history =
            CoreMailboxApi.eventHistory host |> Async.StartAsTask
        Assert.DoesNotContain(history.events, hasActorStart seed.focusId)
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
