namespace Gambol.Server

open Gambol.Shared

/// Builds an ActorOrder from the client ActorStart.
[<RequireQualifiedAccess>]
module ActorOrderFromStart =

    /// Cancelable node for this start. One place, so a later actor can replace it.
    let private actorNodeIdOf (start: ActorStart) : NodeId =
        start.runDefinitionId

    let orderFromStart
        (makeActor: NodeId -> NodeId -> Gambol.Shared.ActorFunction)
        (actorName: string)
        (start: ActorStart)
        : ActorOrder =
        { actor = makeActor start.runDefinitionId start.focusId
          actorNodeId = actorNodeIdOf start
          actorName = actorName }
