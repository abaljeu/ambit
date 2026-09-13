namespace Gambol.Server

open Gambol.Shared

type MailboxHost = {
    mailbox: MailboxProcessor<CoreMsg>
    pool: CoreActorPool
    callers: CallerTable
    credentials: CoreCredentials
    events: EventLog
    isReady: unit -> bool
    flushSnapshot: unit -> Async<Result<unit, string>>
    dispose: unit -> unit
}
