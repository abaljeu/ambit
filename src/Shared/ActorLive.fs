namespace Gambol.Shared

open System
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

    let cancelControlClass = "amb-actor-cancel"

    let offersCancel (focusId: NodeId) (live: Set<NodeId>) =
        Set.contains focusId live

    let cancelEffect (focusId: NodeId) (live: Set<NodeId>) : Effect option =
        if offersCancel focusId live then Some (SubmitCancel focusId)
        else None

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

    let private failedText message =
        if message = "" then "Actor failed." else message

    let private titleCaseName (token: string) =
        if token = "" then token
        else
            string (Char.ToUpperInvariant token[0])
            + token.Substring(1)

    /// First `?` Command text from Focus up the owner path to zoom.
    let private commandTextOnPath
        (graph: Graph)
        (focusId: NodeId)
        (zoomRoot: NodeId)
        : string option =
        let atBoundOrCommand (node: Node) =
            CommandRequest.isCommandText node.text
            || node.id = zoomRoot
        GraphQuery.enclosing graph atBoundOrCommand focusId
        |> Option.bind (fun id -> Map.tryFind id graph.nodes)
        |> Option.bind (fun node ->
            if CommandRequest.isCommandText node.text then
                Some node.text
            else
                None)

    let private displayLabel
        (graph: Graph)
        (focusId: NodeId)
        (zoomRoot: NodeId)
        : string option =
        commandTextOnPath graph focusId zoomRoot
        |> Option.bind CommandRequest.actorNameFromText
        |> Option.map titleCaseName

    let private startResult
        (graph: Graph)
        (focusId: NodeId)
        (zoomRoot: NodeId)
        : CmdLastResult =
        let label =
            displayLabel graph focusId zoomRoot
            |> Option.defaultValue "Actor"
        CmdLastResult.Detail (Some "Run", $"{label} started.")

    let private resultOf
        (graph: Graph)
        (zoomRoot: NodeId)
        (event: Ev)
        : CmdLastResult option =
        match event.body with
        | EventBody.ActorStart start ->
            Some (startResult graph start.focusId start.zoomId)
        | EventBody.ActorStop(focusId, result) ->
            let chip = displayLabel graph focusId zoomRoot
            match result with
            | ActorSucceeded ->
                Some (CmdLastResult.Detail (chip, "Actor succeeded."))
            | ActorFailed message ->
                Some (CmdLastResult.Error (chip, failedText message))
            | ActorCancelled ->
                Some (CmdLastResult.Error (chip, "Actor cancelled."))
        | EventBody.Change _
        | EventBody.Undo _
        | EventBody.Redo _ -> None

    let lastCmdResult
        (graph: Graph)
        (zoomRoot: NodeId)
        (events: Ev list)
        : CmdLastResult option =
        events
        |> List.fold
            (fun acc event ->
                match resultOf graph zoomRoot event with
                | Some result -> Some result
                | None -> acc)
            None
