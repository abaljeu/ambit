namespace Gambol.Server

open Gambol.Shared

/// Mailbox reply functions. Compiled before CoreDoor so actors can use the door.
[<RequireQualifiedAccess>]
module CoreMailboxApi =

    let private unwrap result =
        match result with
        | Ok value -> value
        | Error error -> failwith error

    /// Private add: Notice joins that same queue. No public door.
    let internal postNotice (host: MailboxHost) (msg: Notice) =
        MailboxHost.postItem host (CoreQueue.Notice msg)

    let private ask host build =
        MailboxHost.postMailboxAndAsyncReply host (fun channel ->
            Mailbox.Ask (build channel))

    let tryGraphAtEvent
        (host: MailboxHost)
        : Async<Result<GraphAtEvent, string>> =
        ask host Query.GraphAtEvent

    let graphAtEvent
        (host: MailboxHost)
        : Async<Result<GraphAtEvent, string>> =
        tryGraphAtEvent host

    let eventsSince
        (host: MailboxHost)
        (after: Gambol.Shared.EventId)
        : Async<Gambol.Shared.EventLog> =
        async {
            let! result =
                ask host (fun channel ->
                    Query.EventsSince(after, channel))
            return unwrap result
        }

    let eventHistory
        (host: MailboxHost)
        : Async<Gambol.Shared.EventLog> =
        eventsSince host EventId.zero

    let getEventId (host: MailboxHost) : Async<Gambol.Shared.EventId> =
        async {
            let! result = ask host Query.LastEventId
            return unwrap result
        }

    let getEventsSince
        (host: MailboxHost)
        (after: Gambol.Shared.EventId)
        : Async<Ev list> =
        async {
            let! log = eventsSince host after
            return log.events
        }

    let private sendsAsOrder (event: Ev) =
        match event.body with
        | EventBody.Undo _
        | EventBody.Redo _
        | EventBody.ActorStart _
        | EventBody.Cancel _ -> true
        | EventBody.Change _ -> event.id = EventId.zero
        | _ -> false

    /// Client events that stay off PlaceOrder.
    /// ActorStop is refused before the zero-id check.
    let private refusedClientEvent (event: Ev) =
        match event.body with
        | EventBody.ActorStop _ ->
            Error "ActorStop is not a client event type"
        | _ when event.id <> EventId.zero ->
            Error "posted EventId must be zero"
        | EventBody.Notice _ ->
            Error "Notice is not a client event type"
        | EventBody.Load _ ->
            Error "Load Events are mailbox-generated"
        | _ ->
            Error "Order Events are mailbox-generated"

    let private placedForPosted (event: Ev) =
        if sendsAsOrder event then
            MailboxLog.placedOrderFromEvent event
        else
            refusedClientEvent event

    let private messageForPosted (caller: Caller) (event: Ev) =
        placedForPosted event
        |> Result.map (fun placed channel ->
            Mailbox.placeForCaller placed caller channel)

    let private acceptOne
        (host: MailboxHost)
        (caller: Caller)
        (event: Ev)
        =
        match placedForPosted event with
        | Error err -> async.Return(Error err)
        | Ok placed ->
            MailboxHost.postPlacedOrder host caller placed

    let private isClientActorStop (event: Ev) =
        match event.body with
        | EventBody.ActorStop _ -> true
        | _ -> false

    let private firstError results =
        results
        |> List.tryPick (function
            | Error err -> Some err
            | Ok _ -> None)

    let private idsOf results =
        results
        |> List.choose (function
            | Ok eventId -> Some eventId
            | Error _ -> None)

    let private afterId ids =
        ids
        |> List.map EventId.value
        |> List.min
        |> fun n -> EventId.fromJson (n - 1)

    let private expandStored log (stored: Ev) =
        match stored.body with
        | EventBody.ActorStart request ->
            match EventLog.tryFind (EventId.next stored.id) log with
            | Some stopped ->
                match stopped.body with
                | EventBody.ActorStop(id, _) when id = request.focusId ->
                    [ stored; stopped ]
                | _ -> [ stored ]
            | None -> [ stored ]
        | _ -> [ stored ]

    let private storedEvents log ids =
        let rec take acc remaining =
            match remaining with
            | [] -> Ok(List.rev acc)
            | id :: rest when not (EventId.isAccepted id) ->
                take acc rest
            | id :: rest ->
                match EventLog.tryFind id log with
                | None ->
                    Error
                        $"stored Event {EventId.display id} not found"
                | Some event ->
                    let acc =
                        expandStored log event
                        |> List.fold
                            (fun collected item -> item :: collected)
                            acc
                    take acc rest
        take [] ids

    let private affirmedStart events =
        match events with
        | [ stored ] -> Ok stored
        | [ stored; stopped ] ->
            match stored.body, stopped.body with
            | EventBody.ActorStart request,
              EventBody.ActorStop(id, _)
                when id = request.focusId ->
                Ok stored
            | _ -> Error "stored Event not found"
        | _ -> Error "stored Event not found"

    /// A duplicate submissionId replies the stored id. The catch-up cursor
    /// stays at the log tip when a later Change is already stored, so the
    /// ack list does not gain that Change.
    let private cursorAfterDuplicateChange
        (log: EventLog)
        (accepted: CoreChangesAccepted)
        =
        let laterChange =
            log.events
            |> List.exists (fun event ->
                event.id > accepted.eventId
                && match event.body with
                   | EventBody.Change _ -> true
                   | _ -> false)
        if EventId.isAccepted accepted.eventId && laterChange then
            let tip =
                log.events
                |> List.fold
                    (fun tip event -> EventId.max tip event.id)
                    accepted.eventId
            { accepted with eventId = tip }
        else
            accepted

    let private acceptedFromIds host posted ids =
        async {
            let! log = eventsSince host (afterId ids)
            match storedEvents log ids with
            | Error err -> return Error err
            | Ok stored ->
                return
                    Ok(
                        CoreChanges.fromPosted
                            (MailboxHost.isReady host ())
                            posted
                            stored
                        |> cursorAfterDuplicateChange log)
        }

    let private mergeIdReplies host posted results =
        match firstError results with
        | Some err -> async.Return(Error err)
        | None -> acceptedFromIds host posted (idsOf results)

    let postEvent
        (host: MailboxHost)
        (caller: Caller)
        (event: Ev)
        : Async<Result<Ev, string>> =
        async {
            let! result = acceptOne host caller event
            match result with
            | Error err -> return Error err
            | Ok eventId ->
                let! log = eventsSince host (afterId [ eventId ])
                match storedEvents log [ eventId ] with
                | Error err -> return Error err
                | Ok events -> return affirmedStart events
        }

    let private clientBatchError (events: Ev list) =
        if List.isEmpty events then
            Some "events must not be empty"
        elif List.exists isClientActorStop events then
            Some "ActorStop is not a client event type"
        else
            None

    let private zeroChange (event: Ev) =
        match event.body with
        | EventBody.Order (Order.Change (ops, origin))
            when event.id = EventId.zero ->
            Some(ops, origin)
        | _ -> None

    let private placedParse (event: Ev) (ops: Op list) =
        MailboxLog.placedOrderFromEvent event
        |> Result.map (fun placed ->
            { placed with
                order = Order.Change(ops, ChangeOrigin.Parse) })

    let private parseBuild (caller: Caller) (event: Ev) =
        if event.id <> EventId.zero then
            Error "posted EventId must be zero"
        else
            match zeroChange event with
            | None -> Error "unsupported"
            | Some(ops, _) ->
                match placedParse event ops with
                | Error err -> Error err
                | Ok placed ->
                    Ok(fun channel ->
                        Mailbox.placeForCaller placed caller channel)

    let private editBuild (caller: Caller) (event: Ev) =
        match zeroChange event with
        | Some(_, ChangeOrigin.Parse) -> Error "unsupported"
        | _ -> messageForPosted caller event

    let private oneBuild caller origin event =
        match origin with
        | ChangeOrigin.Parse -> parseBuild caller event
        | ChangeOrigin.Edit -> editBuild caller event

    let private zipReplies prepared sent =
        let rec take acc prepared sent =
            match prepared, sent with
            | [], _ -> List.rev acc
            | Error err :: rest, _ ->
                take (Error err :: acc) rest sent
            | Ok _ :: rest, reply :: more ->
                take (reply :: acc) rest more
            | Ok _ :: _, [] ->
                List.rev (Error "short reply" :: acc)
        take [] prepared sent

    let private postPrepared host events prepared =
        let sends =
            prepared
            |> List.choose (function
                | Ok build -> Some build
                | Error _ -> None)
        async {
            let! sent =
                if List.isEmpty sends then
                    async.Return []
                else
                    MailboxHost.postForReplies host sends
            return! mergeIdReplies host events (zipReplies prepared sent)
        }

    /// One mailbox Change post. Origin Parse skips the file write.
    let postMailboxChange
        (host: MailboxHost)
        (caller: Caller)
        (origin: ChangeOrigin)
        (events: Ev list)
        : Async<Result<CoreChangesAccepted, string>> =
        match origin, clientBatchError events with
        | ChangeOrigin.Edit, Some err -> async { return Error err }
        | _ ->
            events
            |> List.map (oneBuild caller origin)
            |> postPrepared host events

    /// Push the list back to back, then rebuild the ack from the log.
    /// The push runs before this function returns, so the next call cannot pass it.
    let postEvents
        (host: MailboxHost)
        (caller: Caller)
        (events: Ev list)
        : Async<Result<CoreChangesAccepted, string>> =
        postMailboxChange host caller ChangeOrigin.Edit events

    /// Graph-only Ev: same flow as postEvents, skips file persist only.
    let postGraphOnly
        (host: MailboxHost)
        (caller: Caller)
        (event: Ev)
        : Async<Result<CoreChangesAccepted, string>> =
        postMailboxChange host caller ChangeOrigin.Parse [ event ]

    /// Place Order.StartActor from a supplied function. Reply is that EventId.
    let coreMailboxStartActorFromFunction
        (host: MailboxHost)
        (caller: Caller)
        (placement: ActorOrderFromStart.FunctionActorPlacement)
        : Async<Result<EventId, string>> =
        ActorOrderFromStart.orderFromPlacement placement
        |> MailboxHost.postActorOrder host caller

    /// Place Find and Move as Order.StartActor. The actor is supplied at boot.
    let recordSearchStart
        (host: MailboxHost)
        (caller: Caller)
        (actor: ActorFunction)
        (request: Gambol.Shared.ActorStart)
        : Async<Result<unit, string>> =
        let placement : ActorOrderFromStart.FunctionActorPlacement =
            { request = request
              actor = actor
              actorName = "search"
              actorNodeIdOf = ActorOrderFromStart.focusIdOf }
        async {
            let! placed =
                coreMailboxStartActorFromFunction
                    host
                    caller
                    placement
            return Result.map ignore placed
        }

    /// Mailbox Load of a Workspace, Directory, or File: push onto the Parse stack.
    let mbLoadNode
        (host: MailboxHost)
        (caller: Caller)
        (subject: NodeId)
        : Async<Result<EventId, string>> =
        MailboxHost.postPlacedOrder host caller {
            orderId = OrderId.New()
            authority = caller.authority
            commandName = None
            order = Order.Load subject }

    let login
        (host: MailboxHost)
        (name: string)
        (secret: Credential)
        : Async<Result<unit, string>> =
        let caller =
            { authority = Authority "Browser"
              name = name
              secret = secret }
        async.Return(Session.login (MailboxHost.session host) caller)

    /// Stop admitting and queue CoreQueue.DropAuthority. The reply is that post.
    let logout (host: MailboxHost) (caller: Caller) : Async<unit> =
        let session = MailboxHost.session host
        match Session.stopAdmitting session caller with
        | None -> ()
        | Some closed ->
            MailboxHost.postItem host (CoreQueue.DropAuthority closed)
        async.Return ()

    let isAdmitted
        (host: MailboxHost)
        (caller: Caller)
        : Async<bool> =
        async.Return(
            Session.admissionIsOpen (MailboxHost.session host) caller)

    let isReady (host: MailboxHost) = MailboxHost.isReady host ()

    let dispose (host: MailboxHost) = MailboxHost.dispose host
