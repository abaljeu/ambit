namespace Gambol.Server

open System
open Gambol.Shared

[<RequireQualifiedAccess>]
module internal CoreEventDispatch =

    type Ev = Gambol.Shared.Ev
    type EventLog = Gambol.Shared.EventLog
    module EventLog = Gambol.Shared.EventLog

    type Context =
        { persist: PersistHandlers
          eventLog: EventLog ref
          restartTip: EventId }

    let private eventAuthority (Authority name) =
        Gambol.Shared.Authority name

    let private tryStored (context: Context) submissionId =
        context.eventLog.Value.events
        |> List.tryFind (fun e -> e.submissionId = submissionId)

    let private catchUpNextId (context: Context) =
        match context.persist.getEventLog () with
        | Error _ -> ()
        | Ok persistLog ->
            let mailbox = context.eventLog.Value
            context.eventLog.Value <-
                { mailbox with
                    nextId =
                        EventId.max mailbox.nextId persistLog.nextId }

    let private appendNew (context: Context) (event: Ev) =
        let stored =
            { event with id = EventLog.nextId context.eventLog.Value }
        match context.persist.appendEvent stored with
        | Error error -> Error error
        | Ok () ->
            context.eventLog.Value <-
                EventLog.append stored context.eventLog.Value
            Ok stored

    /// submissionId is Guid dedup (event-abstraction): replay returns the stored Ev.
    let private commit (context: Context) (event: Ev) =
        catchUpNextId context
        match tryStored context event.submissionId with
        | Some existing -> Ok existing
        | None -> appendNew context event

    /// A Notice has no caller. Envelope fields stay blank; the body is the case.
    let logNotice (context: Context) (notice: Gambol.Shared.Notice) =
        catchUpNextId context
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Gambol.Shared.Authority ""
          commandName = ""
          body = Gambol.Shared.EventBody.Notice notice }
        |> appendNew context

    let private lifecycleEvent
        (caller: Caller)
        (body: Gambol.Shared.EventBody)
        : Ev =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = eventAuthority caller.authority
          commandName = ""
          body = body }

    let actorStart (context: Context) caller request =
        lifecycleEvent
            caller
            (Gambol.Shared.EventBody.ActorStart request)
        |> commit context
        |> Result.map ignore

    let actorStop (context: Context) caller focusId result =
        let sharedResult =
            match result with
            | ActorSucceeded ->
                Gambol.Shared.ActorResult.ActorSucceeded
            | ActorFailed message ->
                Gambol.Shared.ActorResult.ActorFailed message
            | ActorCancelled ->
                Gambol.Shared.ActorResult.ActorCancelled
            | ActorQuery ids ->
                Gambol.Shared.ActorResult.ActorQuery ids
        lifecycleEvent
            caller
            (Gambol.Shared.EventBody.ActorStop(
                focusId,
                sharedResult))
        |> commit context
        |> Result.map ignore

    /// Caller already checked submissionId. This append does not scan again.
    let appendLifecycle
        (context: Context)
        (caller: Caller)
        (submissionId: System.Guid)
        (body: Gambol.Shared.EventBody)
        : Result<Ev, string> =
        catchUpNextId context
        { lifecycleEvent caller body with
            submissionId = submissionId }
        |> appendNew context

    let private targetBeforeRestart (context: Context) (target: EventId) =
        target <> EventId.zero
        && EventId.value target <= EventId.value context.restartTip

    let private completeAction (context: Context) (event: Ev) =
        match event.body with
        | Gambol.Shared.EventBody.Undo(target, _)
        | Gambol.Shared.EventBody.Redo(target, _) when
            targetBeforeRestart context target ->
            Error "target Event is from before the restart"
        | Gambol.Shared.EventBody.Undo(target, [])
        | Gambol.Shared.EventBody.Redo(target, []) ->
            match EventLog.tryFind target context.eventLog.Value with
            | None -> Error "target Event not found"
            | Some targetEvent ->
                match Ev.inverseOps targetEvent with
                | None -> Error "target Event has no inverse Ops"
                | Some ops ->
                    let body =
                        match event.body with
                        | Gambol.Shared.EventBody.Undo _ ->
                            Gambol.Shared.EventBody.Undo(target, ops)
                        | _ ->
                            Gambol.Shared.EventBody.Redo(target, ops)
                    Ok { event with body = body }
        | Gambol.Shared.EventBody.Change _
        | Gambol.Shared.EventBody.Undo _
        | Gambol.Shared.EventBody.Redo _
        | Gambol.Shared.EventBody.Notice _ -> Ok event
        | Gambol.Shared.EventBody.Load _ ->
            Error "Load Events are mailbox-generated"
        | Gambol.Shared.EventBody.ActorStart _
        | Gambol.Shared.EventBody.ActorStop _
        | Gambol.Shared.EventBody.Cancel _ ->
            Error "Actor lifecycle Events are mailbox-generated"

    let private withConfirmedOps
        (event: Ev)
        (accepted: CoreChangesAccepted)
        : Ev =
        let confirmed =
            accepted.events
            |> List.tryFind (fun stored ->
                stored.submissionId = event.submissionId)
        let confirmedOps =
            confirmed
            |> Option.bind Ev.ops
        match confirmedOps, event.body with
        | Some ops, Gambol.Shared.EventBody.Change _ ->
            { event with
                body = Gambol.Shared.EventBody.Change ops }
        | Some ops, Gambol.Shared.EventBody.Undo(target, _) ->
            { event with
                body = Gambol.Shared.EventBody.Undo(target, ops) }
        | Some ops, Gambol.Shared.EventBody.Redo(target, _) ->
            { event with
                body = Gambol.Shared.EventBody.Redo(target, ops) }
        | _ -> event

    let private prepare
        (context: Context)
        (caller: Caller)
        (event: Ev)
        =
        let withCallerAuthority =
            { event with
                id = EventLog.nextId context.eventLog.Value
                authority = eventAuthority caller.authority }
        completeAction context withCallerAuthority

    let internal persistedNode (graph: Graph) (nodeId: NodeId) =
        match Map.tryFind nodeId graph.nodes with
        | Some node when
            node.documentState.persistState = PersistState.Persisted ->
            true
        | _ -> false

    let internal writeAxis
        (persist: PersistHandlers)
        (notice: Gambol.Shared.Notice)
        (mutate: Graph -> Result<Graph, string>)
        : Result<unit, string> =
        match persist.getState () with
        | Error err -> Error err
        | Ok state ->
            match mutate state.graph with
            | Error err -> Error err
            | Ok graph ->
                persist.replaceGraph notice graph
                Ok ()

    let private needsUnpersistedMark (node: Node) (version: EventId) =
        node.documentState.persistState = PersistState.Persisted
        || EventId.value node.documentState.changedVersion
            < EventId.value version

    let private markForPersist
        (persist: PersistHandlers)
        (version: EventId)
        (nodeId: NodeId)
        : Result<unit, string> =
        match persist.getState () with
        | Error err -> Error err
        | Ok state ->
            match Map.tryFind nodeId state.graph.nodes with
            | Some node when needsUnpersistedMark node version ->
                writeAxis
                    persist
                    (Gambol.Shared.Notice.NodePersisting nodeId)
                    (fun graph ->
                        Ok (
                            GraphMutate.setUnpersistedAt
                                nodeId version graph))
            | _ -> Ok ()

    let private pushChangeSpecials
        (context: Context)
        (completed: Ev)
        (accepted: CoreChangesAccepted)
        : Result<unit, string> =
        match completed.body with
        | Gambol.Shared.EventBody.Change _
        | Gambol.Shared.EventBody.Undo _
        | Gambol.Shared.EventBody.Redo _ ->
            match context.persist.getState () with
            | Error err -> Error err
            | Ok state ->
                let ops =
                    withConfirmedOps completed accepted
                    |> Ev.ops
                    |> Option.defaultValue []
                let ids =
                    DocumentOpImpact.owningSpecials state.graph ops
                let marked =
                    ids
                    |> List.fold
                        (fun acc nodeId ->
                            acc
                            |> Result.bind (fun () ->
                                markForPersist
                                    context.persist
                                    completed.id
                                    nodeId))
                        (Ok ())
                marked
                |> Result.map (fun () ->
                    List.iter context.persist.pushPersist ids)
        | _ -> Ok ()

    let private applyPostedNotice
        (context: Context)
        (notice: Gambol.Shared.Notice)
        (event: Ev)
        =
        match context.persist.getState () with
        | Error err -> Error err
        | Ok state ->
            match Ev.apply event state with
            | ApplyResult.Invalid (_, msg) -> Error msg
            | ApplyResult.Changed next
            | ApplyResult.Unchanged next ->
                context.persist.replaceGraph notice next.graph
                Ok None

    let private persist (context: Context) (completed: Ev) (graphOnly: bool) =
        match completed.body with
        | Gambol.Shared.EventBody.Notice notice ->
            applyPostedNotice context notice completed
        | _ ->
            match Ev.ops completed with
            | None -> Ok None
            | Some _ ->
                match context.persist.applyEvent completed graphOnly with
                | Error error -> Error error
                | Ok accepted ->
                    if graphOnly then
                        Ok(Some accepted)
                    else
                        pushChangeSpecials context completed accepted
                        |> Result.map (fun () -> Some accepted)

    let private store context accepted completed =
        let confirmed =
            match accepted with
            | None -> completed
            | Some result -> withConfirmedOps completed result
        commit context confirmed

    let private persistNew
        (context: Context)
        (caller: Caller)
        (event: Ev)
        (graphOnly: bool)
        =
        if event.id <> EventId.zero then
            Error "posted EventId must be zero"
        else
            catchUpNextId context
            match prepare context caller event with
            | Error error -> Error error
            | Ok completed ->
                match persist context completed graphOnly with
                | Error error -> Error error
                | Ok accepted ->
                    match store context accepted completed with
                    | Error error -> Error error
                    | Ok stored -> Ok(stored, accepted)

    let postEvent
        (context: Context)
        (caller: Caller)
        (event: Ev)
        (graphOnly: bool)
        : Result<Ev * CoreChangesAccepted option, string> =
        match tryStored context event.submissionId with
        | Some existing -> Ok(existing, None)
        | None -> persistNew context caller event graphOnly
