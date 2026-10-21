namespace Gambol.Shared

/// After DocumentPersistence stamps artifact roots, emit ops for the change log / poll tail.
[<RequireQualifiedAccess>]
module PersistStamp =

    let private timeStampOp
        (id: NodeId)
        (beforeTime: System.DateTime)
        (afterTime: System.DateTime)
        =
        let oldTime = NodeUpdateTime.toDbPrecision beforeTime
        let newTime = NodeUpdateTime.toDbPrecision afterTime
        if oldTime = newTime then None
        else Some(Op.SetUpdateTime(id, oldTime, newTime))

    let private stampOpsFor (nodeId: NodeId) (beforeNode: Node option) (afterNode: Node) =
        let beforeTime =
            match beforeNode with
            | Some node -> node.updateTime
            | None -> NodeUpdateTime.missing
        match timeStampOp nodeId beforeTime afterNode.updateTime with
        | None -> []
        | Some op -> [ op ]

    let opsBetween (before: Graph) (after: Graph) : Op list =
        after.nodes
        |> Map.toList
        |> List.collect (fun (id, afterNode) ->
            stampOpsFor id (Map.tryFind id before.nodes) afterNode)

    let appendToOps (ops: Op list) (stampOps: Op list) : Op list =
        if stampOps.IsEmpty then ops else ops @ stampOps

    let appendToEvent (event: Ev) (stampOps: Op list) : Ev =
        if stampOps.IsEmpty then
            event
        else
            match event.body with
            | EventBody.Change ops ->
                { event with body = EventBody.Change(appendToOps ops stampOps) }
            | EventBody.Undo(target, ops) ->
                { event with
                    body = EventBody.Undo(target, appendToOps ops stampOps) }
            | EventBody.Redo(target, ops) ->
                { event with
                    body = EventBody.Redo(target, appendToOps ops stampOps) }
            | EventBody.ActorStart _ | EventBody.Cancel _
            | EventBody.ActorStop _ | EventBody.Notice _ -> event

    let appendToLastEvent (events: Ev list) (stampOps: Op list) : Ev list =
        if stampOps.IsEmpty || events.IsEmpty then
            events
        else
            match List.rev events with
            | [] -> events
            | last :: rest ->
                List.rev (appendToEvent last stampOps :: rest)
