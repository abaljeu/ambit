namespace Gambol.Shared

[<RequireQualifiedAccess>]
module SyncBatch =
    let private withoutCarried (event: Ev) =
        match event.body with
        | EventBody.Undo (target, _) ->
            { event with body = EventBody.Undo(target, None) }
        | EventBody.Redo (target, _) ->
            { event with body = EventBody.Redo(target, None) }
        | _ -> event

    /// Pending events stay EventId.zero on the wire. Undo and Redo post the target only.
    let toWireBatch (events: Ev list) : Ev list =
        events
        |> List.map (fun event ->
            { withoutCarried event with id = EventId.zero })
