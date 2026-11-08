module Gambol.Server.Tests.ActorOrderFromStartTests

open Xunit
open Gambol.Shared

let private startWithDistinctIds () : ActorStart =
    let focusId = NodeId.New()
    let runDefinitionId = NodeId.New()
    Assert.NotEqual(focusId, runDefinitionId)
    { zoomId = NodeId.New()
      focusId = focusId
      runDefinitionId = runDefinitionId
      graphIds = []
      eventId = EventId.zero }

let private orderFor (actorName: string) (start: ActorStart) =
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
            start
    order, marker

[<Fact>]
let ``orderFromStart sets actorNodeId to runDefinitionId and the given actor name`` () =
    let start = startWithDistinctIds ()
    let order, marker = orderFor "test" start
    Assert.Equal(start.runDefinitionId, order.actorNodeId)
    Assert.Equal("test", order.actorName)
    Assert.Same(marker, order.actor)
    let named, _ = orderFor "query" start
    Assert.Equal(start.runDefinitionId, named.actorNodeId)
    Assert.Equal("query", named.actorName)
