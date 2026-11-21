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
          persistState: string
          persistedVersion: int
          changedVersion: int
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
        && Node.documentStateEnum a = Node.documentStateEnum b
        && a.documentState.persistState = b.documentState.persistState
        && a.documentState.persistedVersion = b.documentState.persistedVersion
        && a.documentState.changedVersion = b.documentState.changedVersion
        && CssClass.toList a.cssClasses = CssClass.toList b.cssClasses
        && a.updateTime = b.updateTime

    let private childListEquals (a: ChildNode list) (b: ChildNode list) : bool =
        List.length a = List.length b
        && List.forall2 (fun x y -> childNodeEquals x y) a b

    /// Structural equality on stored shape (`root` + `nodes` + `childMap`).
    let graphEquals (a: Graph) (b: Graph) : bool =
        if a.root.Value <> b.root.Value
           || a.nodes.Count <> b.nodes.Count
           || a.childMap.Count <> b.childMap.Count then
            false
        else
            let nodesEqual =
                a.nodes
                |> Map.forall (fun nid na ->
                    match Map.tryFind nid b.nodes with
                    | None -> false
                    | Some nb -> nodeEquals na nb)
            let childMapsEqual =
                a.childMap
                |> Map.forall (fun pid kids ->
                    match Map.tryFind pid b.childMap with
                    | None -> false
                    | Some other -> childListEquals kids other)
            nodesEqual && childMapsEqual

    let private storedDocumentWord (state: DocumentState) : string =
        match state.parseState with
        | ParseState.Parsed -> "current"
        | ParseState.Unparsed -> "unparsed"

    let private storedPersistWord (state: PersistState) : string =
        match state with
        | PersistState.Persisted -> "persisted"
        | PersistState.Unpersisted -> "unpersisted"

    let private documentEnumFromStoredWord (word: string) : DocumentStateEnum =
        match word with
        | "unparsed"
        | "noServerFile" -> Unparsed
        | _ -> Current

    let private persistFromStoredWord (word: string) : PersistState =
        match word with
        | "unpersisted" -> PersistState.Unpersisted
        | _ -> PersistState.Persisted

    let nodeRowFromNode (node: Node) : NodePersistenceRow =
        { id = node.id.Value
          text = node.text
          name = Filename.tryValue node.name
          kind = NodeKindPersistence.toPersistString node.kind
          documentState = storedDocumentWord node.documentState
          persistState = storedPersistWord node.documentState.persistState
          persistedVersion =
            EventId.toJson node.documentState.persistedVersion
          changedVersion = EventId.toJson node.documentState.changedVersion
          cssClassNames = CssClass.toList node.cssClasses
          updateTime = node.updateTime }

    let nodeRowsFromGraph (g: Graph) : NodePersistenceRow list =
        g.nodes
        |> Map.toList
        |> List.map (snd >> nodeRowFromNode)

    let childRowsFromNode
        (graph: Graph)
        (parentId: NodeId)
        (children: ChildNode list)
        : ChildPersistenceRow list =
        children
        |> List.mapi (fun ordinal child ->
            { parentId = parentId.Value
              ordinal = ordinal
              childId = child.id.Value
              ownership = Node.childOwnership graph parentId child })

    let childRowsFromGraph (g: Graph) : ChildPersistenceRow list =
        g.childMap
        |> Map.toList
        |> List.collect (fun (parentId, children) ->
            childRowsFromNode g parentId children)

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
                let created =
                    Node.Create(
                        nid,
                        text = r.text,
                        name = name,
                        cssClasses = CssClass.ofList r.cssClassNames,
                        kind =
                            NodeKindPersistence.legacyKindForCanonical
                                nid
                                parsedKind,
                        documentState = documentEnumFromStoredWord r.documentState,
                        persistState = persistFromStoredWord r.persistState,
                        updateTime = r.updateTime)
                nid,
                { created with
                    documentState =
                        { created.documentState with
                            persistedVersion =
                                EventId.fromJson r.persistedVersion
                            changedVersion =
                                EventId.fromJson r.changedVersion } })
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

        let childMap =
            baseNodes
            |> Map.fold
                (fun acc nid _ ->
                    match Map.tryFind nid.Value grouped with
                    | None -> Map.add nid [] acc
                    | Some rows ->
                        let sorted = rows |> List.sortBy (fun r -> r.ordinal)
                        let ch =
                            sorted
                            |> List.map (fun r ->
                                ChildNode.ofOwnership
                                    r.ownership
                                    (NodeId r.childId))
                        Map.add nid ch acc)
                Map.empty

        Ok (Graph.fromNodes rootId baseNodes childMap)

    /// Round-trip for tests: graph → rows → graph.
    let graphRoundTrip (g: Graph) : Result<Graph, string> =
        let nr = nodeRowsFromGraph g
        let cr = childRowsFromGraph g
        graphFromPersistence g.root nr cr
