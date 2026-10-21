namespace Gambol.Server

type MailboxHost =
    private {
        mailbox: MailboxProcessor<CoreQueue>
        gate: obj
        isReady: unit -> bool
        dispose: unit -> unit
        session: Session
    }

[<RequireQualifiedAccess>]
module MailboxHost =

    let internal create
        mailbox
        (persist: PersistFilling)
        session
        (shutdown: unit -> unit)
        : MailboxHost = {
        mailbox = mailbox
        gate = obj ()
        isReady = persist.isReady
        dispose =
            fun () ->
                shutdown ()
                persist.dispose ()
        session = session
    }

    let internal session (host: MailboxHost) = host.session

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

    let private postItemAndAsyncReply
        (host: MailboxHost)
        (build: AsyncReplyChannel<'a> -> CoreQueue)
        : Async<'a> =
        Async.FromContinuations(fun (cont, econt, ccont) ->
            lock host.gate (fun () ->
                Async.StartWithContinuations(
                    host.mailbox.PostAndAsyncReply build,
                    cont,
                    econt,
                    ccont)))

    let internal postAndAsyncReply
        (host: MailboxHost)
        (build: AsyncReplyChannel<'a> -> CoreMsg)
        : Async<'a> =
        postItemAndAsyncReply host (fun channel ->
            coreMsgItem (build channel))

    let internal postMailboxAndAsyncReply
        (host: MailboxHost)
        (build: AsyncReplyChannel<'a> -> Mailbox)
        : Async<'a> =
        postItemAndAsyncReply host (fun channel ->
            CoreQueue.Public (build channel))

    let isReady (host: MailboxHost) = host.isReady

    let dispose (host: MailboxHost) = host.dispose ()
