namespace Gambol.Server

open Gambol.Shared

type Credential = Credential of string

/// Named source that submits requests to Core (Browser, Actor, …).
type Authority = Authority of string

/// Public Authority, login instance name, and secret at a Core door.
/// `name` is this browser/session instance, not the global user name.
type Caller =
    { authority: Authority
      name: string
      secret: Credential }

type ActorResult =
    | ActorSucceeded
    | ActorFailed

type CoreChangesAccepted =
    { revision: Revision
      changes: Change list
      externalChanges: bool
      message: string option
      isReady: bool }

/// The Core Changes contract. Every Change reaches persistence through this handle.
/// HTTP uses `postEvents` (Event list from wire). `postChange` is graph-apply only
/// (Change list) — CoreEventDispatch builds Changes from Event ops and calls postChange.
type CoreChanges =
    { getState: unit -> Async<Result<State, string>>
      getRevision: unit -> Async<Gambol.Shared.Events.EventId>
      getEventsSince: Gambol.Shared.Events.EventId -> Async<Gambol.Shared.Events.Event list>
      isReady: unit -> bool
      postChange: Change list -> Async<Result<CoreChangesAccepted, string>>
      postEvents: Gambol.Shared.Events.Event list -> Async<Result<CoreChangesAccepted, string>>
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
        (confirmed: Change list)
        (externalChanges: bool)
        (message: string option)
        : CoreChangesAccepted =
        { revision = revision
          changes = confirmed
          externalChanges = externalChanges
          message = message
          isReady = isReady }

    let mergeAccepted
        (prior: CoreChangesAccepted)
        (next: CoreChangesAccepted)
        : CoreChangesAccepted =
        { revision = next.revision
          changes = prior.changes @ next.changes
          externalChanges =
            prior.externalChanges || next.externalChanges
          message = next.message |> Option.orElse prior.message
          isReady = next.isReady }
