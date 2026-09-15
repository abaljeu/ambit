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
        { events = log.events @ [ { event with id = log.nextId } ]
          nextId = EventId(n + 1) }

    let since (EventId after) (log: EventLog) : Event list =
        log.events
        |> List.filter (fun event ->
            let (EventId n) = event.id
            n > after)

    let tryFind (eventId: EventId) (log: EventLog) : Event option =
        log.events |> List.tryFind (fun event -> event.id = eventId)

    let private unusedBySubmission
        (known: Set<Guid>)
        (persisted: Event list)
        : Event list =
        let folder (acc, seen) event =
            if Set.contains event.submissionId seen then
                acc, seen
            else
                event :: acc, Set.add event.submissionId seen

        persisted
        |> List.fold folder ([], known)
        |> fst
        |> List.rev

    let restore (persisted: Event list) (log: EventLog) : EventLog =
        let known =
            log.events
            |> List.map (fun event -> event.submissionId)
            |> Set.ofList
        let fresh = unusedBySubmission known persisted
        if List.isEmpty fresh then
            log
        else
            let (EventId start) = log.nextId
            let next =
                fresh
                |> List.fold
                    (fun acc event ->
                        let (EventId n) = event.id
                        max acc (n + 1))
                    start
            { events = log.events @ fresh
              nextId = EventId next }
