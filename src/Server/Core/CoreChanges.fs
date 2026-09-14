namespace Gambol.Server

open Gambol.Shared

type Credential = Credential of string

/// Named source that submits requests to Core (Browser, Actor, …).
type Authority = Authority of string

/// Public Authority and secret presented together at a Core door.
type Caller =
    { authority: Authority
      secret: Credential }

type CoreChangesAccepted =
    { revision: Revision
      changes: Change list
      externalChanges: bool
      message: string option
      isReady: bool }

/// The Core Changes contract. Every Change reaches persistence through this handle.
/// `postChange` is a stamped view: Caller is closed over and sent on
/// CoreMsg PostChange for mailbox validation before PersistHandlers.
type CoreChanges =
    { getState: unit -> Async<Result<State, string>>
      getRevision: unit -> Async<Revision>
      getChangesSince: Revision -> Async<Change list>
      isReady: unit -> bool
      postChange: Change list -> Async<Result<CoreChangesAccepted, string>>
      postGraphOnlyChange:
        Change list -> Async<Result<CoreChangesAccepted, string>>
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
