namespace Gambol.Shared

/// Build SetClasses ops that toggle one user class on named nodes.
[<RequireQualifiedAccess>]
module CssClassToggle =

    /// Toggle `name` on each existing node. Skip missing ids and no-ops.
    let toggleClassOps
        (name: string)
        (graph: Graph)
        (nodeIds: NodeId list)
        : Op list =
        nodeIds
        |> List.choose (fun nid ->
            match Map.tryFind nid graph.nodes with
            | None -> None
            | Some node ->
                let oldClasses = node.cssClasses
                let newClasses = CssClass.toggle name oldClasses
                if oldClasses = newClasses then None
                else Some (Op.SetClasses(nid, oldClasses, newClasses)))

    let private userClassNames (node: Node) : string list =
        node.cssClasses
        |> CssClass.toList
        |> List.filter (fun c -> not (c.StartsWith("amb-")))

    /// Space-separated user classes. None when any id has no Header.
    let tryInitialUserClasses
        (graph: Graph)
        (nodeIds: NodeId list)
        : string option =
        let rec read acc remaining =
            match remaining with
            | [] ->
                acc
                |> List.distinct
                |> String.concat " "
                |> Some
            | nid :: rest ->
                match Map.tryFind nid graph.nodes with
                | None -> None
                | Some node -> read (acc @ userClassNames node) rest
        read [] nodeIds
