namespace Gambol.Server

open System
open Gambol.Shared

[<RequireQualifiedAccess>]
module internal CoreEventDispatch =

    type Ev = Gambol.Shared.Ev
    type EventLog = Gambol.Shared.EventLog
    module EventLog = Gambol.Shared.EventLog

    type Context =
        { admit: Caller -> Result<unit, string>
          persist: PersistHandlers
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

    /// submissionId is Guid dedup (event-abstraction): replay returns the stored Ev.
    let private commit (context: Context) (event: Ev) =
        catchUpNextId context
        match tryStored context event.submissionId with
        | Some existing -> Ok existing
        | None ->
            let stored =
                { event with id = EventLog.nextId context.eventLog.Value }
            match context.persist.appendEvent stored with
            | Error error -> Error error
            | Ok () ->
                context.eventLog.Value <-
                    EventLog.append stored context.eventLog.Value
                Ok stored

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
            | ActorFailed ->
                Gambol.Shared.ActorResult.ActorFailed
        lifecycleEvent
            caller
            (Gambol.Shared.EventBody.ActorStop(
                focusId,
                sharedResult))
        |> commit context
        |> Result.map ignore

    let private completeAction (eventLog: EventLog) (event: Ev) =
        match event.body with
        | Gambol.Shared.EventBody.Undo(target, [])
        | Gambol.Shared.EventBody.Redo(target, []) ->
            match EventLog.tryFind target eventLog with
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
        | Gambol.Shared.EventBody.Redo _ -> Ok event
        | Gambol.Shared.EventBody.ActorStart _
        | Gambol.Shared.EventBody.ActorStop _ ->
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
        let admitted =
            { event with
                id = EventLog.nextId context.eventLog.Value
                authority = eventAuthority caller.authority }
        completeAction context.eventLog.Value admitted

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

    let postEvent
        (context: Context)
        (caller: Caller)
        (event: Ev)
        (graphOnly: bool)
        : Result<Ev * CoreChangesAccepted option, string> =
        match context.admit caller with
        | Error error -> Error error
        | Ok () ->
            match tryStored context event.submissionId with
            | Some existing -> Ok(existing, None)
            | None -> persistNew context caller event graphOnly
