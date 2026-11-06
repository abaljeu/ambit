module Gambol.Server.Tests.TestActorCommandErrorTests

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

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private eventPast host =
    async {
        let! history = CoreMailboxApi.eventHistory host
        return history.events
    }

let rec private waitUntil remainingMs (check: unit -> Task<bool>) =
    task {
        let! ok = check ()
        if ok then return true
        elif remainingMs <= 0 then return false
        else
            do! Task.Delay 10
            return! waitUntil (remainingMs - 10) check
    }

let private tryActorStop host focusId =
    task {
        let! events = eventPast host |> Async.StartAsTask
        return
            events
            |> List.tryPick (fun event ->
                match event.body with
                | EventBody.ActorStop(fid, result) when fid = focusId ->
                    Some result
                | _ -> None)
    }

let private waitForActorStop host focusId timeoutMs =
    task {
        let! found =
            waitUntil timeoutMs (fun () -> task {
                let! result = tryActorStop host focusId
                return result.IsSome
            })
        if found then return! tryActorStop host focusId
        else return None
    }

let private helloChildren (graph: Graph) focusId actorNodeId =
    Graph.children graph focusId
    |> List.filter (fun child ->
        child.ref = Ownership.Owner
        && child.id <> actorNodeId
        && match Map.tryFind child.id graph.nodes with
           | Some node -> node.text = "hello"
           | None -> false)

let private sampleRequest focusId actorNodeId : ActorStart =
    { zoomId = focusId
      focusId = focusId
      actorNodeId = actorNodeId
      graphIds = [ Graph.rootId; actorNodeId ]
      eventId = EventId.zero }

let private createHost () =
    let dataDir = newTempDir ()
    let pool = CoreActorPool.create ()
    pool.register (ActorName "test") TestActor.actorFn
    let host =
        CoreMailbox.host
            pool
            (FileAgent.persist (FileAgent.create dataDir))
            admittedCredentials
    host, pool

let private withHost body =
    task {
        let host, pool = createHost ()
        try
            do! body host pool
        finally
            CoreMailboxApi.dispose host
    }

let private seedCommand host text =
    task {
        let actorNodeId = NodeId.New()
        let event =
            { id = EventId.zero
              submissionId = Guid.NewGuid()
              authority = Authority "Browser"
              commandName = ""
              body =
                EventBody.Change
                    [ Op.NewNode(actorNodeId, text)
                      Op.Replace(
                          Graph.rootId,
                          [],
                          [ ChildNode.owner actorNodeId ]) ] }
        let! posted =
            CoreMailboxApi.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" posted |> ignore
        return actorNodeId
    }

let private seedCommandWithChildren host text childTexts =
    task {
        let actorNodeId = NodeId.New()
        let childIds = childTexts |> List.map (fun _ -> NodeId.New())
        let newChildOps =
            List.zip childIds childTexts
            |> List.map (fun (id, childText) ->
                Op.NewNode(id, childText))
        let childEdges = childIds |> List.map ChildNode.owner
        let ops =
            Op.NewNode(actorNodeId, text)
            :: newChildOps
            @ [ Op.Replace(
                    Graph.rootId,
                    [],
                    [ ChildNode.owner actorNodeId ])
                Op.Replace(actorNodeId, [], childEdges) ]
        let event =
            { id = EventId.zero
              submissionId = Guid.NewGuid()
              authority = Authority "Browser"
              commandName = ""
              body = EventBody.Change ops }
        let! posted =
            CoreMailboxApi.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" posted |> ignore
        return actorNodeId
    }

let private ownedChildFacts (graph: Graph) focusId =
    Graph.children graph focusId
    |> List.filter (fun child -> child.ref = Ownership.Owner)
    |> List.map (fun child ->
        let text =
            match Map.tryFind child.id graph.nodes with
            | Some node -> node.text
            | None -> ""
        child.id, child.ref, text)

let private nodeTexts (graph: Graph) =
    graph.nodes
    |> Map.toList
    |> List.map (fun (id, node) -> id, node.text)

let private changeEvents (events: Ev list) =
    events
    |> List.filter (fun event ->
        match event.body with
        | EventBody.Change _ -> true
        | _ -> false)

let private eventsAfterStart (events: Ev list) focusId =
    let chronological =
        events
        |> List.sortBy (fun event -> EventId.value event.id)
    let rec skip remaining =
        match remaining with
        | [] -> []
        | event :: rest ->
            match event.body with
            | EventBody.Order (Order.ActorOrderCase order)
                when order.focusId = focusId ->
                rest
            | _ -> skip rest
    skip chronological

