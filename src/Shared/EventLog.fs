namespace Gambol.Shared

open Thoth.Json.Core
open Thoth.Json.JavaScript
open Gambol.Shared

type EventLog =
    { events: Ev list
      nextId: EventId }

[<RequireQualifiedAccess>]
module EventLog =
    // EventId retires Revision: cursor 0 is before the first Ev; first id is 1.
    let empty: EventLog = { events = []; nextId = EventId.next EventId.zero }

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

    let all (log: EventLog) : EventLog = since EventId.beforeAll log

    let tryFind (eventId: EventId) (log: EventLog) : Ev option =
        log.events |> List.tryFind (fun event -> event.id = eventId)

    /// Merge persisted Events; cons so oldest-first persist input yields newest-head.
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
        { log with events = events }

    /// Merge persisted Events into an empty log, dedupe by `submissionId`, set `nextId` past max id.
    let restorePersisted (persisted: Ev list) : EventLog =
        let nextId =
            match persisted with
            | [] -> empty.nextId
            | _ ->
                persisted
                |> List.map Ev.id
                |> List.reduce EventId.max
                |> EventId.next
        restore persisted { empty with nextId = nextId }

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

    let private applyRecover event state =
        let next =
            match Ev.apply event state with
            | ApplyResult.Changed next -> next
            | ApplyResult.Unchanged next -> next
            | ApplyResult.Invalid (next, _) -> next
        { next with eventId = event.id }

    /// Load reconcile: Graph id vs EventLog tip. Ids move only via apply.
    let recover (state: State) (log: EventLog) : State * EventLog =
        let graphId = EventId.value state.eventId
        let logId = EventId.value (tip log)
        if graphId > logId then
            state, afterCheckpoint state.eventId
        elif graphId = logId then
            state, log
        else
            let ahead =
                log.events
                |> List.rev
                |> List.filter (fun event ->
                    EventId.value event.id > graphId)
            List.fold applyRecover state ahead, log
