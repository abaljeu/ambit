namespace Gambol.Shared

/// Pure helpers: compare graphs and map to/from relational projection rows (no SQL).

[<RequireQualifiedAccess>]
module GraphProjection =

    type NodePersistenceRow =
        { id: System.Guid
          text: string
          name: string option
          cssClassNames: string list }

    type ChildPersistenceRow =
        { parentId: System.Guid
          ordinal: int
          childId: System.Guid
          ownership: Ownership }

    let private childNodeEquals (a: ChildNode) (b: ChildNode) : bool =
        a.ref = b.ref && a.id.Value = b.id.Value

    let private nodeEquals (a: Node) (b: Node) : bool =
        a.id.Value = b.id.Value
        && a.text = b.text
        && a.name = b.name
        && CssClass.toList a.cssClasses = CssClass.toList b.cssClasses
        && List.length a.children = List.length b.children
        && List.forall2 (fun x y -> childNodeEquals x y) a.children b.children

    /// Structural equality on stored shape (`root` + `nodes` map).
    let graphEquals (a: Graph) (b: Graph) : bool =
        if a.root.Value <> b.root.Value || a.nodes.Count <> b.nodes.Count then
            false
        else
            a.nodes
            |> Map.forall (fun nid na ->
                match Map.tryFind nid b.nodes with
                | None -> false
                | Some nb -> nodeEquals na nb)

    let nodeRowsFromGraph (g: Graph) : NodePersistenceRow list =
        g.nodes
        |> Map.toList
        |> List.map (fun (_, n) ->
            { id = n.id.Value
              text = n.text
              name = n.name
              cssClassNames = CssClass.toList n.cssClasses })

    let childRowsFromGraph (g: Graph) : ChildPersistenceRow list =
        g.nodes
        |> Map.toList
        |> List.collect (fun (pId, node) ->
            node.children
            |> List.mapi (fun i c ->
                { parentId = pId.Value
                  ordinal = i
                  childId = c.id.Value
                  ownership =
                    match c.ref with
                    | Ownership.Owner -> Ownership.Owner
                    | Ownership.Ref -> Ownership.Ref }))

    let graphFromPersistence
        (rootId: NodeId)
        (nodeRows: NodePersistenceRow list)
        (childRows: ChildPersistenceRow list)
        : Result<Graph, string> =
        let idSet =
            nodeRows |> List.map (fun r -> r.id) |> Set.ofList

        if not (Set.contains rootId.Value idSet) then
            Error "graphFromPersistence: root id missing from nodes"
        else

        let baseNodes : Map<NodeId, Node> =
            nodeRows
            |> List.map (fun r ->
                let nid = NodeId r.id

                nid,
                { id = nid
                  text = r.text
                  name = r.name
                  children = []
                  cssClasses = CssClass.ofList r.cssClassNames
                  owner = Graph.rootId
                  kind = Normal })
            |> Map.ofList

        let badRef (g: ChildPersistenceRow) =
            not (Set.contains g.parentId idSet) || not (Set.contains g.childId idSet)

        match childRows |> List.tryFind badRef with
        | Some r ->
            Error $"graphFromPersistence: unknown parent or child ({r.parentId}, {r.childId})"
        | None ->

        let grouped =
            childRows
            |> List.groupBy (fun r -> r.parentId)
            |> Map.ofList

        let ordinalsDense (rows: ChildPersistenceRow list) =
            let sorted = rows |> List.sortBy (fun r -> r.ordinal)
            sorted |> List.mapi (fun i r -> r.ordinal = i) |> List.forall id

        if grouped |> Map.exists (fun _ rows -> not (ordinalsDense rows)) then
            Error "graphFromPersistence: child ordinals must be 0..n-1 per parent"
        else

        let withChildren =
            baseNodes
            |> Map.map (fun nid node ->
                match Map.tryFind nid.Value grouped with
                | None -> node
                | Some rows ->
                    let sorted = rows |> List.sortBy (fun r -> r.ordinal)

                    let ch =
                        sorted
                        |> List.map (fun r ->
                            { ref =
                                match r.ownership with
                                | Ownership.Owner -> Ownership.Owner
                                | Ownership.Ref -> Ownership.Ref
                              id = NodeId r.childId })

                    { node with children = ch })

        Ok (Graph.fromNodes rootId withChildren)

    /// Round-trip for tests: graph → rows → graph.
    let graphRoundTrip (g: Graph) : Result<Graph, string> =
        let nr = nodeRowsFromGraph g
        let cr = childRowsFromGraph g
        graphFromPersistence g.root nr cr
