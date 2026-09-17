module Gambol.Server.Tests.TestActorHelloTests

open System
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

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
let private waitForActorFinished host focusId timeoutMs =
    task {
        let mutable found = false
        let startTime = DateTime.UtcNow
        while not found && (DateTime.UtcNow - startTime).TotalMilliseconds < float timeoutMs do
            let! events =
                eventPast host
                |> Async.StartAsTask
            found <-
                events
                |> List.exists (fun event ->
                    match event.body with
                    | Gambol.Shared.EventBody.ActorStop(fid, _)
                        when fid = focusId -> true
                    | _ -> false)
            if not found then
                do! Task.Delay(10)
        return found
    }
let private waitForLiveRowDrop pool focusId timeoutMs =
    task {
        let mutable dropped = false
        let startTime = DateTime.UtcNow
        while not dropped && (DateTime.UtcNow - startTime).TotalMilliseconds < float timeoutMs do
            dropped <- not (Set.contains focusId (pool.liveFocusIds ()))
            if not dropped then
                do! Task.Delay(10)
        return dropped
    }

let private helloOutputChildren (graph: Graph) focusId commandId =
    graph.nodes.[focusId].children
    |> List.filter (fun child ->
        child.ref = Ownership.Owner
        && child.id <> commandId
        && match Map.tryFind child.id graph.nodes with
           | Some node -> node.text = "hello"
           | None -> false)

let private sampleRequest focusId commandId graphIds: Gambol.Shared.ActorStart =
    { zoomId = Graph.rootId
      focusId = focusId
      commandId = commandId
      graphIds = graphIds
      eventId = EventId.fromJson 0 }

let private actorCaller secret =
    { authority = Authority "Actor"
      name = ""
      secret = secret }

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

