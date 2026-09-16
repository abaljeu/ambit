namespace Gambol.Server

open System
open Gambol.Shared

[<RequireQualifiedAccess>]
module internal CoreEventDispatch =

    type Event = Gambol.Shared.Events.Event
    type EventLog = Gambol.Shared.Events.EventLog
    module EventLog = Gambol.Shared.Events.EventLog

    type Context =
        { admit: Caller -> Result<unit, string>
          persist: PersistHandlers
          eventLog: EventLog ref }

    let private eventAuthority (Authority name) =
        Gambol.Shared.Events.Authority name

    /// submissionId is Guid dedup (event-abstraction): replay returns the stored Event.
    let private commit (context: Context) (event: Event) =
        match
            context.eventLog.Value.events
            |> List.tryFind (fun e -> e.submissionId = event.submissionId)
        with
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
        (body: Gambol.Shared.Events.EventBody)
        : Event =
        { id = Gambol.Shared.Events.EventId 0
          submissionId = Guid.NewGuid()
          authority = eventAuthority caller.authority
          commandName = ""
          body = body }

    let actorStart (context: Context) caller request =
        lifecycleEvent
            caller
            (Gambol.Shared.Events.EventBody.ActorStart request)
        |> commit context
        |> Result.map ignore

    let actorStop (context: Context) caller focusId result =
        let sharedResult =
            match result with
            | ActorSucceeded ->
                Gambol.Shared.Events.ActorResult.ActorSucceeded
            | ActorFailed ->
                Gambol.Shared.Events.ActorResult.ActorFailed
        lifecycleEvent
            caller
            (Gambol.Shared.Events.EventBody.ActorStop(
                focusId,
                sharedResult))
        |> commit context
        |> Result.map ignore

    let private completeAction (eventLog: EventLog) (event: Event) =
        match event.body with
        | Gambol.Shared.Events.EventBody.Undo(target, [])
        | Gambol.Shared.Events.EventBody.Redo(target, []) ->
            match EventLog.tryFind target eventLog with
            | None -> Error "target Event not found"
            | Some targetEvent ->
                match Gambol.Shared.Events.Event.inverseOps targetEvent with
                | None -> Error "target Event has no inverse Ops"
                | Some ops ->
                    let body =
                        match event.body with
                        | Gambol.Shared.Events.EventBody.Undo _ ->
                            Gambol.Shared.Events.EventBody.Undo(target, ops)
                        | _ ->
                            Gambol.Shared.Events.EventBody.Redo(target, ops)
                    Ok { event with body = body }
        | Gambol.Shared.Events.EventBody.Change _
        | Gambol.Shared.Events.EventBody.Undo _
        | Gambol.Shared.Events.EventBody.Redo _ -> Ok event
        | Gambol.Shared.Events.EventBody.ActorStart _
        | Gambol.Shared.Events.EventBody.ActorStop _ ->
            Error "Actor lifecycle Events are mailbox-generated"

    let private withConfirmedOps
        (event: Event)
        (accepted: CoreChangesAccepted)
        : Event =
        let confirmed =
            accepted.events
            |> List.tryFind (fun stored ->
                stored.submissionId = event.submissionId)
        let confirmedOps =
            confirmed
            |> Option.bind Gambol.Shared.Events.Event.ops
        match confirmedOps, event.body with
        | Some ops, Gambol.Shared.Events.EventBody.Change _ ->
            { event with
                body = Gambol.Shared.Events.EventBody.Change ops }
        | Some ops, Gambol.Shared.Events.EventBody.Undo(target, _) ->
            { event with
                body = Gambol.Shared.Events.EventBody.Undo(target, ops) }
        | Some ops, Gambol.Shared.Events.EventBody.Redo(target, _) ->
            { event with
                body = Gambol.Shared.Events.EventBody.Redo(target, ops) }
        | _ -> event

    let private prepare
        (context: Context)
        (caller: Caller)
        (event: Event)
        =
        let admitted =
            { event with
                id = EventLog.nextId context.eventLog.Value
                authority = eventAuthority caller.authority }
        completeAction context.eventLog.Value admitted

    let private persist (context: Context) (completed: Event) (graphOnly: bool) =
        match Gambol.Shared.Events.Event.ops completed with
        | None -> Ok None
        | Some ops ->
            match context.persist.getRevision () with
            | Error error -> Error error
            | Ok revision ->
                let change =
                    { id = revision.Value
                      changeId = completed.submissionId
                      ops = ops }
                if graphOnly then
                    context.persist.postGraphOnlyChange [ change ]
                    |> Result.map Some
                else
                    context.persist.postChange [ change ]
                    |> Result.map Some

    let private store context accepted completed =
        let confirmed =
            match accepted with
            | None -> completed
            | Some result -> withConfirmedOps completed result
        commit context confirmed

    let postEvent
        (context: Context)
        (caller: Caller)
        (event: Event)
        (graphOnly: bool)
        : Result<Event * CoreChangesAccepted option, string> =
        match context.admit caller with
        | Error error -> Error error
        | Ok () ->
            match prepare context caller event with
            | Error error -> Error error
            | Ok completed ->
                match persist context completed graphOnly with
                | Error error -> Error error
                | Ok accepted ->
                    match store context accepted completed with
                    | Error error -> Error error
                    | Ok stored -> Ok(stored, accepted)

    /// Wire/transport batches loop singular postEvent; preview keeps
    /// all-or-nothing Reject before any commit (arch: postEvent door).
    let previewEvents
        (knownSubmissionIds: Set<Guid>)
        (state: State)
        (events: Event list)
        : Result<unit, string> =
        let step (acc: Result<State, string>) (event: Event) =
            match acc with
            | Error error -> Error error
            | Ok s when Set.contains event.submissionId knownSubmissionIds ->
                Ok s
            | Ok s ->
                match Gambol.Shared.Events.Event.apply event s with
                | ApplyResult.Invalid (_, msg) -> Error msg
                | ApplyResult.Unchanged _ ->
                    Error "Unchanged submission is rejected."
                | ApplyResult.Changed s' ->
                    Ok {
                        s' with
                            revision =
                                Revision(s.revision.Value + 1)
                    }
        List.fold step (Ok state) events
        |> Result.map ignore
