namespace Gambol.Server

open Gambol.Shared

/// CoreMailbox door — public API for MailboxHost.
///
/// Actor lifecycle:
/// - startActor: Start an Actor with ActorStart (includes revision).
///   Returns startActor bookkeeping result; does not wait for Actor body.
/// - actorStop: Stop an Actor with ActorResult.
/// - postEvents / coreChanges: Credentialed Actor Events use the same mailbox
///   as Browser Events; no second Actor mailbox.
///
/// Secrets:
/// - The mailbox owns one CoreCredentials set of Caller on the loop.
/// - There is no public add-credential door. Login maps name+secret to a
///   Browser Caller and adds it. Logout removes that Caller. Actor liveness
///   is the live row, not this set.
///
/// Data exposure:
/// - getState: Read the Graph with lockPresent overlay. Returns Graph facts only.
/// - eventHistory: The mailbox EventLog. Change + Actor Events.
[<RequireQualifiedAccess>]
module CoreMailbox =

    let private unwrap result =
        match result with
        | Ok value -> value
        | Error error -> failwith error

    /// Public add: CoreMsg joins the one mailbox queue.
    let internal addCoreMsg (host: MailboxHost) (msg: CoreMsg) =
        MailboxHost.postItem host (QueueSum.Core msg)

    /// Private add: InMsg joins that same queue. No public door.
    let internal addInMsg (host: MailboxHost) (msg: InMsg) =
        MailboxHost.postItem host (QueueSum.In msg)

    let private reply host build =
        MailboxHost.postAndAsyncReply host build

    let tryGetState
        (host: MailboxHost)
        : Async<Result<State, string>> =
        reply host GetState

    let getState
        (host: MailboxHost)
        : Async<Result<State, string>> =
        tryGetState host

    let eventHistory
        (host: MailboxHost)
        : Async<Gambol.Shared.EventLog> =
        reply host GetEventHistory

    let getEventId (host: MailboxHost) : Async<Gambol.Shared.EventId> =
        async {
            let! result = reply host GetEventId
            return unwrap result
        }

    let getEventsSince
        (host: MailboxHost)
        (after: Gambol.Shared.EventId)
        : Async<Ev list> =
        async {
            let! result =
                reply host (fun channel -> GetEventsSince(after, channel))
            return unwrap result
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
    let postEvents
        (host: MailboxHost)
        (caller: Caller)
        (events: Ev list)
        : Async<Result<CoreChangesAccepted, string>> =
        async {
            if List.isEmpty events then
                return Error "events must not be empty"
            elif List.exists isClientActorStop events then
                return Error "ActorStop is not a client event type"
            else
                let builds =
                    events
                    |> List.map (fun (event: Ev) ->
                        fun channel -> PostEvent(caller, event, channel))
                let! results = MailboxHost.postForReplies host builds
                return mergeReplies host results
        }

    let eventsSince
        (host: MailboxHost)
        (after: Gambol.Shared.EventId)
        : Async<Gambol.Shared.EventLog> =
        reply host (fun channel -> EventsSince(after, channel))

    /// Graph-only Ev: same flow as postEvents, skips file persist only.
    let postGraphOnly
        (host: MailboxHost)
        (caller: Caller)
        (event: Ev)
        : Async<Result<CoreChangesAccepted, string>> =
        reply host (fun channel ->
            PostGraphOnly(caller, event, channel))

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

    /// Mailbox Load of a File node: push onto the Parse stack (clear-fast).
    let load
        (host: MailboxHost)
        (caller: Caller)
        (subject: NodeId)
        : Async<Result<unit, string>> =
        reply host (fun channel ->
            Load(caller, subject, channel))

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

    let coreChanges
        (host: MailboxHost)
        (caller: Caller)
        : CoreChanges =
        let rec make (c: Caller) : CoreChanges =
            { getState = fun () -> tryGetState host
              getEventId = fun () -> getEventId host
              getEventsSince = getEventsSince host
              isReady = MailboxHost.isReady host
              postEvents = postEvents host c
              postGraphOnly = fun event -> postGraphOnly host c event
              actorStop = fun result -> actorStop host c result
              asCaller = make }
        make caller

    let isReady (host: MailboxHost) = MailboxHost.isReady host ()

    let flushSnapshot (host: MailboxHost) =
        MailboxHost.flushSnapshot host

    let dispose (host: MailboxHost) = MailboxHost.dispose host

    let internal hostWithParsePush
        (pool: CoreActorPool)
        (persist: PersistFilling)
        (credentials: CoreCredentials)
        (parsePush: NodeId -> unit)
        : MailboxHost =
        let context =
            CoreMailboxBackend.makeMailBox
                credentials
                persist
                pool
                parsePush
        let started = CoreMailboxBackend.start context persist
        let created = MailboxHost.create started.processor persist
        persist.bindSnapshot (addInMsg created)
        started.bindCoreChanges (coreChanges created)
        created

    let internal host
        (pool: CoreActorPool)
        (persist: PersistFilling)
        (credentials: CoreCredentials)
        : MailboxHost =
        hostWithParsePush
            pool
            persist
            credentials
            (fun _ -> ())

    let createFile
        (dataDir: string)
        (credentials: CoreCredentials)
        : MailboxHost =
        host
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create dataDir))
            credentials

    let createDb
        (connectionString: string)
        (credentials: CoreCredentials)
        : MailboxHost =
        host
            (CoreActorPool.create ())
            (DbAgent.persist (DbAgent.create connectionString))
            credentials

    let createDbWithDataDir
        (connectionString: string)
        (dataDir: string)
        (credentials: CoreCredentials)
        : MailboxHost =
        host
            (CoreActorPool.create ())
            (DbAgent.persist
                (DbAgent.createWithDataDir connectionString dataDir))
            credentials