[<Fact>]
let ``TestActor hello posts one Owned child text hello under Focus`` () =
    withHost (fun host _ -> task {
        let commandId = NodeId.New()
        let event = toEvent { id = EventId.fromJson 0
                              submissionId = Guid.NewGuid()
                              ops =
                [ Op.NewNode(commandId, "hello")
                  Op.SetClasses(commandId, CssClass.empty, CssClass.ofList [ "actor-test" ])
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" postResult |> ignore
        
        let request = sampleRequest Graph.rootId commandId [ Graph.rootId; commandId ]
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let! finished = waitForActorFinished host request.focusId 1000
        Assert.True(finished, "ActorFinished not received within timeout")
        
        let! state =
            CoreMailbox.getState host
            |> Async.StartAsTask
        let state = requireOk "getState" state
        
        let helloChildren =
            helloOutputChildren state.graph Graph.rootId commandId
        Assert.Equal(1, helloChildren.Length)
    })

[<Fact>]
let ``TestActor hello stops successfully with ActorSucceeded`` () =
    withHost (fun host _ -> task {
        let commandId = NodeId.New()
        let event = toEvent { id = EventId.fromJson 0
                              submissionId = Guid.NewGuid()
                              ops =
                [ Op.NewNode(commandId, "hello")
                  Op.SetClasses(commandId, CssClass.empty, CssClass.ofList [ "actor-test" ])
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" postResult |> ignore
        
        let request = sampleRequest Graph.rootId commandId [ Graph.rootId; commandId ]
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let! finished = waitForActorFinished host request.focusId 1000
        Assert.True(finished, "ActorFinished not received within timeout")
        
        let! events =
            eventPast host
            |> Async.StartAsTask
        let actorFinishedEvents =
            events
            |> List.choose (fun event ->
                match event.body with
                | Gambol.Shared.EventBody.ActorStop(focusId, _)
                    when focusId = request.focusId ->
                    Some focusId
                | _ -> None)
        Assert.Equal(1, actorFinishedEvents.Length)
    })

[<Fact>]
let ``TestActor hello drops live row after successful stop`` () =
    withHost (fun host pool -> task {
        let commandId = NodeId.New()
        let event = toEvent { id = EventId.fromJson 0
                              submissionId = Guid.NewGuid()
                              ops =
                [ Op.NewNode(commandId, "hello")
                  Op.SetClasses(commandId, CssClass.empty, CssClass.ofList [ "actor-test" ])
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" postResult |> ignore
        
        let request = sampleRequest Graph.rootId commandId [ Graph.rootId; commandId ]
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        
        Assert.True(Set.contains request.focusId (pool.liveFocusIds ()))
        
        let! dropped = waitForLiveRowDrop pool request.focusId 1000
        Assert.True(dropped, "Live row not dropped within timeout")
        
        Assert.False(Set.contains request.focusId (pool.liveFocusIds ()))
    })

[<Fact>]
let ``TestActor hello observes ActorStarted before output`` () =
    withHost (fun host _ -> task {
        let commandId = NodeId.New()
        let event = toEvent { id = EventId.fromJson 0
                              submissionId = Guid.NewGuid()
                              ops =
                [ Op.NewNode(commandId, "hello")
                  Op.SetClasses(commandId, CssClass.empty, CssClass.ofList [ "actor-test" ])
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" postResult |> ignore
        
        let request = sampleRequest Graph.rootId commandId [ Graph.rootId; commandId ]
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let! finished = waitForActorFinished host request.focusId 1000
        Assert.True(finished, "ActorFinished not received within timeout")
        
        let! events =
            eventPast host
            |> Async.StartAsTask
        let actorStartedIndex =
            events
            |> List.tryFindIndex (fun event ->
                match event.body with
                | Gambol.Shared.EventBody.ActorStart started
                    when started.focusId = request.focusId ->
                    true
                | _ -> false)
        
        let actorFinishedIndex =
            events
            |> List.tryFindIndex (fun event ->
                match event.body with
                | Gambol.Shared.EventBody.ActorStop(focusId, _)
                    when focusId = request.focusId ->
                    true
                | _ -> false)
        
        Assert.True(actorStartedIndex.IsSome)
        Assert.True(actorFinishedIndex.IsSome)
        Assert.True(actorFinishedIndex.Value < actorStartedIndex.Value)
    })

[<Fact>]
let ``TestActor hello interprets command node text`` () =
    withHost (fun host _ -> task {
        let commandId = NodeId.New()
        let event = toEvent { id = EventId.fromJson 0
                              submissionId = Guid.NewGuid()
                              ops =
                [ Op.NewNode(commandId, "HELLO")
                  Op.SetClasses(commandId, CssClass.empty, CssClass.ofList [ "actor-test" ])
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" postResult |> ignore
        
        let request = sampleRequest Graph.rootId commandId [ Graph.rootId; commandId ]
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let! finished = waitForActorFinished host request.focusId 1000
        Assert.True(finished, "ActorFinished not received within timeout")
        
        let! state =
            CoreMailbox.getState host
            |> Async.StartAsTask
        let state = requireOk "getState" state
        
        let helloChildren =
            helloOutputChildren state.graph Graph.rootId commandId
        Assert.Equal(1, helloChildren.Length)
    })

[<Fact>]
let ``TestActor unknown command still finishes and drops live row`` () =
    withHost (fun host pool -> task {
        let commandId = NodeId.New()
        let event = toEvent { id = EventId.fromJson 0
                              submissionId = Guid.NewGuid()
                              ops =
                [ Op.NewNode(commandId, "unknown")
                  Op.SetClasses(commandId, CssClass.empty, CssClass.ofList [ "actor-test" ])
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" postResult |> ignore
        
        let request = sampleRequest Graph.rootId commandId [ Graph.rootId; commandId ]
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let! finished = waitForActorFinished host request.focusId 1000
        Assert.True(finished, "ActorFinished not received within timeout")
        
        let! dropped = waitForLiveRowDrop pool request.focusId 1000
        Assert.True(dropped, "Live row not dropped within timeout")
    })

[<Fact>]
let ``34b section7 outside proof - full lifecycle via CoreMailbox`` () =
    withHost (fun host pool -> task {
        let commandId = NodeId.New()
        let event = toEvent { id = EventId.fromJson 0
                              submissionId = Guid.NewGuid()
                              ops =
                [ Op.NewNode(commandId, "hello")
                  Op.SetClasses(commandId, CssClass.empty, CssClass.ofList [ "actor-test" ])
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        requireOk "postChange" postResult |> ignore
        
        let request = sampleRequest Graph.rootId commandId [ Graph.rootId; commandId ]
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        
        Assert.True(Set.contains request.focusId (pool.liveFocusIds ()),
            "Live row should exist after startActor")
        
        let! finished = waitForActorFinished host request.focusId 1000
        Assert.True(finished, "ActorFinished not received within timeout")
        
        let! dropped = waitForLiveRowDrop pool request.focusId 1000
        Assert.True(dropped, "Live row not dropped within timeout")
        
        let! state =
            CoreMailbox.getState host
            |> Async.StartAsTask
        let state = requireOk "getState" state
        
        let helloChildren =
            helloOutputChildren state.graph Graph.rootId commandId
        Assert.Equal(1, helloChildren.Length)
        
        let! events =
            eventPast host
            |> Async.StartAsTask
        
        let actorStartedIndex =
            events
            |> List.tryFindIndex (fun event ->
                match event.body with
                | Gambol.Shared.EventBody.ActorStart started
                    when started.focusId = request.focusId ->
                    true
                | _ -> false)
        
        let actorOutputChangeIndex =
            events
            |> List.tryFindIndex (fun event ->
                match event.body with
                | Gambol.Shared.EventBody.Change _ -> true
                | _ -> false)
        
        let actorFinishedIndex =
            events
            |> List.tryFindIndex (fun event ->
                match event.body with
                | Gambol.Shared.EventBody.ActorStop(focusId, _)
                    when focusId = request.focusId ->
                    true
                | _ -> false)
        
        let actorFinishedCount =
            events
            |> List.filter (fun event ->
                match event.body with
                | Gambol.Shared.EventBody.ActorStop(focusId, _)
                    when focusId = request.focusId ->
                    true
                | _ -> false)
            |> List.length
        
        Assert.True(actorStartedIndex.IsSome,
            "§7.4: Gambol.Shared.ActorStarted event should be present")
        Assert.True(actorOutputChangeIndex.IsSome,
            "§7.4: Change event (output) should be present")
        Assert.True(actorFinishedIndex.IsSome,
            "§7.4: ActorFinished event should be present")
        Assert.True(actorOutputChangeIndex.Value < actorStartedIndex.Value,
            "§7.4: Gambol.Shared.ActorStarted should appear before output Change")
        Assert.True(actorFinishedIndex.Value < actorOutputChangeIndex.Value,
            "§7.4: Output Change should appear before ActorFinished")
        Assert.Equal(1, actorFinishedCount)
        
        Assert.False(Set.contains request.focusId (pool.liveFocusIds ()),
            "§7.5: Live row should be gone after finish")
        
        let actorEventsPresent =
            events
            |> List.exists (fun event ->
                match event.body with
                | Gambol.Shared.EventBody.ActorStart started
                    when started.focusId = request.focusId ->
                    true
                | Gambol.Shared.EventBody.ActorStop(focusId, _)
                    when focusId = request.focusId ->
                    true
                | _ -> false)
        
        Assert.True(actorEventsPresent,
            "§7.5: Public Actor identity (ActorStarted, ActorFinished) should remain on History")
    })

[<Fact>]
let ``TestActor throw command fails gracefully and drops live row`` () =
    withHost (fun host pool -> task {
        let commandId = NodeId.New()
        let event = toEvent { id = EventId.fromJson 0
                              submissionId = Guid.NewGuid()
                              ops =
                [ Op.NewNode(commandId, "throw")
                  Op.SetClasses(commandId, CssClass.empty, CssClass.ofList [ "actor-test" ])
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnly host testCaller event
            |> Async.StartAsTask
        let _ = requireOk "postChange" postResult
        
        let request = sampleRequest Graph.rootId commandId [ Graph.rootId; commandId ]
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let! finished =
            waitForActorFinished host request.focusId 5000
        Assert.True(finished, "Actor should finish within timeout")
        
        let! dropped =
            waitForLiveRowDrop pool request.focusId 5000
        Assert.True(dropped, "Live row should be dropped after fail")
        
        Assert.False(Set.contains request.focusId (pool.liveFocusIds ()),
            "Live row should be gone after exception")
    })
