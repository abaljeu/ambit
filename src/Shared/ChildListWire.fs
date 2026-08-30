namespace Gambol.Shared

/// Full-list Replace helpers for wire-valid child-list Change posts.
[<RequireQualifiedAccess>]
module ChildListWire =

    let replace (parentId: NodeId) (oldList: ChildNode list) (newList: ChildNode list) : Op =
        Op.Replace(parentId, oldList, newList)

    let dropRange (children: ChildNode list) (start: int) (count: int) : ChildNode list =
        children
        |> List.indexed
        |> List.filter (fun (i, _) -> i < start || i >= start + count)
        |> List.map snd

    let insertInto (children: ChildNode list) (index: int) (inserted: ChildNode list) : ChildNode list =
        let before = List.take index children
        let after = List.skip index children
        before @ inserted @ after

    let updateAt (children: ChildNode list) (index: int) (item: ChildNode) : ChildNode list =
        children |> List.mapi (fun i c -> if i = index then item else c)

    let excludingIndices (children: ChildNode list) (indices: Set<int>) : ChildNode list =
        children
        |> List.indexed
        |> List.filter (fun (i, _) -> not (Set.contains i indices))
        |> List.map snd

    let removeIndices (parentId: NodeId) (children: ChildNode list) (indices: Set<int>) : Op =
        replace parentId children (excludingIndices children indices)

    let removeRange (parentId: NodeId) (children: ChildNode list) (start: int) (count: int) : Op =
        replace parentId children (dropRange children start count)

    let insertAt
        (parentId: NodeId)
        (children: ChildNode list)
        (index: int)
        (inserted: ChildNode list)
        : Op =
        replace parentId children (insertInto children index inserted)

    let updateChildAt (parentId: NodeId) (children: ChildNode list) (index: int) (item: ChildNode) : Op =
        replace parentId children (updateAt children index item)

    let append (parentId: NodeId) (children: ChildNode list) (appended: ChildNode list) : Op =
        insertAt parentId children children.Length appended

    let edit
        (parentId: NodeId)
        (children: ChildNode list)
        (removeStart: int)
        (removeCount: int)
        (insertIdx: int)
        (inserted: ChildNode list)
        : Op =
        replace
            parentId
            children
            (insertInto (dropRange children removeStart removeCount) insertIdx inserted)
