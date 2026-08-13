namespace Gambol.Shared

open System

/// Pure planner: GC unreachable projection rows and repair Owned into a ROOT tree.
[<RequireQualifiedAccess>]
module ProjectionOwnershipRepair =

    type private NodeRow = GraphProjection.NodePersistenceRow
    type private ChildRow = GraphProjection.ChildPersistenceRow

    type OwnershipUpdate =
        { parentId: Guid
          ordinal: int
          childId: Guid
          ownership: Ownership }

    type RootOrdinalUpdate =
        { childId: Guid
          fromOrdinal: int
          ordinal: int }

    type LogFacts =
        { deletedCount: int
          ownershipUpdateCount: int
          insertNodeCount: int
          insertChildCount: int
          ordinalShiftCount: int
          affectedNodeIds: Guid list }

    type Plan =
        { deleteNodeIds: Guid list
          ownershipUpdates: OwnershipUpdate list
          insertNodes: NodeRow list
          insertChildren: ChildRow list
          rootOrdinalUpdates: RootOrdinalUpdate list
          logFacts: LogFacts }

    let emptyPlan: Plan =
        { deleteNodeIds = []
          ownershipUpdates = []
          insertNodes = []
          insertChildren = []
          rootOrdinalUpdates = []
          logFacts =
            { deletedCount = 0
              ownershipUpdateCount = 0
              insertNodeCount = 0
              insertChildCount = 0
              ordinalShiftCount = 0
              affectedNodeIds = [] } }

    let isNoOp (plan: Plan) : bool =
        List.isEmpty plan.deleteNodeIds
        && List.isEmpty plan.ownershipUpdates
        && List.isEmpty plan.insertNodes
        && List.isEmpty plan.insertChildren
        && List.isEmpty plan.rootOrdinalUpdates

    type private CanonicalSpec =
        { nodeId: NodeId
          text: string
          kind: NodeKind
          name: string option
          beforeTrash: bool }

    let private missingRoot = "root id missing from nodes"

    let private nodeIdSet (nodes: NodeRow list) =
        nodes |> List.map (fun (n: NodeRow) -> n.id) |> Set.ofList

    let private adjacency (children: ChildRow list) =
        children
        |> List.groupBy (fun (r: ChildRow) -> r.parentId)
        |> List.map (fun (pid, rows) ->
            pid, rows |> List.map (fun (r: ChildRow) -> r.childId))
        |> Map.ofList

    let rec private bfs adj visited =
        function
        | [] -> visited
        | id :: rest when Set.contains id visited -> bfs adj visited rest
        | id :: rest ->
            let kids = Map.tryFind id adj |> Option.defaultValue []
            bfs adj (Set.add id visited) (List.fold (fun acc k -> k :: acc) rest kids)

    let private reachableFrom rootId (children: ChildRow list) =
        bfs (adjacency children) Set.empty [ rootId ]

    let private ownedFrom rootId (children: ChildRow list) =
        let owners =
            children
            |> List.filter (fun (r: ChildRow) -> r.ownership = Ownership.Owner)
        bfs (adjacency owners) Set.empty [ rootId ]

    let private parentKindRank parentId =
        if parentId = Graph.workspacesId.Value then 0
        elif parentId = Graph.trashId.Value then 2
        else 1

    let private ownedParentKey ownedSet parentId =
        let onPath = if Set.contains parentId ownedSet then 0 else 1
        onPath, parentKindRank parentId, parentId

    let private refIngressKey ownedSet (row: ChildRow) =
        let onPath, kind, pid = ownedParentKey ownedSet row.parentId
        onPath, kind, pid, row.ordinal

    let private canonicalSpecs: CanonicalSpec list =
        [ { nodeId = Graph.workspacesId
            text = "Workspaces"
            kind = Special Workspaces
            name = None
            beforeTrash = true }
          { nodeId = Graph.systemId
            text = "System"
            kind = Special Directory
            name = Some "SYSTEM"
            beforeTrash = true }
          { nodeId = Graph.trashId
            text = "Trash"
            kind = Special Directory
            name = Some "TRASH"
            beforeTrash = false } ]

    let private canonicalRow (spec: CanonicalSpec) : NodeRow =
        let node =
            match spec.name with
            | Some n ->
                Node.Create(
                    spec.nodeId,
                    text = spec.text,
                    name = Filename.Ok n,
                    kind = spec.kind)
            | None ->
                Node.Create(spec.nodeId, text = spec.text, kind = spec.kind)
        GraphProjection.nodeRowFromNode node

    let private setOwnership parentId ordinal childId ownership (children: ChildRow list) =
        children
        |> List.map (fun (r: ChildRow) ->
            if r.parentId = parentId && r.ordinal = ordinal && r.childId = childId then
                { r with ownership = ownership }
            else
                r)

    let private childrenOf parentId (children: ChildRow list) =
        children
        |> List.filter (fun (r: ChildRow) -> r.parentId = parentId)
        |> List.sortBy (fun (r: ChildRow) -> r.ordinal)

    let private insertOwnedUnderRoot rootId childId beforeTrash (children: ChildRow list) =
        let rootKids = childrenOf rootId children
        let insertAt =
            if not beforeTrash then
                List.length rootKids
            else
                match
                    rootKids
                    |> List.tryFindIndex (fun (r: ChildRow) ->
                        r.childId = Graph.trashId.Value)
                with
                | Some i -> i
                | None -> List.length rootKids
        let inserted: ChildRow =
            { parentId = rootId
              ordinal = insertAt
              childId = childId
              ownership = Ownership.Owner }
        let before, after = rootKids |> List.splitAt insertAt
        let newRoot =
            (before @ [ inserted ] @ after)
            |> List.mapi (fun i (r: ChildRow) -> { r with ordinal = i })
        let withoutRoot =
            children |> List.filter (fun (r: ChildRow) -> r.parentId <> rootId)
        withoutRoot @ newRoot

    let private ensureOneCanonical
        rootId
        (nodes: NodeRow list)
        (children: ChildRow list)
        (spec: CanonicalSpec)
        =
        let id = spec.nodeId.Value
        let nodes' =
            if nodes |> List.exists (fun (n: NodeRow) -> n.id = id) then nodes
            else nodes @ [ canonicalRow spec ]
        match
            children
            |> List.tryFind (fun (r: ChildRow) -> r.parentId = rootId && r.childId = id)
        with
        | Some row when row.ownership = Ownership.Owner -> nodes', children
        | Some row ->
            nodes', setOwnership rootId row.ordinal id Ownership.Owner children
        | None ->
            nodes', insertOwnedUnderRoot rootId id spec.beforeTrash children

    let private ensureCanonicals rootId nodes children =
        canonicalSpecs
        |> List.fold
            (fun (n, c) spec -> ensureOneCanonical rootId n c spec)
            (nodes, children)

    let private demoteRootOwners rootId (children: ChildRow list) =
        children
        |> List.map (fun (r: ChildRow) ->
            if r.childId = rootId && r.ownership = Ownership.Owner then
                { r with ownership = Ownership.Ref }
            else
                r)

    let private promoteOnce rootId survivorIds (children: ChildRow list) =
        let owned = ownedFrom rootId children
        let missing =
            survivorIds
            |> Set.filter (fun id -> id <> rootId && not (Set.contains id owned))
        if Set.isEmpty missing then
            None
        else
            let candidates =
                children
                |> List.filter (fun (r: ChildRow) ->
                    r.ownership = Ownership.Ref && Set.contains r.childId missing)
            match candidates with
            | [] -> None
            | rows ->
                let best = rows |> List.minBy (refIngressKey owned)
                Some(
                    setOwnership
                        best.parentId
                        best.ordinal
                        best.childId
                        Ownership.Owner
                        children)

    let rec private promoteIngress rootId survivorIds children =
        match promoteOnce rootId survivorIds children with
        | None -> children
        | Some next -> promoteIngress rootId survivorIds next

    let private demoteOwnersOfChild (keep: ChildRow) (many: ChildRow list) acc =
        many
        |> List.fold
            (fun a (r: ChildRow) ->
                if r.parentId = keep.parentId && r.ordinal = keep.ordinal then a
                else setOwnership r.parentId r.ordinal r.childId Ownership.Ref a)
            acc

    let private demoteExtraOwners rootId (children: ChildRow list) =
        let owned = ownedFrom rootId children
        children
        |> List.filter (fun (r: ChildRow) -> r.ownership = Ownership.Owner)
        |> List.groupBy (fun (r: ChildRow) -> r.childId)
        |> List.fold
            (fun acc (_, owners) ->
                match owners with
                | []
                | [ _ ] -> acc
                | many ->
                    let keep: ChildRow =
                        many
                        |> List.minBy (fun (r: ChildRow) ->
                            ownedParentKey owned r.parentId)
                    demoteOwnersOfChild keep many acc)
            children

    let private ownerCountByChild (children: ChildRow list) =
        children
        |> List.filter (fun (r: ChildRow) -> r.ownership = Ownership.Owner)
        |> List.groupBy (fun (r: ChildRow) -> r.childId)
        |> List.map (fun (id, rows) -> id, List.length rows)
        |> Map.ofList

    let private ownerParentMap (children: ChildRow list) =
        children
        |> List.filter (fun (r: ChildRow) -> r.ownership = Ownership.Owner)
        |> List.map (fun (r: ChildRow) -> r.childId, r.parentId)
        |> Map.ofList

    let rec private reachesRoot rootId ownerOf visited id =
        if id = rootId then true
        elif Set.contains id visited then false
        else
            match Map.tryFind id ownerOf with
            | None -> false
            | Some parent -> reachesRoot rootId ownerOf (Set.add id visited) parent

    let private validate rootId (nodes: NodeRow list) (children: ChildRow list) =
        let ids = nodeIdSet nodes
        let counts = ownerCountByChild children
        let rootOwners = Map.tryFind rootId counts |> Option.defaultValue 0
        if rootOwners <> 0 then
            Error "ROOT has incoming owner"
        elif
            ids
            |> Set.exists (fun id ->
                id <> rootId && Map.tryFind id counts <> Some 1)
        then
            Error "survivor lacks unique owner"
        else
            let ownerOf = ownerParentMap children
            let bad =
                ids
                |> Set.exists (fun id ->
                    id <> rootId
                    && not (reachesRoot rootId ownerOf Set.empty id))
            if bad then Error "owned chain does not reach ROOT" else Ok ()

    let private origPairMap (original: ChildRow list) =
        original
        |> List.map (fun (r: ChildRow) -> (r.parentId, r.childId), r)
        |> Map.ofList

    let private diffOwnership
        (origByPair: Map<Guid * Guid, ChildRow>)
        (working: ChildRow list)
        : OwnershipUpdate list =
        working
        |> List.choose (fun (w: ChildRow) ->
            match Map.tryFind (w.parentId, w.childId) origByPair with
            | Some o when o.ordinal = w.ordinal && o.ownership <> w.ownership ->
                let update: OwnershipUpdate =
                    { parentId = w.parentId
                      ordinal = w.ordinal
                      childId = w.childId
                      ownership = w.ownership }
                Some update
            | _ -> None)

    let rec private zipRootOrdinals origs works acc =
        match origs, works with
        | [], [] -> List.rev acc
        | (o: ChildRow) :: ot, (w: ChildRow) :: wt ->
            let acc' =
                if o.ordinal = w.ordinal then
                    acc
                else
                    { childId = w.childId
                      fromOrdinal = o.ordinal
                      ordinal = w.ordinal }
                    :: acc
            zipRootOrdinals ot wt acc'
        | _ -> List.rev acc

    let private diffRootOrdinals
        rootId
        (original: ChildRow list)
        (origPairs: Set<Guid * Guid>)
        (working: ChildRow list)
        : RootOrdinalUpdate list =
        let origRoot = childrenOf rootId original
        let workingKept =
            childrenOf rootId working
            |> List.filter (fun (w: ChildRow) ->
                Set.contains (w.parentId, w.childId) origPairs)
        zipRootOrdinals origRoot workingKept []

    let private toPlan
        rootId
        (originalNodes: NodeRow list)
        (originalChildren: ChildRow list)
        (workingNodes: NodeRow list)
        (workingChildren: ChildRow list)
        deleteIds
        : Plan =
        let origIds = nodeIdSet originalNodes
        let origPairs =
            originalChildren
            |> List.map (fun (r: ChildRow) -> r.parentId, r.childId)
            |> Set.ofList
        let origByPair = origPairMap originalChildren
        let insertNodes =
            workingNodes |> List.filter (fun (n: NodeRow) -> not (Set.contains n.id origIds))
        let insertChildren =
            workingChildren
            |> List.filter (fun (w: ChildRow) ->
                not (Set.contains (w.parentId, w.childId) origPairs))
        let ownershipUpdates = diffOwnership origByPair workingChildren
        let rootOrdinalUpdates =
            diffRootOrdinals rootId originalChildren origPairs workingChildren
        let affected =
            deleteIds
            @ (ownershipUpdates |> List.map _.childId)
            @ (insertNodes |> List.map (fun (n: NodeRow) -> n.id))
            @ (insertChildren |> List.map (fun (c: ChildRow) -> c.childId))
            @ (rootOrdinalUpdates |> List.map _.childId)
            |> List.distinct
            |> List.sort
        { deleteNodeIds = deleteIds
          ownershipUpdates = ownershipUpdates
          insertNodes = insertNodes
          insertChildren = insertChildren
          rootOrdinalUpdates = rootOrdinalUpdates
          logFacts =
            { deletedCount = List.length deleteIds
              ownershipUpdateCount = List.length ownershipUpdates
              insertNodeCount = List.length insertNodes
              insertChildCount = List.length insertChildren
              ordinalShiftCount = List.length rootOrdinalUpdates
              affectedNodeIds = affected } }

    let private repairSurvivors rootId nodes children =
        let withNodes, withChildren = ensureCanonicals rootId nodes children
        let afterRoot = demoteRootOwners rootId withChildren
        let afterPromote = promoteIngress rootId (nodeIdSet withNodes) afterRoot
        withNodes, demoteExtraOwners rootId afterPromote

    let plan
        (rootId: Guid)
        (protectedNodeIds: Guid list)
        (nodes: NodeRow list)
        (children: ChildRow list)
        : Result<Plan, string> =
        let ids = nodeIdSet nodes
        if not (Set.contains rootId ids) then
            Error missingRoot
        else
            let live = reachableFrom rootId children
            let protectedSet = Set.ofList protectedNodeIds
            let deleteIds =
                ids
                |> Set.filter (fun id ->
                    not (Set.contains id live) && not (Set.contains id protectedSet))
                |> Set.toList
                |> List.sort
            let survivorIds =
                ids
                |> Set.filter (fun id ->
                    Set.contains id live || Set.contains id protectedSet)
            let survivorNodes =
                nodes |> List.filter (fun (n: NodeRow) -> Set.contains n.id survivorIds)
            let survivorChildren =
                children
                |> List.filter (fun (r: ChildRow) ->
                    Set.contains r.parentId survivorIds
                    && Set.contains r.childId survivorIds)
            let workingNodes, workingChildren =
                repairSurvivors rootId survivorNodes survivorChildren
            match validate rootId workingNodes workingChildren with
            | Error e -> Error e
            | Ok () ->
                Ok(
                    toPlan
                        rootId
                        survivorNodes
                        survivorChildren
                        workingNodes
                        workingChildren
                        deleteIds)
