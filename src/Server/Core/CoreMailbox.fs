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
        (authority: Authority)
        (secret: Credential)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        host.mailbox.PostAndAsyncReply(fun reply ->
            PostChange(authority, secret, changes, reply))

    let postGraphOnlyChange
        (host: MailboxHost)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        host.mailbox.PostAndAsyncReply(fun reply ->
            PostGraphOnlyChange(changes, reply))

    let coreChanges
        (host: MailboxHost)
        (credentials: CoreCredentials)
        (authority: Authority)
        (secret: Credential)
        : CoreChanges =
        let rec make auth sec : CoreChanges =
            { getState = fun () -> tryGetState host
              getRevision = fun () -> getRevision host
              getChangesSince = getChangesSince host
              isReady = host.isReady
              postChange =
                fun changes ->
                    CoreAuth.post
                        credentials
                        sec
                        (postChange host auth sec)
                        changes
              postGraphOnlyChange =
                fun changes ->
                    CoreAuth.post
                        credentials
                        sec
                        (postGraphOnlyChange host)
                        changes
              asCaller = fun a s -> make a s }
        make authority secret

    let isReady (host: MailboxHost) = host.isReady ()

    let flushSnapshot (host: MailboxHost) = host.flushSnapshot ()

    let dispose (host: MailboxHost) = host.dispose ()

    let startFile (credentials: CoreCredentials) : FileAgent.MailboxStarter =
        fun handlers onError formatError ->
            CoreMailboxBackend.start credentials handlers onError formatError

    let startDb (credentials: CoreCredentials) =
        fun handlers onError formatError until ->
            CoreMailboxBackend.startWithPrelude
                credentials
                handlers
                onError
                formatError
                until

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
