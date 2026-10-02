namespace Gambol.Server

open Gambol.Shared

type MailboxHost =
    private {
        mailbox: MailboxProcessor<QueueSum>
        isReady: unit -> bool
        flushSnapshot: unit -> Async<Result<unit, string>>
        dispose: unit -> unit
    }

[<RequireQualifiedAccess>]
module MailboxHost =

    let internal create mailbox (persist: PersistFilling) : MailboxHost = {
        mailbox = mailbox
        isReady = persist.isReady
        flushSnapshot = persist.flushSnapshot
        dispose = persist.dispose
    }

    let internal postItem (host: MailboxHost) (item: QueueSum) =
        host.mailbox.Post item

    let internal postAndAsyncReply
        (host: MailboxHost)
        (build: AsyncReplyChannel<'a> -> CoreMsg)
        : Async<'a> =
        host.mailbox.PostAndAsyncReply(fun channel ->
            QueueSum.Core(build channel))

    let isReady (host: MailboxHost) = host.isReady

    let flushSnapshot (host: MailboxHost) = host.flushSnapshot ()

    let dispose (host: MailboxHost) = host.dispose ()
