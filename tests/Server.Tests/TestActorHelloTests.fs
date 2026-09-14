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

let private waitForActorFinished host focusId timeoutMs =
    task {
        let mutable found = false
        let startTime = DateTime.UtcNow
        while not found && (DateTime.UtcNow - startTime).TotalMilliseconds < float timeoutMs do
            let! events =
                CoreMailbox.eventHistory host
                |> Async.StartAsTask
            found <-
                events
                |> List.exists (fun event ->
                    match event with
                    | ActorEvent (_, ActorFinished fid) when fid = focusId -> true
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

let private sampleRequest focusId commandId graphIds: StartActorRequest =
    { zoomId = Graph.rootId
      focusId = focusId
      commandId = commandId
      graphIds = graphIds
      revision = Revision 0 }

let private actorCaller secret =
    { authority = Authority "Actor"
      secret = secret }

let private createHost () =
    let dataDir = newTempDir ()
    let credentials = admittedCredentials ()
    let pool = CoreActorPool.create credentials
    pool.register (ActorName "test") TestActor.actorFn
    let host =
        CoreMailbox.host
            credentials
            pool
            (FileAgent.persist (FileAgent.create dataDir))
    host, credentials, pool

let private withHost body =
    task {
        let host, credentials, pool = createHost ()
        try
            do! body host credentials pool
        finally
            CoreMailbox.dispose host
    }

[<Fact>]
let ``TestActor hello posts one Owned child text hello under Focus`` () =
    withHost (fun host credentials pool -> task {
        let commandId = NodeId.New()
        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(commandId, "hello")
                  Op.SetClasses(commandId, CssClass.empty, CssClass.ofList [ "actor-test" ])
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnlyChange host [ change ]
            |> Async.StartAsTask
        requireOk "postChange" postResult
        
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
            state.graph.nodes.[Graph.rootId].children
            |> List.filter (fun child ->
                match child with
                | Owner nodeId ->
                    match Map.tryFind nodeId state.graph.nodes with
                    | Some node -> node.text = "hello"
                    | None -> false
                | _ -> false)
        
        Assert.Equal(1, helloChildren.Length)
    })

[<Fact>]
let ``TestActor hello stops successfully with ActorSucceeded`` () =
    withHost (fun host credentials pool -> task {
        let commandId = NodeId.New()
        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(commandId, "hello")
                  Op.SetClasses(commandId, CssClass.empty, CssClass.ofList [ "actor-test" ])
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnlyChange host [ change ]
            |> Async.StartAsTask
        requireOk "postChange" postResult
        
        let request = sampleRequest Graph.rootId commandId [ Graph.rootId; commandId ]
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let! finished = waitForActorFinished host request.focusId 1000
        Assert.True(finished, "ActorFinished not received within timeout")
        
        let! events =
            CoreMailbox.eventHistory host
            |> Async.StartAsTask
        
        let actorFinishedEvents =
            events
            |> List.choose (fun event ->
                match event with
                | ActorEvent (_, ActorFinished focusId) when focusId = request.focusId ->
                    Some focusId
                | _ -> None)
        
        Assert.Equal(1, actorFinishedEvents.Length)
    })

[<Fact>]
let ``TestActor hello drops live row after successful stop`` () =
    withHost (fun host credentials pool -> task {
        let commandId = NodeId.New()
        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(commandId, "hello")
                  Op.SetClasses(commandId, CssClass.empty, CssClass.ofList [ "actor-test" ])
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnlyChange host [ change ]
            |> Async.StartAsTask
        requireOk "postChange" postResult
        
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
    withHost (fun host credentials pool -> task {
        let commandId = NodeId.New()
        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(commandId, "hello")
                  Op.SetClasses(commandId, CssClass.empty, CssClass.ofList [ "actor-test" ])
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnlyChange host [ change ]
            |> Async.StartAsTask
        requireOk "postChange" postResult
        
        let request = sampleRequest Graph.rootId commandId [ Graph.rootId; commandId ]
        
        let! result =
            CoreMailbox.startActor host testCaller request
            |> Async.StartAsTask
        requireOk "startActor" result
        
        let! finished = waitForActorFinished host request.focusId 1000
        Assert.True(finished, "ActorFinished not received within timeout")
        
        let! events =
            CoreMailbox.eventHistory host
            |> Async.StartAsTask
        
        let actorStartedIndex =
            events
            |> List.tryFindIndex (fun event ->
                match event with
                | ActorEvent (_, ActorStarted (focusId, _)) when focusId = request.focusId ->
                    true
                | _ -> false)
        
        let actorFinishedIndex =
            events
            |> List.tryFindIndex (fun event ->
                match event with
                | ActorEvent (_, ActorFinished focusId) when focusId = request.focusId ->
                    true
                | _ -> false)
        
        Assert.True(actorStartedIndex.IsSome)
        Assert.True(actorFinishedIndex.IsSome)
        Assert.True(actorStartedIndex.Value < actorFinishedIndex.Value)
    })

[<Fact>]
let ``TestActor hello interprets command node text`` () =
    withHost (fun host credentials pool -> task {
        let commandId = NodeId.New()
        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(commandId, "HELLO")
                  Op.SetClasses(commandId, CssClass.empty, CssClass.ofList [ "actor-test" ])
                  Op.Replace(Graph.rootId, [], [ ChildNode.owner commandId ]) ] }
        let! postResult =
            CoreMailbox.postGraphOnlyChange host [ change ]
            |> Async.StartAsTask
        requireOk "postChange" postResult
        
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
            state.graph.nodes.[Graph.rootId].children
            |> List.filter (fun child ->
                match child with
                | Owner nodeId ->
                    match Map.tryFind nodeId state.graph.nodes with
                    | Some node -> node.text = "hello"
                    | None -> false
                | _ -> false)
        
        Assert.Equal(1, helloChildren.Length)
    })
