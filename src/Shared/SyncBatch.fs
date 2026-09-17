namespace Gambol.Shared

[<RequireQualifiedAccess>]
module SyncBatch =
    /// Pending events stay EventId.zero on the wire. Server EventLog assigns ids.
    let toWireBatch (events: Ev list) : Ev list = events
