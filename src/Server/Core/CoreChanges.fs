namespace Gambol.Server

open Gambol.Shared

type Credential = Credential of string

/// Public Authority, login instance name, and secret at a Core door.
/// `name` is this browser/session instance, not the global user name.
type Caller =
    { authority: Authority
      name: string
      secret: Credential }

type CoreChangesAccepted =
    { eventId: EventId
      /// Stored Events for POST /events ACK (submission / request order).
      events: Ev list
      externalChanges: bool
      isReady: bool }

[<RequireQualifiedAccess>]
module CoreChanges =

    let accepted
        (eventId: EventId)
        (isReady: bool)
        (events: Ev list)
        (externalChanges: bool)
        : CoreChangesAccepted =
        { eventId = eventId
          events = events
          externalChanges = externalChanges
          isReady = isReady }

    let fromPosted
        (isReady: bool)
        (posted: Ev list)
        (stored: Ev list)
        : CoreChangesAccepted =
        let eventId =
            match List.tryLast stored with
            | Some storedEv -> storedEv.id
            | None -> EventId.zero
        let opsDiffer postedEv storedEv =
            postedEv.submissionId = storedEv.submissionId
            && Ev.ops postedEv <> Ev.ops storedEv
        { eventId = eventId
          events = stored
          externalChanges =
            posted
            |> List.exists (fun postedEv ->
                stored |> List.exists (opsDiffer postedEv))
          isReady = isReady }

    let mergeAccepted
        (prior: CoreChangesAccepted)
        (next: CoreChangesAccepted)
        : CoreChangesAccepted =
        { eventId = next.eventId
          events = prior.events @ next.events
          externalChanges =
            prior.externalChanges || next.externalChanges
          isReady = next.isReady }
