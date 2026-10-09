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
        let settledIndex = Task.WaitAny([| task :> Task |], timeoutMs)
        if settledIndex = -1 then
            Error "change processing timed out"
        else
            task.GetAwaiter().GetResult()

    let withAppliedOps
        (event: Gambol.Shared.Ev)
        (ops: Op list)
        : Gambol.Shared.Ev =
        match event.body with
        | EventBody.Change _ -> { event with body = EventBody.Change ops }
        | EventBody.Undo(target, _) ->
            { event with body = EventBody.Undo(target, ops) }
        | EventBody.Redo(target, _) ->
            { event with body = EventBody.Redo(target, ops) }
        | EventBody.ActorStart _ | EventBody.Cancel _
        | EventBody.ActorStop _ -> event

    let overlayFreshEvents
        (confirmations: Gambol.Shared.Ev list)
        (fresh: Gambol.Shared.Ev list)
        (stampOps: Op list)
        : Gambol.Shared.Ev list * Gambol.Shared.Ev list =
        let stamped = PersistStamp.appendToLastEvent fresh stampOps
        let stampedById =
            stamped
            |> List.map (fun event -> event.submissionId, event)
            |> Map.ofList
        let confirmed =
            confirmations
            |> List.map (fun event ->
                Map.tryFind event.submissionId stampedById
                |> Option.defaultValue event)
        stamped, confirmed

    let operationContext msg =
        match msg with
        | GetState _ -> "GetState", ""
        | GetEventId _ -> "GetEventId", ""
        | GetEventsSince (after, _) ->
            "GetEventsSince", $"after={after}"
        | GetEventHistory _ -> "GetEventHistory", ""
        | PostGraphOnly (_, event, _) ->
            let n = Ev.ops event |> Option.defaultValue [] |> List.length
            "PostGraphOnly", $"ops={n}"
        | StartActor _ -> "StartActor", ""
        | StartPeerActor (_, PeerActorName name, _, _) ->
            "StartPeerActor", name
        | StartLoadSaveCommand (_, path, PeerActorName name, _, _) ->
            "StartLoadSaveCommand", $"{path}:{name}"
        | RecordSearchStart _ -> "RecordSearchStart", ""
        | RecordSearchStop _ -> "RecordSearchStop", ""
        | Load (_, subject, _) ->
            "Load", $"{subject}"
        | ActorStop (_, result, _) ->
            match result with
            | ActorSucceeded -> "ActorStop", "ActorSucceeded"
            | ActorFailed _ -> "ActorStop", "ActorFailed"
            | ActorCancelled -> "ActorStop", "ActorCancelled"
            | ActorQuery _ -> "ActorStop", "ActorQuery"
        | CancelActor _ -> "CancelActor", ""
        | Login _ -> "Login", ""
        | Logout _ -> "Logout", ""
        | AdmitCaller _ -> "AdmitCaller", ""
        | PostEvent _ -> "PostEvent", ""
        | EventsSince (after, _) -> "EventsSince", $"after={after}"

    let replyFailure error msg =
        match msg with
        | GetState reply -> reply.Reply(Error error)
        | GetEventId reply -> reply.Reply(Error error)
        | GetEventsSince (_, reply) -> reply.Reply(Error error)
        | GetEventHistory reply -> reply.Reply(EventLog.empty)
        | PostGraphOnly (_, _, reply) -> reply.Reply(Error error)
        | StartActor (_, _, reply) -> reply.Reply(Error error)
        | StartPeerActor (_, _, _, reply) -> reply.Reply(Error error)
        | StartLoadSaveCommand (_, _, _, _, reply) ->
            reply.Reply(Error error)
        | RecordSearchStart (_, _, reply) ->
            reply.Reply(Error error)
        | RecordSearchStop (_, _, reply) ->
            reply.Reply(Error error)
        | Load (_, _, reply) -> reply.Reply(Error error)
        | ActorStop (_, _, reply) -> reply.Reply(Error error)
        | CancelActor (_, _, reply) -> reply.Reply(Error error)
        | Login (_, reply) -> reply.Reply(Error error)
        | Logout (_, reply) -> reply.Reply(Error error)
        | AdmitCaller (_, reply) -> reply.Reply(false)
        | PostEvent (_, _, reply) -> reply.Reply(Error error)
        | EventsSince (_, reply) -> reply.Reply(EventLog.empty)

    type Started = {
        processor: MailboxProcessor<QueueSum>
        bindCoreChanges: (Caller -> CoreChanges) -> unit
    }

    type MailboxContext = {
        credentials: CoreCredentials ref
        persist: PersistHandlers
        pool: CoreActorPool
        parsePush: NodeId -> Result<unit, string>
        onError: string -> string -> exn -> unit
        formatError: string -> string
        eventLog: EventLog ref
        coreChanges: (Caller -> CoreChanges) option ref
    }

    let addCaller (context: MailboxContext) caller =
        context.credentials.Value <-
            CoreCredentials.add caller context.credentials.Value

    let removeCaller (context: MailboxContext) caller =
        context.credentials.Value <-
            CoreCredentials.remove caller context.credentials.Value

    let hasCaller (context: MailboxContext) caller =
        CoreCredentials.contains caller context.credentials.Value

    let admitCaller (context: MailboxContext) (caller: Caller) =
        match caller.authority with
        | Authority name when String.IsNullOrWhiteSpace name ->
            Error CoreAuth.refuse
        | Authority "Actor" ->
            match CoreAuth.admit (context.pool.isLive caller.secret) with
            | Error err -> Error(CoreAdmissionError.text err)
            | Ok () -> Ok ()
        | _ ->
            match CoreAuth.admit (hasCaller context caller) with
            | Error err -> Error(CoreAdmissionError.text err)
            | Ok () -> Ok ()

    let eventDispatchContext context : CoreEventDispatch.Context =
        { admit = admitCaller context
          persist = context.persist
          eventLog = context.eventLog }

    let graphNow context =
        match context.persist.getState () with
        | Ok state -> state.graph
        | Error _ -> Graph.create ()
    let dispatchPostGraphOnly
        (context: MailboxContext)
        (caller: Caller)
        (event: Ev)
        (reply: AsyncReplyChannel<Result<CoreChangesAccepted, string>>)
        : unit =
        match CoreEventDispatch.postEvent (eventDispatchContext context) caller event true with
        | Error err -> reply.Reply(Error err)
        | Ok (_, Some accepted) -> reply.Reply(Ok accepted)
        | Ok (_, None) ->
            match context.persist.getEventId () with
            | Error err -> reply.Reply(Error err)
            | Ok eventId ->
                reply.Reply(
                    Ok(
                        CoreChanges.accepted
                            eventId
                            true
                            []
                            false
                            None))

    type PostedReply =
        AsyncReplyChannel<
            Result<Ev * CoreChangesAccepted option, string>>

    let storedBySubmission context submissionId =
        context.eventLog.Value.events
        |> List.tryFind (fun (event: Ev) ->
            event.submissionId = submissionId)

    let acceptEvents (context: MailboxContext) (events: Ev list) =
        match context.persist.getEventId () with
        | Error err -> Error err
        | Ok eventId ->
            Ok(CoreChanges.accepted eventId true events false None)

    let replyList
        (reply: PostedReply) context tail (events: Ev list) =
        match acceptEvents context events with
        | Error err -> reply.Reply(Error err)
        | Ok accepted -> reply.Reply(Ok(tail, Some accepted))

    let eventsForStored context (stored: Ev) =
        match stored.body with
        | EventBody.ActorStart request ->
            let nextId = EventId.next stored.id
            match
                context.eventLog.Value.events
                |> List.tryFind (fun (event: Ev) -> event.id = nextId)
            with
            | Some stopped ->
                match stopped.body with
                | EventBody.ActorStop(id, _) when id = request.focusId ->
                    [ stored; stopped ]
                | _ -> [ stored ]
            | None -> [ stored ]
        | _ -> [ stored ]

