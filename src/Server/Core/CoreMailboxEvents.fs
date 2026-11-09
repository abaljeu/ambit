namespace Gambol.Server

open System
open System.Threading.Tasks
open Gambol.Shared

[<RequireQualifiedAccess>]
module internal CoreMailboxEvents =

    type Ev = Gambol.Shared.Ev
    type EventLog = Gambol.Shared.EventLog
    module Ev = Gambol.Shared.Ev
    module EventLog = Gambol.Shared.EventLog

    /// Bound for non-file change work that may keep the mailbox context busy.
    /// Never wrap work-tree-gated file Persist: timeout abandonment would leave a late writer.
    [<Literal>]
    let ChangeProcessingTimeoutMs = 8000

    /// Runs a synchronous computation on a background Task, bounding wall-clock time. If the
    /// timeout elapses, the background Task is abandoned and may still complete later.
    /// Uses WaitAny (not Wait/Result) because WaitAny reports timeout vs settled without
    /// itself throwing on a faulted task; GetAwaiter().GetResult() then rethrows `f`'s
    /// original exception unwrapped (as if called synchronously), so the caller's existing
    /// exception handling is unaffected.
    let runBounded
        (timeoutMs: int)
        (f: unit -> Result<'a, string>)
        : Result<'a, string> =
        let task = Task.Run(fun () -> f ())
        ServerRunRegistry.track task
        let settledIndex = Task.WaitAny([| task :> Task |], timeoutMs)
        if settledIndex = -1 then
            Error "change processing timed out"
        else
            task.GetAwaiter().GetResult()

    let operationContext msg =
        match msg with
        | StartLoadSaveCommand (_, path, PeerActorName name, _, _) ->
            "StartLoadSaveCommand", $"{path}:{name}"
        | RecordSearchStart _ -> "RecordSearchStart", ""
        | RecordSearchStop _ -> "RecordSearchStop", ""
        | ActorStop (_, result, _) ->
            match result with
            | ActorSucceeded -> "ActorStop", "ActorSucceeded"
            | ActorFailed _ -> "ActorStop", "ActorFailed"
            | ActorCancelled -> "ActorStop", "ActorCancelled"
            | ActorQuery _ -> "ActorStop", "ActorQuery"
        | PostEvent _ -> "PostEvent", ""

    let replyFailure error msg =
        match msg with
        | StartLoadSaveCommand (_, _, _, _, reply) ->
            reply.Reply(Error error)
        | RecordSearchStart (_, _, reply) ->
            reply.Reply(Error error)
        | RecordSearchStop (_, _, reply) ->
            reply.Reply(Error error)
        | ActorStop (_, _, reply) -> reply.Reply(Error error)
        | PostEvent (_, _, reply) -> reply.Reply(Error error)

    type Started = {
        processor: MailboxProcessor<CoreQueue>
        bindCoreChanges: (Caller -> CoreChangesDoor) -> unit
        shutdown: unit -> unit
    }

    type MailboxContext = {
        persist: CoreMailboxAccess
        pool: CoreActorPool
        parsePush: NodeId -> Result<unit, string>
        onError: string -> string -> exn -> unit
        formatError: string -> string
        eventLog: EventLog ref
        restartTip: EventId
        eventLogSeedError: string option
        coreChanges: (Caller -> CoreChangesDoor) option ref
        session: Session
    }

    /// Pool receipt for Authority "Actor". Other authorities are not receipts.
    let actorReceiptIsLive (pool: CoreActorPool) (caller: Caller) =
        match caller.authority with
        | Authority "Actor" -> pool.isLive caller.secret
        | _ -> false

    let admitCaller (context: MailboxContext) (caller: Caller) =
        match caller.authority with
        | Authority name when String.IsNullOrWhiteSpace name ->
            Error CoreAuth.refuse
        | Authority "Actor" ->
            let live = actorReceiptIsLive context.pool caller
            match CoreAuth.admit live with
            | Error err -> Error(CoreAdmissionError.text err)
            | Ok () -> Ok ()
        | _ -> Ok ()

    let eventDispatchContext context : CoreEventDispatch.Context =
        { persist = context.persist
          eventLog = context.eventLog
          restartTip = context.restartTip }

    let graphNow context =
        match context.persist.graphAtEvent () with
        | Ok state -> state.graph
        | Error _ -> Graph.create ()

    type PostedReply =
        AsyncReplyChannel<Result<EventId, string>>

    let storedBySubmission context submissionId =
        context.eventLog.Value.events
        |> List.tryFind (fun (event: Ev) ->
            event.submissionId = submissionId)

    let replyStored (reply: PostedReply) (stored: Ev) =
        reply.Reply(Ok stored.id)

