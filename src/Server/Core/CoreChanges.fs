namespace Gambol.Server

open Gambol.Shared

type CoreChangesAccepted =
    { revision: Revision
      changes: Change list
      externalChanges: bool
      message: string option
      isReady: bool }

/// The Core Changes contract. Every Change reaches persistence through this handle.
type CoreChanges =
    { getState: unit -> Async<Result<State, string>>
      getRevision: unit -> Async<Revision>
      getChangesSince: Revision -> Async<Change list>
      isReady: unit -> bool
      postChange: Change list -> Async<Result<CoreChangesAccepted, string>>
      postGraphOnlyChange:
        Change list -> Async<Result<CoreChangesAccepted, string>> }

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
