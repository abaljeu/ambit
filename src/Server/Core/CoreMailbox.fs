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

    let private acceptedFromPosted
        (host: MailboxHost)
        (posted:
            Result<
                Ev *
                CoreChangesAccepted option,
                string>)
        : Async<Result<CoreChangesAccepted, string>> =
        async {
            match posted with
            | Error error -> return Error error
            | Ok (stored, Some accepted) ->
                return Ok { accepted with events = [ stored ] }
            | Ok (stored, None) ->
                let! eventId = getEventId host
                return
                    Ok(
                        CoreChanges.accepted
                            eventId
                            (MailboxHost.isReady host ())
                            [ stored ]
                            false
                            None)
        }

    let private mergePostLoop postOne host caller first rest =
        async {
            let! firstAccepted = postOne host caller first
            match firstAccepted with
            | Error error -> return Error error
            | Ok accepted ->
                let folder acc item =
                    async {
                        match! acc with
                        | Error error -> return Error error
                        | Ok prior ->
                            match! postOne host caller item with
                            | Error error -> return Error error
                            | Ok next ->
                                return
                                    Ok(
                                        CoreChanges.mergeAccepted
                                            prior
                                            next)
                    }
                return!
                    List.fold
                        folder
                        (async.Return(Ok accepted))
                        rest
        }

    let postEvent
        (host: MailboxHost)
        (caller: Caller)
        (event: Ev)
        : Async<Result<Ev, string>> =
        async {
            let! result = postEventAccepted host caller event
            return result |> Result.map fst
        }

    let private postOneEvent host caller event =
        async {
            let! posted = postEventAccepted host caller event
            return! acceptedFromPosted host posted
        }

    /// Transport may pass an Ev list; each Ev becomes one PostEvent
    /// on the mailbox queue (no multi-Ev CoreMsg / postMany).
    let postEvents
        (host: MailboxHost)
        (caller: Caller)
        (events: Ev list)
        : Async<Result<CoreChangesAccepted, string>> =
        async {
            match events with
            | [] -> return Error "events must not be empty"
            | first :: rest ->
                return!
                    mergePostLoop
                        postOneEvent
                        host
                        caller
                        first
                        rest
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

    let host
        (pool: CoreActorPool)
        (persist: PersistFilling)
        (credentials: CoreCredentials)
        : MailboxHost =
        let context =
            CoreMailboxBackend.makeMailBox
                credentials
                persist.handlers
                pool
                persist.onError
                persist.formatError
        let started =
            match persist.until with
            | None -> CoreMailboxBackend.start context
            | Some until ->
                CoreMailboxBackend.startWithPrelude context until
        persist.bindSnapshot (fun graph ->
            started.processor.Post(SnapshotDone graph))
        let created =
            MailboxHost.create
                started.processor
                persist.isReady
                persist.flushSnapshot
                persist.dispose
        started.bindCoreChanges (coreChanges created)
        created

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
