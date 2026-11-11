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

    let private classActorName (node: Node) =
        node.cssClasses
        |> CssClass.toList
        |> List.tryPick (fun cls ->
            if cls.StartsWith("actor-") && cls.Length > 6 then
                Some(cls.Substring(6))
            else
                None)

    let private actorNameFor (node: Node) =
        match CommandRequest.actorNameFromText node.text with
        | Some name -> name
        | None ->
            match classActorName node with
            | Some name -> name
            | None -> node.text.Trim().ToLowerInvariant()

    /// Bag start. `ActorFn` is not an `ActorFunction`, so the log keeps the stand-in.
    let private orderOfAffirmedStart
        (context: MailboxContext)
        (request: Gambol.Shared.ActorStart)
        : ActorOrder =
        let actorName =
            let nodes = (CoreMailboxEvents.graphNow context).nodes
            match Map.tryFind request.runDefinitionId nodes with
            | Some node -> actorNameFor node
            | None -> ""
        { actor = EventJson.stubActor
          actorNodeId = request.focusId
          actorName = actorName }

    let private finishReadyStart
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (secret: Credential)
        (make: Caller -> CoreChangesDoor)
        (submissionId: System.Guid)
        =
        let body =
            EventBody.Order(
                Order.StartActor(orderOfAffirmedStart context request))
        match
            CoreEventDispatch.appendLifecycle
                (CoreMailboxEvents.eventDispatchContext context)
                caller
                submissionId
                body
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

    /// Refusal leaves the log empty and returns the error to the caller.
    let private recordFailedStart (reply: PostedReply) message =
        reply.Reply(Error message)

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

    /// Admitted Run. `actorNodeId` is the focus id.
    let private queueAdmittedFocus
        (makeActor: NodeId -> NodeId -> Gambol.Shared.ActorFunction)
        (actorName: string)
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (reply: PostedReply)
        =
        let order =
            ActorOrderFromStart.orderFromStart
                makeActor
                actorName
                ActorOrderFromStart.focusIdOf
                request
        postBuiltOrder context caller reply order

    /// Admitted `?test` Run. The ack is the placed StartActor EventId.
    let private queueAdmittedTest =
        queueAdmittedFocus TestActor.functionActorAt "test"

    /// Admitted query Run. `actorNodeId` is the focus id.
    let private queueAdmittedQuery =
        queueAdmittedFocus SearchActor.functionActorAt "query"

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
            recordFailedStart reply (failedMessage error)
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

    let private functionStartFor (name: string) =
        match name with
        | "test" -> Some queueAdmittedTest
        | "ai" -> Some queueAdmittedAi
        | "query" -> Some queueAdmittedQuery
        | _ -> None

    /// Text name, then a query line, then an `actor-*` class.
    let private admittedQueue context caller request reply =
        let graph = CoreMailboxEvents.graphNow context
        let node =
            Map.tryFind request.runDefinitionId graph.nodes
        let fromText =
            node
            |> Option.bind (fun item ->
                CommandRequest.actorNameFromText item.text)
            |> Option.bind functionStartFor
        let chosen =
            match fromText with
            | Some start -> Some start
            | None when SearchActor.isQueryRequest graph request ->
                Some queueAdmittedQuery
            | None ->
                node
                |> Option.bind classActorName
                |> Option.bind functionStartFor
        match chosen with
        | Some start ->
            Some (fun () -> start context caller request reply)
        | None -> None

    let private postActorStart context caller event request reply =
        match CoreMailboxEvents.storedBySubmission context event.submissionId with
        | Some stored ->
            CoreMailboxEvents.replyStored reply stored
        | None ->
            match admittedQueue context caller request reply with
            | Some queue ->
                whenAdmitted context caller reply queue
            | None ->
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
