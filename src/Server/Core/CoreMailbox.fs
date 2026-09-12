namespace Gambol.Server

open System
open Gambol.Shared

type CoreMsg =
    | GetState of AsyncReplyChannel<Result<State, string>>
    | GetRevision of AsyncReplyChannel<Result<Revision, string>>
    | GetChangesSince of
        after: int * AsyncReplyChannel<Result<Change list, string>>
    | PostChange of
        changes: Change list *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
    | PostGraphOnlyChange of
        changes: Change list *
        AsyncReplyChannel<Result<CoreChangesAccepted, string>>
    | SnapshotDone of graph: Graph option

[<RequireQualifiedAccess>]
module CoreMailbox =

    let private unwrap result =
        match result with
        | Ok value -> value
        | Error error -> failwith error

    let tryGetState
        (mailbox: MailboxProcessor<CoreMsg>)
        : Async<Result<State, string>> =
        mailbox.PostAndAsyncReply GetState

    let getState (mailbox: MailboxProcessor<CoreMsg>) : Async<State> =
        async {
            let! result = tryGetState mailbox
            return unwrap result
        }

    let getRevision
        (mailbox: MailboxProcessor<CoreMsg>)
        : Async<Revision> =
        async {
            let! result = mailbox.PostAndAsyncReply GetRevision
            return unwrap result
        }

    let getChangesSince
        (mailbox: MailboxProcessor<CoreMsg>)
        (after: int)
        : Async<Change list> =
        async {
            let! result =
                mailbox.PostAndAsyncReply(fun reply ->
                    GetChangesSince(after, reply))
            return unwrap result
        }

    let postChange
        (mailbox: MailboxProcessor<CoreMsg>)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        mailbox.PostAndAsyncReply(fun reply -> PostChange(changes, reply))

    let postGraphOnlyChange
        (mailbox: MailboxProcessor<CoreMsg>)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        mailbox.PostAndAsyncReply(fun reply ->
            PostGraphOnlyChange(changes, reply))

    let coreChanges
        (isReady: unit -> bool)
        (mailbox: MailboxProcessor<CoreMsg>)
        : CoreChanges =
        { getState = fun () -> tryGetState mailbox
          getRevision = fun () -> getRevision mailbox
          getChangesSince = fun after -> getChangesSince mailbox after.Value
          isReady = isReady
          postChange = postChange mailbox
          postGraphOnlyChange = postGraphOnlyChange mailbox }
