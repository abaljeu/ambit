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

    let postEvent
        (host: MailboxHost)
        (caller: Caller)
        (event: Ev)
        : Async<Result<Ev, string>> =
        async {
            let! result = postEventAccepted host caller event
            return result |> Result.map fst
        }

    let private isClientActorStop (event: Ev) =
        match event.body with
        | EventBody.ActorStop _ -> true
        | _ -> false

    let private replyOf host (stored: Ev, accepted) =
        match accepted with
        | Some value -> value
        | None ->
            CoreChanges.accepted
                stored.id
                (MailboxHost.isReady host ())
                [ stored ]
                false
                None

    let private consEvents reversed events =
        List.fold (fun acc event -> event :: acc) reversed events

    let private mergeReplies host results =
        let step
            (state: Result<CoreChangesAccepted, string>, reversed)
            result
            =
            match state, result with
            | Error err, _ -> Error err, reversed
            | _, Error err -> Error err, reversed
            | Ok prior, Ok pair ->
                let next = replyOf host pair
                let merged =
                    CoreChanges.accepted
                        next.eventId
                        next.isReady
                        []
                        (prior.externalChanges || next.externalChanges)
                        (next.message |> Option.orElse prior.message)
                Ok merged, consEvents reversed next.events
        match results with
        | [] -> Error "events must not be empty"
        | Error err :: _ -> Error err
        | Ok pair :: rest ->
            let first = replyOf host pair
            match List.fold step (Ok first, consEvents [] first.events) rest with
            | Error err, _ -> Error err
            | Ok last, reversed ->
                Ok(
                    CoreChanges.accepted
                        last.eventId
                        (MailboxHost.isReady host ())
                        (List.rev reversed)
                        last.externalChanges
                        last.message)

    /// Push the list back to back, then merge the replies.
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
                    fun channel -> PostEvent(caller, event, channel))
            let waiting = MailboxHost.postForReplies host builds
            async {
                let! results = waiting
                return mergeReplies host results
            }

    /// Graph-only Ev: same flow as postEvents, skips file persist only.
    let postGraphOnly
        (host: MailboxHost)
        (caller: Caller)
        (event: Ev)
        : Async<Result<CoreChangesAccepted, string>> =
        reply host (fun channel ->
            PostGraphOnly(caller, event, channel))

    let private placeUnit
        (host: MailboxHost)
        (caller: Caller)
        (order: Order)
        : Async<Result<unit, string>> =
        async {
            let! result =
                MailboxHost.postMailboxAndAsyncReply host (fun channel ->
                    Mailbox.PlaceOrder(
                        caller,
                        System.Guid.NewGuid(),
                        None,
                        order,
                        channel))
            return result |> Result.map ignore
        }

    let startActor
        (host: MailboxHost)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        : Async<Result<unit, string>> =
        placeUnit host caller (Order.StartActor request)

    let startPeerActor
        (host: MailboxHost)
        (caller: Caller)
        (peerName: PeerActorName)
        (request: Gambol.Shared.ActorStart)
        : Async<Result<unit, string>> =
        placeUnit host caller (Order.StartPeerActor(peerName, request))

    /// Append ActorStart for a Search Actor. The walk stays off this queue.
    let recordSearchStart
        (host: MailboxHost)
        (caller: Caller)
        (request: Gambol.Shared.ActorStart)
        : Async<Result<unit, string>> =
        placeUnit host caller (Order.RecordSearchStart request)

    /// Enqueue ActorStop for that Search Actor. The id is the root.
    let recordSearchStop
        (host: MailboxHost)
        (caller: Caller)
        (rootId: NodeId)
        : Async<Result<unit, string>> =
        MailboxHost.postMailboxAndAsyncReply host (fun channel ->
            Mailbox.SearchStop(caller, rootId, channel))

    let startLoadSaveCommand
        (host: MailboxHost)
        (caller: Caller)
        (path: LoadSavePath)
        (peerName: PeerActorName)
        (request: LoadSaveCommandRequest)
        : Async<Result<unit, string>> =
        placeUnit
            host
            caller
            (Order.LoadSave(path, peerName, request))

    /// Mailbox Load of a Workspace, Directory, or File: push onto the Parse stack.
    let mbLoadNode
        (host: MailboxHost)
        (caller: Caller)
        (subject: NodeId)
        : Async<Result<EventId, string>> =
        MailboxHost.postMailboxAndAsyncReply host (fun channel ->
            Mailbox.PlaceOrder(
                caller,
                System.Guid.NewGuid(),
                None,
                Order.Load subject,
                channel))

    /// Mailbox Save of a Workspace: append EventBody.Save and return its id.
    let mbSaveNode
        (host: MailboxHost)
        (caller: Caller)
        (subject: NodeId)
        : Async<Result<EventId, string>> =
        MailboxHost.postMailboxAndAsyncReply host (fun channel ->
            Mailbox.PlaceOrder(
                caller,
                System.Guid.NewGuid(),
                None,
                Order.Save subject,
                channel))

    /// The caller secret selects the live focus. The mailbox assigns the id.
    let actorStop
        (host: MailboxHost)
        (caller: Caller)
        (result: ActorResult)
        : Async<Result<EventId, string>> =
        match caller.authority with
        | Authority "Actor" ->
            let (Credential callerId) = caller.secret
            MailboxHost.postMailboxAndAsyncReply host (fun channel ->
                Mailbox.PlaceOrder(
                    caller,
                    System.Guid.NewGuid(),
                    None,
                    Order.ActorStop(callerId, result),
                    channel))
        | _ -> async.Return (Error CoreAuth.refuse)

    let cancelByFocus
        (host: MailboxHost)
        (caller: Caller)
        (focusId: NodeId)
        : Async<Result<unit, string>> =
        placeUnit host caller (Order.Cancel focusId)

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
