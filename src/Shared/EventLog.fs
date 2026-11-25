namespace Gambol.Shared

open Thoth.Json.Core
open Thoth.Json.JavaScript
open Gambol.Shared

type EventLog =
    { events: Ev list
      nextId: EventId }

[<RequireQualifiedAccess>]
module EventLog =
    // Empty nextId is the first assignable stored Int, not next of Zero.
    let empty: EventLog = { events = []; nextId = EventId.fromJson 1 }

    let nextId (log: EventLog) : EventId = log.nextId

    // Append-only newest-head sequence (arch Module map EventLog).
    let append (event: Ev) (log: EventLog) : EventLog =
        { events = { event with id = log.nextId } :: log.events
          nextId = EventId.next log.nextId }

    let since after (log: EventLog) : EventLog =
        { log with
            events =
                log.events
                |> List.filter (fun event ->
                    EventId.value event.id > EventId.value after) }

    let all (log: EventLog) : EventLog = since EventId.zero log

    let tryFind (eventId: EventId) (log: EventLog) : Ev option =
        log.events |> List.tryFind (fun event -> event.id = eventId)

    /// Merge persisted Events; cons so oldest-first persist input yields newest-head.
    /// nextId is past max of the source nextId and every merged Event id.
    let restore (persisted: Ev list) (log: EventLog) : EventLog =
        let known =
            log.events
            |> List.map (fun event -> event.submissionId)
            |> Set.ofList
        let folder (events, seen) (event: Ev) =
            if Set.contains event.submissionId seen then
                events, seen
            else
                event :: events, Set.add event.submissionId seen
        let events, _ = List.fold folder (log.events, known) persisted
        let nextId =
            match events |> List.map Ev.id with
            | [] -> log.nextId
            | ids ->
                EventId.max log.nextId (EventId.next (List.reduce EventId.max ids))
        { events = events; nextId = nextId }

    /// Merge persisted Events into an empty log, dedupe by `submissionId`.
    let restorePersisted (persisted: Ev list) : EventLog =
        restore persisted empty

    /// Raise nextId past eventId when that id is already stored.
    let advancePast (eventId: EventId) (log: EventLog) : EventLog =
        { log with nextId = EventId.max log.nextId (EventId.next eventId) }

    /// Max restored Ev.id, or EventId.zero when the log is empty.
    let tip (log: EventLog) : EventId =
        match log.events with
        | [] -> EventId.zero
        | events -> events |> List.map Ev.id |> List.reduce EventId.max

    /// Adopt a newest-head Ev list without a second cons-fold.
    let adoptNewestHead (events: Ev list) : EventLog =
        { events = events
          nextId =
            match events with
            | [] -> empty.nextId
            | _ ->
                events
                |> List.map Ev.id
                |> List.reduce EventId.max
                |> EventId.next }

    /// Empty log whose next assignable id is past a Graph checkpoint.
    let afterCheckpoint (eventId: EventId) : EventLog =
        { empty with nextId = EventId.next eventId }

    /// Persist Notices and parse Notices. Replay leaves the file and parse state.
    let private replaySkipsNotice (notice: Notice) =
        match notice with
        | Notice.NodePersisting _
        | Notice.NodePersisted _
        | Notice.PersistFailed _
        | Notice.NodeUnpersisted _
        | Notice.NodeParsing _
        | Notice.NodeParsed _
        | Notice.NodeUnparsed _
        | Notice.NodeReceived _
        | Notice.NodeUploading _
        | Notice.ActorStopped _ -> true
        | Notice.ParseFailed _ -> false

    /// Replay of Parse writes the owning special at the Event id.
    let private flagParseReplay (event: Ev) (state: GraphAtEvent) =
        if event.commandName <> "Parse" then
            state
        else
            let ops = Ev.ops event |> Option.defaultValue []
            let graph =
                DocumentOpImpact.owningSpecials state.graph ops
                |> List.fold
                    (fun graph nodeId ->
                        GraphMutate.setUnpersistedAt
                            nodeId event.id graph)
                    state.graph
            { state with graph = graph }

    /// Resulting state, plus the `ApplyResult.Invalid` reason when that event
    /// was skipped. `appliedState` below is this with the reason dropped.
    let private appliedStateResult (state: GraphAtEvent) (event: Ev) =
        match event.body with
        | EventBody.Notice notice when replaySkipsNotice notice ->
            state, None
        | _ ->
            match Ev.apply event state with
            | ApplyResult.Changed next -> flagParseReplay event next, None
            | ApplyResult.Unchanged next -> next, None
            | ApplyResult.Invalid (next, reason) -> next, Some reason

    let private appliedState state event =
        fst (appliedStateResult state event)

    let private applyRecover state event =
        { appliedState state event with eventId = event.id }

    /// Same per-event step as `applyRecover`; also accumulates (oldest-first
    /// after a final reverse) each skipped event's id and invalid reason.
    let private applyRecoverWithSkip
        ((state, skips): GraphAtEvent * (EventId * string) list)
        (event: Ev)
        : GraphAtEvent * (EventId * string) list =
        let next, invalidReason = appliedStateResult state event
        let state' = { next with eventId = event.id }
        match invalidReason with
        | None -> state', skips
        | Some reason -> state', (event.id, reason) :: skips

    let private eventsAhead (state: GraphAtEvent) (log: EventLog) : Ev list =
        let graphId = EventId.value state.eventId
        log.events
        |> List.rev
        |> List.filter (fun event -> EventId.value event.id > graphId)

    /// Same computation as `recover`; also returns, oldest first, every event
    /// `recover` advances past via `ApplyResult.Invalid`, with its id and the
    /// invalid reason. Diagnostic only — `recover` discards this third value.
    let recoverWithInvalidSkips
        (state: GraphAtEvent)
        (log: EventLog)
        : GraphAtEvent * EventLog * (EventId * string) list =
        let graphId = EventId.value state.eventId
        let logId = EventId.value (tip log)
        if graphId > logId then
            state, afterCheckpoint state.eventId, []
        elif graphId = logId then
            state, log, []
        else
            let finalState, skips =
                List.fold applyRecoverWithSkip (state, []) (eventsAhead state log)
            finalState, log, List.rev skips

    /// Load reconcile: Graph id vs EventLog tip. Ids move only via apply.
    let recover (state: GraphAtEvent) (log: EventLog) : GraphAtEvent * EventLog =
        let state, log, _ = recoverWithInvalidSkips state log
        state, log
