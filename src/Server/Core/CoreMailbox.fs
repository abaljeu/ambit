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
            PostChange(changes, None, reply))

    let postActorChange
        (host: MailboxHost)
        (authority: Authority)
        (secret: Credential)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        host.mailbox.PostAndAsyncReply(fun reply ->
            PostChange(changes, Some(authority, secret), reply))

    let addCaller
        (host: MailboxHost)
        (authority: Authority)
        (secret: Credential)
        : unit =
        host.callers.add authority secret

    let startActor
        (host: MailboxHost)
        (request: StartActorRequest)
        : Async<Result<ActorStarted, string>> =
        host.mailbox.PostAndAsyncReply(fun reply ->
            StartActor(request, reply))

    let actorStop
        (host: MailboxHost)
        (authority: Authority)
        (secret: Credential)
        (result: ActorResult)
        : Async<Result<unit, string>> =
        host.mailbox.PostAndAsyncReply(fun reply ->
            ActorStop(authority, secret, result, reply))

    let lifecycleEvents (host: MailboxHost) : CoreEvent list =
        host.events.all ()

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
