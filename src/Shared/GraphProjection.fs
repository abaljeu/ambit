namespace Gambol.Shared

/// Pure helpers: compare graphs and map to/from relational projection rows (no SQL).

[<RequireQualifiedAccess>]
module GraphProjection =

    type NodePersistenceRow =
        { id: System.Guid
          text: string
          name: string option
          kind: string
          documentState: string
          cssClassNames: string list
          updateTime: System.DateTime }

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
        && a.kind = b.kind
        && a.documentState = b.documentState
        && a.childrenStatus = b.childrenStatus
        && CssClass.toList a.cssClasses = CssClass.toList b.cssClasses
        && a.updateTime = b.updateTime
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

    let nodeRowFromNode (node: Node) : NodePersistenceRow =
        { id = node.id.Value
          text = node.text
          name = Filename.tryValue node.name
          kind = NodeKindPersistence.toPersistString node.kind
          documentState =
            match node.documentState with
            | Current -> "current"
            | Unparsed -> "unparsed"
            | NoServerFile -> "noServerFile"
          cssClassNames = CssClass.toList node.cssClasses
          updateTime = node.updateTime }

    let nodeRowsFromGraph (g: Graph) : NodePersistenceRow list =
        g.nodes
        |> Map.toList
        |> List.map (snd >> nodeRowFromNode)

    let childRowsFromNode (graph: Graph) (node: Node) : ChildPersistenceRow list =
        node.children
        |> List.mapi (fun ordinal child ->
            { parentId = node.id.Value
              ordinal = ordinal
              childId = child.id.Value
              ownership = Node.childOwnership graph node.id child })

    let childRowsFromGraph (g: Graph) : ChildPersistenceRow list =
        g.nodes
        |> Map.toList
        |> List.collect (fun (_, node) -> childRowsFromNode g node)

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
        match
            nodeRows
            |> List.tryPick (fun r ->
                match NodeKindPersistence.fromPersistString r.kind with
                | Error e -> Some e
                | Ok _ -> None)
        with
        | Some e -> Error e
        | None ->

        let baseNodes : Map<NodeId, Node> =
            nodeRows
            |> List.map (fun r ->
                let nid = NodeId r.id
                let parsedKind =
                    match NodeKindPersistence.fromPersistString r.kind with
                    | Ok k -> k
                    | Error _ -> Normal

                let name =
                    r.name
                    |> Option.map Filename.create
                    |> Option.defaultValue Filename.Empty
                let documentState =
                    match r.documentState with
                    | "unparsed" -> Unparsed
                    | "noServerFile" -> NoServerFile
                    | _ -> Current
                nid,
                Node.Create(
                    nid,
                    text = r.text,
                    name = name,
                    cssClasses = CssClass.ofList r.cssClassNames,
                    kind = NodeKindPersistence.legacyKindForCanonical nid parsedKind,
                    documentState = documentState,
                    updateTime = r.updateTime))
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
