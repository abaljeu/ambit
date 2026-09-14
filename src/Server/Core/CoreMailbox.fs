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

    let startFileWithActors
        (credentials: CoreCredentials)
        (pool: CoreActorPool)
        : FileAgent.MailboxStarter =
        fun handlers onError formatError ->
            CoreMailboxBackend.start
                credentials handlers pool onError formatError

    let startFile (credentials: CoreCredentials) : FileAgent.MailboxStarter =
        startFileWithActors credentials (CoreActorPool.create credentials)

    let startDbWithActors
        (credentials: CoreCredentials)
        (pool: CoreActorPool)
        =
        fun handlers onError formatError until ->
            CoreMailboxBackend.startWithPrelude
                credentials
                handlers
                pool
                onError
                formatError
                until

    let startDb (credentials: CoreCredentials) =
        startDbWithActors credentials (CoreActorPool.create credentials)

    let createFile
        (startMailbox: FileAgent.MailboxStarter)
        (dataDir: string)
        : MailboxHost =
        FileAgent.create startMailbox dataDir |> FileAgent.mailboxHost

    let createDb
        (startMailbox:
            PersistHandlers
                -> (string -> string -> exn -> unit)
                -> (string -> string)
                -> Async<Result<unit, string>>
                -> MailboxProcessor<CoreMsg>)
        (connectionString: string)
        : MailboxHost =
        DbAgent.create startMailbox connectionString |> DbAgent.mailboxHost

    let createDbWithDataDir
        (startMailbox:
            PersistHandlers
                -> (string -> string -> exn -> unit)
                -> (string -> string)
                -> Async<Result<unit, string>>
                -> MailboxProcessor<CoreMsg>)
        (connectionString: string)
        (dataDir: string)
        : MailboxHost =
        DbAgent.createWithDataDir startMailbox connectionString dataDir
        |> DbAgent.mailboxHost
