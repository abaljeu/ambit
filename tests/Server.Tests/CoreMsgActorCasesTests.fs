module Gambol.Server.Tests.CoreMsgActorCasesTests

open System
open System.Diagnostics
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

/// Seam: CoreMsg union (StartActor, Actor PostChange live-table, ActorStop).

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private sampleRequest: StartActorRequest =
    { zoomId = Graph.rootId
      focusId = Graph.rootId
      commandId = Graph.rootId
      graphIds = [ Graph.rootId ] }

let private addRootChild text =
    let childId = NodeId.New()
    { id = 0
      changeId = Guid.NewGuid()
      ops =
        [ Op.NewNode(childId, text)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ] }

let private actorAuthority = Authority "Actor"

let private recordingActors () =
    let started = ResizeArray<StartActorRequest>()
    let stopped = ResizeArray<Credential * ActorResult>()
    let live = ResizeArray<Credential>()
    let actors: ActorMailboxHandlers = {
        startActor =
            fun request ->
                started.Add request
                async.Return (Ok ())
        isLive = fun secret -> live.Contains secret
        actorStop =
            fun secret result ->
                live.Remove secret |> ignore
                stopped.Add(secret, result)
                Ok ()
    }
    started, stopped, live, actors

let private createHost dataDir actors =
    let credentials = admittedCredentials ()
    let host =
        CoreMailbox.createFile
            (CoreMailbox.startFileWithActors credentials actors)
            dataDir
    host, credentials

let private postStartActor host authority secret request =
    host.mailbox.PostAndAsyncReply(fun reply ->
        StartActor(authority, secret, request, reply))

let private postActorStop host authority secret result =
    host.mailbox.PostAndAsyncReply(fun reply ->
        ActorStop(authority, secret, result, reply))

[<Fact>]
let ``StartActor with live credentials hands off StartActorRequest`` () =
    task {
        let dataDir = newTempDir ()
        let started, _, _, actors = recordingActors ()
        let host, _ = createHost dataDir actors
        try
            let! result =
                postStartActor host testAuthority testSecret sampleRequest
                |> Async.StartAsTask
            requireOk "StartActor" result
            Assert.Equal(1, started.Count)
            Assert.Equal(sampleRequest.zoomId, started.[0].zoomId)
            Assert.Equal(sampleRequest.focusId, started.[0].focusId)
            Assert.Equal(sampleRequest.commandId, started.[0].commandId)
            Assert.Equal<NodeId list>(
                sampleRequest.graphIds, started.[0].graphIds)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``StartActor with inactive secret does not hand off`` () = task {
    let dataDir = newTempDir ()
    let started, _, _, actors = recordingActors ()
    let host, _ = createHost dataDir actors
    try
        let! result =
            postStartActor
                host
                testAuthority
                (Credential "inactive")
                sampleRequest
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
        Assert.Equal(0, started.Count)
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``StartActor with blank Authority does not hand off`` () = task {
    let dataDir = newTempDir ()
    let started, _, _, actors = recordingActors ()
    let host, _ = createHost dataDir actors
    try
        let! result =
            postStartActor
                host
                (Authority "  ")
                testSecret
                sampleRequest
            |> Async.StartAsTask
        Assert.Equal(Error CoreAuth.refuse, result)
        Assert.Equal(0, started.Count)
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``Actor PostChange with live row reaches PersistHandlers`` () = task {
    let dataDir = newTempDir ()
    let _, _, live, actors = recordingActors ()
    let actorSecret = Credential "actor-live"
    live.Add actorSecret
    let host, credentials = createHost dataDir actors
    try
        do! credentials.add actorSecret |> Async.StartAsTask
        let change = addRootChild "actor-hello"
        let! result =
            CoreMailbox.postChange
                host actorAuthority actorSecret [ change ]
            |> Async.StartAsTask
        let accepted = requireOk "Actor post" result
        Assert.Equal(Revision 1, accepted.revision)
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``Actor PostChange without live row is refused before persist`` () =
    task {
        let dataDir = newTempDir ()
        let _, _, _, actors = recordingActors ()
        let actorSecret = Credential "actor-not-live"
        let host, credentials = createHost dataDir actors
        try
            do! credentials.add actorSecret |> Async.StartAsTask
            let handle =
                CoreMailbox.coreChanges
                    host credentials testAuthority testSecret
            let! before = handle.getRevision () |> Async.StartAsTask
            let! result =
                CoreMailbox.postChange
                    host
                    actorAuthority
                    actorSecret
                    [ addRootChild "nope" ]
                |> Async.StartAsTask
            let! after = handle.getRevision () |> Async.StartAsTask
            Assert.Equal(Error CoreAuth.refuse, result)
            Assert.Equal(before, after)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``Browser PostChange does not require a live row`` () = task {
    let dataDir = newTempDir ()
    let _, _, _, actors = recordingActors ()
    let host, _ = createHost dataDir actors
    try
        let! result =
            CoreMailbox.postChange
                host
                (Authority "Browser")
                testSecret
                [ addRootChild "browser" ]
            |> Async.StartAsTask
        let accepted = requireOk "Browser post" result
        Assert.Equal(Revision 1, accepted.revision)
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``ActorStop ActorSucceeded drops live row without waiting`` () = task {
    let dataDir = newTempDir ()
    let _, stopped, live, actors = recordingActors ()
    let actorSecret = Credential "actor-stop"
    live.Add actorSecret
    let host, credentials = createHost dataDir actors
    try
        do! credentials.add actorSecret |> Async.StartAsTask
        let lingering = Task.Delay 5000
        let sw = Stopwatch.StartNew()
        let! result =
            postActorStop
                host actorAuthority actorSecret ActorSucceeded
            |> Async.StartAsTask
        sw.Stop()
        requireOk "ActorStop" result
        Assert.Equal(1, stopped.Count)
        Assert.Equal(actorSecret, fst stopped.[0])
        Assert.Equal(ActorSucceeded, snd stopped.[0])
        Assert.False(live.Contains actorSecret)
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds 1.0)
        Assert.False(lingering.IsCompleted)
    finally
        CoreMailbox.dispose host
}
