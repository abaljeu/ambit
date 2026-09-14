namespace Gambol.Server

open Gambol.Shared

type MailboxHost = {
    mailbox: MailboxProcessor<CoreMsg>
    isReady: unit -> bool
    flushSnapshot: unit -> Async<Result<unit, string>>
    dispose: unit -> unit
}

/// Persist + lifecycle the mailbox hosts. File and Db build this; CoreMailbox
/// does not read agent fields.
type PersistFilling = {
    handlers: PersistHandlers
    onError: string -> string -> exn -> unit
    formatError: string -> string
    isReady: unit -> bool
    flushSnapshot: unit -> Async<Result<unit, string>>
    dispose: unit -> unit
    until: Async<Result<unit, string>> option
    bindMailbox: MailboxProcessor<CoreMsg> -> unit
}
