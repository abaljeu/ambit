namespace Gambol.Server

open Gambol.Shared

type MailboxHost =
    private {
        mailbox: MailboxProcessor<QueueSum>
        gate: obj
        isReady: unit -> bool
        flushSnapshot: unit -> Async<Result<unit, string>>
        dispose: unit -> unit
    }

[<RequireQualifiedAccess>]
module MailboxHost =

    let internal create mailbox (persist: PersistFilling) : MailboxHost = {
        mailbox = mailbox
        gate = obj ()
        isReady = persist.isReady
        flushSnapshot = persist.flushSnapshot
        dispose = persist.dispose
    }

    let internal postItem (host: MailboxHost) (item: QueueSum) =
        lock host.gate (fun () -> host.mailbox.Post item)

    /// Post each message back to back. The gate keeps another caller out.
    let internal postForReplies
        (host: MailboxHost)
        (builds: (AsyncReplyChannel<'Reply> -> CoreMsg) list)
        : Async<'Reply list> =
        let tasks =
            lock host.gate (fun () ->
                builds
                |> List.map (fun build ->
                    let source =
                        System.Threading.Tasks.TaskCompletionSource<_>()
                    let post =
                        host.mailbox.PostAndAsyncReply(fun channel ->
                            QueueSum.Core(build channel))
                    Async.StartWithContinuations(
                        post,
                        (fun reply -> source.TrySetResult reply |> ignore),
                        (fun ex -> source.TrySetException ex |> ignore),
                        (fun _ -> ()))
                    source.Task))
        async {
            let! replies =
                tasks
                |> List.map Async.AwaitTask
                |> Async.Parallel
            return Array.toList replies
        }

    let internal postAndAsyncReply
        (host: MailboxHost)
        (build: AsyncReplyChannel<'a> -> CoreMsg)
        : Async<'a> =
        Async.FromContinuations(fun (cont, econt, ccont) ->
            lock host.gate (fun () ->
                Async.StartWithContinuations(
                    host.mailbox.PostAndAsyncReply(fun channel ->
                        QueueSum.Core(build channel)),
                    cont,
                    econt,
                    ccont)))

    let isReady (host: MailboxHost) = host.isReady

    let flushSnapshot (host: MailboxHost) = host.flushSnapshot ()

    let dispose (host: MailboxHost) = host.dispose ()
