namespace Gambol.Shared

open Gambol.Shared

type ClientHistory =
    private
        { eventPast: Ev list
          eventFuture: Ev list
          nextEventId: EventId }

[<RequireQualifiedAccess>]
module ClientHistory =
    let clear () : ClientHistory =
        { eventPast = []
          eventFuture = []
          nextEventId = EventId.zero }

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
        (id: EventId)
        : Ev =
        let inverse =
            Ev.inverseOps action |> Option.defaultValue []
        { id = id
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
        { history with
            eventPast = event :: foldedPast
            eventFuture = []
            nextEventId =
                EventId.max history.nextEventId (EventId.next event.id) }

    let undoEvent
        (history: ClientHistory)
        : (Ev * ClientHistory) option =
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
        : (Ev * ClientHistory) option =
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

    let private unwrap (EventId n) = n

    let private callerRecordId (event: Ev) : int =
        unwrap (Ev.target event |> Option.defaultValue event.id)

    let mintChange (commandName: string) (ops: Op list) : Ev =
        { id = EventId.zero
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = commandName
          body = EventBody.Change ops }

    let record
        (event: Ev)
        (history: ClientHistory)
        : ClientHistory * int =
        let stored = { event with id = history.nextEventId }
        recordEvent event.commandName stored history, unwrap stored.id

    let private yieldMinted
        (submissionId: System.Guid)
        (produced: Ev)
        (nextHistory: ClientHistory)
        : Ev * ClientHistory * int =
        { produced with
            id = EventId.zero
            submissionId = submissionId },
        nextHistory,
        callerRecordId produced

    let undo
        (submissionId: System.Guid)
        (history: ClientHistory)
        : (Ev * ClientHistory * int) option =
        match undoEvent history with
        | None -> None
        | Some (produced, nextHistory) ->
            Some(yieldMinted submissionId produced nextHistory)

    let redo
        (submissionId: System.Guid)
        (history: ClientHistory)
        : (Ev * ClientHistory * int) option =
        match redoEvent history with
        | None -> None
        | Some (produced, nextHistory) ->
            Some(yieldMinted submissionId produced nextHistory)
