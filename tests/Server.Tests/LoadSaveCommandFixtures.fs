module Gambol.Server.Tests.LoadSaveCommandFixtures

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

let encodeRequest request =
    EventJson.encodeLoadSaveCommandRequest request
    |> Encode.toString 0

let request operation prePick nodeId eventId =
    { operation = operation
      prePick = prePick
      holdStack = false
      start =
        { zoomId = nodeId
          focusId = nodeId
          runDefinitionId = nodeId
          graphIds = [ Graph.rootId; nodeId ]
          eventId = eventId } }

let requireResponse (result: IResult) =
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

let requireOk label =
    function
    | Result.Ok value -> value
    | Result.Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

type Seed =
    { workspaceId: NodeId
      runDefinitionId: NodeId
      focusId: NodeId
      eventId: EventId }

type GitHarness =
    { dataDir: string
      parent: string
      remote: string
      source: string
      workspace: string
      host: MailboxHost
      seed: Seed }

let git root arguments =
    GitSave.runGit root arguments |> requireOk arguments

let configureIdentity root =
    git root "config user.email test@gambol" |> ignore
    git root "config user.name test" |> ignore

let commitFile (root: string) (path: string) (text: string) (message: string) =
    let full = Path.Combine(root, path)
    Directory.CreateDirectory(Path.GetDirectoryName full) |> ignore
    File.WriteAllText(full, text)
    git root "add -A" |> ignore
    git root $"commit -m {message}" |> ignore

let createTrackedWorkspace dataDir =
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

let createProductionHost dataDir =
    let pool = CoreActorPool.create ()
    CoreMailbox.host
        pool
        (FileAgent.persist (FileAgent.create dataDir))
        admittedCredentials

/// The production host shape: a Parse stack with its consumer thread.
let createParsingHost dataDir =
    let push, consumer = ParseStack.create ()
    let host =
        CoreMailbox.hostWithParsePush
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create dataDir))
            admittedCredentials
            push
    let handle = CoreDoor.create true host testCaller
    let postNotice = CoreMailboxApi.postNotice host
    ParseThread.start
        { dataDir = dataDir
          consumer = consumer
          push = push
          getGraph = ParseThread.graphFromHost host
          postOps = ParseThread.postParseOps postNotice handle
          markUnparsed = fun nodeId -> postNotice (Notice.NodeUnparsed nodeId)
          finishParse = fun nodeId -> postNotice (Notice.NodeParsed nodeId) }
    host

let postSeed host ops =
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change ops }
    CoreMailboxApi.postGraphOnly host testCaller event
    |> Async.RunSynchronously
    |> requireOk "seed workspace"

let markCurrent host nodeId =
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

let seedWorkspace host operationName =
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

let createGitHarnessOn createHost operationName =
    let dataDir = newTempDir ()
    let parent, remote, source, workspace =
        createTrackedWorkspace dataDir
    let host = createHost dataDir
    { dataDir = dataDir
      parent = parent
      remote = remote
      source = source
      workspace = workspace
      host = host
      seed = seedWorkspace host operationName }

let createGitHarness = createGitHarnessOn createProductionHost

let operationFor =
    function
    | LoadSaveOperation.Load -> GithubTransportOperation.Load
    | LoadSaveOperation.Save -> GithubTransportOperation.Save

let routerFor host dataDir =
    { resolvePath = LoadSaveRouting.resolvePath
      startCommand =
        LoadSaveStart.startCommand
            (CoreDoor.create true host testCaller)
            dataDir }

let productionRouter harness =
    routerFor harness.host harness.dataDir

let requestFor operation prePick seed =
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

let requestForFocus operation prePick seed focusId =
    let command = requestFor operation prePick seed
    { command with
        start =
            { command.start with
                zoomId = focusId
                focusId = focusId } }

