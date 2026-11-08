namespace Gambol.Server

open Gambol.Shared

/// Builds an ActorOrder from the client ActorStart.
/// The caller picks the cancelable node for that Actor.
[<RequireQualifiedAccess>]
module ActorOrderFromStart =

    type FunctionActorPlacement =
        { request: ActorStart
          actor: Gambol.Shared.ActorFunction
          actorName: string
          actorNodeIdOf: ActorStart -> NodeId }

    let orderFromStart
        (makeActor: NodeId -> NodeId -> Gambol.Shared.ActorFunction)
        (actorName: string)
        (actorNodeIdOf: ActorStart -> NodeId)
        (start: ActorStart)
        : ActorOrder =
        { actor = makeActor start.runDefinitionId start.focusId
          actorNodeId = actorNodeIdOf start
          actorName = actorName }

    let orderFromPlacement (placement: FunctionActorPlacement) : ActorOrder =
        orderFromStart
            (fun _ _ -> placement.actor)
            placement.actorName
            placement.actorNodeIdOf
            placement.request
