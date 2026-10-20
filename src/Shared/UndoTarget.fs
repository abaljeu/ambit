namespace Gambol.Shared

/// Ops for an Undo or Redo, loaded from the target event.
[<RequireQualifiedAccess>]
module UndoTarget =

    let private find (log: Ev list) (target: EventId) =
        log |> List.tryFind (fun event -> event.id = target)

    let rec private forwardSeen
        (log: Ev list)
        (seen: Set<System.Guid>)
        (event: Ev)
        : Op list option =
        if Set.contains event.submissionId seen then
            None
        else
            let seen = Set.add event.submissionId seen
            match event.body with
            | EventBody.Change ops
            | EventBody.Undo (_, Some ops)
            | EventBody.Redo (_, Some ops) -> Some ops
            | EventBody.Undo (target, None) ->
                find log target
                |> Option.bind (inverseSeen log seen)
            | EventBody.Redo (target, None) ->
                find log target
                |> Option.bind (forwardSeen log seen)
            | EventBody.ActorStart _
            | EventBody.ActorStop _
            | EventBody.Cancel _ -> None

    and private inverseSeen log seen event =
        forwardSeen log seen event |> Option.map Op.invertAll

    /// Ops the event applied. An old payload's carried list is that result.
    let forwardOfEvent (log: Ev list) (event: Ev) : Op list option =
        forwardSeen log Set.empty event

    /// Inverse of the ops the event applied.
    let inverseOfEvent (log: Ev list) (event: Ev) : Op list option =
        inverseSeen log Set.empty event

    /// Forward ops of the event with this id.
    let forwardOfTarget (log: Ev list) (target: EventId) : Op list option =
        find log target |> Option.bind (forwardOfEvent log)

    /// Inverse ops of the event with this id.
    let inverseOfTarget (log: Ev list) (target: EventId) : Op list option =
        find log target |> Option.bind (inverseOfEvent log)
