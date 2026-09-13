namespace Gambol.Server

open Gambol.Shared

type MailboxHost = {
    mailbox: MailboxProcessor<CoreMsg>
    isReady: unit -> bool
    flushSnapshot: unit -> Async<Result<unit, string>>
    dispose: unit -> unit
}
