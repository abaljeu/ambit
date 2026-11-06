namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared

[<RequireQualifiedAccess>]
module internal CoreMailboxActors =

    type MailboxContext = CoreMailboxEvents.MailboxContext
    type PostedReply = CoreMailboxEvents.PostedReply

    let private prepareActorStart
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (start:
            Gambol.Shared.ActorStart ->
                (unit -> Graph) ->
                Result<Credential option, StartError>)
        =
        match CoreMailboxEvents.admitCaller context caller with
        | Error err -> Error (StartError.Rejected err)
        | Ok () ->
            match context.coreChanges.Value with
            | None ->
                Error (StartError.Rejected "mailbox not initialized")
            | Some make ->
                let getState () = CoreMailboxEvents.graphNow context
                match start request getState with
                | Error err -> Error err
                | Ok None -> Ok(None, make)
                | Ok (Some secret) ->
                    match context.pool.admit secret with
                    | Ok () -> Ok(Some secret, make)
                    | Error err ->
                        context.pool.drop secret
                        Error (StartError.Rejected err)

    let private finishReadyStart
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (secret: Credential)
        (make: Caller -> CoreChangesDoor)
        (submissionId: System.Guid)
        =
        match
            CoreEventDispatch.appendLifecycle
                (CoreMailboxEvents.eventDispatchContext context)
                caller
                submissionId
                (EventBody.ActorStart request)
        with
        | Error err ->
            context.pool.drop secret
            Error err
        | Ok stored ->
            context.pool.schedule secret (make caller)
            Ok stored

    let dispatchActorStartResult
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        start
        =
        match prepareActorStart context caller request start with
        | Error error ->
            reply.Reply(Error (StartError.text error))
        | Ok (None, _) -> reply.Reply(Ok ())
        | Ok (Some secret, make) ->
            match
                finishReadyStart
                    context caller request secret make (Guid.NewGuid())
            with
            | Error err -> reply.Reply(Error err)
            | Ok _ -> reply.Reply(Ok ())

    /// Query and named starts both use the ID-bag start.
    /// `startFunction` stays unused on this path.
    let private startChosen
        (context: MailboxContext)
        (request: Gambol.Shared.ActorStart)
        (_getState: unit -> Graph)
        : Result<Credential option, StartError> =
        context.pool.startActor request _getState
        |> Result.map Some

    let dispatchStartActor context caller request reply =
        dispatchActorStartResult
            context
            caller
            request
            reply
            (fun start getState ->
                context.pool.startActor start getState
                |> Result.map Some)

    let dispatchStartPeerActor
        context caller peerName request reply =
        dispatchActorStartResult
            context
            caller
            request
            reply
            (fun start getState ->
                context.pool.startPeerActor peerName start getState
                |> Result.mapError StartError.Rejected
                |> Result.map Some)
    let dispatchActorStop
        (context: MailboxContext)
        (caller: Caller)
        (result: ActorResult)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match caller.authority with
        | Authority "Actor" ->
            match context.pool.getFocusId caller.secret with
            | None -> reply.Reply(Ok ())
            | Some focusId ->
                match
                    CoreEventDispatch.actorStop
                        (CoreMailboxEvents.eventDispatchContext context)
                        caller
                        focusId
                        result
                with
                | Error err -> reply.Reply(Error err)
                | Ok () ->
                    reply.Reply(
                        context.pool.finish caller.secret result)
        | _ ->
            reply.Reply(Error CoreAuth.refuse)

    let private runCancel
        (context: MailboxContext)
        (caller: Caller)
        (focusId: NodeId)
        (submissionId: System.Guid)
        : Result<Ev option, string> =
        match context.pool.trySecretForFocus focusId with
        | None -> Ok None
        | Some secret ->
            match
                CoreEventDispatch.appendLifecycle
                    (CoreMailboxEvents.eventDispatchContext context)
                    caller
                    submissionId
                    (EventBody.ActorStop(focusId, ActorCancelled))
            with
            | Error err -> Error err
            | Ok stored ->
                match context.pool.finish secret ActorCancelled with
                | Error err -> Error err
                | Ok () -> Ok(Some stored)

    /// Every Cancel reply is the draft EventId.
    let replyCancel
        (context: MailboxContext)
        (placed: PlacedOrder)
        (caller: Caller)
        (reply: AsyncReplyChannel<Result<EventId, string>>)
        (focusId: NodeId)
        =
        match
            runCancel
                context
                caller
                focusId
                placed.orderId.Value
        with
        | Error err -> reply.Reply(Error err)
        | Ok _ -> reply.Reply(Ok EventId.zero)

    let private failedMessage error =
        match error with
        | StartError.UnknownActor _ -> "unknown actor"
        | StartError.Rejected message -> message

    let private recordFailedStart
        context caller (request: Gambol.Shared.ActorStart)
        submissionId message (reply: PostedReply) =
        let dispatch = CoreMailboxEvents.eventDispatchContext context
        let stop =
            EventBody.ActorStop(request.focusId, ActorFailed message)
        match
            CoreEventDispatch.appendLifecycle
                dispatch caller submissionId (EventBody.ActorStart request)
        with
        | Error err -> reply.Reply(Error err)
        | Ok started ->
            match
                CoreEventDispatch.appendLifecycle
                    dispatch caller (Guid.NewGuid()) stop
            with
            | Error err -> reply.Reply(Error err)
            | Ok stopped ->
                CoreMailboxEvents.replyStored reply started

    let private actorNameOn (context: MailboxContext) (nodeId: NodeId) =
        let graph = CoreMailboxEvents.graphNow context
        match Map.tryFind nodeId graph.nodes with
        | None -> None
        | Some node -> CommandRequest.actorNameFromText node.text

    let private replyPlaced
        (reply: PostedReply)
        (stored: Ev)
        (placed: Result<EventId, string>)
        =
        match placed with
        | Error err -> reply.Reply(Error err)
        | Ok _ -> CoreMailboxEvents.replyStored reply stored

    /// Admitted Run. Appends ActorStart, then posts ActorOrder.
    let private queueAdmittedOrder
        (context: MailboxContext)
        (caller: Caller)
        (event: Ev)
        (request: Gambol.Shared.ActorStart)
        (reply: PostedReply)
        (order: ActorOrder)
        =
        match context.coreChanges.Value with
        | None -> reply.Reply(Error "mailbox not initialized")
        | Some make ->
            let host = (make caller).host
            let dispatch =
                CoreMailboxEvents.eventDispatchContext context
            match
                CoreEventDispatch.appendLifecycle
                    dispatch
                    caller
                    event.submissionId
                    (EventBody.ActorStart request)
            with
            | Error err -> reply.Reply(Error err)
            | Ok stored ->
                Async.Start(async {
                    let! placed =
                        MailboxHost.postActorOrder host caller order
                    replyPlaced reply stored placed
                })

    /// Admitted `?test` Run. Closes over the actor node and queues ActorOrder.
    let private queueAdmittedTest
        (context: MailboxContext)
        (caller: Caller)
        (event: Ev)
        (request: Gambol.Shared.ActorStart)
        (reply: PostedReply)
        =
        let order =
            { actor =
                TestActor.functionActorAt
                    request.actorNodeId
                    request.focusId
              focusId = request.focusId
              actorName = "test" }
        queueAdmittedOrder context caller event request reply order

    let private replyOrder
        (reply: PostedReply)
        (placed: Result<EventId, string>)
        =
        match placed with
        | Error err -> reply.Reply(Error err)
        | Ok id -> reply.Reply(Ok id)

    /// `?ai` failure is ActorFailed. It does not store ActorStart.
    let private recordAiFailed
        (context: MailboxContext)
        (caller: Caller)
        (focusId: NodeId)
        (submissionId: System.Guid)
        (message: string)
        (reply: PostedReply)
        =
        let dispatch = CoreMailboxEvents.eventDispatchContext context
        let stop =
            EventBody.ActorStop(focusId, ActorFailed message)
        match
            CoreEventDispatch.appendLifecycle
                dispatch caller submissionId stop
        with
        | Error err -> reply.Reply(Error err)
        | Ok stored ->
            CoreMailboxEvents.replyStored reply stored

    let private commandTextIsAi (graph: Graph) (id: NodeId) =
        match Map.tryFind id graph.nodes with
        | Some node ->
            CommandRequest.actorNameFromText node.text = Some "ai"
        | None -> false

    let private aiCommandId (context: MailboxContext) (focusId: NodeId) =
        let graph = CoreMailboxEvents.graphNow context
        let rec walk id seen =
            if Set.contains id seen then None
            else
                if commandTextIsAi graph id then Some id
                else
                    match Map.tryFind id graph.ownerParentByChild with
                    | None -> None
                    | Some parent ->
                        walk parent (Set.add id seen)
        walk focusId Set.empty

    let private placeAiOrder
        (host: MailboxHost)
        (caller: Caller)
        (submissionId: System.Guid)
        (order: ActorOrder)
        =
        let placed =
            { orderId = OrderId submissionId
              authority = caller.authority
              commandName = Some order.actorName
              order = Order.ActorOrder order }
        MailboxHost.postMailboxAndAsyncReply host (fun channel ->
            Mailbox.placeForCaller placed caller channel)

    let private aiActor (context: MailboxContext) (focusId: NodeId) =
        match aiCommandId context focusId with
        | None -> Error "ai command not found"
        | Some nodeId ->
            match RunAgentActor.actorForNode context.pool nodeId with
            | Some actor -> Ok (nodeId, actor)
            | None -> Error "ai actor is not bound"

    /// Admitted `?ai` order. The function closes over the command node.
    let private queueAiOrder
        (context: MailboxContext)
        (caller: Caller)
        (event: Ev)
        (order: ActorOrder)
        (reply: PostedReply)
        =
        match CoreMailboxEvents.admitCaller context caller with
        | Error err ->
            recordAiFailed
                context caller order.focusId
                event.submissionId err reply
        | Ok () ->
            match context.coreChanges.Value with
            | None -> reply.Reply(Error "mailbox not initialized")
            | Some make ->
                match aiActor context order.focusId with
                | Error message ->
                    recordAiFailed
                        context caller order.focusId
                        event.submissionId message reply
                | Ok (_, actor) ->
                    let host = (make caller).host
                    let real = { order with actor = actor }
                    Async.Start(async {
                        let! placed =
                            placeAiOrder
                                host caller event.submissionId real
                        replyOrder reply placed
                    })

    /// Same `admitCaller` gate as other command starts, then queue ActorOrder.
    let private queueTestOrder
        (context: MailboxContext)
        (caller: Caller)
        (event: Ev)
        (request: Gambol.Shared.ActorStart)
        (reply: PostedReply)
        =
        match CoreMailboxEvents.admitCaller context caller with
        | Error err ->
            recordFailedStart
                context
                caller
                request
                event.submissionId
                err
                reply
        | Ok () ->
            queueAdmittedTest context caller event request reply

    let private postChosenStart
        context caller event request reply =
        let start req getState =
            startChosen context req getState
        match prepareActorStart context caller request start with
        | Error error ->
            recordFailedStart
                context caller request event.submissionId
                (failedMessage error) reply
        | Ok (None, _) -> CoreMailboxEvents.replyStored reply event
        | Ok (Some secret, make) ->
            match
                finishReadyStart
                    context caller request secret make
                    event.submissionId
            with
            | Error err -> reply.Reply(Error err)
            | Ok stored ->
                CoreMailboxEvents.replyStored reply stored

    let private postActorStart context caller event request reply =
        match CoreMailboxEvents.storedBySubmission context event.submissionId with
        | Some stored ->
            CoreMailboxEvents.replyStored reply stored
        | None ->
            match actorNameOn context request.actorNodeId with
            | Some "ai" ->
                recordAiFailed
                    context
                    caller
                    request.focusId
                    event.submissionId
                    "ai does not use ActorStart"
                    reply
            | Some "test" ->
                queueTestOrder context caller event request reply
            | _ ->
                postChosenStart context caller event request reply

    let private postCancel context caller event focusId reply =
        match CoreMailboxEvents.storedBySubmission context event.submissionId with
        | Some stored -> CoreMailboxEvents.replyStored reply stored
        | None ->
            match
                runCancel
                    context caller focusId event.submissionId
            with
            | Error err -> reply.Reply(Error err)
            | Ok (Some stored) ->
                CoreMailboxEvents.replyStored reply stored
            | Ok None -> CoreMailboxEvents.replyStored reply event

    let dispatchPostEvent
        (context: MailboxContext)
        (caller: Caller)
        (event: Ev)
        (reply: PostedReply)
        =
        match event.body with
        | EventBody.ActorStop _ ->
            reply.Reply(Error "ActorStop is not a client event type")
        | EventBody.Order (Order.ActorOrder order)
            when order.actorName = "ai" ->
            queueAiOrder context caller event order reply
        | EventBody.ActorStart request ->
            postActorStart context caller event request reply
        | EventBody.Cancel focusId ->
            postCancel context caller event focusId reply
        | _ ->
            match
                CoreEventDispatch.postEvent
                    (CoreMailboxEvents.eventDispatchContext context)
                    caller
                    event
            with
            | Error err -> reply.Reply(Error err)
            | Ok (stored, _) ->
                CoreMailboxEvents.replyStored reply stored
