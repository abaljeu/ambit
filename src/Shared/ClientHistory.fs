namespace Gambol.Shared

open Gambol.Shared

type ClientHistory =
    private
        { eventPast: Ev list
          eventFuture: Ev list
          kept: Ev list
          zeroTargetSource: Map<System.Guid, System.Guid> }

[<RequireQualifiedAccess>]
module ClientHistory =
    let clear () : ClientHistory =
        { eventPast = []
          eventFuture = []
          kept = []
          zeroTargetSource = Map.empty }

    let private tryTakeAction (stack: Ev list) : (Ev list * Ev) option =
        let rec walk skipped remaining =
            match remaining with
            | [] -> None
            | action :: rest when Ev.isAction action ->
                let kept = List.fold (fun acc x -> x :: acc) rest skipped
                Some(kept, action)
            | actor :: rest -> walk (actor :: skipped) rest
        walk [] stack

    let private logOf (history: ClientHistory) =
        history.kept @ history.eventPast @ history.eventFuture

    let private remember (event: Ev) (history: ClientHistory) =
        { history with kept = event :: history.kept }

    let private linkZero
        (produced: Ev)
        (action: Ev)
        (history: ClientHistory)
        =
        if action.id <> EventId.zero then
            history
        else
            let source =
                match Map.tryFind action.submissionId history.zeroTargetSource with
                | Some earlier -> earlier
                | None -> action.submissionId
            { history with
                zeroTargetSource =
                    Map.add produced.submissionId source history.zeroTargetSource }

    let private sourceEvent
        (history: ClientHistory)
        (target: EventId)
        (submissionId: System.Guid)
        =
        if target <> EventId.zero then
            logOf history |> List.tryFind (fun event -> event.id = target)
        else
            match Map.tryFind submissionId history.zeroTargetSource with
            | None -> None
            | Some source ->
                logOf history
                |> List.tryFind (fun event -> event.submissionId = source)

    /// Ops this event applied. Target-only Undo and Redo load the target.
    let appliedOps (history: ClientHistory) (event: Ev) : Op list option =
        let log = logOf history
        match event.body with
        | EventBody.Change ops
        | EventBody.Undo (_, Some ops)
        | EventBody.Redo (_, Some ops) -> Some ops
        | EventBody.Undo (target, None) ->
            sourceEvent history target event.submissionId
            |> Option.bind (UndoTarget.inverseOfEvent log)
        | EventBody.Redo (target, None) ->
            sourceEvent history target event.submissionId
            |> Option.bind (UndoTarget.forwardOfEvent log)
        | EventBody.ActorStart _
        | EventBody.ActorStop _
        | EventBody.Cancel _ -> None

    let private minted (action: Ev) (body: EventBody) : Ev =
        { id = EventId.zero
          submissionId = System.Guid.NewGuid()
          authority = action.authority
          commandName = action.commandName
          body = body }

    let private undoOps (history: ClientHistory) (action: Ev) =
        let log = logOf history
        match action.body with
        | EventBody.Redo (target, None) when target = EventId.zero ->
            sourceEvent history target action.submissionId
            |> Option.bind (UndoTarget.inverseOfEvent log)
        | EventBody.Undo (target, None) when target = EventId.zero ->
            sourceEvent history target action.submissionId
            |> Option.bind (UndoTarget.forwardOfEvent log)
        | _ when action.id <> EventId.zero ->
            UndoTarget.inverseOfTarget log action.id
        | _ -> UndoTarget.inverseOfEvent log action

    let private redoTarget (action: Ev) =
        match action.body with
        | EventBody.Undo (target, _)
        | EventBody.Redo (target, _) -> target
        | _ -> action.id

    let private redoOps (history: ClientHistory) (action: Ev) =
        let target = redoTarget action
        if target <> EventId.zero then
            UndoTarget.forwardOfTarget (logOf history) target
        else
            sourceEvent history target action.submissionId
            |> Option.bind (UndoTarget.forwardOfEvent (logOf history))

    let recordEvent
        (commandName: string)
        (event: Ev)
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
            eventFuture = [] }

    let undoEvent
        (history: ClientHistory)
        : (Ev * ClientHistory * Op list) option =
        match tryTakeAction history.eventPast with
        | None -> None
        | Some (remainingPast, action) ->
            let ops = undoOps history action |> Option.defaultValue []
            let produced = minted action (EventBody.Undo(action.id, None))
            let next =
                { history with eventPast = remainingPast }
                |> remember action
                |> linkZero produced action
            let next =
                { next with eventFuture = produced :: next.eventFuture }
            Some(produced, next, ops)

    let redoEvent
        (history: ClientHistory)
        : (Ev * ClientHistory * Op list) option =
        match tryTakeAction history.eventFuture with
        | None -> None
        | Some (remainingFuture, action) ->
            let target = redoTarget action
            let ops = redoOps history action |> Option.defaultValue []
            let produced = minted action (EventBody.Redo(target, None))
            let next =
                { history with eventFuture = remainingFuture }
                |> remember action
                |> linkZero produced action
            let next = { next with eventPast = produced :: next.eventPast }
            Some(produced, next, ops)

    let private tryPeekAction (stack: Ev list) : Ev option =
        let rec walk remaining =
            match remaining with
            | [] -> None
            | event :: _ when Ev.isAction event -> Some event
            | _ :: rest -> walk rest
        walk stack

    let tryPeekUndoEvent (history: ClientHistory) : Ev option =
        tryPeekAction history.eventPast

    let tryPeekRedoEvent (history: ClientHistory) : Ev option =
        tryPeekAction history.eventFuture

    let tryPeekUndoName (history: ClientHistory) : string option =
        tryPeekUndoEvent history |> Option.map (fun event -> event.commandName)

    let tryPeekRedoName (history: ClientHistory) : string option =
        tryPeekRedoEvent history |> Option.map (fun event -> event.commandName)

    let mintChange (commandName: string) (ops: Op list) : Ev =
        { id = EventId.zero
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = commandName
          body = EventBody.Change ops }

    let record (event: Ev) (history: ClientHistory) : ClientHistory =
        recordEvent event.commandName { event with id = EventId.zero } history

    let private retargetSubmission
        (fromId: System.Guid)
        (toId: System.Guid)
        (history: ClientHistory)
        =
        let swap (event: Ev) =
            if event.submissionId = fromId then
                { event with submissionId = toId }
            else
                event
        let zeroTargetSource =
            match Map.tryFind fromId history.zeroTargetSource with
            | None -> history.zeroTargetSource
            | Some source ->
                history.zeroTargetSource
                |> Map.remove fromId
                |> Map.add toId source
        { eventPast = List.map swap history.eventPast
          eventFuture = List.map swap history.eventFuture
          kept = List.map swap history.kept
          zeroTargetSource = zeroTargetSource }

    let private yieldMinted
        (submissionId: System.Guid)
        (produced: Ev)
        (nextHistory: ClientHistory)
        (ops: Op list)
        =
        let history =
            retargetSubmission produced.submissionId submissionId nextHistory
        { produced with
            id = EventId.zero
            submissionId = submissionId },
        history,
        ops

    let undo
        (submissionId: System.Guid)
        (history: ClientHistory)
        : (Ev * ClientHistory * Op list) option =
        match undoEvent history with
        | None -> None
        | Some (produced, nextHistory, ops) ->
            Some(yieldMinted submissionId produced nextHistory ops)

    let redo
        (submissionId: System.Guid)
        (history: ClientHistory)
        : (Ev * ClientHistory * Op list) option =
        match redoEvent history with
        | None -> None
        | Some (produced, nextHistory, ops) ->
            Some(yieldMinted submissionId produced nextHistory ops)

    let private stampEvent
        (ids: Map<System.Guid, EventId>)
        (openTargets: Map<System.Guid, System.Guid>)
        (event: Ev)
        =
        let event =
            match Map.tryFind event.submissionId ids with
            | Some eventId -> { event with id = eventId }
            | None -> event
        let retarget target carried wrap =
            if target <> EventId.zero then
                event
            else
                match Map.tryFind event.submissionId openTargets with
                | None -> event
                | Some source ->
                    match Map.tryFind source ids with
                    | None -> event
                    | Some id -> { event with body = wrap id carried }
        match event.body with
        | EventBody.Undo(target, carried) ->
            retarget target carried (fun id carried ->
                EventBody.Undo(id, carried))
        | EventBody.Redo(target, carried) ->
            retarget target carried (fun id carried ->
                EventBody.Redo(id, carried))
        | _ -> event

    let private confirmedIds (confirmed: Ev list) =
        confirmed
        |> List.map (fun event -> event.submissionId, event.id)
        |> Map.ofList

    /// Replace EventId.zero with the server id for matching submissionId.
    /// Also fills Undo and Redo targets written while that id was zero.
    let approve (confirmed: Ev list) (history: ClientHistory) : ClientHistory =
        let stamp = stampEvent (confirmedIds confirmed) history.zeroTargetSource
        { history with
            eventPast = List.map stamp history.eventPast
            eventFuture = List.map stamp history.eventFuture
            kept = List.map stamp history.kept }

    /// Fill a zero Undo or Redo target on a pending event from confirmed ids.
    let retarget
        (confirmed: Ev list)
        (history: ClientHistory)
        (event: Ev)
        : Ev =
        stampEvent (confirmedIds confirmed) history.zeroTargetSource event
