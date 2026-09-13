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
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        host.mailbox.PostAndAsyncReply(fun reply ->
            PostChange(changes, reply))

    let postGraphOnlyChange
        (host: MailboxHost)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        host.mailbox.PostAndAsyncReply(fun reply ->
            PostGraphOnlyChange(changes, reply))

    let coreChanges (host: MailboxHost) : CoreChanges =
        { getState = fun () -> tryGetState host
          getRevision = fun () -> getRevision host
          getChangesSince = getChangesSince host
          isReady = host.isReady
          postChange = postChange host
          postGraphOnlyChange = postGraphOnlyChange host }

    let isReady (host: MailboxHost) = host.isReady ()

    let flushSnapshot (host: MailboxHost) = host.flushSnapshot ()

    let dispose (host: MailboxHost) = host.dispose ()

    let createFile (dataDir: string) : MailboxHost =
        FileAgent.create dataDir |> FileAgent.mailboxHost

    let createDb (connectionString: string) : MailboxHost =
        DbAgent.create connectionString |> DbAgent.mailboxHost

    let createDbWithDataDir
        (connectionString: string)
        (dataDir: string)
        : MailboxHost =
        DbAgent.createWithDataDir connectionString dataDir
        |> DbAgent.mailboxHost
