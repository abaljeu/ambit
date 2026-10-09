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
                | Ok secret -> Ok(secret, make)

    let private finishReadyStart
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (secret: Credential)
        (make: Caller -> CoreChanges)
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

    /// Query reads post-edit State. `graphIds` do not replace that Graph.
    let private startChosen
        (context: MailboxContext)
        (request: Gambol.Shared.ActorStart)
        (getState: unit -> Graph)
        : Result<Credential option, StartError> =
        if SearchActor.isQueryRequest (getState ()) request then
            SearchActor.functionStart request getState
            |> context.pool.startFunction
            |> Result.mapError StartError.Rejected
            |> Result.map Some
        else
            context.pool.startActor request getState
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
        match CoreMailboxEvents.admitCaller context caller with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
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
        match CoreMailboxEvents.admitCaller context caller with
        | Error err -> Error err
        | Ok () ->
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

    let dispatchCancelActor
        (context: MailboxContext)
        (caller: Caller)
        (focusId: NodeId)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        =
        match runCancel context caller focusId (Guid.NewGuid()) with
        | Error err -> reply.Reply(Error err)
        | Ok _ -> reply.Reply(Ok ())

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
                CoreMailboxEvents.replyList reply context stopped [ started; stopped ]

    let private postActorStart context caller event request reply =
        match CoreMailboxEvents.storedBySubmission context event.submissionId with
        | Some stored ->
            let events = CoreMailboxEvents.eventsForStored context stored
            CoreMailboxEvents.replyList reply context (List.last events) events
        | None ->
            let start req getState =
                startChosen context req getState
            match prepareActorStart context caller request start with
            | Error error ->
                recordFailedStart
                    context caller request event.submissionId
                    (failedMessage error) reply
            | Ok (None, _) -> CoreMailboxEvents.replyList reply context event []
            | Ok (Some secret, make) ->
                match
                    finishReadyStart
                        context caller request secret make
                        event.submissionId
                with
                | Error err -> reply.Reply(Error err)
                | Ok stored ->
                    CoreMailboxEvents.replyList reply context stored [ stored ]

    let private postCancel context caller event focusId reply =
        match CoreMailboxEvents.storedBySubmission context event.submissionId with
        | Some stored -> CoreMailboxEvents.replyList reply context stored [ stored ]
        | None ->
            match
                runCancel
                    context caller focusId event.submissionId
            with
            | Error err -> reply.Reply(Error err)
            | Ok (Some stored) ->
                CoreMailboxEvents.replyList reply context stored [ stored ]
            | Ok None -> CoreMailboxEvents.replyList reply context event []

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
                    false
            with
            | Ok (stored, None) ->
                CoreMailboxEvents.replyList reply context stored [ stored ]
            | other -> reply.Reply other

    let dispatchRecordSearchStart
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match CoreMailboxEvents.admitCaller context caller with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            CoreEventDispatch.actorStart
                (CoreMailboxEvents.eventDispatchContext context)
                caller
                request
            |> reply.Reply

    /// The queue puts ActorStop on the event source. The id is the root.
    let dispatchRecordSearchStop
        (context: MailboxContext)
        (caller: Caller)
        (rootId: NodeId)
        (reply: AsyncReplyChannel<Result<unit, string>>)
        : unit =
        match CoreMailboxEvents.admitCaller context caller with
        | Error err -> reply.Reply(Error err)
        | Ok () ->
            CoreEventDispatch.actorStop
                (CoreMailboxEvents.eventDispatchContext context)
                caller
                rootId
                ActorSucceeded
            |> reply.Reply
