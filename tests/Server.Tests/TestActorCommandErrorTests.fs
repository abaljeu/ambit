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
        let! history = CoreMailbox.eventHistory host
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

let private helloChildren (graph: Graph) focusId commandId =
    Graph.children graph focusId
    |> List.filter (fun child ->
        child.ref = Ownership.Owner
        && child.id <> commandId
        && match Map.tryFind child.id graph.nodes with
           | Some node -> node.text = "hello"
           | None -> false)

let private sampleRequest focusId commandId : ActorStart =
    { zoomId = focusId
      focusId = focusId
      commandId = commandId
      graphIds = [ Graph.rootId; commandId ]
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
            CoreMailbox.dispose host
    }

let private seedCommand host text =
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
                      Op.Replace(
                          Graph.rootId,
                          [],
                          [ ChildNode.owner commandId ]) ] }
        let! posted =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" posted |> ignore
        return commandId
    }

let private seedCommandWithChildren host text childTexts =
    task {
        let commandId = NodeId.New()
        let childIds = childTexts |> List.map (fun _ -> NodeId.New())
        let newChildOps =
            List.zip childIds childTexts
            |> List.map (fun (id, childText) ->
                Op.NewNode(id, childText))
        let childEdges = childIds |> List.map ChildNode.owner
        let ops =
            Op.NewNode(commandId, text)
            :: newChildOps
            @ [ Op.Replace(
                    Graph.rootId,
                    [],
                    [ ChildNode.owner commandId ])
                Op.Replace(commandId, [], childEdges) ]
        let event =
            { id = EventId.zero
              submissionId = Guid.NewGuid()
              authority = Authority "Browser"
              commandName = ""
              body = EventBody.Change ops }
        let! posted =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" posted |> ignore
        return commandId
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
            | EventBody.ActorStart started
                when started.focusId = focusId ->
                rest
            | _ -> skip rest
    skip chronological

[<Theory>]
[<InlineData("?unknown", "unknown")>]
[<InlineData("?nope", "nope")>]
let ``unregistered Actor name fails start`` (text: string) (name: string) =
    withHost (fun host pool -> task {
        let! commandId = seedCommand host text
        let request = sampleRequest commandId commandId
        let! result =
            CoreMailbox.startActor host testCaller request
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
        let! commandId = seedCommand host text
        let request = sampleRequest commandId commandId
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        let! stop = waitForActorStop host request.focusId 1000
        match stop with
        | Some (ActorFailed _) -> ()
        | other -> Assert.Fail($"expected ActorFailed, got {other}")
        Assert.False(Set.contains request.focusId (pool.liveFocusIds ()))
        let! state =
            CoreMailbox.getState host |> Async.StartAsTask
        let state = requireOk "getState" state
        Assert.Empty(helloChildren state.graph commandId commandId)
    })

[<Fact>]
let ``TestActor non-hello preserves Focus Children and posts no Change`` () =
    withHost (fun host pool -> task {
        let! commandId =
            seedCommandWithChildren
                host
                "?test unknown"
                [ "keep-me"; "also-keep" ]
        let request = sampleRequest commandId commandId
        let! beforeState =
            CoreMailbox.getState host |> Async.StartAsTask
        let before = requireOk "getState before" beforeState
        let beforeChildren = ownedChildFacts before.graph commandId
        let beforeTexts = nodeTexts before.graph
        Assert.Equal(2, beforeChildren.Length)
        let! start =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" start
        let! stop = waitForActorStop host request.focusId 1000
        match stop with
        | Some (ActorFailed _) -> ()
        | other -> Assert.Fail($"expected ActorFailed, got {other}")
        Assert.False(Set.contains request.focusId (pool.liveFocusIds ()))
        let! afterState =
            CoreMailbox.getState host |> Async.StartAsTask
        let after = requireOk "getState after" afterState
        Assert.Equal<(NodeId * Ownership * string) list>(
            beforeChildren,
            ownedChildFacts after.graph commandId)
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
        let! commandId = seedCommand host "?test hello"
        let request = sampleRequest commandId commandId
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        let! stop = waitForActorStop host request.focusId 1000
        match stop with
        | Some ActorSucceeded -> ()
        | other -> Assert.Fail($"expected ActorSucceeded, got {other}")
        let! state =
            CoreMailbox.getState host |> Async.StartAsTask
        let state = requireOk "getState" state
        Assert.Equal(
            1,
            helloChildren state.graph commandId commandId
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
        Encode.toString 0 (EventJson.encodeEventBatch { events = events })
    use content = jsonPost body
    return! client.PostAsync("/ambit/changes", content)
}

let private seedQuestion (client: HttpClient) rootId commandId text = task {
    let seed =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body =
            EventBody.Change
                [ Op.NewNode(commandId, text)
                  Op.Replace(
                      rootId,
                      [],
                      [ ChildNode.owner commandId ]) ] }
    let! seedResp = postEventsHttp client [ seed ]
    Assert.Equal(HttpStatusCode.OK, seedResp.StatusCode)
}

[<Theory>]
[<InlineData("?unknown")>]
[<InlineData("?nope")>]
let ``production Command of unregistered Actor name is not success``
    (text: string)
    =
    task {
        use client = createClientForDir (newTempDir ())
        let! _, graph = getFullState client
        let commandId = NodeId.New()
        do! seedQuestion client graph.root commandId text
        let! afterId, _ = getFullState client
        let request: ActorStart =
            { zoomId = commandId
              focusId = commandId
              commandId = commandId
              graphIds = [ commandId ]
              eventId = afterId }
        use commandBody = jsonPost (encodeRequest request)
        let! commandResp =
            client.PostAsync("/ambit/command", commandBody)
        let! commandJson = commandResp.Content.ReadAsStringAsync()
        Assert.True(
            commandResp.StatusCode <> HttpStatusCode.OK,
            $"expected Command failure, got {commandResp.StatusCode}: {commandJson}")
        Assert.Equal(HttpStatusCode.BadRequest, commandResp.StatusCode)
    }
