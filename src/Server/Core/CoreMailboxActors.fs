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

    let private appendIdleStart
        (context: MailboxContext)
        (caller: Caller)
        (orderId: System.Guid)
        (request: Gambol.Shared.ActorStart)
        =
        let dispatch = CoreMailboxEvents.eventDispatchContext context
        match
            CoreEventDispatch.appendLifecycle
                dispatch
                caller
                orderId
                (EventBody.ActorStart request)
        with
        | Error err -> Error err
        | Ok started ->
            match
                CoreEventDispatch.appendLifecycle
                    dispatch
                    caller
                    (Guid.NewGuid())
                    (EventBody.ActorStop(
                        request.focusId,
                        ActorSucceeded))
            with
            | Error err -> Error err
            | Ok _ -> Ok started.id

    let placedActorStart
        (context: MailboxContext)
        (caller: Caller)
        (orderId: System.Guid)
        (request: Gambol.Shared.ActorStart)
        start
        =
        match CoreMailboxEvents.storedBySubmission context orderId with
        | Some stored -> Ok stored.id
        | None ->
            match prepareActorStart context caller request start with
            | Error error -> Error (StartError.text error)
            | Ok (None, _) ->
                appendIdleStart context caller orderId request
            | Ok (Some secret, make) ->
                finishReadyStart
                    context caller request secret make orderId
                |> Result.map (fun stored -> stored.id)

    let placedSearchStart
        (context: MailboxContext)
        (caller: Caller)
        (orderId: System.Guid)
        (request: Gambol.Shared.ActorStart)
        =
        match CoreMailboxEvents.storedBySubmission context orderId with
        | Some stored -> Ok stored.id
        | None ->
            CoreEventDispatch.appendLifecycle
                (CoreMailboxEvents.eventDispatchContext context)
                caller
                orderId
                (EventBody.ActorStart request)
            |> Result.map (fun stored -> stored.id)

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

    let placedActorStop
        (context: MailboxContext)
        (caller: Caller)
        (orderId: System.Guid)
        (callerId: string)
        (result: ActorResult)
        =
        match CoreMailboxEvents.storedBySubmission context orderId with
        | Some stored -> Ok stored.id
        | None ->
            match context.pool.getFocusId (Credential callerId) with
            | None -> Error "no running actor"
            | Some focusId ->
                match
                    CoreEventDispatch.appendLifecycle
                        (CoreMailboxEvents.eventDispatchContext context)
                        caller
                        orderId
                        (EventBody.ActorStop(focusId, result))
                with
                | Error err -> Error err
                | Ok stored ->
                    match context.pool.finish (Credential callerId) result with
                    | Error err -> Error err
                    | Ok () -> Ok stored.id

    let placedCancel
        (context: MailboxContext)
        (caller: Caller)
        (orderId: System.Guid)
        (focusId: NodeId)
        =
        match context.pool.trySecretForFocus focusId with
        | None -> Error "no running actor"
        | Some secret ->
            let notice =
                Notice.ActorStopped(focusId, ActorCancelled)
            match
                CoreEventDispatch.logNotice
                    (CoreMailboxEvents.eventDispatchContext context)
                    notice
            with
            | Error err -> Error err
            | Ok stored ->
                match context.pool.finish secret ActorCancelled with
                | Error err -> Error err
                | Ok () -> Ok stored.id

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
                CoreMailboxEvents.replyList
                    reply context stopped [ started; stopped ]

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
            let start req getState =
                startChosen context req getState
            match
                placedActorStart
                    context caller event.submissionId request start
            with
            | Error message ->
                let stored =
                    if message.Contains "not registered" then
                        "unknown actor"
                    else
                        message
                recordFailedStart
                    context
                    caller
                    request
                    event.submissionId
                    stored
                    reply
            | Ok eventId ->
                let stored =
                    context.eventLog.Value.events
                    |> List.find (fun item -> item.id = eventId)
                CoreMailboxEvents.replyList
                    reply context stored [ stored ]
        | EventBody.Cancel focusId ->
            match
                placedCancel
                    context caller event.submissionId focusId
            with
            | Error err -> reply.Reply(Error err)
            | Ok eventId when eventId = EventId.zero ->
                CoreMailboxEvents.replyList reply context event []
            | Ok eventId ->
                let stored =
                    context.eventLog.Value.events
                    |> List.find (fun item -> item.id = eventId)
                CoreMailboxEvents.replyList
                    reply context stored [ stored ]
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
