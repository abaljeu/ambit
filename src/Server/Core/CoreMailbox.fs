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
/// - The mailbox owns the Browser secret set on the loop thread.
/// - There is no public add-credential door. Login is a mailbox message;
///   the mailbox privately adds that Browser secret. Actor liveness is
///   the live row, not this set.
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

    let tryGetState
        (host: MailboxHost)
        : Async<Result<State, string>> =
        host.mailbox.PostAndAsyncReply GetState

    let getState
        (host: MailboxHost)
        : Async<Result<State, string>> =
        tryGetState host

    let eventHistory
        (host: MailboxHost)
        : Async<HistoryEvent list> =
        host.mailbox.PostAndAsyncReply GetEventHistory

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

    let login
        (host: MailboxHost)
        (secret: Credential)
        : Async<Result<unit, string>> =
        host.mailbox.PostAndAsyncReply(fun reply -> Login(secret, reply))

    let isAdmitted
        (host: MailboxHost)
        (secret: Credential)
        : Async<bool> =
        host.mailbox.PostAndAsyncReply(fun reply -> AdmitSecret(secret, reply))

    let coreChanges
        (host: MailboxHost)
        (caller: Caller)
        : CoreChanges =
        let rec make (c: Caller) : CoreChanges =
            { getState = fun () -> tryGetState host
              getRevision = fun () -> getRevision host
              getChangesSince = getChangesSince host
              isReady = host.isReady
              postChange = fun changes -> postChange host c changes
              postGraphOnlyChange =
                fun changes -> postGraphOnlyChange host changes
              actorStop = fun result -> actorStop host c result
              asCaller = make }
        make caller

    let isReady (host: MailboxHost) = host.isReady ()

    let flushSnapshot (host: MailboxHost) = host.flushSnapshot ()

    let dispose (host: MailboxHost) = host.dispose ()

    let host
        (pool: CoreActorPool)
        (persist: PersistFilling)
        (initialSecrets: Set<Credential>)
        : MailboxHost =
        let mailbox =
            match persist.until with
            | None ->
                CoreMailboxBackend.start
                    initialSecrets
                    persist.handlers
                    pool
                    persist.onError
                    persist.formatError
            | Some until ->
                CoreMailboxBackend.startWithPrelude
                    initialSecrets
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
        (dataDir: string)
        (initialSecrets: Set<Credential>)
        : MailboxHost =
        host
            (CoreActorPool.create ())
            (FileAgent.persist (FileAgent.create dataDir))
            initialSecrets

    let createDb
        (connectionString: string)
        (initialSecrets: Set<Credential>)
        : MailboxHost =
        host
            (CoreActorPool.create ())
            (DbAgent.persist (DbAgent.create connectionString))
            initialSecrets

    let createDbWithDataDir
        (connectionString: string)
        (dataDir: string)
        (initialSecrets: Set<Credential>)
        : MailboxHost =
        host
            (CoreActorPool.create ())
            (DbAgent.persist
                (DbAgent.createWithDataDir connectionString dataDir))
            initialSecrets
