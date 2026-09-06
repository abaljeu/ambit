namespace Gambol.Server

open Gambol.Shared

type Credential = Credential of string

type CoreCredentials =
    { add: Credential -> Async<unit>
      remove: Credential -> Async<unit>
      contains: Credential -> Async<bool> }

[<RequireQualifiedAccess>]
module CoreAuth =

    [<Literal>]
    let refuse = "Unauthorized"

    let isAuthRefuse (err: string) = err = refuse

    let admit (live: bool) : Result<unit, string> =
        if live then Ok () else Error refuse

    let post
        (credentials: CoreCredentials)
        (sender: Credential)
        (enqueue:
            Change list -> Async<Result<CoreChangesAccepted, string>>)
        (changes: Change list)
        : Async<Result<CoreChangesAccepted, string>> =
        async {
            let! live = credentials.contains sender
            match admit live with
            | Error err -> return Error err
            | Ok () -> return! enqueue changes
        }

[<RequireQualifiedAccess>]
module CoreCredentials =

    type private Msg =
        | Add of Credential * AsyncReplyChannel<unit>
        | Remove of Credential * AsyncReplyChannel<unit>
        | Contains of Credential * AsyncReplyChannel<bool>

    let create () : CoreCredentials =
        let mailbox =
            MailboxProcessor.Start(fun inbox ->
                let rec loop set = async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Add(cred, reply) ->
                        reply.Reply()
                        return! loop (Set.add cred set)
                    | Remove(cred, reply) ->
                        reply.Reply()
                        return! loop (Set.remove cred set)
                    | Contains(cred, reply) ->
                        reply.Reply(Set.contains cred set)
                        return! loop set
                }
                loop Set.empty)
        { add =
            fun cred ->
                mailbox.PostAndAsyncReply(fun reply -> Add(cred, reply))
          remove =
            fun cred ->
                mailbox.PostAndAsyncReply(fun reply -> Remove(cred, reply))
          contains =
            fun cred ->
                mailbox.PostAndAsyncReply(fun reply ->
                    Contains(cred, reply)) }