[<Theory>]
[<InlineData("?unknown", "unknown")>]
[<InlineData("?nope", "nope")>]
let ``unregistered Actor name fails start`` (text: string) (name: string) =
    withHost (fun host pool -> task {
        let! actorNodeId = seedCommand host text
        let request = sampleRequest actorNodeId actorNodeId
        let! result =
            CoreMailboxApi.coreMailboxStartActor host testCaller request
            |> Async.StartAsTask
        match result with
        | Error err ->
            Assert.Contains("not registered", err)
            Assert.Contains(name, err)
        | Ok () ->
            Assert.Fail("expected start/Command failure")
        Assert.False(Set.contains request.focusId (pool.liveFocusIds ()))
        let! events = eventPast host |> Async.StartAsTask
        let started =
            events
            |> List.exists (fun event ->
                match event.body with
                | EventBody.ActorStart started ->
                    started.focusId = request.focusId
                | _ -> false)
        Assert.False(started)
    })

[<Theory>]
[<InlineData("?test unknown")>]
[<InlineData("?test nope")>]
let ``TestActor non-hello command fails without Owned child`` (text: string) =
    withHost (fun host pool -> task {
        let! actorNodeId = seedCommand host text
        let request = sampleRequest actorNodeId actorNodeId
        let! result = postTestActorOrder host request
        requireOk "postActorOrder" result |> ignore
        let! stop = waitForActorStop host request.focusId 1000
        match stop with
        | Some (ActorFailed _) -> ()
        | other -> Assert.Fail($"expected ActorFailed, got {other}")
        Assert.False(Set.contains request.focusId (pool.liveFocusIds ()))
        let! state =
            CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
        let state = requireOk "getState" state
        Assert.Empty(helloChildren state.graph actorNodeId actorNodeId)
    })

[<Fact>]
let ``TestActor non-hello preserves Focus Children and posts no Change`` () =
    withHost (fun host pool -> task {
        let! actorNodeId =
            seedCommandWithChildren
                host
                "?test unknown"
                [ "keep-me"; "also-keep" ]
        let request = sampleRequest actorNodeId actorNodeId
        let! beforeState =
            CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
        let before = requireOk "getState before" beforeState
        let beforeChildren = ownedChildFacts before.graph actorNodeId
        let beforeTexts = nodeTexts before.graph
        Assert.Equal(2, beforeChildren.Length)
        let! start = postTestActorOrder host request
        requireOk "postActorOrder" start |> ignore
        let! stop = waitForActorStop host request.focusId 1000
        match stop with
        | Some (ActorFailed _) -> ()
        | other -> Assert.Fail($"expected ActorFailed, got {other}")
        Assert.False(Set.contains request.focusId (pool.liveFocusIds ()))
        let! afterState =
            CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
        let after = requireOk "getState after" afterState
        Assert.Equal<(NodeId * Ownership * string) list>(
            beforeChildren,
            ownedChildFacts after.graph actorNodeId)
        Assert.Equal<(NodeId * string) list>(
            beforeTexts,
            nodeTexts after.graph)
        let! events = eventPast host |> Async.StartAsTask
        Assert.Equal(1, changeEvents events |> List.length)
        let afterStart = eventsAfterStart events request.focusId
        match afterStart with
        | [ stopEvent ] ->
            Assert.Equal("", stopEvent.commandName)
            match stopEvent.body with
            | EventBody.ActorStop(fid, ActorFailed _) ->
                Assert.Equal(request.focusId, fid)
            | other ->
                Assert.Fail($"expected ActorStop ActorFailed, got {other}")
        | other ->
            Assert.Fail($"expected one ActorStop after start, got {other}")
    })

[<Fact>]
let ``TestActor ?test hello posts Owned hello and succeeds`` () =
    withHost (fun host _ -> task {
        let! actorNodeId = seedCommand host "?test hello"
        let request = sampleRequest actorNodeId actorNodeId
        let! result = postTestActorOrder host request
        requireOk "postActorOrder" result |> ignore
        let! stop = waitForActorStop host request.focusId 1000
        match stop with
        | Some ActorSucceeded -> ()
        | other -> Assert.Fail($"expected ActorSucceeded, got {other}")
        let! state =
            CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
        let state = requireOk "getState" state
        Assert.Equal(
            1,
            helloChildren state.graph actorNodeId actorNodeId
            |> List.length)
    })

