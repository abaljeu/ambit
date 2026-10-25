namespace Gambol.Server

open Gambol.Shared

/// Mailbox reply functions. Compiled before CoreDoor so actors can use the door.
[<RequireQualifiedAccess>]
module CoreMailboxApi =

    let private unwrap result =
        match result with
        | Ok value -> value
        | Error error -> failwith error

    /// Public add: today's CoreMsg joins the queue as Mailbox.CoreMsg.
    let internal addCoreMsg (host: MailboxHost) (msg: CoreMsg) =
        MailboxHost.postItem host (MailboxHost.coreMsgItem msg)

    /// Private add: Notice joins that same queue. No public door.
    let internal postNotice (host: MailboxHost) (msg: Notice) =
        MailboxHost.postItem host (CoreQueue.Notice msg)

    let private reply host build =
        MailboxHost.postAndAsyncReply host build

    let private ask host build =
        MailboxHost.postMailboxAndAsyncReply host (fun channel ->
            Mailbox.Ask (build channel))

    let tryGetState
        (host: MailboxHost)
        : Async<Result<State, string>> =
        ask host Query.State

    let getState
        (host: MailboxHost)
        : Async<Result<State, string>> =
        tryGetState host

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

    let private postEventAccepted
        (host: MailboxHost)
        (caller: Caller)
        (event: Ev)
        =
        reply host (fun channel -> PostEvent(caller, event, channel))

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
            let! result = postEventAccepted host caller event
            match result with
            | Error err -> return Error err
            | Ok eventId ->
                let! log = eventsSince host (afterId [ eventId ])
                match storedEvents log [ eventId ] with
                | Error err -> return Error err
                | Ok [ stored ] -> return Ok stored
                | Ok _ -> return Error "stored Event not found"
        }

    let private postedMailbox
        (caller: Caller)
        (event: Ev)
        (channel: AsyncReplyChannel<Result<EventId, string>>)
        =
        match event.body with
        | EventBody.Change _ when event.id = EventId.zero ->
            match MailboxLog.ofEv event with
            | Ok { body = MailboxLog.MailboxEventBody.Order placed } ->
                Mailbox.placeForCaller placed caller channel
            | _ ->
                Mailbox.CoreMsg(PostEvent(caller, event, channel))
        | _ ->
            Mailbox.CoreMsg(PostEvent(caller, event, channel))

    /// Push the list back to back, then rebuild the ack from the log.
    /// The push runs before this function returns, so the next call cannot pass it.
    let postEvents
        (host: MailboxHost)
        (caller: Caller)
        (events: Ev list)
        : Async<Result<CoreChangesAccepted, string>> =
        if List.isEmpty events then
            async { return Error "events must not be empty" }
        elif List.exists isClientActorStop events then
            async { return Error "ActorStop is not a client event type" }
        else
            let builds =
                events
                |> List.map (fun (event: Ev) ->
                    fun channel -> postedMailbox caller event channel)
            let waiting = MailboxHost.postForReplies host builds
            async {
                let! results = waiting
                return! mergeIdReplies host events results
            }

    /// Graph-only Ev: same flow as postEvents, skips file persist only.
    let postGraphOnly
        (host: MailboxHost)
        (caller: Caller)
        (event: Ev)
        : Async<Result<CoreChangesAccepted, string>> =
        async {
            let! result =
                reply host (fun channel ->
                    PostGraphOnly(caller, event, channel))
            match result with
            | Error err -> return Error err
            | Ok eventId ->
                return! acceptedFromIds host [ event ] [ eventId ]
        }

    let startActor
        (host: MailboxHost)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        : Async<Result<unit, string>> =
        reply host (fun channel ->
            StartActor(caller, request, channel))

    let startPeerActor
        (host: MailboxHost)
        (caller: Caller)
        (peerName: PeerActorName)
        (request: Gambol.Shared.ActorStart)
        : Async<Result<unit, string>> =
        reply host (fun channel ->
            StartPeerActor(caller, peerName, request, channel))

    /// Append ActorStart for a Search Actor. The walk stays off this queue.
    let recordSearchStart
        (host: MailboxHost)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        : Async<Result<unit, string>> =
        reply host (fun channel ->
            RecordSearchStart(caller, request, channel))

    /// Enqueue ActorStop for that Search Actor. The id is the root.
    let recordSearchStop
        (host: MailboxHost)
        (caller: Caller)
        (rootId: NodeId)
        : Async<Result<unit, string>> =
        reply host (fun channel ->
            RecordSearchStop(caller, rootId, channel))

    let startLoadSaveCommand
        (host: MailboxHost)
        (caller: Caller)
        (path: LoadSavePath)
        (peerName: PeerActorName)
        (request: LoadSaveCommandRequest)
        : Async<Result<unit, string>> =
        reply host (fun channel ->
            StartLoadSaveCommand(
                caller,
                path,
                peerName,
                request,
                channel))

    /// Mailbox Load of a Workspace, Directory, or File: push onto the Parse stack.
    let mbLoadNode
        (host: MailboxHost)
        (caller: Caller)
        (subject: NodeId)
        : Async<Result<EventId, string>> =
        MailboxHost.postMailboxAndAsyncReply host (fun channel ->
            Mailbox.placeForCaller
                { orderId = OrderId.New()
                  authority = caller.authority
                  commandName = None
                  order = Order.Load subject }
                caller
                channel)

    let actorStop
        (host: MailboxHost)
        (caller: Caller)
        (result: ActorResult)
        : Async<Result<unit, string>> =
        reply host (fun channel ->
            ActorStop(caller, result, channel))

    let cancelByFocus
        (host: MailboxHost)
        (caller: Caller)
        (focusId: NodeId)
        : Async<Result<unit, string>> =
        reply host (fun channel ->
            CancelActor(caller, focusId, channel))

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
