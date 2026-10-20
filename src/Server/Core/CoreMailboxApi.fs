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

    let persistMessage (host: MailboxHost) (id: EventId) =
        ask host (fun channel -> Query.PersistMessage(id, channel))

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

    let private commandFrom (name: string) =
        if name = "" then None else Some name

    let private placeChange
        (host: MailboxHost)
        (caller: Caller)
        (orderId: System.Guid)
        (commandName: string option)
        (ops: Op list)
        (origin: ChangeOrigin)
        : Async<Result<EventId, string>> =
        MailboxHost.beginMailbox host (fun channel ->
            Mailbox.PlaceOrder(
                caller,
                orderId,
                commandName,
                Order.Change(ops, origin),
                channel))

    let private idOfPosted (stored: Ev, _) = stored.id

    let private placeOne
        (host: MailboxHost)
        (caller: Caller)
        (event: Ev)
        : Async<Result<EventId, string>> =
        if event.id <> EventId.zero then
            async { return Error "posted EventId must be zero" }
        else
            match event.body with
            | EventBody.Change ops ->
                placeChange
                    host
                    caller
                    event.submissionId
                    (commandFrom event.commandName)
                    ops
                    ChangeOrigin.Edit
            | EventBody.Undo _
            | EventBody.Redo _
            | EventBody.ActorStart _
            | EventBody.Cancel _ ->
                async {
                    let! result =
                        MailboxHost.beginMailbox host (fun channel ->
                            Mailbox.CoreMsg(PostEvent(caller, event, channel)))
                    return result |> Result.map idOfPosted
                }
            | EventBody.ActorStop _ ->
                async { return Error "ActorStop is not a client event type" }
            | EventBody.NodeUnpersisted _ ->
                async { return Error "NodeUnpersisted is a Notice" }

    let private lastId
        (results: Result<EventId, string> list)
        : Result<EventId, string> =
        let step state result =
            match state, result with
            | Error err, _ -> Error err
            | _, Error err -> Error err
            | _, Ok id -> Ok id
        match results with
        | [] -> Error "events must not be empty"
        | head :: rest -> List.fold step head rest

    let private storedById (host: MailboxHost) (id: EventId) =
        async {
            let! events = getEventsSince host EventId.zero
            match events |> List.tryFind (fun event -> event.id = id) with
            | Some stored -> return Ok stored
            | None -> return Error "stored event missing"
        }

    let postEvent
        (host: MailboxHost)
        (caller: Caller)
        (event: Ev)
        : Async<Result<Ev, string>> =
        match event.body with
        | EventBody.Change _ ->
            async {
                let! placed = placeOne host caller event
                match placed with
                | Error err -> return Error err
                | Ok id -> return! storedById host id
            }
        | _ ->
            async {
                let! result = postEventAccepted host caller event
                return result |> Result.map fst
            }

    /// Push the list back to back, then take the last assigned id.
    /// The push runs before this function returns, so the next call cannot pass it.
    let postEvents
        (host: MailboxHost)
        (caller: Caller)
        (events: Ev list)
        : Async<Result<EventId, string>> =
        if List.isEmpty events then
            async { return Error "events must not be empty" }
        elif List.exists isClientActorStop events then
            async { return Error "ActorStop is not a client event type" }
        else
            let waiting =
                events
                |> List.map (placeOne host caller)
                |> MailboxHost.postForReplyList host
            async {
                let! results = waiting
                return lastId results
            }

    /// Parse Change. Skips the document-file write.
    let postGraphOnly
        (host: MailboxHost)
        (caller: Caller)
        (event: Ev)
        : Async<Result<EventId, string>> =
        if event.id <> EventId.zero then
            async { return Error "posted EventId must be zero" }
        else
            match event.body with
            | EventBody.Change ops ->
                placeChange
                    host
                    caller
                    event.submissionId
                    (commandFrom event.commandName)
                    ops
                    ChangeOrigin.Parse
            | _ -> async { return Error "Parse change requires ops" }

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
        : Async<Result<unit, string>> =
        reply host (fun channel ->
            LoadNode(caller, subject, channel))

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
        reply host (fun channel -> Login(caller, channel))

    let logout
        (host: MailboxHost)
        (caller: Caller)
        : Async<Result<unit, string>> =
        reply host (fun channel -> Logout(caller, channel))

    let isAdmitted
        (host: MailboxHost)
        (caller: Caller)
        : Async<bool> =
        reply host (fun channel -> AdmitCaller(caller, channel))

    let isReady (host: MailboxHost) = MailboxHost.isReady host ()

    let dispose (host: MailboxHost) = MailboxHost.dispose host
