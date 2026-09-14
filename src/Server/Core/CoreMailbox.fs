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
              postChange = fun changes -> postChange host auth sec changes
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

    let createFile
        (credentials: CoreCredentials)
        (dataDir: string)
        : MailboxHost =
        FileAgent.create credentials dataDir |> FileAgent.mailboxHost

    let createDb
        (credentials: CoreCredentials)
        (connectionString: string)
        : MailboxHost =
        DbAgent.create credentials connectionString
        |> DbAgent.mailboxHost

    let createDbWithDataDir
        (credentials: CoreCredentials)
        (connectionString: string)
        (dataDir: string)
        : MailboxHost =
        DbAgent.createWithDataDir credentials connectionString dataDir
        |> DbAgent.mailboxHost
