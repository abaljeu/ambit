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
        start
        : Result<unit, string> =
        match prepareActorStart context caller request start with
        | Error error ->
            Error (StartError.text error)
        | Ok (None, _) -> Ok ()
        | Ok (Some secret, make) ->
            match
                finishReadyStart
                    context caller request secret make (Guid.NewGuid())
            with
            | Error err -> Error err
            | Ok _ -> Ok ()

    /// A named start uses the ID-bag start.
    /// A query Run uses `queueAdmittedQuery`.
    /// `startFunction` stays unused on this path.
    let private startChosen
        (context: MailboxContext)
        (request: Gambol.Shared.ActorStart)
        (_getState: unit -> Graph)
        : Result<Credential option, StartError> =
        context.pool.startActor request _getState
        |> Result.map Some

    let dispatchStartActor
        context
        caller
        request
        (reply: Result<unit, string> -> unit)
        =
        reply (
            dispatchActorStartResult
                context
                caller
                request
                (fun start getState ->
                    context.pool.startActor start getState
                    |> Result.map Some))

    let dispatchActorStop
        (context: MailboxContext)
        (caller: Caller)
        (result: ActorResult)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match caller.authority with
        | Authority "Actor" ->
            match context.pool.getActorNodeId caller.secret with
            | None -> reply.Reply(Ok ())
            | Some actorNodeId ->
                match
                    CoreEventDispatch.actorStop
                        (CoreMailboxEvents.eventDispatchContext context)
                        caller
                        actorNodeId
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
        match context.pool.trySecretForActorNode focusId with
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

    let private postBuiltOrder
        (context: MailboxContext)
        (caller: Caller)
        (reply: PostedReply)
        (order: ActorOrder)
        =
        match context.coreChanges.Value with
        | None -> reply.Reply(Error "mailbox not initialized")
        | Some make ->
            let host = (make caller).host
            Async.Start(async {
                let! placed =
                    MailboxHost.postActorOrder host caller order
                reply.Reply(placed)
            })

    /// Admitted `?test` Run. The ack is the placed StartActor EventId.
    let private queueAdmittedTest
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (reply: PostedReply)
        =
        let order =
            ActorOrderFromStart.orderFromStart
                TestActor.functionActorAt
                "test"
                (fun start -> start.focusId)
                request
        postBuiltOrder context caller reply order

    /// Admitted query Run. `actorNodeId` is the focus id.
    let private queueAdmittedQuery
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (reply: PostedReply)
        =
        let order =
            ActorOrderFromStart.orderFromStart
                SearchActor.functionActorAt
                "query"
                (fun start -> start.focusId)
                request
        postBuiltOrder context caller reply order

    /// Admitted `?ai` Run. `actorNodeId` is the focus id `cancelByFocus` uses.
    let private queueAdmittedAi
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (reply: PostedReply)
        =
        match context.pool.bootActors with
        | None -> reply.Reply(Error "ai actor is not installed")
        | Some install ->
            let order =
                RunAgentActor.aiActorOrder
                    install
                    context.pool.sessionIdFor
                    request
            postBuiltOrder context caller reply order

    /// Admitted caller queues an ActorOrder. A refusal stores no lifecycle row.
    let private whenAdmitted
        (context: MailboxContext)
        (caller: Caller)
        (reply: PostedReply)
        (queue: unit -> unit)
        =
        match CoreMailboxEvents.admitCaller context caller with
        | Error err -> reply.Reply(Error err)
        | Ok () -> queue ()

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
            match actorNameOn context request.runDefinitionId with
            | Some "test" ->
                whenAdmitted context caller reply (fun () ->
                    queueAdmittedTest context caller request reply)
            | Some "ai" ->
                whenAdmitted context caller reply (fun () ->
                    queueAdmittedAi context caller request reply)
            | _ when
                SearchActor.isQueryRequest
                    (CoreMailboxEvents.graphNow context)
                    request ->
                whenAdmitted context caller reply (fun () ->
                    queueAdmittedQuery context caller request reply)
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
