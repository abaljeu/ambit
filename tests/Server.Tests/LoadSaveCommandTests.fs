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
        | Result.Error err -> failwith err
    | other ->
        failwith $"expected JSON content, got {other.GetType().Name}"

let private requireOk label =
    function
    | Result.Ok value -> value
    | Result.Error err -> failwith $"{label}: {err}"

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
                    CoreMailbox.startLoadSaveCommand
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
                (CoreMailbox.coreChanges host testCaller)
                body
            |> Async.StartAsTask

        let response = requireResponse result
        Assert.Equal(LoadSavePath.Desk, response.path)
        Assert.Equal(None, response.command)
    finally
        CoreMailbox.dispose host
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
        GithubTransportActor.productionDependencies dataDir
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
        @ [ Op.NewNode(commandId, operationName)
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
        CoreMailbox.postGraphOnly host testCaller event
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
            CoreMailbox.startLoadSaveCommand
                harness.host
                testCaller
                path
                (GithubTransportActor.peerName
                    (operationFor command.operation))
                command }

let private requestFor operation prePick harness =
    let seed = harness.seed
    { operation = operation
      prePick = prePick
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

let private postRouted operation prePick harness = task {
    let command = requestFor operation prePick harness
    let! result =
        Api.postLoadSaveCommand
            (productionRouter harness)
            (CoreMailbox.coreChanges harness.host testCaller)
            (encodeRequest command)
        |> Async.StartAsTask
    let response = requireResponse result
    Assert.Equal(LoadSavePath.Git, response.path)
    Assert.True(Option.isSome response.command)
}

let rec private waitForStop host focusId remainingMs = task {
    let! history =
        CoreMailbox.eventHistory host |> Async.StartAsTask
    let stopped =
        history.events
        |> List.tryPick (fun event ->
            match event.body with
            | EventBody.ActorStop(id, result) when id = focusId ->
                Some result
            | _ -> None)
    match stopped with
    | Some result -> return Some result
    | None when remainingMs <= 0 -> return None
    | None ->
        do! Task.Delay 10
        return! waitForStop host focusId (remainingMs - 10)
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
            (CoreMailbox.coreChanges harness.host testCaller)
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
            waitForStop harness.host harness.seed.focusId 2000
        Assert.Equal(Some ActorSucceeded, stopped)
        let verify = Path.Combine(harness.parent, "verify")
        git harness.parent $"clone {harness.remote} verify" |> ignore
        Assert.True(File.Exists(Path.Combine(verify, "saved.txt")))
        Assert.Equal(
            git verify "rev-parse refs/heads/main",
            git harness.workspace "rev-parse refs/heads/main")
    finally
        CoreMailbox.dispose harness.host
}

[<SkippableFact>]
let ``routed Git Load pulls and Poll sees Parse lifecycle`` () = task {
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
            postRouted
                LoadSaveOperation.Load
                LoadSavePrePick.Plain
                harness
        let! stopped =
            waitForStop harness.host harness.seed.focusId 2000
        Assert.Equal(Some ActorSucceeded, stopped)
        Assert.Equal(
            "pulled",
            File.ReadAllText(
                Path.Combine(harness.workspace, "from-remote.txt")))
        let! poll = pollSince harness
        Assert.Contains(poll.events, hasActorStart harness.seed.focusId)
        Assert.Contains(poll.events, hasActorStop harness.seed.focusId)
        Assert.Contains(poll.events, isParseChange)
    finally
        CoreMailbox.dispose harness.host
}
