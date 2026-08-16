namespace Gambol.Shared

type HistoryRecord =
    { recordId: int
      commandName: string
      applied: Change }


type ClientHistory =
    private
        { past: HistoryRecord list
          future: HistoryRecord list
          nextRecordId: int }


[<RequireQualifiedAccess>]
module ClientHistory =
    let clear () : ClientHistory =
        { past = []
          future = []
          nextRecordId = 0 }

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

    let tryPeekUndoName (history: ClientHistory) : string option =
        match history.past with
        | [] -> None
        | historyRecord :: _ -> Some historyRecord.commandName

    let tryPeekRedoName (history: ClientHistory) : string option =
        match history.future with
        | [] -> None
        | historyRecord :: _ -> Some historyRecord.commandName
