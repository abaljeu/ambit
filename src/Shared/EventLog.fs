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

    let since (EventId after) (log: EventLog) : EventLog =
        { log with
            events =
                log.events
                |> List.filter (fun event ->
                    let (EventId n) = event.id
                    n > after) }

    let tryFind (eventId: EventId) (log: EventLog) : Ev option =
        log.events |> List.tryFind (fun event -> event.id = eventId)

    /// Merge persisted Events; cons so oldest-first persist input yields newest-head.
    let restore (persisted: Ev list) (log: EventLog) : EventLog =
        let known =
            log.events
            |> List.map (fun event -> event.submissionId)
            |> Set.ofList
        let folder (events, seen) event =
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