let postRoutedFocus operation prePick focusId harness = task {
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

let postRouted operation prePick harness =
    postRoutedFocus operation prePick harness.seed.focusId harness

let assertDeskResponse (result: IResult) =
    let response = requireResponse result
    Assert.Equal(LoadSavePath.Desk, response.path)
    Assert.True(response.command.IsNone)

let postCommand operation prePick harness = task {
    let command = requestFor operation prePick harness.seed
    let! result =
        Api.postLoadSaveCommand
            (productionRouter harness)
            (CoreDoor.create true harness.host testCaller)
            (encodeRequest command)
        |> Async.StartAsTask
    return result
}

let rec waitForStopUntil host focusId (deadline: DateTime) (delayMs: int) =
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

let waitForStop host focusId =
    waitForStopUntil
        host focusId (DateTime.UtcNow.AddSeconds 30.0) 10

let actorStops host focusId = task {
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

let rec waitForStopCount host focusId count deadline = task {
    let! found = actorStops host focusId
    if found >= count || DateTime.UtcNow >= deadline then
        return found
    else
        do! Task.Delay 10
        return! waitForStopCount host focusId count deadline
}

let decodePoll result =
    match box result with
    | :? ContentHttpResult as content ->
        Decode.fromString
            ApiResponseSerialization.decodeChangeSuccessResponseDecoder
            content.ResponseContent
        |> requireOk "decode Poll"
    | other ->
        failwith $"expected Poll JSON, got {other.GetType().Name}"

let pollSince harness = task {
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

let pollFrom host after = task {
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

let hasActorStart focusId event =
    match event.body with
    | EventBody.ActorStart start -> start.focusId = focusId
    | _ -> false

let hasStartedActor focusId event =
    match event.body with
    | EventBody.Order (Order.StartActor order) ->
        order.actorNodeId = focusId
    | _ -> false

let hasActorStop focusId event =
    match event.body with
    | EventBody.ActorStop(id, ActorSucceeded) -> id = focusId
    | _ -> false

let actorLeft focusId event =
    match event.body with
    | EventBody.ActorStop(id, _) -> id = focusId
    | _ -> false

/// Dispose joins the persist thread, not the Git actor. After start,
/// that actor can still hold the Workspace lock, so wait until it stops.
let rec waitUntilActorStops host focusId remainingMs = task {
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

let isParseChange event =
    event.commandName = "Parse"
    && match event.body with
       | EventBody.Change _ -> true
       | _ -> false

let postChange host ops =
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

let readGraph host =
    CoreMailboxApi.graphAtEvent host
    |> Async.RunSynchronously
    |> requireOk "state"
    |> fun state -> state.graph

let childNamed graph parentId name =
    Graph.children graph parentId
    |> List.pick (fun child ->
        match Filename.tryValue graph.nodes.[child.id].name with
        | Some candidate when candidate = name -> Some graph.nodes.[child.id]
        | _ -> None)

let publishStaging parent =
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

let fetchSwitchStaging workspace remote =
    git workspace "init -b master" |> ignore
    git workspace $"remote add origin {remote}" |> ignore
    git workspace "fetch" |> ignore
    git workspace "switch staging" |> ignore

let applyTail label events state =
    match SyncLogic.applyServerTail events state with
    | Ok next -> next
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let countingGit () =
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

let withRemote (remoteExists: string -> Result<bool, string>) =
    let pulls, pushes, dependencies = countingGit ()
    pulls,
    pushes,
    { git = { dependencies.git with remoteExists = remoteExists } }

let actorRouter host dataDir dependencies operation =
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

let private fileParsedWith host fileId text =
    let graph = readGraph host
    match Map.tryFind fileId graph.nodes with
    | Some node when Node.documentStateEnum node = Current ->
        Graph.children graph fileId
        |> List.exists (fun kid -> graph.nodes.[kid.id].text = text)
    | _ -> false

let private directoryParsedWith host dirId childName =
    let graph = readGraph host
    match Map.tryFind dirId graph.nodes with
    | Some node when Node.documentStateEnum node = Current ->
        Graph.children graph dirId
        |> List.exists (fun kid ->
            Filename.tryValue graph.nodes.[kid.id].name = Some childName)
    | _ -> false

let rec private waitUntilPredicate predicate (deadline: DateTime) = task {
    if predicate () then
        return true
    elif DateTime.UtcNow >= deadline then
        return false
    else
        do! Task.Delay 10
        return! waitUntilPredicate predicate deadline
}

/// Plain Load through the production router. The response path is Desk.
let postPlainLoad host dataDir seed focusId = task {
    let command =
        requestForFocus LoadSaveOperation.Load LoadSavePrePick.Plain seed focusId
    let! result =
        Api.postLoadSaveCommand
            (routerFor host dataDir)
            (CoreDoor.create true host testCaller)
            (encodeRequest command)
        |> Async.StartAsTask
    Assert.Equal(LoadSavePath.Desk, (requireResponse result).path)
}

let assertNoLoadActor host focusIds =
    let history =
        CoreMailboxApi.eventHistory host |> Async.RunSynchronously
    for focusId in focusIds do
        Assert.DoesNotContain(history.events, githubLoadStarted focusId)

let assertParsed host fileId text = task {
    let! parsed =
        waitUntilPredicate
            (fun () -> fileParsedWith host fileId text)
            (DateTime.UtcNow.AddSeconds 5.0)
    Assert.True(parsed, "Load left the File Unparsed with no children")
}

let assertDirectoryParsed host dirId childName = task {
    let! parsed =
        waitUntilPredicate
            (fun () -> directoryParsedWith host dirId childName)
            (DateTime.UtcNow.AddSeconds 5.0)
    Assert.True(
        parsed,
        $"Load left the Directory Unparsed without child {childName}")
}
