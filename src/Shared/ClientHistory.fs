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

    let private tryPeekActionName (stack: Ev list) : string option =
        let rec walk remaining =
            match remaining with
            | [] -> None
            | event :: _ when Ev.isAction event -> Some event.commandName
            | _ :: rest -> walk rest
        walk stack

    let tryPeekUndoName (history: ClientHistory) : string option =
        tryPeekActionName history.eventPast

    let tryPeekRedoName (history: ClientHistory) : string option =
        tryPeekActionName history.eventFuture

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

    let private stampEvent (confirmed: Map<System.Guid, EventId>) (event: Ev) =
        match Map.tryFind event.submissionId confirmed with
        | Some eventId -> { event with id = eventId }
        | None -> event

    /// Replace EventId.zero with the server id for matching submissionId.
    let approve (confirmed: Ev list) (history: ClientHistory) : ClientHistory =
        let ids =
            confirmed
            |> List.map (fun event -> event.submissionId, event.id)
            |> Map.ofList
        { eventPast = List.map (stampEvent ids) history.eventPast
          eventFuture = List.map (stampEvent ids) history.eventFuture }
