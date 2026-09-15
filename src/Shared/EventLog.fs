namespace Gambol.Shared.Events

open Thoth.Json.Core
open Thoth.Json.JavaScript
open Gambol.Shared

type EventLog =
    { events: Event list
      nextId: EventId }

[<RequireQualifiedAccess>]
module EventLog =
    let empty: EventLog = { events = []; nextId = EventId.zero }

    let nextId (log: EventLog) : EventId = log.nextId

    let append (event: Event) (log: EventLog) : EventLog =
        { events = { event with id = log.nextId } :: log.events
          nextId = EventId.next log.nextId }

    let since (EventId after) (log: EventLog) : EventLog =
        { log with
            events =
                log.events
                |> List.filter (fun event ->
                    let (EventId n) = event.id
                    n > after) }

    let tryFind (eventId: EventId) (log: EventLog) : Event option =
        log.events |> List.tryFind (fun event -> event.id = eventId)

    let restore (persisted: Event list) (log: EventLog) : EventLog =
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
    let restorePersisted (persisted: Event list) : EventLog =
        let nextId =
            match persisted with
            | [] -> EventId.zero
            | _ ->
                persisted
                |> List.map Event.id
                |> List.reduce EventId.max
                |> EventId.next
        restore persisted { empty with nextId = nextId }
