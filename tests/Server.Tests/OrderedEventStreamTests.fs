module Gambol.Server.Tests.OrderedEventStreamTests

open System
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Encode = Thoth.Json.Newtonsoft.Encode

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

let private posted (commandName: string) (body: EventBody) : Ev =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = commandName
      body = body }

let private newNode (commandName: string) (text: string) =
    posted commandName (EventBody.Change [ Op.NewNode(NodeId.New(), text) ])

let private commandNames (events: Ev list) =
    events
    |> List.sortBy (fun event -> EventId.value event.id)
    |> List.map (fun event -> event.commandName)

let private hostWith pool =
    CoreMailbox.host
        pool
        (FileAgent.persist (FileAgent.create (newTempDir ())))
        admittedCredentials

let private testHost () =
    let pool = CoreActorPool.create ()
    pool.register (ActorName "test") TestActor.actorFn
    hostWith pool

[<Fact>]
let ``two posted lists do not interleave`` () = task {
    let host = testHost ()
    try
        let list name =
            [ 1; 2; 3 ]
            |> List.map (fun n -> newNode $"{name}{n}" $"{name}{n}")
        let post events =
            CoreMailbox.postEvents host testCaller events
        let! pair =
            Async.Parallel [ post (list "A"); post (list "B") ]
            |> Async.StartAsTask
        pair |> Array.iter (requireOk "post" >> ignore)
        let! history =
            CoreMailbox.eventHistory host |> Async.StartAsTask
        let ordered = commandNames history.events
        let forward = [ "A1"; "A2"; "A3"; "B1"; "B2"; "B3" ]
        let reverse = [ "B1"; "B2"; "B3"; "A1"; "A2"; "A3" ]
        Assert.True(
            ordered = forward || ordered = reverse,
            $"interleaved order {ordered}")
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``credential refusal applies nothing from the list`` () = task {
    let host = testHost ()
    try
        let stranger = { testCaller with secret = Credential "nope" }
        let! before =
            CoreMailbox.eventHistory host |> Async.StartAsTask
        let events =
            [ 1; 2; 3 ] |> List.map (fun n -> newNode $"C{n}" $"C{n}")
        let! result =
            CoreMailbox.postEvents host stranger events
            |> Async.StartAsTask
        match result with
        | Error err -> Assert.True(CoreAuth.isAuthRefuse err, err)
        | Ok _ -> failwith "expected credential refusal"
        let! after =
            CoreMailbox.eventHistory host |> Async.StartAsTask
        Assert.Equal(before.events.Length, after.events.Length)
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``ActorStop in a posted list applies nothing`` () = task {
    let host = testHost ()
    try
        let focusId = NodeId.New()
        let events =
            [ newNode "edit" "row"
              posted
                  "stop"
                  (EventBody.ActorStop(focusId, ActorSucceeded)) ]
        let! before =
            CoreMailbox.eventHistory host |> Async.StartAsTask
        let! result =
            CoreMailbox.postEvents host testCaller events
            |> Async.StartAsTask
        match result with
        | Error err ->
            Assert.Contains("not a client event type", err)
        | Ok _ -> failwith "expected ActorStop to be refused"
        let! after =
            CoreMailbox.eventHistory host |> Async.StartAsTask
        Assert.Equal(before.events.Length, after.events.Length)
    finally
        CoreMailbox.dispose host
}

let private seedCommand host text = task {
    let commandId = NodeId.New()
    let event =
        posted
            "seed"
            (EventBody.Change
                [ Op.NewNode(commandId, text)
                  Op.Replace(
                      Graph.rootId, [], [ ChildNode.owner commandId ]) ])
    let! postedResult =
        CoreMailbox.postEvents host testCaller [ event ]
        |> Async.StartAsTask
    requireOk "seed" postedResult |> ignore
    return commandId
}

let private launch commandId eventId : Ev =
    let request: ActorStart =
        { zoomId = commandId
          focusId = commandId
          commandId = commandId
          graphIds = [ commandId ]
          eventId = eventId }
    posted "Exec" (EventBody.ActorStart request)

let private waitHello host focusId timeoutMs = task {
    let mutable found = false
    let started = DateTime.UtcNow
    while
        not found
        && (DateTime.UtcNow - started).TotalMilliseconds < float timeoutMs
        do
        let! state = CoreMailbox.getState host |> Async.StartAsTask
        match state with
        | Ok graphState ->
            let helloChild child =
                child.ref = Ownership.Owner
                && (
                    match Map.tryFind child.id graphState.graph.nodes with
                    | Some node -> node.text = "hello"
                    | None -> false)
            found <-
                Graph.children graphState.graph focusId
                |> List.exists helloChild
        | Error _ -> ()
        if not found then do! Task.Delay 10
    return found
}

[<Fact>]
let ``edit then ActorStart classifies the line after the edit`` () = task {
    let host = testHost ()
    try
        let! commandId = seedCommand host "?test nope"
        let! before =
            CoreMailbox.getEventId host |> Async.StartAsTask
        let edit =
            posted
                "edit"
                (EventBody.Change
                    [ Op.SetText(commandId, "?test nope", "?test hello") ])
        let! result =
            CoreMailbox.postEvents
                host testCaller [ edit; launch commandId before ]
            |> Async.StartAsTask
        let accepted = requireOk "list" result
        let started =
            accepted.events
            |> List.exists (fun event ->
                match event.body with
                | EventBody.ActorStart start ->
                    start.commandId = commandId
                | _ -> false)
        Assert.True(started, "stored ActorStart missing")
        let! hello = waitHello host commandId 1000
        Assert.True(hello, "hello child missing; edit was not visible")
    finally
        CoreMailbox.dispose host
}

[<Fact>]
let ``ActorStart body does not run inside the mailbox turn`` () = task {
    let hold = TaskCompletionSource<unit>()
    let actor (input: ActorInput) (changes: CoreChanges) =
        async {
            do! Async.AwaitTask hold.Task
            let caller =
                { authority = Authority "Actor"
                  name = ""
                  secret = input.secret }
            let! _ =
                changes.asCaller(caller).actorStop ActorSucceeded
            return ()
        }
    let pool = CoreActorPool.create ()
    pool.register (ActorName "gate") actor
    let host = hostWith pool
    try
        let! commandId = seedCommand host "?gate"
        let! before =
            CoreMailbox.getEventId host |> Async.StartAsTask
        let posting =
            CoreMailbox.postEvents
                host testCaller [ launch commandId before ]
            |> Async.StartAsTask
        Assert.True(posting.Wait(1000), "list did not return while body held")
        Assert.False(hold.Task.IsCompleted)
        hold.TrySetResult(()) |> ignore
        requireOk "launch" posting.Result |> ignore
    finally
        CoreMailbox.dispose host
}

let private postEventsHttp (client: HttpClient) (events: Ev list) = task {
    let body =
        Encode.toString 0 (
            ApiResponseSerialization.encodeChangeRequest
                { events = events; want = [] })
    use content = new StringContent(body, Encoding.UTF8, "application/json")
    return! client.PostAsync("/ambit/events", content)
}

let private requireHttp (label: string) (response: HttpResponseMessage) = task {
    let! json = response.Content.ReadAsStringAsync()
    Assert.True(response.StatusCode = HttpStatusCode.OK, $"{label}: {json}")
    match
        Thoth.Json.Newtonsoft.Decode.fromString
            ApiResponseSerialization.decodeChangeSuccessResponseDecoder
            json
    with
    | Ok accepted -> return accepted
    | Error err -> return failwith $"{label}: {err}"
}

let private seedOwned (client: HttpClient) text = task {
    let commandId = NodeId.New()
    let seed =
        posted
            "seed"
            (EventBody.Change
                [ Op.NewNode(commandId, text)
                  Op.Replace(
                      Graph.rootId, [], [ ChildNode.owner commandId ]) ])
    let! response = postEventsHttp client [ seed ]
    let! _ = requireHttp "seed" response
    return commandId
}

let private graphFromState (client: HttpClient) = task {
    let! response = client.GetAsync("/ambit/state?scope=full")
    let! json = response.Content.ReadAsStringAsync()
    Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    let decoder =
        Thoth.Json.Core.Decode.object (fun get ->
            get.Required.Field "graph" Serialization.decodeGraph)
    match Thoth.Json.Newtonsoft.Decode.fromString decoder json with
    | Ok graph -> return graph
    | Error err -> return failwith err
}

let private waitHelloHttp client focusId timeoutMs = task {
    let mutable found = false
    let started = DateTime.UtcNow
    while
        not found
        && (DateTime.UtcNow - started).TotalMilliseconds < float timeoutMs
        do
        let! graph = graphFromState client
        let helloChild child =
            child.ref = Ownership.Owner
            && (
                match Map.tryFind child.id graph.nodes with
                | Some node -> node.text = "hello"
                | None -> false)
        found <-
            Graph.children graph focusId |> List.exists helloChild
        if not found then do! Task.Delay 20
    return found
}

[<Fact>]
let ``events door applies an edit then ActorStart as one list`` () = task {
    use client = createClientForDir (newTempDir ())
    let! commandId = seedOwned client "?test nope"
    let edit =
        posted
            "edit"
            (EventBody.Change
                [ Op.SetText(commandId, "?test nope", "?test hello") ])
    let! response =
        postEventsHttp client [ edit; launch commandId EventId.zero ]
    let! accepted = requireHttp "list" response
    let started =
        accepted.events
        |> List.exists (fun event ->
            match event.body with
            | EventBody.ActorStart start -> start.commandId = commandId
            | _ -> false)
    Assert.True(started, "events door did not store ActorStart")
    let! hello = waitHelloHttp client commandId 2000
    Assert.True(hello, "hello child missing on the events door")
}

[<Fact>]
let ``events door accepts an edit followed by Cancel`` () = task {
    use client = createClientForDir (newTempDir ())
    let! commandId = seedOwned client "row"
    let edit =
        posted
            "edit"
            (EventBody.Change [ Op.SetText(commandId, "row", "row two") ])
    let cancel = posted "Cancel" (EventBody.Cancel commandId)
    let! response = postEventsHttp client [ edit; cancel ]
    let! accepted = requireHttp "cancel list" response
    let edited =
        accepted.events
        |> List.exists (fun event ->
            match event.body with
            | EventBody.Change _ -> true
            | _ -> false)
    Assert.True(edited, "events door did not store the edit")
}

[<Fact>]
let ``edit then unknown ActorStart stores a failed ActorStop`` () = task {
    use client = createClientForDir (newTempDir ())
    let! commandId = seedOwned client "row"
    let edit =
        posted
            "edit"
            (EventBody.Change
                [ Op.SetText(commandId, "row", "?nope") ])
    let! response =
        postEventsHttp client [ edit; launch commandId EventId.zero ]
    let! accepted = requireHttp "unknown" response
    let started =
        accepted.events
        |> List.exists (fun event ->
            match event.body with
            | EventBody.ActorStart start ->
                start.commandId = commandId
            | _ -> false)
    let failed =
        accepted.events
        |> List.exists (fun event ->
            match event.body with
            | EventBody.ActorStop(id, ActorFailed "unknown actor") ->
                id = commandId
            | _ -> false)
    Assert.True(started, "ActorStart was not stored")
    Assert.True(failed, "failed ActorStop was not stored")
}

[<Fact>]
let ``edit then rejected ActorStart stores a failed ActorStop`` () = task {
    use client = createClientForDir (newTempDir ())
    let! commandId = seedOwned client "row"
    let edit =
        posted
            "edit"
            (EventBody.Change
                [ Op.SetText(commandId, "row", "row b") ])
    let request: ActorStart =
        { zoomId = commandId
          focusId = commandId
          commandId = commandId
          graphIds = []
          eventId = EventId.zero }
    let start = posted "Exec" (EventBody.ActorStart request)
    let! response = postEventsHttp client [ edit; start ]
    let! accepted = requireHttp "rejected" response
    let started =
        accepted.events
        |> List.exists (fun event ->
            match event.body with
            | EventBody.ActorStart _ -> true
            | _ -> false)
    let failed =
        accepted.events
        |> List.exists (fun event ->
            match event.body with
            | EventBody.ActorStop(id, ActorFailed message) ->
                id = commandId && message.StartsWith("graphIds required")
            | _ -> false)
    Assert.True(started, "ActorStart was not stored")
    Assert.True(failed, "failed ActorStop was not stored")
}

[<Fact>]
let ``events door refuses a missing cookie before apply`` () = task {
    use client = createClientForDirWithoutCookie (newTempDir ())
    let body =
        Encode.toString 0 (
            ApiResponseSerialization.encodeChangeRequest
                { events = [ newNode "x" "x" ]; want = [] })
    use content = new StringContent(body, Encoding.UTF8, "application/json")
    let! response = client.PostAsync("/ambit/events", content)
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode)
}
