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
/// Lifecycle fact exposure (partial):
/// - getState: Read the Graph and lockPresent facts after Actor start/stop.
/// - Lifecycle Events (ActorStarted, ActorFinished) are NOT yet exposed;
///   blocked on §4 History append.
[<RequireQualifiedAccess>]
module CoreMailbox =

    let private unwrap result =
        match result with
        | Ok value -> value
        | Error error -> failwith error

    let tryGetState
        (host: MailboxHost)
        : Async<Result<State, string>> =
        host.mailbox.PostAndAsyncReply GetState

    let getState
        (host: MailboxHost)
        : Async<Result<State, string>> =
        tryGetState host

    let getRevision (host: MailboxHost) : Async<Revision> =
        async {
            let! result = host.mailbox.PostAndAsyncReply GetRevision
            return unwrap result
        }

    let getChangesSince
        (host: MailboxHost)
        (after: Revision)
        : Async<Change list> =
        async {
            let! result =
                host.mailbox.PostAndAsyncReply(fun reply ->
                    GetChangesSince(after, reply))
            return unwrap result
        }

    let postChange
        (host: MailboxHost)
        (caller: Caller)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        host.mailbox.PostAndAsyncReply(fun reply ->
            PostChange(caller, changes, reply))

    let postGraphOnlyChange
        (host: MailboxHost)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        host.mailbox.PostAndAsyncReply(fun reply ->
            PostGraphOnlyChange(changes, reply))

    let startActor
        (host: MailboxHost)
        (caller: Caller)
        (request: StartActorRequest)
        : Async<Result<unit, string>> =
        host.mailbox.PostAndAsyncReply(fun reply ->
            StartActor(caller, request, reply))

    let actorStop
        (host: MailboxHost)
        (caller: Caller)
        (result: ActorResult)
        : Async<Result<unit, string>> =
        host.mailbox.PostAndAsyncReply(fun reply ->
            ActorStop(caller, result, reply))

    let coreChanges
        (host: MailboxHost)
        (credentials: CoreCredentials)
        (caller: Caller)
        : CoreChanges =
        let rec make c : CoreChanges =
            { getState = fun () -> tryGetState host
              getRevision = fun () -> getRevision host
              getChangesSince = getChangesSince host
              isReady = host.isReady
              postChange =
                fun changes ->
                    CoreAuth.post
                        credentials
                        c.secret
                        (postChange host c)
                        changes
              postGraphOnlyChange =
                fun changes ->
                    CoreAuth.post
                        credentials
                        c.secret
                        (postGraphOnlyChange host)
                        changes
              asCaller = make }
        make caller

    let isReady (host: MailboxHost) = host.isReady ()

    let flushSnapshot (host: MailboxHost) = host.flushSnapshot ()

    let dispose (host: MailboxHost) = host.dispose ()

    let host
        (credentials: CoreCredentials)
        (pool: CoreActorPool)
        (persist: PersistFilling)
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
        persist.bindMailbox mailbox
        { mailbox = mailbox
          isReady = persist.isReady
          flushSnapshot = persist.flushSnapshot
          dispose = persist.dispose }

    let createFile
        (credentials: CoreCredentials)
        (dataDir: string)
        : MailboxHost =
        host
            credentials
            (CoreActorPool.create credentials)
            (FileAgent.persist (FileAgent.create dataDir))

    let createDb
        (credentials: CoreCredentials)
        (connectionString: string)
        : MailboxHost =
        host
            credentials
            (CoreActorPool.create credentials)
            (DbAgent.persist (DbAgent.create connectionString))

    let createDbWithDataDir
        (credentials: CoreCredentials)
        (connectionString: string)
        (dataDir: string)
        : MailboxHost =
        host
            credentials
            (CoreActorPool.create credentials)
            (DbAgent.persist
                (DbAgent.createWithDataDir connectionString dataDir))
