namespace Gambol.Shared

/// Occurrence-bag diff and Accept Both merge for same-parent child lists.
[<RequireQualifiedAccess>]
module ChildListMerge =

    /// Multiset diff from anchor to observed: removes in anchor order, adds in observed order.
    let diff (anchor: ChildNode list) (observed: ChildNode list) : ChildNode list * ChildNode list =
        let rec walk observedRemaining anchorRemaining addsRev =
            match observedRemaining with
            | [] -> anchorRemaining, List.rev addsRev
            | observed :: rest ->
                match List.tryFindIndex ((=) observed) anchorRemaining with
                | Some index ->
                    let before = anchorRemaining |> List.take index
                    let after = anchorRemaining |> List.skip (index + 1)
                    walk rest (before @ after) addsRev
                | None ->
                    walk rest anchorRemaining (observed :: addsRev)

        walk observed anchor []

    let private unionRemoves (left: ChildNode list) (right: ChildNode list) =
        let rec merge remaining extra =
            match extra with
            | [] -> remaining
            | item :: rest when List.exists ((=) item) remaining ->
                merge remaining rest
            | item :: rest -> merge (item :: remaining) rest

        merge left right

    let private dropRemoves (items: ChildNode list) (removes: ChildNode list) =
        items |> List.filter (fun item -> not (List.exists ((=) item) removes))

    let private anchorPredecessors (anchor: ChildNode list) (newList: ChildNode list) (add: ChildNode) =
        newList
        |> List.takeWhile ((<>) add)
        |> List.filter (fun item -> List.exists ((=) item) anchor)

    let private insertAfterLastPredecessor
        (spine: ChildNode list)
        (predecessors: ChildNode list)
        (add: ChildNode)
        =
        let spinePredecessors =
            predecessors |> List.filter (fun pred -> List.exists ((=) pred) spine)

        match List.tryLast spinePredecessors with
        | None -> add :: spine
        | Some last ->
            let index = List.findIndex ((=) last) spine
            (spine |> List.take (index + 1)) @ [ add ] @ (spine |> List.skip (index + 1))

    let private insertIntentAdds
        (anchor: ChildNode list)
        (newList: ChildNode list)
        (spine: ChildNode list)
        (adds: ChildNode list)
        =
        adds
        |> List.fold
            (fun current add ->
                insertAfterLastPredecessor current (anchorPredecessors anchor newList add) add)
            spine

    /// Deterministic Accept Both per replace-amendment §4.
    let acceptBoth
        (anchor: ChildNode list)
        (current: ChildNode list)
        (intentRemoves: ChildNode list)
        (intentAdds: ChildNode list)
        (newList: ChildNode list)
        : ChildNode list
        =
        let contextRemoves, _ = diff anchor current
        let allRemoves = unionRemoves contextRemoves intentRemoves
        let spine = dropRemoves current allRemoves
        insertIntentAdds anchor newList spine intentAdds

    /// Three-way resolve: current, intent newList, anchor oldList → target children.
    let resolve (anchor: ChildNode list) (current: ChildNode list) (newList: ChildNode list) =
        if current = anchor then
            newList
        else
            let intentRemoves, intentAdds = diff anchor newList
            acceptBoth anchor current intentRemoves intentAdds newList
