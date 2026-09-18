namespace Gambol.Shared

open Gambol.Shared

type ClientHistory =
    private
        { eventPast: Ev list
          eventFuture: Ev list }

[<RequireQualifiedAccess>]
module ClientHistory =
    let clear () : ClientHistory =
        { eventPast = []
          eventFuture = [] }

    let private tryTakeAction
        (stack: Ev list)
        : (Ev list * Ev) option =
        let rec walk skipped remaining =
            match remaining with
            | [] -> None
            | action :: rest when Ev.isAction action ->
                let kept = List.fold (fun acc x -> x :: acc) rest skipped
                Some(kept, action)
            | actor :: rest ->
                walk (actor :: skipped) rest
        walk [] stack

    let private invertAs
        (wrap: EventId * Op list -> EventBody)
        (action: Ev)
        : Ev =
        let inverse =
            Ev.inverseOps action |> Option.defaultValue []
        { id = EventId.zero
          submissionId = System.Guid.NewGuid()
          authority = action.authority
          commandName = action.commandName
          body = wrap (Ev.id action, inverse) }

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
        { eventPast = event :: foldedPast
          eventFuture = [] }

    let undoEvent
        (history: ClientHistory)
        : (Ev * ClientHistory) option =
        match tryTakeAction history.eventPast with
        | None -> None
        | Some (remainingPast, action) ->
            let produced = invertAs EventBody.Undo action
            let nextHistory =
                { eventPast = remainingPast
                  eventFuture = produced :: history.eventFuture }
            Some(produced, nextHistory)

    let redoEvent
        (history: ClientHistory)
        : (Ev * ClientHistory) option =
        match tryTakeAction history.eventFuture with
        | None -> None
        | Some (remainingFuture, action) ->
            let produced = invertAs EventBody.Redo action
            let nextHistory =
                { eventPast = produced :: history.eventPast
                  eventFuture = remainingFuture }
            Some(produced, nextHistory)

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

    let record
        (event: Ev)
        (history: ClientHistory)
        : ClientHistory =
        recordEvent event.commandName { event with id = EventId.zero } history

    let private yieldMinted
        (submissionId: System.Guid)
        (produced: Ev)
        (nextHistory: ClientHistory)
        : Ev * ClientHistory =
        { produced with
            id = EventId.zero
            submissionId = submissionId },
        nextHistory

    let undo
        (submissionId: System.Guid)
        (history: ClientHistory)
        : (Ev * ClientHistory) option =
        match undoEvent history with
        | None -> None
        | Some (produced, nextHistory) ->
            Some(yieldMinted submissionId produced nextHistory)

    let redo
        (submissionId: System.Guid)
        (history: ClientHistory)
        : (Ev * ClientHistory) option =
        match redoEvent history with
        | None -> None
        | Some (produced, nextHistory) ->
            Some(yieldMinted submissionId produced nextHistory)

    let private resolveTarget (confirmed: Ev list) target ops =
        if target <> EventId.zero then
            target
        else
            confirmed
            |> List.tryFind (fun event ->
                match Ev.inverseOps event with
                | Some inverse -> inverse = ops
                | None -> false)
            |> Option.map (fun event -> event.id)
            |> Option.defaultValue EventId.zero

    let private stampBody (confirmed: Ev list) body =
        match body with
        | EventBody.Undo(target, ops) ->
            EventBody.Undo(resolveTarget confirmed target ops, ops)
        | EventBody.Redo(target, ops) ->
            EventBody.Redo(resolveTarget confirmed target ops, ops)
        | other -> other

    let private stampEvent
        (confirmed: Ev list)
        (ids: Map<System.Guid, EventId>)
        (event: Ev)
        =
        let event =
            match Map.tryFind event.submissionId ids with
            | Some eventId -> { event with id = eventId }
            | None -> event
        { event with body = stampBody confirmed event.body }

    /// Replace EventId.zero with the server id for matching submissionId.
    /// Also fills Undo/Redo targets written while the original id was zero.
    let approve (confirmed: Ev list) (history: ClientHistory) : ClientHistory =
        let ids =
            confirmed
            |> List.map (fun event -> event.submissionId, event.id)
            |> Map.ofList
        { eventPast = List.map (stampEvent confirmed ids) history.eventPast
          eventFuture =
            List.map (stampEvent confirmed ids) history.eventFuture }
