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

    let private logDeskOrder
        (context: MailboxContext)
        (caller: Caller)
        (request: LoadSaveCommandRequest)
        =
        let order =
            { actor = EventJson.stubActor
              getGraph = fun () -> Graph.create ()
              focusId = request.start.focusId
              actorName = "" }
        CoreEventDispatch.appendLifecycle
            (CoreMailboxEvents.eventDispatchContext context)
            caller
            (System.Guid.NewGuid())
            (EventBody.Order(Order.ActorOrder order))
        |> Result.map ignore

    let internal dispatchStartLoadSaveCommand
        (context: MailboxContext)
        (caller: Caller)
        (path: LoadSavePath)
        (peerName: PeerActorName)
        (request: LoadSaveCommandRequest)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        =
        let logged =
            if path = LoadSavePath.Desk then
                logDeskOrder context caller request
            else
                Ok ()
        match logged with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            CoreMailboxActors.dispatchActorStartResult
                context
                caller
                request.start
                reply
                (fun _ getGraph ->
                    DeskLoadPush.runLoadSave
                        { pool = context.pool
                          getGraph = getGraph
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
