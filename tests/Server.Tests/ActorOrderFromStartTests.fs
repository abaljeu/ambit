module Gambol.Server.Tests.ActorOrderFromStartTests

open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private startWithDistinctIds () : ActorStart =
    let focusId = NodeId.New()
    let runDefinitionId = NodeId.New()
    Assert.NotEqual(focusId, runDefinitionId)
    { zoomId = NodeId.New()
      focusId = focusId
      runDefinitionId = runDefinitionId
      graphIds = []
      eventId = EventId.zero }

let private orderFor
    (actorNodeIdOf: ActorStart -> NodeId)
    (actorName: string)
    (start: ActorStart)
    =
    let marker : Gambol.Shared.ActorFunction =
        fun _ _ -> async { return () }
    let makeActor
        (runDefinitionId: NodeId)
        (focusId: NodeId)
        : Gambol.Shared.ActorFunction =
        Assert.Equal(start.runDefinitionId, runDefinitionId)
        Assert.Equal(start.focusId, focusId)
        marker
    let order =
        Gambol.Server.ActorOrderFromStart.orderFromStart
            makeActor
            actorName
            actorNodeIdOf
            start
    order, marker

[<Fact>]
let ``orderFromStart sets actorNodeId from the selector and the actor name`` () =
    let start = startWithDistinctIds ()
    let order, marker =
        orderFor ActorOrderFromStart.focusIdOf "test" start
    Assert.Equal(start.focusId, order.actorNodeId)
    Assert.Equal("test", order.actorName)
    Assert.Same(marker, order.actor)
    let named, _ =
        orderFor (fun item -> item.runDefinitionId) "query" start
    Assert.Equal(start.runDefinitionId, named.actorNodeId)
    Assert.Equal("query", named.actorName)

[<Fact>]
let ``coreMailboxStartActorFromFunction places StartActor from the selector`` () =
    task {
        let focusId = NodeId.New()
        let runDefinitionId = NodeId.New()
        Assert.NotEqual(focusId, runDefinitionId)
        let request : ActorStart =
            { zoomId = NodeId.New()
              focusId = focusId
              runDefinitionId = runDefinitionId
              graphIds = []
              eventId = EventId.zero }
        let actor : ActorFunction =
            fun _ _ -> async { return () }
        let host, _ = createAdmittedFile (newTempDir ())
        try
            let placement : ActorOrderFromStart.FunctionActorPlacement =
                { request = request
                  actor = actor
                  actorName = "supplied"
                  actorNodeIdOf = fun start -> start.runDefinitionId }
            let! placed =
                CoreMailboxApi.coreMailboxStartActorFromFunction
                    host
                    testCaller
                    placement
                |> Async.StartAsTask
            let placedId =
                match placed with
                | Ok id -> id
                | Error err -> failwith err
            let! history =
                CoreMailboxApi.eventHistory host |> Async.StartAsTask
            let starts =
                history.events
                |> List.choose (fun event ->
                    match event.body with
                    | EventBody.Order (Order.StartActor order) ->
                        Some (event.id, order)
                    | _ -> None)
            match starts with
            | [ id, order ] ->
                Assert.Equal(placedId, id)
                Assert.Equal("supplied", order.actorName)
                Assert.Equal(runDefinitionId, order.actorNodeId)
            | other ->
                Assert.Fail($"expected one StartActor, got {List.length other}")
            let deprecated =
                history.events
                |> List.exists (fun event ->
                    match event.body with
                    | EventBody.ActorStart _ -> true
                    | _ -> false)
            Assert.False(deprecated)
        finally
            CoreMailboxApi.dispose host
    }
