namespace Gambol.Server

open Gambol.Shared

type MailboxHost =
    private {
        mailbox: MailboxProcessor<CoreMsg>
        isReady: unit -> bool
        flushSnapshot: unit -> Async<Result<unit, string>>
        dispose: unit -> unit
    }

[<RequireQualifiedAccess>]
module MailboxHost =

    let internal create
        mailbox
        isReady
        flushSnapshot
        dispose
        : MailboxHost =
        { mailbox = mailbox
          isReady = isReady
          flushSnapshot = flushSnapshot
          dispose = dispose }

    let internal postAndAsyncReply
        (host: MailboxHost)
        (build: AsyncReplyChannel<'a> -> CoreMsg)
        : Async<'a> =
        host.mailbox.PostAndAsyncReply build

    let isReady (host: MailboxHost) = host.isReady

    let flushSnapshot (host: MailboxHost) = host.flushSnapshot ()

    let dispose (host: MailboxHost) = host.dispose ()

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
    bindSnapshot: (Graph option -> unit) -> unit
}
