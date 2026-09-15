namespace Gambol.Server

open Gambol.Shared

/// CoreMailbox door — public API for MailboxHost.
///
/// Actor lifecycle:
/// - startActor: Start an Actor with StartActorRequest (includes revision).
///   Returns startActor bookkeeping result; does not wait for Actor body.
/// - actorStop: Stop an Actor with ActorResult.
/// - postChange / coreChanges: Credentialed Actor Changes use the same mailbox
///   as Browser Changes; no second Actor mailbox.
///
/// Secrets:
/// - The mailbox owns one CoreCredentials set of Caller on the loop.
/// - There is no public add-credential door. Login maps name+secret to a
///   Browser Caller and adds it. Logout removes that Caller. Actor liveness
///   is the live row, not this set.
///
/// Data exposure:
/// - getState: Read the Graph with lockPresent overlay. Returns Graph facts only.
/// - eventHistory: Read mailbox-owned History (Change + Actor lifecycle Events).
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
        : Async<HistoryEvent list> =
        reply host GetEventHistory

    let getRevision (host: MailboxHost) : Async<Revision> =
        async {
            let! result = reply host GetRevision
            return unwrap result
        }

    let getChangesSince
        (host: MailboxHost)
        (after: Revision)
        : Async<Change list> =
        async {
            let! result =
                reply host (fun channel -> GetChangesSince(after, channel))
            return unwrap result
        }

    let postChange
        (host: MailboxHost)
        (caller: Caller)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        reply host (fun channel -> PostChange(caller, changes, channel))

    let postGraphOnlyChange
        (host: MailboxHost)
        (caller: Caller)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        reply host (fun channel ->
            PostGraphOnlyChange(caller, changes, channel))

    let startActor
        (host: MailboxHost)
        (caller: Caller)
        (request: StartActorRequest)
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
              getRevision = fun () -> getRevision host
              getChangesSince = getChangesSince host
              isReady = MailboxHost.isReadyFn host
              postChange = fun changes -> postChange host c changes
              postGraphOnlyChange =
                fun changes -> postGraphOnlyChange host c changes
              actorStop = fun result -> actorStop host c result
              asCaller = make }
        make caller

    let isReady (host: MailboxHost) = MailboxHost.isReady host

    let flushSnapshot (host: MailboxHost) =
        MailboxHost.flushSnapshot host

    let dispose (host: MailboxHost) = MailboxHost.dispose host

    let host
        (pool: CoreActorPool)
        (persist: PersistFilling)
        (credentials: CoreCredentials)
        : MailboxHost =
        let mailbox =
            match persist.until with
            | None ->
                CoreMailboxBackend.start
                    credentials
                    persist.handlers
                    pool
                    persist.onError
                    persist.formatError
            | Some until ->
                CoreMailboxBackend.startWithPrelude
                    credentials
                    persist.handlers
                    pool
                    persist.onError
                    persist.formatError
                    until
        persist.bindSnapshot (fun graph ->
            mailbox.Post(SnapshotDone graph))
        MailboxHost.create
            mailbox
            persist.isReady
            persist.flushSnapshot
            persist.dispose

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
