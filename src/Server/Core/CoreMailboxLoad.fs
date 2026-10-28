namespace Gambol.Server

open Gambol.Shared

[<RequireQualifiedAccess>]
module internal CoreMailboxLoad =

    type MailboxContext = CoreMailboxEvents.MailboxContext

    let private loadSubjectError =
        function
        | LoadSubjectRejection.NotLoadSubject ->
            "Load subject is not a Workspace, Directory, or File node"
        | LoadSubjectRejection.NotFound -> "Load subject not found"

    let private isLoadSubject (graph: Graph) (subject: NodeId) =
        LoadSubject.ofId graph subject
        |> Result.map (fun _ -> ())
        |> Result.mapError loadSubjectError

    /// Log the notice, then write its axis. The push waits on Ok.
    let private applyLoggedNotice
        (context: MailboxContext)
        (notice: Notice)
        : Result<unit, string> =
        match
            CoreEventDispatch.logNotice
                (CoreMailboxEvents.eventDispatchContext context)
                notice
        with
        | Error err -> Error err
        | Ok _ ->
            CoreEventDispatch.writeAxis
                context.persist
                notice
                (NoticeGraph.writeGraph notice)

    /// Mailbox Load door. Desk Load calls this in the same turn.
    /// NodeUnparsed is applied before the node is pushed.
    let private dispatchLoad
        (context: MailboxContext)
        (caller: Caller)
        (subject: NodeId)
        : Result<unit, string> =
        match context.persist.getState () with
        | Error err -> Error err
        | Ok state ->
            match isLoadSubject state.graph subject with
            | Error err -> Error err
            | Ok () ->
                applyLoggedNotice
                    context
                    (Notice.NodeUnparsed subject)
                |> Result.bind (fun () -> context.parsePush subject)

    let internal dispatchStartLoadSaveCommand
        context
        caller
        path
        peerName
        (request: LoadSaveCommandRequest)
        reply
        =
        CoreMailboxActors.dispatchActorStartResult
            context
            caller
            request.start
            reply
            (fun _ getState ->
                DeskLoadPush.runLoadSave
                    { pool = context.pool
                      getState = getState
                      peerName = peerName }
                    (dispatchLoad context caller)
                    request
                    path
                |> Result.mapError StartError.Rejected)

    let internal replyLoad
        (context: MailboxContext)
        (placed: PlacedOrder)
        (caller: Caller)
        (reply: AsyncReplyChannel<Result<EventId, string>>)
        (subject: NodeId)
        =
        match context.persist.getState () with
        | Error err -> reply.Reply(Error err)
        | Ok state ->
            match isLoadSubject state.graph subject with
            | Error err -> reply.Reply(Error err)
            | Ok () ->
                match
                    CoreEventDispatch.appendLifecycle
                        (CoreMailboxEvents.eventDispatchContext context)
                        caller
                        placed.orderId.Value
                        (EventBody.Load subject)
                with
                | Error err -> reply.Reply(Error err)
                | Ok stored ->
                    match dispatchLoad context caller subject with
                    | Error err -> reply.Reply(Error err)
                    | Ok () -> reply.Reply(Ok stored.id)
