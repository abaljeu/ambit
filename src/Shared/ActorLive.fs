namespace Gambol.Shared

open Gambol.Shared.ViewModel

/// Browser graph, EventId, and ClientHistory used by local and remote apply.
type ClientSyncState =
    { graph: Graph
      eventId: EventId
      history: ClientHistory
      eventLog: EventLog
      actorLiveFocusIds: Set<NodeId> }

[<RequireQualifiedAccess>]
module ClientSyncState =
    let create graph eventId history : ClientSyncState =
        { graph = graph
          eventId = eventId
          history = history
          eventLog = EventLog.empty
          actorLiveFocusIds = Set.empty }

/// Live Focus set from ActorStart / ActorStop, plus boot seed from GetState.
/// StateResponse.seedLiveFocusIds is the mailbox lockPresent overlay (live table),
/// not a persisted Graph field and not a second Poll product.
[<RequireQualifiedAccess>]
module ActorLive =

    let liveRowClass = "amb-actor-live"

    let applyEvent (event: Ev) (live: Set<NodeId>) : Set<NodeId> =
        match event.body with
        | EventBody.ActorStart start -> Set.add start.focusId live
        | EventBody.ActorStop(focusId, _) -> Set.remove focusId live
        | EventBody.Change _
        | EventBody.Undo _
        | EventBody.Redo _ -> live

    let applyEvents (events: Ev list) (live: Set<NodeId>) : Set<NodeId> =
        List.fold (fun acc event -> applyEvent event acc) live events

    let focusIdsFromLockPresent (graph: Graph) : Set<NodeId> =
        graph.nodes
        |> Map.fold
            (fun acc id node ->
                if node.lockPresent then Set.add id acc else acc)
            Set.empty

    let toApplied (state: ClientSyncState) : AppliedBrowserGraph =
        { projectedGraph = state.graph
          projectedEventId = state.eventId
          projectedHistory = state.history
          projectedLiveFocusIds = state.actorLiveFocusIds }

    let lastCmdResult (events: Ev list) : CmdLastResult option =
        events
        |> List.fold
            (fun acc event ->
                match event.body with
                | EventBody.ActorStart _ ->
                    Some (CmdLastResult.Detail (Some "Run", "AI started."))
                | EventBody.ActorStop (_, ActorFailed) ->
                    Some (CmdLastResult.Error (Some "Ask", "Actor failed."))
                | EventBody.ActorStop (_, ActorCancelled) ->
                    Some (CmdLastResult.Error (Some "Ask", "Actor cancelled."))
                | _ -> acc)
            None
