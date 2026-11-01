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

    let private undoRedoOf (body: Gambol.Shared.EventBody) =
        match body with
        | Gambol.Shared.EventBody.Undo(target, ops)
        | Gambol.Shared.EventBody.Redo(target, ops) ->
            Some(target, ops)
        | _ -> None

    let private fillFromTarget
        (context: Context)
        (event: Ev)
        (target: EventId)
        =
        match EventLog.tryFind target context.eventLog.Value with
        | None -> Error "target Event not found"
        | Some targetEvent ->
            match Ev.inverseOps targetEvent with
            | None -> Error "target Event has no inverse Ops"
            | Some ops -> Ok(Ev.withOps event ops)

    let private completeUndoRedo
        (context: Context)
        (event: Ev)
        (target: EventId)
        (ops: Op list)
        =
        if targetBeforeRestart context target then
            Error "target Event is from before the restart"
        elif List.isEmpty ops then
            fillFromTarget context event target
        else
            Ok event

    /// Order.Change passes through. Other Order cases stay refused
    /// until each one leaves its old EventBody arm.
    let private completeOther (event: Ev) =
        match event.body with
        | Gambol.Shared.EventBody.Order (Order.Change _) -> Ok event
        | Gambol.Shared.EventBody.Notice _ ->
            Error "Notice is not a client event type"
        | Gambol.Shared.EventBody.Load _ ->
            Error "Load Events are mailbox-generated"
        | Gambol.Shared.EventBody.ActorStart _
        | Gambol.Shared.EventBody.ActorStop _
        | Gambol.Shared.EventBody.Cancel _ ->
            Error "Actor lifecycle Events are mailbox-generated"
        | Gambol.Shared.EventBody.Order _ ->
            Error "Order Events are mailbox-generated"

    let private completeAction (context: Context) (event: Ev) =
        match undoRedoOf event.body with
        | Some(target, ops) ->
            completeUndoRedo context event target ops
        | None -> completeOther event

    let private withConfirmedOps
        (event: Ev)
        (accepted: CoreChangesAccepted)
        : Ev =
        let confirmed =
            accepted.events
            |> List.tryFind (fun stored ->
                stored.submissionId = event.submissionId)
        match confirmed |> Option.bind Ev.ops with
        | Some ops -> Ev.withOps event ops
        | None -> event

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
        (context: Context)
        (version: EventId)
        (nodeId: NodeId)
        : Result<unit, string> =
        match context.persist.getState () with
        | Error err -> Error err
        | Ok state ->
            match Map.tryFind nodeId state.graph.nodes with
            | Some node when needsUnpersistedMark node version ->
                let notice =
                    Gambol.Shared.Notice.NodeUnpersisted nodeId
                match logNotice context notice with
                | Error err -> Error err
                | Ok _ ->
                    writeAxis
                        context.persist
                        notice
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
        | Gambol.Shared.EventBody.Order (Order.Change _)
        | Gambol.Shared.EventBody.Order (Order.Undo _)
        | Gambol.Shared.EventBody.Order (Order.Redo _) ->
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
                                    context
                                    completed.id
                                    nodeId))
                        (Ok ())
                marked
                |> Result.map (fun () ->
                    List.iter context.persist.pushPersist ids)
        | _ -> Ok ()

    let private persist (context: Context) (completed: Ev) (graphOnly: bool) =
        match Ev.ops completed with
        | None -> Ok None
        | Some _ ->
            context.persist.applyEvent completed graphOnly
            |> Result.map Some

    let private store context accepted completed =
        let confirmed =
            match accepted with
            | None -> completed
            | Some result -> withConfirmedOps completed result
        commit context confirmed

    /// The Change is stored first. NodeUnpersisted then takes the Notice
    /// path: logNotice, then the axis write.
    let private markAcceptedSpecials
        (context: Context)
        (graphOnly: bool)
        (completed: Ev)
        (accepted: CoreChangesAccepted option)
        =
        if graphOnly then
            Ok ()
        else
            match accepted with
            | None -> Ok ()
            | Some result ->
                pushChangeSpecials context completed result

    let private withParseOrigin (graphOnly: bool) (event: Ev) =
        if not graphOnly then
            event
        else
            match event.body with
            | EventBody.Order (Order.Change (ops, _)) ->
                { event with
                    body =
                        EventBody.Order(
                            Order.Change(ops, ChangeOrigin.Parse)) }
            | _ -> event

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
                let completed = withParseOrigin graphOnly completed
                match persist context completed graphOnly with
                | Error error -> Error error
                | Ok accepted ->
                    match store context accepted completed with
                    | Error error -> Error error
                    | Ok stored ->
                        markAcceptedSpecials
                            context graphOnly completed accepted
                        |> Result.map (fun () -> stored, accepted)

    let postEvent
        (context: Context)
        (caller: Caller)
        (event: Ev)
        (graphOnly: bool)
        : Result<Ev * CoreChangesAccepted option, string> =
        match tryStored context event.submissionId with
        | Some existing -> Ok(existing, None)
        | None -> persistNew context caller event graphOnly
