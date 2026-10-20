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
          eventLog: EventLog ref }

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

    let private completeAction (event: Ev) =
        match event.body with
        | Gambol.Shared.EventBody.Change _ -> Ok event
        | Gambol.Shared.EventBody.Undo _
        | Gambol.Shared.EventBody.Redo _ ->
            Error "Undo and Redo are PlaceOrder"
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
        completeAction withCallerAuthority

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

    let private commandText (name: string option) =
        match name with
        | None -> ""
        | Some text -> text

    let private orderBody (order: Order) =
        match order with
        | Order.Undo target ->
            Some(Gambol.Shared.EventBody.Undo(target, None))
        | Order.Redo target ->
            Some(Gambol.Shared.EventBody.Redo(target, None))
        | _ -> None

    let private resolvedOps (log: Ev list) (order: Order) =
        let targetAnd resolve target =
            match log |> List.tryFind (fun event -> event.id = target) with
            | None -> Error "target Event not found"
            | Some _ ->
                match resolve log target with
                | None -> Error "target Event has no inverse Ops"
                | Some ops -> Ok ops
        match order with
        | Order.Undo target -> targetAnd UndoTarget.inverseOfTarget target
        | Order.Redo target -> targetAnd UndoTarget.forwardOfTarget target
        | _ -> Error "unsupported"

    let private carriedBody (body: Gambol.Shared.EventBody) (ops: Op list) =
        match body with
        | Gambol.Shared.EventBody.Undo(target, _) ->
            Gambol.Shared.EventBody.Undo(target, Some ops)
        | Gambol.Shared.EventBody.Redo(target, _) ->
            Gambol.Shared.EventBody.Redo(target, Some ops)
        | other -> other

    let private draftUndo
        (caller: Caller)
        (submissionId: System.Guid)
        (commandName: string option)
        (body: Gambol.Shared.EventBody)
        : Ev =
        { id = EventId.zero
          submissionId = submissionId
          authority = eventAuthority caller.authority
          commandName = commandText commandName
          body = body }

    let private writeUndo
        (context: Context)
        (caller: Caller)
        (submissionId: System.Guid)
        (commandName: string option)
        (body: Gambol.Shared.EventBody)
        (ops: Op list)
        =
        catchUpNextId context
        let prepared =
            { draftUndo caller submissionId commandName body with
                id = EventLog.nextId context.eventLog.Value }
        let forApply = { prepared with body = carriedBody body ops }
        match persist context forApply false with
        | Error error -> Error error
        | Ok accepted ->
            match commit context prepared with
            | Error error -> Error error
            | Ok stored ->
                let accepted =
                    accepted
                    |> Option.map (fun result ->
                        { result with events = [ stored ] })
                Ok(stored, accepted)

    /// Place Undo or Redo of a target. The stored body has that target and no ops.
    let acceptUndoRedo
        (context: Context)
        (caller: Caller)
        (submissionId: System.Guid)
        (postedId: EventId)
        (commandName: string option)
        (order: Order)
        : Result<Ev * CoreChangesAccepted option, string> =
        match tryStored context submissionId with
        | Some existing -> Ok(existing, None)
        | None when postedId <> EventId.zero ->
            Error "posted EventId must be zero"
        | None ->
            match orderBody order with
            | None -> Error "unsupported"
            | Some body ->
                match resolvedOps context.eventLog.Value.events order with
                | Error error -> Error error
                | Ok ops ->
                    writeUndo
                        context caller submissionId commandName body ops

    let postEvent
        (context: Context)
        (caller: Caller)
        (event: Ev)
        (graphOnly: bool)
        : Result<Ev * CoreChangesAccepted option, string> =
        match tryStored context event.submissionId with
        | Some existing -> Ok(existing, None)
        | None -> persistNew context caller event graphOnly
