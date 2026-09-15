namespace Gambol.Shared

open Gambol.Shared.Events

type ClientHistory =
    private
        { eventPast: Event list
          eventFuture: Event list
          nextEventId: EventId }

[<RequireQualifiedAccess>]
module ClientHistory =
    let clear () : ClientHistory =
        { eventPast = []
          eventFuture = []
          nextEventId = EventId.zero }

    let private tryTakeAction
        (stack: Event list)
        : (Event list * Event) option =
        let rec walk skipped remaining =
            match remaining with
            | [] -> None
            | action :: rest when Event.isAction action ->
                let kept = List.fold (fun acc x -> x :: acc) rest skipped
                Some(kept, action)
            | actor :: rest ->
                walk (actor :: skipped) rest
        walk [] stack

    let private invertAs
        (wrap: EventId * Op list -> EventBody)
        (action: Event)
        (id: EventId)
        : Event =
        let inverse =
            Event.inverseOps action |> Option.defaultValue []
        { id = id
          submissionId = System.Guid.NewGuid()
          authority = action.authority
          commandName = action.commandName
          body = wrap (Event.id action, inverse) }

    let recordEvent
        (commandName: string)
        (event: Event)
        (history: ClientHistory)
        : ClientHistory =
        let event = { event with commandName = commandName }
        let foldedPast =
            List.foldBack
                (fun futureEvent past -> futureEvent :: past)
                history.eventFuture
                history.eventPast
        { history with
            eventPast = event :: foldedPast
            eventFuture = []
            nextEventId =
                EventId.max history.nextEventId (EventId.next event.id) }

    let undoEvent
        (history: ClientHistory)
        : (Event * ClientHistory) option =
        match tryTakeAction history.eventPast with
        | None -> None
        | Some (remainingPast, action) ->
            let produced = invertAs EventBody.Undo action history.nextEventId
            let nextHistory =
                { history with
                    eventPast = remainingPast
                    eventFuture = produced :: history.eventFuture
                    nextEventId = EventId.next history.nextEventId }
            Some(produced, nextHistory)

    let redoEvent
        (history: ClientHistory)
        : (Event * ClientHistory) option =
        match tryTakeAction history.eventFuture with
        | None -> None
        | Some (remainingFuture, action) ->
            let produced = invertAs EventBody.Redo action history.nextEventId
            let nextHistory =
                { history with
                    eventPast = produced :: history.eventPast
                    eventFuture = remainingFuture
                    nextEventId = EventId.next history.nextEventId }
            Some(produced, nextHistory)

    let private tryPeekActionName (stack: Event list) : string option =
        let rec walk remaining =
            match remaining with
            | [] -> None
            | event :: _ when Event.isAction event -> Some event.commandName
            | _ :: rest -> walk rest
        walk stack

    let tryPeekUndoName (history: ClientHistory) : string option =
        tryPeekActionName history.eventPast

    let tryPeekRedoName (history: ClientHistory) : string option =
        tryPeekActionName history.eventFuture

    let private unwrap (EventId n) = n

    let private callerRecordId (event: Event) : int =
        unwrap (Event.target event |> Option.defaultValue event.id)

    let private asChange
        (Revision rev)
        (changeId: System.Guid)
        (event: Event)
        : Change =
        { id = rev
          changeId = changeId
          ops = Event.ops event |> Option.defaultValue [] }

    let record
        (commandName: string)
        (change: Change)
        (history: ClientHistory)
        : ClientHistory * int =
        let event =
            { id = history.nextEventId
              submissionId = change.changeId
              authority = Authority "Browser"
              commandName = commandName
              body = EventBody.Change change.ops }
        recordEvent commandName event history, unwrap event.id

    let undo
        (baseRevision: Revision)
        (changeId: System.Guid)
        (history: ClientHistory)
        : (Change * string * ClientHistory * int) option =
        match undoEvent history with
        | None -> None
        | Some (produced, nextHistory) ->
            Some(
                asChange baseRevision changeId produced,
                produced.commandName,
                nextHistory,
                callerRecordId produced)

    let redo
        (baseRevision: Revision)
        (changeId: System.Guid)
        (history: ClientHistory)
        : (Change * string * ClientHistory * int) option =
        match redoEvent history with
        | None -> None
        | Some (produced, nextHistory) ->
            Some(
                asChange baseRevision changeId produced,
                produced.commandName,
                nextHistory,
                callerRecordId produced)
