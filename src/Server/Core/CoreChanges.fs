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
      message: string option
      isReady: bool }

[<RequireQualifiedAccess>]
module CoreChanges =

    let accepted
        (eventId: EventId)
        (isReady: bool)
        (events: Ev list)
        (externalChanges: bool)
        (message: string option)
        : CoreChangesAccepted =
        { eventId = eventId
          events = events
          externalChanges = externalChanges
          message = message
          isReady = isReady }

    let mergeAccepted
        (prior: CoreChangesAccepted)
        (next: CoreChangesAccepted)
        : CoreChangesAccepted =
        { eventId = next.eventId
          events = prior.events @ next.events
          externalChanges =
            prior.externalChanges || next.externalChanges
          message = next.message |> Option.orElse prior.message
          isReady = next.isReady }
