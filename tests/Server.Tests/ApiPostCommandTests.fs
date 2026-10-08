module Gambol.Server.Tests.ApiPostCommandTests

open System
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend
open Thoth.Json.Newtonsoft

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private encodeRequest (request: ActorStart) =
    Encode.toString 0 (EventJson.encodeStartRequest request)

let private sampleRequest commandId : ActorStart =
    { zoomId = commandId
      focusId = commandId
      commandId = commandId
      graphIds = [ commandId ]
      eventId = EventId.zero }

let private jsonContent body =
    new StringContent(body, Encoding.UTF8, "application/json")

[<Fact>]
let ``POST command is absent`` () = task {
    let dataDir = newTempDir ()
    use client = createClientForDir dataDir
    let request = sampleRequest (NodeId.New())
    use body = jsonContent (encodeRequest request)
    let! response = client.PostAsync("/ambit/command", body)
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode)
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

let private launchEvent (request: ActorStart) : Ev =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = "Exec"
      body = EventBody.ActorStart request }

let private startViaEvents host (request: ActorStart) =
    CoreMailbox.postEvents host testCaller [ launchEvent request ]

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
                    Graph.children s.graph focusId
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
            let! result = startViaEvents host request |> Async.StartAsTask
            let accepted =
                match result with
                | Ok value -> value
                | Error err -> failwith err
            let latest = EventId.value accepted.eventId
            let before = EventId.value beforeId
            Assert.True(latest > before, "eventId should advance")
            let started =
                accepted.events
                |> List.exists (fun ev ->
                    match ev.body with
                    | EventBody.ActorStart started ->
                        started.commandId = commandId
                    | _ -> false)
            Assert.True(started, "ActorStart missing from events")
            let! state = CoreMailbox.getState host |> Async.StartAsTask
            match state with
            | Ok s -> Assert.True(s.graph.nodes.ContainsKey commandId)
            | Error err -> failwith err
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
            let! result = startViaEvents host request |> Async.StartAsTask
            match result with
            | Ok _ -> ()
            | Error err -> failwith err
            let! hello = waitForHello host commandId 1000
            Assert.True(hello, "hello child not posted under Focus")
        finally
            CoreMailbox.dispose host
    }

let private decodeStateGraph json =
    let decoder =
        Thoth.Json.Core.Decode.object (fun get ->
            let eventId =
                get.Required.Field "eventId" EventJson.decodeEventId
            let graph =
                get.Required.Field "graph" Serialization.decodeGraph
            eventId, graph)
    match Decode.fromString decoder json with
    | Ok pair -> pair
    | Error err -> failwith err

let private jsonPost body =
    new StringContent(body, Encoding.UTF8, "application/json")

let private getFullState (client: HttpClient) = task {
    let! resp = client.GetAsync("/ambit/state?scope=full")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    let! json = resp.Content.ReadAsStringAsync()
    return decodeStateGraph json
}

let private postEventsHttp (client: HttpClient) (events: Ev list) = task {
    let body =
        Encode.toString 0 (
            ApiResponseSerialization.encodeChangeRequest
                { events = events; want = [] })
    use content = jsonPost body
    return! client.PostAsync("/ambit/changes", content)
}

let private seedQuestionTest (client: HttpClient) rootId commandId = task {
    let seed =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body =
            EventBody.Change
                [ Op.NewNode(commandId, "?test hello")
                  Op.Replace(
                      rootId,
                      [],
                      [ ChildNode.owner commandId ]) ] }
    let! seedResp = postEventsHttp client [ seed ]
    Assert.Equal(HttpStatusCode.OK, seedResp.StatusCode)
}

let private waitStateHello
    (client: HttpClient)
    (focusId: NodeId)
    timeoutMs
    =
    task {
        let mutable found = false
        let startTime = DateTime.UtcNow
        while not found
              && (DateTime.UtcNow - startTime).TotalMilliseconds
                 < float timeoutMs do
            let! resp = client.GetAsync("/ambit/state?scope=full")
            let! json = resp.Content.ReadAsStringAsync()
            if resp.StatusCode = HttpStatusCode.OK then
                let _, graph = decodeStateGraph json
                found <-
                    match Map.tryFind focusId graph.nodes with
                    | None -> false
                    | Some node ->
                        Graph.children graph focusId
                        |> List.exists (fun child ->
                            child.ref = Ownership.Owner
                            && match Map.tryFind child.id graph.nodes with
                               | Some n -> n.text = "hello"
                               | None -> false)
            if not found then do! Task.Delay 20
        return found
    }

[<Fact>]
let ``production boot Run of ?test hello posts Owned hello child`` () =
    task {
        use client = createClientForDir (newTempDir ())
        let! _, graph = getFullState client
        let commandId = NodeId.New()
        do! seedQuestionTest client graph.root commandId
        let! afterId, _ = getFullState client
        let request: ActorStart =
            { zoomId = commandId
              focusId = commandId
              commandId = commandId
              graphIds = [ commandId ]
              eventId = afterId }
        let launch = launchEvent request
        let! commandResp = postEventsHttp client [ launch ]
        let! commandJson = commandResp.Content.ReadAsStringAsync()
        Assert.True(
            commandResp.StatusCode = HttpStatusCode.OK,
            $"events {commandResp.StatusCode}: {commandJson}")
        let! hello = waitStateHello client commandId 2000
        Assert.True(
            hello,
            "production Actor test did not post hello under Focus")
    }
