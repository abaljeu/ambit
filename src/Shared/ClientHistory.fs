namespace Gambol.Shared

open Gambol.Shared.Events

type HistoryRecord =
    { recordId: int
      commandName: string
      applied: Change }

type EventAction =
    { commandName: string
      event: Event }

type ClientHistory =
    private
        { past: HistoryRecord list
          future: HistoryRecord list
          nextRecordId: int
          eventPast: EventAction list
          eventFuture: EventAction list
          nextEventId: Gambol.Shared.Events.EventId }


[<RequireQualifiedAccess>]
module ClientHistory =
    let clear () : ClientHistory =
        { past = []
          future = []
          nextRecordId = 0
          eventPast = []
          eventFuture = []
          nextEventId = EventId 0 }

    let record
        (commandName: string)
        (change: Change)
        (history: ClientHistory)
        : ClientHistory * int =
        let historyRecord =
            { recordId = history.nextRecordId
              commandName = commandName
              applied = change }
        let foldedPast =
            List.foldBack
                (fun futureRecord past -> futureRecord :: past)
                history.future
                history.past
        let nextHistory =
            { history with
                past = historyRecord :: foldedPast
                future = []
                nextRecordId = history.nextRecordId + 1 }
        nextHistory,
        historyRecord.recordId

    let undo
        (baseRevision: Revision)
        (changeId: System.Guid)
        (history: ClientHistory)
        : (Change * string * ClientHistory * int) option =
        match history.past with
        | [] -> None
        | historyRecord :: remainingPast ->
            let inverse =
                Change.inverse baseRevision changeId historyRecord.applied
            let movedRecord = { historyRecord with applied = inverse }
            let nextHistory =
                { history with
                    past = remainingPast
                    future = movedRecord :: history.future }
            Some(
                inverse,
                historyRecord.commandName,
                nextHistory,
                historyRecord.recordId)

    let redo
        (baseRevision: Revision)
        (changeId: System.Guid)
        (history: ClientHistory)
        : (Change * string * ClientHistory * int) option =
        match history.future with
        | [] -> None
        | historyRecord :: remainingFuture ->
            let inverse =
                Change.inverse baseRevision changeId historyRecord.applied
            let movedRecord = { historyRecord with applied = inverse }
            let nextHistory =
                { history with
                    past = movedRecord :: history.past
                    future = remainingFuture }
            Some(
                inverse,
                historyRecord.commandName,
                nextHistory,
                historyRecord.recordId)

    let private eventHasOps (event: Event) : bool =
        match event.body with
        | EventBody.Change _
        | EventBody.Undo _
        | EventBody.Redo _ -> true
        | EventBody.ActorStart _
        | EventBody.ActorStop _ -> false

    let private tryTakeAction
        (stack: EventAction list)
        : (EventAction list * EventAction) option =
        let rec walk skipped remaining =
            match remaining with
            | [] -> None
            | action :: rest when eventHasOps action.event ->
                let kept = List.fold (fun acc x -> x :: acc) rest skipped
                Some(kept, action)
            | actor :: rest ->
                walk (actor :: skipped) rest
        walk [] stack

    let private bumpEventId (EventId n) = EventId(n + 1)

    let private maxEventId (EventId a) (EventId b) = EventId(max a b)

    let private invertAs
        (wrap: EventId * Op list -> EventBody)
        (action: EventAction)
        (id: EventId)
        : Event =
        let inverse =
            Event.inverseOps action.event |> Option.defaultValue []
        { id = id
          submissionId = System.Guid.NewGuid()
          authority = action.event.authority
          commandName = action.event.commandName
          body = wrap (Event.id action.event, inverse) }

    let recordEvent
        (commandName: string)
        (event: Event)
        (history: ClientHistory)
        : ClientHistory =
        let event = { event with commandName = commandName }
        let action = { commandName = commandName; event = event }
        let foldedPast =
            List.foldBack
                (fun futureAction past -> futureAction :: past)
                history.eventFuture
                history.eventPast
        let (EventId n) = event.id
        { history with
            eventPast = action :: foldedPast
            eventFuture = []
            nextEventId = maxEventId history.nextEventId (EventId(n + 1)) }

    let undoEvent
        (history: ClientHistory)
        : (Event * ClientHistory) option =
        match tryTakeAction history.eventPast with
        | None -> None
        | Some (remainingPast, action) ->
            let produced = invertAs EventBody.Undo action history.nextEventId
            let moved = { action with event = produced }
            let nextHistory =
                { history with
                    eventPast = remainingPast
                    eventFuture = moved :: history.eventFuture
                    nextEventId = bumpEventId history.nextEventId }
            Some(produced, nextHistory)

    let redoEvent
        (history: ClientHistory)
        : (Event * ClientHistory) option =
        match tryTakeAction history.eventFuture with
        | None -> None
        | Some (remainingFuture, action) ->
            let produced = invertAs EventBody.Redo action history.nextEventId
            let moved = { action with event = produced }
            let nextHistory =
                { history with
                    eventPast = moved :: history.eventPast
                    eventFuture = remainingFuture
                    nextEventId = bumpEventId history.nextEventId }
            Some(produced, nextHistory)

    let private tryPeekActionName (stack: EventAction list) : string option =
        let rec walk remaining =
            match remaining with
            | [] -> None
            | action :: _ when eventHasOps action.event ->
                Some action.commandName
            | _ :: rest -> walk rest
        walk stack

    let private tryPeekChangeName
        (stack: HistoryRecord list)
        : string option =
        match stack with
        | [] -> None
        | historyRecord :: _ -> Some historyRecord.commandName

    let tryPeekUndoName (history: ClientHistory) : string option =
        match tryPeekActionName history.eventPast with
        | Some name -> Some name
        | None -> tryPeekChangeName history.past

    let tryPeekRedoName (history: ClientHistory) : string option =
        match tryPeekActionName history.eventFuture with
        | Some name -> Some name
        | None -> tryPeekChangeName history.future
