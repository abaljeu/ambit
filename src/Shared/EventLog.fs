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
