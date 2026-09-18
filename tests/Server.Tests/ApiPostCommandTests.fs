module Gambol.Server.Tests.ApiPostCommandTests

open System
open System.Net
open System.Net.Http
open System.Text
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

let private decodeUniversal json =
    Decode.fromString
        ApiResponseSerialization.decodeUniversalResponseDecoder
        json

let private encodeRequest (request: ActorStart) =
    Encode.toString 0 (EventJson.encodeStartRequest request)

let private unusedHandle
    (state: State)
    (events: Ev list)
    (latestId: EventId)
    : CoreChanges =
    { getState = fun () -> async.Return(Result.Ok state)
      getEventId = fun () -> async.Return latestId
      getEventsSince = fun _ -> async.Return events
      isReady = fun () -> true
      postEvents = fun _ -> async.Return(Result.Error "unused")
      postGraphOnly = fun _ -> async.Return(Result.Error "unused")
      actorStop = fun _ -> async.Return(Result.Error "unused")
      asCaller = fun _ -> Unchecked.defaultof<CoreChanges> }

let private sampleRequest commandId : ActorStart =
    { zoomId = commandId
      focusId = commandId
      commandId = commandId
      graphIds = [ commandId ]
      eventId = EventId.zero }

let private startedEvent (request: ActorStart) : Ev =
    { id = EventId.fromJson 1
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = "Exec"
      body = EventBody.ActorStart request }

[<Fact>]
let ``postCommand decodes named Core ids and calls startActor`` () = task {
    let commandId = NodeId.New()
    let request = sampleRequest commandId
    let started = ResizeArray<ActorStart>()
    let startActor received =
        started.Add received
        async.Return(Result.Ok())
    let event = startedEvent request
    let handle =
        unusedHandle
            { graph = Graph.create (); eventId = EventId.fromJson 1 }
            [ event ]
            (EventId.fromJson 1)
    let! result =
        Api.postCommand startActor handle (encodeRequest request)
        |> Async.StartAsTask
    Assert.Equal(1, started.Count)
    Assert.Equal(request, started.[0])
    match box result with
    | :? ContentHttpResult as content ->
        match decodeUniversal content.ResponseContent with
        | Error err -> failwith err
        | Ok (response: UniversalResponse) ->
            Assert.Equal(EventId.fromJson 1, response.latestId)
            Assert.Equal(1, response.events.Length)
            match response.events.[0].body with
            | EventBody.ActorStart startedBody ->
                Assert.Equal(commandId, startedBody.commandId)
                Assert.Equal(commandId, startedBody.zoomId)
                Assert.Equal(commandId, startedBody.focusId)
            | _ -> failwith "expected ActorStart"
    | other ->
        failwith $"expected JSON content, got {other.GetType().Name}"
}

[<Fact>]
let ``postCommand invalid JSON does not call startActor`` () = task {
    let started = ResizeArray<ActorStart>()
    let startActor received =
        started.Add received
        async.Return(Result.Ok())
    let handle =
        unusedHandle
            { graph = Graph.create (); eventId = EventId.zero }
            []
            EventId.zero
    let! result =
        Api.postCommand startActor handle "{}"
        |> Async.StartAsTask
    Assert.Empty(started)
    match box result with
    | :? BadRequest<obj> -> ()
    | other ->
        failwith $"expected BadRequest, got {other.GetType().Name}"
}

[<Fact>]
let ``postCommand startActor Error is not a universal response`` () = task {
    let commandId = NodeId.New()
    let request = sampleRequest commandId
    let startActor _ = async.Return(Result.Error "actor 'test' not registered")
    let handle =
        unusedHandle
            { graph = Graph.create (); eventId = EventId.zero }
            []
            EventId.zero
    let! result =
        Api.postCommand startActor handle (encodeRequest request)
        |> Async.StartAsTask
    match box result with
    | :? BadRequest<obj> -> ()
    | other ->
        failwith $"expected BadRequest, got {other.GetType().Name}"
}

let private jsonContent body =
    new StringContent(body, Encoding.UTF8, "application/json")

