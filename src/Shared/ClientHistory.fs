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
          nextEventId: int }


[<RequireQualifiedAccess>]
module ClientHistory =
    let clear () : ClientHistory =
        { past = []
          future = []
          nextRecordId = 0
          eventPast = []
          eventFuture = []
          nextEventId = 0 }

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
            nextEventId = max history.nextEventId (n + 1) }

    let undoEvent
        (history: ClientHistory)
        : (Event * ClientHistory) option =
        match history.eventPast with
        | [] -> None
        | action :: remainingPast ->
            let inverse =
                Event.inverseOps action.event |> Option.defaultValue []
            let undoEvent =
                { id = EventId history.nextEventId
                  submissionId = System.Guid.NewGuid()
                  authority = action.event.authority
                  commandName = action.event.commandName
                  body = EventBody.Undo(Event.id action.event, inverse) }
            let moved = { action with event = undoEvent }
            let nextHistory =
                { history with
                    eventPast = remainingPast
                    eventFuture = moved :: history.eventFuture
                    nextEventId = history.nextEventId + 1 }
            Some(undoEvent, nextHistory)

    let redoEvent
        (history: ClientHistory)
        : (Event * ClientHistory) option =
        match history.eventFuture with
        | [] -> None
        | action :: remainingFuture ->
            let inverse =
                Event.inverseOps action.event |> Option.defaultValue []
            let redoEvent =
                { id = EventId history.nextEventId
                  submissionId = System.Guid.NewGuid()
                  authority = action.event.authority
                  commandName = action.event.commandName
                  body = EventBody.Redo(Event.id action.event, inverse) }
            let moved = { action with event = redoEvent }
            let nextHistory =
                { history with
                    eventPast = moved :: history.eventPast
                    eventFuture = remainingFuture
                    nextEventId = history.nextEventId + 1 }
            Some(redoEvent, nextHistory)

    let tryPeekUndoName (history: ClientHistory) : string option =
        match history.eventPast with
        | action :: _ -> Some action.commandName
        | [] ->
            match history.past with
            | [] -> None
            | historyRecord :: _ -> Some historyRecord.commandName

    let tryPeekRedoName (history: ClientHistory) : string option =
        match history.eventFuture with
        | action :: _ -> Some action.commandName
        | [] ->
            match history.future with
            | [] -> None
            | historyRecord :: _ -> Some historyRecord.commandName
