namespace Gambol.Server

open Gambol.Shared

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

    let hostFile
        (credentials: CoreCredentials)
        (pool: CoreActorPool)
        (file: FileAgent)
        : MailboxHost =
        let mailbox =
            CoreMailboxBackend.start
                credentials
                (FileAgent.handlers file)
                pool
                (FileAgent.onError file)
                (FileAgent.formatError file)
        { mailbox = mailbox
          isReady = FileAgent.isReady file
          flushSnapshot = FileAgent.flushSnapshot file
          dispose = FileAgent.dispose file }

    let hostDb
        (credentials: CoreCredentials)
        (pool: CoreActorPool)
        (db: DbAgent)
        : MailboxHost =
        let mailbox =
            CoreMailboxBackend.startWithPrelude
                credentials
                (DbAgent.persistOf db)
                pool
                (DbAgent.onErrorOf db)
                (DbAgent.formatErrorOf db)
                (DbAgent.untilOf db)
        DbAgent.attachMailbox db mailbox
        { mailbox = mailbox
          isReady = DbAgent.isReadyOf db
          flushSnapshot = DbAgent.flushOf db
          dispose = DbAgent.disposeOf db }

    let createFile
        (credentials: CoreCredentials)
        (dataDir: string)
        : MailboxHost =
        hostFile
            credentials
            (CoreActorPool.create credentials)
            (FileAgent.create dataDir)

    let createDb
        (credentials: CoreCredentials)
        (connectionString: string)
        : MailboxHost =
        hostDb
            credentials
            (CoreActorPool.create credentials)
            (DbAgent.create connectionString)

    let createDbWithDataDir
        (credentials: CoreCredentials)
        (connectionString: string)
        (dataDir: string)
        : MailboxHost =
        hostDb
            credentials
            (CoreActorPool.create credentials)
            (DbAgent.createWithDataDir connectionString dataDir)