let private runLaunch (request: ActorStart) (caller: Caller) : Ev =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = caller.authority
      commandName = "Exec"
      body = EventBody.ActorStart request }

let private orderPosted (events: Ev list) =
    events
    |> List.exists (fun event ->
        match event.body with
        | EventBody.Order (Order.ActorOrderCase _) -> true
        | _ -> false)

[<Fact>]
let ``?test Run refuses a blank authority before ActorOrder`` () =
    withHost (fun host pool -> task {
        let! actorNodeId = seedCommand host "?test hello"
        let request = sampleRequest actorNodeId actorNodeId
        let blank =
            { authority = Authority ""
              name = ""
              secret = Credential "blank" }
        let! result =
            CoreMailboxApi.postEvents
                host blank [ runLaunch request blank ]
            |> Async.StartAsTask
        requireOk "blank start" result |> ignore
        let! events = eventPast host |> Async.StartAsTask
        Assert.False(orderPosted events)
        let stop =
            events
            |> List.tryPick (fun event ->
                match event.body with
                | EventBody.ActorStop(fid, found)
                    when fid = actorNodeId ->
                    Some found
                | _ -> None)
        Assert.Equal(Some (ActorFailed CoreAuth.refuse), stop)
        Assert.False(Set.contains actorNodeId (pool.liveFocusIds ()))
        let! state =
            CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
        let state = requireOk "getState" state
        Assert.Empty(helloChildren state.graph actorNodeId actorNodeId)
    })

[<Fact>]
let ``?test Run admits the caller and queues ActorOrder`` () =
    withHost (fun host _ -> task {
        let! actorNodeId = seedCommand host "?test hello"
        let request = sampleRequest actorNodeId actorNodeId
        let! result =
            CoreMailboxApi.postEvents
                host testCaller [ runLaunch request testCaller ]
            |> Async.StartAsTask
        requireOk "admitted start" result |> ignore
        let! stop = waitForActorStop host request.focusId 1000
        match stop with
        | Some ActorSucceeded -> ()
        | other -> Assert.Fail($"expected ActorSucceeded, got {other}")
        let! events = eventPast host |> Async.StartAsTask
        Assert.True(orderPosted events, "ActorOrder missing")
        let! state =
            CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
        let state = requireOk "getState" state
        Assert.Equal(
            1,
            helloChildren state.graph actorNodeId actorNodeId
            |> List.length)
    })

let private encodeRequest (request: ActorStart) =
    Encode.toString 0 (EventJson.encodeStartRequest request)

let private jsonPost body =
    new StringContent(body, Encoding.UTF8, "application/json")

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

let private seedQuestion (client: HttpClient) rootId actorNodeId text = task {
    let seed =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body =
            EventBody.Change
                [ Op.NewNode(actorNodeId, text)
                  Op.Replace(
                      rootId,
                      [],
                      [ ChildNode.owner actorNodeId ]) ] }
    let! seedResp = postEventsHttp client [ seed ]
    Assert.Equal(HttpStatusCode.OK, seedResp.StatusCode)
}

[<Theory>]
[<InlineData("?unknown")>]
[<InlineData("?nope")>]
let ``production events door records a failed ActorStop for an unknown actor``
    (text: string)
    =
    task {
        use client = createClientForDir (newTempDir ())
        let! _, graph = getFullState client
        let actorNodeId = NodeId.New()
        do! seedQuestion client graph.root actorNodeId text
        let! afterId, _ = getFullState client
        let request: ActorStart =
            { zoomId = actorNodeId
              focusId = actorNodeId
              actorNodeId = actorNodeId
              graphIds = [ actorNodeId ]
              eventId = afterId }
        let launch =
            { id = EventId.zero
              submissionId = Guid.NewGuid()
              authority = Authority "Browser"
              commandName = "Exec"
              body = EventBody.ActorStart request }
        let! commandResp = postEventsHttp client [ launch ]
        let! commandJson = commandResp.Content.ReadAsStringAsync()
        Assert.True(
            commandResp.StatusCode = HttpStatusCode.OK,
            $"events {commandResp.StatusCode}: {commandJson}")
        match
            Decode.fromString
                ApiResponseSerialization.decodeChangeSuccessResponseDecoder
                commandJson
        with
        | Error err -> failwith err
        | Ok accepted ->
            let failed =
                accepted.events
                |> List.exists (fun event ->
                    match event.body with
                    | EventBody.ActorStop(id, ActorFailed "unknown actor") ->
                        id = actorNodeId
                    | _ -> false)
            Assert.True(failed, "failed ActorStop missing")
    }
