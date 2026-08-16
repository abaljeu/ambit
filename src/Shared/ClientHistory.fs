namespace Gambol.Shared

type HistoryRecord =
    { recordId: int
      commandName: string
      applied: Change }


[<RequireQualifiedAccess>]
type PendingTransitionKind =
    | Normal
    | Undo
    | Redo


type PendingTransition =
    { recordId: int
      submittedChangeId: System.Guid
      kind: PendingTransitionKind }


type ClientHistory =
    private
        { past: HistoryRecord list
          future: HistoryRecord list
          nextRecordId: int
          pendingByRecord:
            Map<int, (PendingTransition * Change) list> }


[<RequireQualifiedAccess>]
module ClientHistory =
    let clear () : ClientHistory =
        { past = []
          future = []
          nextRecordId = 0
          pendingByRecord = Map.empty }

    let private appendPending
        (transition: PendingTransition)
        (submitted: Change)
        (history: ClientHistory)
        : ClientHistory =
        let pending =
            history.pendingByRecord
            |> Map.tryFind transition.recordId
            |> Option.defaultValue []
        { history with
            pendingByRecord =
                Map.add
                    transition.recordId
                    (pending @ [ transition, submitted ])
                    history.pendingByRecord }

    let record
        (commandName: string)
        (change: Change)
        (history: ClientHistory)
        : ClientHistory * PendingTransition =
        let historyRecord =
            { recordId = history.nextRecordId
              commandName = commandName
              applied = change }
        let transition =
            { recordId = historyRecord.recordId
              submittedChangeId = change.changeId
              kind = PendingTransitionKind.Normal }
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
            |> appendPending transition change
        nextHistory,
        transition

    let undo
        (baseRevision: Revision)
        (changeId: System.Guid)
        (history: ClientHistory)
        : (Change * string * ClientHistory * PendingTransition) option =
        match history.past with
        | [] -> None
        | historyRecord :: remainingPast ->
            let inverse =
                Change.inverse baseRevision changeId historyRecord.applied
            let movedRecord = { historyRecord with applied = inverse }
            let transition =
                { recordId = historyRecord.recordId
                  submittedChangeId = changeId
                  kind = PendingTransitionKind.Undo }
            let nextHistory =
                { history with
                    past = remainingPast
                    future = movedRecord :: history.future }
                |> appendPending transition inverse
            Some(
                inverse,
                historyRecord.commandName,
                nextHistory,
                transition)

    let redo
        (baseRevision: Revision)
        (changeId: System.Guid)
        (history: ClientHistory)
        : (Change * string * ClientHistory * PendingTransition) option =
        match history.future with
        | [] -> None
        | historyRecord :: remainingFuture ->
            let inverse =
                Change.inverse baseRevision changeId historyRecord.applied
            let movedRecord = { historyRecord with applied = inverse }
            let transition =
                { recordId = historyRecord.recordId
                  submittedChangeId = changeId
                  kind = PendingTransitionKind.Redo }
            let nextHistory =
                { history with
                    past = movedRecord :: history.past
                    future = remainingFuture }
                |> appendPending transition inverse
            Some(
                inverse,
                historyRecord.commandName,
                nextHistory,
                transition)

    let private tryFindRecord
        (recordId: int)
        (history: ClientHistory)
        : HistoryRecord option =
        history.past
        |> List.tryFind (fun historyRecord ->
            historyRecord.recordId = recordId)
        |> Option.orElseWith (fun () ->
            history.future
            |> List.tryFind (fun historyRecord ->
                historyRecord.recordId = recordId))

    let private amendRecord
        (recordId: int)
        (applied: Change)
        (history: ClientHistory)
        : ClientHistory =
        let amend (historyRecord: HistoryRecord) =
            if historyRecord.recordId = recordId then
                { historyRecord with applied = applied }
            else
                historyRecord
        { history with
            past = history.past |> List.map amend
            future = history.future |> List.map amend }

    let rec private isExactPrefix (prefix: Op list) (complete: Op list) : bool =
        match prefix, complete with
        | [], _ -> true
        | expected :: restPrefix, actual :: restComplete
            when expected = actual ->
            isExactPrefix restPrefix restComplete
        | _ -> false

    let private validateConfirmation
        (transition: PendingTransition)
        (confirmed: Change)
        (history: ClientHistory)
        : Result<(PendingTransition * Change) list, string> =
        match
            tryFindRecord transition.recordId history,
            Map.tryFind transition.recordId history.pendingByRecord
        with
        | None, _ ->
            Error "confirmation record identity was not found"
        | _, None
        | _, Some [] ->
            Error "confirmation transition is not pending"
        | Some _, Some ((expected, submitted) :: remaining) ->
            if expected <> transition then
                Error "confirmation transition is out of lineage order"
            elif confirmed.changeId <> submitted.changeId then
                Error "confirmed Change identity does not match the submission"
            elif not (isExactPrefix submitted.ops confirmed.ops) then
                Error "confirmed Change does not preserve the submitted Ops prefix"
            else
                Ok remaining

    let private finishConfirmation
        (recordId: int)
        (confirmed: Change)
        (history: ClientHistory)
        : ClientHistory =
        let amended = history |> amendRecord recordId confirmed
        { amended with
            pendingByRecord =
                Map.remove recordId amended.pendingByRecord }

    let private reviseDependent
        (recordId: int)
        (confirmed: Change)
        ((dependentTransition, dependent), later)
        (history: ClientHistory)
        : ClientHistory * Change =
        let revised =
            Change.inverse
                (Revision dependent.id)
                dependent.changeId
                confirmed
        let amended =
            if List.isEmpty later then
                history |> amendRecord recordId revised
            else
                history
        let revisedPending = (dependentTransition, revised) :: later
        { amended with
            pendingByRecord =
                Map.add recordId revisedPending amended.pendingByRecord },
        revised

    let confirm
        (transition: PendingTransition)
        (confirmed: Change)
        (history: ClientHistory)
        : Result<ClientHistory * Change option, string> =
        match validateConfirmation transition confirmed history with
        | Error error -> Error error
        | Ok [] ->
            Ok(
                finishConfirmation transition.recordId confirmed history,
                None)
        | Ok (dependent :: later) ->
            let amended, revised =
                reviseDependent
                    transition.recordId
                    confirmed
                    (dependent, later)
                    history
            Ok(amended, Some revised)
