namespace Gambol.Server

open Gambol.Shared

type CoreChangesAccepted =
    { revision: Revision
      changes: Change list
      externalChanges: bool
      message: string option
      isReady: bool }