[<Fact>]
let ``POST command without cookie is unauthorized`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDirWithoutCookie dataDir
    let request = sampleRequest (NodeId.New())
    use body = jsonContent (encodeRequest request)
    let! response = client.PostAsync("/ambit/command", body)
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
}

[<Fact>]
let ``POST command with cookie reaches startActor`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDir dataDir
    let request = sampleRequest (NodeId.New())
    use body = jsonContent (encodeRequest request)
    let! response = client.PostAsync("/ambit/command", body)
    Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode)
    Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode)
}

let private createHost () =
    let dataDir = newTempDir ()
    let pool = CoreActorPool.create ()
    pool.register (ActorName "test") TestActor.actorFn
    let host =
        CoreMailbox.host
            pool
            (FileAgent.persist (FileAgent.create dataDir))
            admittedCredentials
    host

let private seedActorCommand host text : Task<NodeId> =
    task {
        let commandId = NodeId.New()
        let event =
            { id = EventId.zero
              submissionId = Guid.NewGuid()
              authority = Authority "Browser"
              commandName = ""
              body =
                EventBody.Change
                    [ Op.NewNode(commandId, text)
                      Op.SetClasses(
                          commandId,
                          CssClass.empty,
                          CssClass.ofList [ "actor-test" ])
                      Op.Replace(
                          Graph.rootId,
                          [],
                          [ ChildNode.owner commandId ]) ] }
        let! posted =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        match posted with
        | Error err -> return failwith err
        | Ok _ -> return commandId
    }

let private startViaApi host (request: ActorStart) =
    Api.postCommand
        (fun start -> CoreMailbox.startActor host testCaller start)
        (CoreMailbox.coreChanges host testCaller)
        (encodeRequest request)

let private requireUniversal (result: IResult) : UniversalResponse =
    match box result with
    | :? ContentHttpResult as content ->
        match decodeUniversal content.ResponseContent with
        | Error err -> failwith err
        | Ok response -> response
    | other ->
        failwith $"expected JSON content, got {other.GetType().Name}"

let private waitForHello host focusId timeoutMs =
    task {
        let mutable found = false
        let startTime = DateTime.UtcNow
        while not found
              && (DateTime.UtcNow - startTime).TotalMilliseconds
                 < float timeoutMs do
            let! state =
                CoreMailbox.getState host |> Async.StartAsTask
            match state with
            | Error _ -> ()
            | Ok s ->
                found <-
                    s.graph.nodes.[focusId].children
                    |> List.exists (fun child ->
                        child.ref = Ownership.Owner
                        && match Map.tryFind child.id s.graph.nodes with
                           | Some node -> node.text = "hello"
                           | None -> false)
            if not found then do! Task.Delay 10
        return found
    }

[<Fact>]
let ``postCommand through CoreMailbox encodes ActorStart in latestId events`` () =
    task {
        let host = createHost ()
        try
            let! commandId = seedActorCommand host "hello"
            let! beforeId =
                CoreMailbox.getEventId host |> Async.StartAsTask
            let request: ActorStart =
                { zoomId = commandId
                  focusId = commandId
                  commandId = commandId
                  graphIds = [ Graph.rootId; commandId ]
                  eventId = beforeId }
            let! result = startViaApi host request |> Async.StartAsTask
            let response = requireUniversal result
            let latest = EventId.value response.latestId
            let before = EventId.value beforeId
            Assert.True(latest > before, "latestId should advance")
            let started =
                response.events
                |> List.exists (fun ev ->
                    match ev.body with
                    | EventBody.ActorStart started ->
                        started.commandId = commandId
                    | _ -> false)
            Assert.True(started, "ActorStart missing from events")
            Assert.Contains(
                response.nodes,
                fun node -> node.id = commandId)
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``postCommand startActor can produce hello child`` () =
    task {
        let host = createHost ()
        try
            let! commandId = seedActorCommand host "hello"
            let! beforeId =
                CoreMailbox.getEventId host |> Async.StartAsTask
            let request: ActorStart =
                { zoomId = commandId
                  focusId = commandId
                  commandId = commandId
                  graphIds = [ Graph.rootId; commandId ]
                  eventId = beforeId }
            let! result = startViaApi host request |> Async.StartAsTask
            requireUniversal result |> ignore
            let! hello = waitForHello host commandId 1000
            Assert.True(hello, "hello child not posted under Focus")
        finally
            CoreMailbox.dispose host
    }
