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
    { revision: Revision
      /// Stored Events for POST /events ACK (submission / request order).
      events: Gambol.Shared.Ev list
      externalChanges: bool
      message: string option
      isReady: bool }

/// The Core Changes contract. Every Change reaches persistence through this handle.
/// HTTP uses `postEvents` (Ev list from wire). `postChange` is graph-apply only
/// (Change list) — CoreEventDispatch builds Changes from Ev ops and calls postChange.
type CoreChanges =
    { getState: unit -> Async<Result<State, string>>
      getRevision: unit -> Async<Gambol.Shared.EventId>
      getEventsSince: Gambol.Shared.EventId -> Async<Gambol.Shared.Ev list>
      isReady: unit -> bool
      postChange: Change list -> Async<Result<CoreChangesAccepted, string>>
      postEvents: Gambol.Shared.Ev list -> Async<Result<CoreChangesAccepted, string>>
      postGraphOnlyChange:
        Change -> Async<Result<CoreChangesAccepted, string>>
      actorStop: ActorResult -> Async<Result<unit, string>>
      /// Rebind posts to another Caller on the same mailbox door.
      asCaller: Caller -> CoreChanges }

[<RequireQualifiedAccess>]
module CoreChanges =

    let accepted
        (revision: Revision)
        (isReady: bool)
        (events: Gambol.Shared.Ev list)
        (externalChanges: bool)
        (message: string option)
        : CoreChangesAccepted =
        { revision = revision
          events = events
          externalChanges = externalChanges
          message = message
          isReady = isReady }

    let mergeAccepted
        (prior: CoreChangesAccepted)
        (next: CoreChangesAccepted)
        : CoreChangesAccepted =
        { revision = next.revision
          events = prior.events @ next.events
          externalChanges =
            prior.externalChanges || next.externalChanges
          message = next.message |> Option.orElse prior.message
          isReady = next.isReady }
