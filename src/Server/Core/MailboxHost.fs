namespace Gambol.Server

open Gambol.Shared

type MailboxHost =
    private {
        mailbox: MailboxProcessor<CoreQueue>
        gate: obj
        isReady: unit -> bool
        dispose: unit -> unit
        noticeLog: Notice list ref
    }

[<RequireQualifiedAccess>]
module MailboxHost =

    let internal create
        mailbox
        (persist: PersistFilling)
        (noticeLog: Notice list ref)
        : MailboxHost = {
        mailbox = mailbox
        gate = obj ()
        isReady = persist.isReady
        dispose = persist.dispose
        noticeLog = noticeLog
    }

    /// Notice posts the mailbox has logged. Newest last.
    let internal loggedNotices (host: MailboxHost) =
        List.rev host.noticeLog.Value

    let internal coreMsgItem (msg: CoreMsg) : CoreQueue =
        CoreQueue.Public (Mailbox.CoreMsg msg)

    let internal postItem (host: MailboxHost) (item: CoreQueue) =
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
                            coreMsgItem (build channel))
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
                        coreMsgItem (build channel)),
                    cont,
                    econt,
                    ccont)))

    let isReady (host: MailboxHost) = host.isReady

    let dispose (host: MailboxHost) = host.dispose ()
