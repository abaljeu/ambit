namespace Gambol.Shared.Events

open System
open Gambol.Shared

type EventLog =
    { events: Event list
      nextId: EventId }

[<RequireQualifiedAccess>]
module EventLog =
    let empty: EventLog = { events = []; nextId = EventId 0 }

    let nextId (log: EventLog) : EventId = log.nextId

    let append (event: Event) (log: EventLog) : EventLog =
        let (EventId n) = log.nextId
        { events = { event with id = log.nextId } :: log.events
          nextId = EventId(n + 1) }

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
        let (EventId start) = log.nextId
        let folder (events, seen, next) event =
            if Set.contains event.submissionId seen then
                events, seen, next
            else
                let (EventId n) = event.id
                event :: events,
                Set.add event.submissionId seen,
                max next (n + 1)
        let events, _, next =
            List.fold folder (log.events, known, start) persisted
        { events = events; nextId = EventId next }
