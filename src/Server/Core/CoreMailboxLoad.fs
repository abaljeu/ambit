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

    /// Mailbox Load door. Desk Load calls this in the same turn.
    let private dispatchLoad
        (context: MailboxContext)
        (caller: Caller)
        (subject: NodeId)
        : Result<unit, string> =
        match context.persist.graphAtEvent () with
        | Error err -> Error err
        | Ok state ->
            match isLoadSubject state.graph subject with
            | Error err -> Error err
            | Ok () -> context.parsePush subject

    let internal dispatchStartLoadSaveCommand
        (context: MailboxContext)
        (caller: Caller)
        (path: LoadSavePath)
        (peerName: PeerActorName)
        (request: LoadSaveCommandRequest)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        =
        let started =
            CoreMailboxActors.dispatchActorStartResult
                context
                caller
                request.start
                (fun _ getGraph ->
                    DeskLoadPush.runLoadSave
                        { pool = context.pool
                          getGraph = getGraph
                          peerName = peerName }
                        (dispatchLoad context caller)
                        request
                        path
                    |> Result.mapError StartError.Rejected)
        reply.Reply started

    let internal replyLoad
        (context: MailboxContext)
        (placed: PlacedOrder)
        (caller: Caller)
        (reply: AsyncReplyChannel<Result<EventId, string>>)
        (subject: NodeId)
        =
        match dispatchLoad context caller subject with
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
            | Ok stored -> reply.Reply(Ok stored.id)
