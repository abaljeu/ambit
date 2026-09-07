namespace Gambol.Server

open Gambol.Shared

type Credential = Credential of string

type CoreAdmissionError =
    | Unauthorized
    | UnknownJob
    | UnknownActor
    | Overlap

module CoreAdmissionError =

    let text (err: CoreAdmissionError) =
        match err with
        | Unauthorized -> "Unauthorized"
        | UnknownJob -> "unknown job"
        | UnknownActor -> "unknown actor"
        | Overlap -> "span overlaps a live job"

type CoreCredentials =
    { add: Credential -> Async<unit>
      remove: Credential -> Async<unit>
      contains: Credential -> Async<bool> }

[<RequireQualifiedAccess>]
module CoreAuth =

    let refuse = CoreAdmissionError.text Unauthorized

    let isAuthRefuse (err: string) = err = refuse

    let admit (live: bool) : Result<unit, CoreAdmissionError> =
        if live then Ok () else Error Unauthorized

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
            | Error err -> return Error(CoreAdmissionError.text err)
            | Ok () -> return! enqueue changes
        }

    let bind
        (credentials: CoreCredentials)
        (sender: Credential)
        (enqueue:
            Change list -> Async<Result<CoreChangesAccepted, string>>)
        : Change list -> Async<Result<CoreChangesAccepted, string>> =
        fun changes -> post credentials sender enqueue changes

    let bindHandle
        (credentials: CoreCredentials)
        (sender: Credential)
        (handle: CoreChanges)
        : CoreChanges =
        { handle with
            postChange = bind credentials sender handle.postChange
            postGraphOnlyChange =
                bind credentials sender handle.postGraphOnlyChange }

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
