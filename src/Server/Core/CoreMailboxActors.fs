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

    let private actorNameOn (context: MailboxContext) (nodeId: NodeId) =
        let graph = CoreMailboxEvents.graphNow context
        match Map.tryFind nodeId graph.nodes with
        | None -> None
        | Some node -> CommandRequest.actorNameFromText node.text

    /// The stored StartActor uses the posted submission id.
    /// A second post of that submission returns the stored event.
    let private postBuiltOrder
        (context: MailboxContext)
        (caller: Caller)
        (reply: PostedReply)
        (submissionId: System.Guid)
        (order: ActorOrder)
        =
        match context.coreChanges.Value with
        | None -> reply.Reply(Error "mailbox not initialized")
        | Some make ->
            let host = (make caller).host
            let commandName =
                match order.actorName with
                | "" -> None
                | name -> Some name
            let placed =
                { orderId = OrderId submissionId
                  authority = caller.authority
                  commandName = commandName
                  order = Order.StartActor order }
            Async.Start(async {
                let! result =
                    MailboxHost.postPlacedOrder host caller placed
                reply.Reply(result)
            })

    /// Admitted Run. `actorNodeId` is the focus id.
    let private queueAdmittedFocus
        (makeActor: NodeId -> NodeId -> Gambol.Shared.ActorFunction)
        (actorName: string)
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (reply: PostedReply)
        (submissionId: System.Guid)
        =
        let order =
            ActorOrderFromStart.orderFromStart
                makeActor
                actorName
                ActorOrderFromStart.focusIdOf
                request
        postBuiltOrder context caller reply submissionId order

    /// Admitted `?test` Run. The ack is the placed StartActor EventId.
    let private queueAdmittedTest =
        queueAdmittedFocus TestActor.functionActorAt "test"

    /// Admitted query Run. `actorNodeId` is the focus id.
    let private queueAdmittedQuery =
        queueAdmittedFocus SearchActor.functionActorAt "query"

    /// Admitted `?ai` Run. `actorNodeId` is the focus id.
    let private queueAdmittedAi
        (context: MailboxContext)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        (reply: PostedReply)
        (submissionId: System.Guid)
        =
        match context.pool.bootActors with
        | None -> reply.Reply(Error "ai actor is not installed")
        | Some install ->
            let order =
                RunAgentActor.aiActorOrder
                    install
                    context.pool.sessionIdFor
                    request
            postBuiltOrder context caller reply submissionId order

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

    let private admittedQueue
        context caller request reply submissionId =
        match actorNameOn context request.runDefinitionId with
        | Some "test" ->
            Some (fun () ->
                queueAdmittedTest
                    context caller request reply submissionId)
        | Some "ai" ->
            Some (fun () ->
                queueAdmittedAi
                    context caller request reply submissionId)
        | _ when
            SearchActor.isQueryRequest
                (CoreMailboxEvents.graphNow context)
                request ->
            Some (fun () ->
                queueAdmittedQuery
                    context caller request reply submissionId)
        | _ -> None

    /// Repeated submission replies the stored event.
    /// `?test`, `?ai`, and a query are placed as Order.StartActor.
    let replyClientRequest
        (context: MailboxContext)
        (placed: PlacedOrder)
        (caller: Caller)
        (reply: PostedReply)
        (request: Gambol.Shared.ActorStart)
        =
        let submissionId = placed.orderId.Value
        match
            CoreMailboxEvents.storedBySubmission context submissionId
        with
        | Some stored ->
            CoreMailboxEvents.replyStored reply stored
        | None ->
            match
                admittedQueue
                    context caller request reply submissionId
            with
            | Some queue ->
                whenAdmitted context caller reply queue
            | None -> reply.Reply(Error "unknown actor")

    /// Stored submission, else the stored stop, else the draft id.
    let replyPlacedCancel
        (context: MailboxContext)
        (placed: PlacedOrder)
        (caller: Caller)
        (reply: PostedReply)
        (focusId: NodeId)
        =
        let submissionId = placed.orderId.Value
        match
            CoreMailboxEvents.storedBySubmission context submissionId
        with
        | Some stored ->
            CoreMailboxEvents.replyStored reply stored
        | None ->
            match runCancel context caller focusId submissionId with
            | Error err -> reply.Reply(Error err)
            | Ok (Some stored) ->
                CoreMailboxEvents.replyStored reply stored
            | Ok None -> reply.Reply(Ok EventId.zero)
