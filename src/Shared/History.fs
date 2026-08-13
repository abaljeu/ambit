namespace Gambol.Shared

[<RequireQualifiedAccess>]
type Op =
    | NewNode of nodeId: NodeId * text: string
    | SetText of nodeId: NodeId * oldText: string * newText: string
    | SetClasses of nodeId: NodeId * oldClasses: CssClasses * newClasses: CssClasses
    | Replace of
        parentId: NodeId *
        index: int *
        oldChildren: ChildNode list *
        newChildren: ChildNode list
    | NewSpecialNode of nodeId: NodeId * kind: SpecialKind * name: string
    | SetName of nodeId: NodeId * oldName: string * newName: string
    | SetDocumentState of
        nodeId: NodeId *
        oldState: DocumentState *
        newState: DocumentState
    /// Server disk mtime after persist. `oldTime` is for undo; apply ignores mismatch.
    | SetUpdateTime of nodeId: NodeId * oldTime: System.DateTime * newTime: System.DateTime


type Change =
    { id: int
      changeId: System.Guid   // unique per network submission; used for server-side dedup
      ops: Op list }


[<RequireQualifiedAccess>]
type ChangeRequest =
    | Change of Change
    | Undo of id: int * changeId: System.Guid
    | Redo of id: int * changeId: System.Guid


[<RequireQualifiedAccess>]
module ChangeRequest =
    let baseRevision =
        function
        | ChangeRequest.Change change -> change.id
        | ChangeRequest.Undo(id, _)
        | ChangeRequest.Redo(id, _) -> id

    let actionId =
        function
        | ChangeRequest.Change change -> change.changeId
        | ChangeRequest.Undo(_, changeId)
        | ChangeRequest.Redo(_, changeId) -> changeId

    let withBaseRevision id =
        function
        | ChangeRequest.Change change ->
            ChangeRequest.Change { change with id = id }
        | ChangeRequest.Undo(_, changeId) ->
            ChangeRequest.Undo(id, changeId)
        | ChangeRequest.Redo(_, changeId) ->
            ChangeRequest.Redo(id, changeId)


type History =
    { past: Change list
      future: Change list
      nextId: int }


type State =
    { graph: Graph
      history: History
      revision: Revision }


[<RequireQualifiedAccess>]
type ApplyResult =
    | Changed of State
    | Unchanged of State
    | Invalid of State * string


[<RequireQualifiedAccess>]
module Op =
    [<Literal>]
    let private unparsedDocumentError =
        "operation cannot modify an unparsed document; parse it first"

    [<Literal>]
    let private reservedPathError =
        "owned artifact path contains a reserved system name"

    let private fromGraphResult (state: State) (result: Result<Graph, string>) : ApplyResult =
        match result with
        | Ok graph -> ApplyResult.Changed { state with graph = graph }
        | Error msg -> ApplyResult.Invalid(state, msg)

    let private fromGraphResultUnchanged (state: State) (result: Result<Graph, string>) : ApplyResult =
        match result with
        | Ok graph -> ApplyResult.Changed { state with graph = graph }
        | Error msg -> ApplyResult.Invalid(state, msg)

    /// Node ids whose ownership facts this op can flip (Owner edges added/removed,
    /// or a newly introduced node). Replace parent is excluded: being the edit site
    /// does not change the parent's own owner-occurrence / chain; placement and
    /// artifact-name checks for that parent run separately in validateOwnershipForChange.
    let involvedNodeIds (graph: Graph) (op: Op) : NodeId list =
        match op with
        | Op.NewNode(nodeId, _)
        | Op.NewSpecialNode(nodeId, _, _) ->
            if Map.containsKey nodeId graph.nodes then [ nodeId ] else []
        | Op.SetText(nodeId, _, _)
        | Op.SetClasses(nodeId, _, _)
        | Op.SetName(nodeId, _, _)
        | Op.SetUpdateTime(nodeId, _, _) -> [ nodeId ]
        | Op.Replace(parentId, _, oldChildren, newChildren) ->
            (oldChildren @ newChildren)
            |> List.choose (fun child ->
                if Node.childOwnership graph parentId child = Ownership.Owner then
                    Some child.id
                else
                    None)
        | Op.SetDocumentState _ -> []

    let private isCurrentDocumentRoot (graph: Graph) (nodeId: NodeId) : bool =
        match Map.tryFind nodeId graph.nodes with
        | Some node ->
            DocumentPartition.isDocumentRootNode graph nodeId
            && node.documentState = Current
        | None -> false

    /// Inaccessible document membership blocks content edits. Structural Replace is allowed
    /// when relocating an inaccessible document root as an opaque unit under a
    /// Current parent (Move Up/Down, Move Selection to Start/End, indent).
    /// Replace that mutates inside an Unparsed document remains blocked, with
    /// two exceptions: Replace under a Current document root (nested parse while
    /// an enclosing Directory/Workspace is Unparsed), and attaching/detaching
    /// document-root stubs under an Unparsed Directory/Workspace shell.
    let private isBlockedByInaccessibleDocument (op: Op) (graph: Graph) : bool =
        let nodeBlocked nodeId =
            DocumentPartition.isMemberOfInaccessibleDocument graph nodeId

        let isUnparsedTreeShell nodeId =
            match Map.tryFind nodeId graph.nodes with
            | Some { kind = Special(Directory | Workspace)
                     documentState = Unparsed } -> true
            | _ -> false

        let ownedAreDocumentRoots parentId children =
            children
            |> List.filter (fun child ->
                Node.childOwnership graph parentId child = Ownership.Owner)
            |> List.forall (fun child ->
                DocumentPartition.isDocumentRootNode graph child.id)

        match op with
        | Op.Replace(parentId, _, oldChildren, newChildren) ->
            let stubAttachUnderShell =
                isUnparsedTreeShell parentId
                && ownedAreDocumentRoots parentId oldChildren
                && ownedAreDocumentRoots parentId newChildren
            let parentBlocked =
                if isCurrentDocumentRoot graph parentId then false
                elif stubAttachUnderShell then false
                else nodeBlocked parentId
            // Document roots may move as opaque units; their Unparsed state
            // must not block sibling reorder / reparent under a Current parent.
            let childBlocked =
                (oldChildren @ newChildren)
                |> List.exists (fun child ->
                    Node.childOwnership graph parentId child = Ownership.Owner
                    && nodeBlocked child.id
                    && not (DocumentPartition.isDocumentRootNode graph child.id))
            parentBlocked || childBlocked
        | _ ->
            involvedNodeIds graph op
            |> List.distinct
            |> List.exists nodeBlocked

    let private applyAllowed (op: Op) (state: State) : ApplyResult =
        match op with
        | Op.NewNode(nodeId, text) ->
            if nodeId = Graph.rootId then
                ApplyResult.Invalid(state, "cannot NewNode with canonical root id")
            else
                let node: Node =
                    Node.Create(nodeId, text = text, updateTime = NodeUpdateTime.now ())

                ApplyResult.Changed
                    { state with
                          graph = Graph.addDetachedNode node state.graph }
        | Op.SetText(nodeId, oldText, newText) ->
            Graph.setText nodeId oldText newText state.graph
            |> fromGraphResult state
        | Op.SetClasses(nodeId, oldClasses, newClasses) ->
            Graph.setClasses nodeId oldClasses newClasses state.graph
            |> fromGraphResult state
        | Op.Replace(parentId, index, oldChildren, newChildren) ->
            match Graph.replace parentId index oldChildren newChildren state.graph with
            | Error msg -> ApplyResult.Invalid(state, msg)
            | Ok graph ->
                let isInvalidOwner child =
                    Node.childOwnership graph parentId child = Ownership.Owner
                    && DocumentPartition.ownedSubtreeHasReservedArtifactPath
                        graph Set.empty child.id
                if List.exists isInvalidOwner newChildren then
                    ApplyResult.Invalid(state, reservedPathError)
                else
                    ApplyResult.Changed { state with graph = graph }
        | Op.NewSpecialNode(nodeId, kind, name) ->
            if Graph.isCanonicalNode nodeId then
                ApplyResult.Invalid(state, "cannot NewSpecialNode with canonical id")
            elif kind = Workspaces then
                ApplyResult.Invalid(state, "cannot NewSpecialNode with system-only kind")
            elif Filename.isReservedSystemName name then
                ApplyResult.Invalid(state, "reserved system name for NewSpecialNode")
            else
                match Filename.create name with
                | Filename.Empty | Filename.Invalid _ ->
                    ApplyResult.Invalid(state, "invalid filename for NewSpecialNode")
                | Filename.Ok _ ->
                    let node: Node =
                        Node.Create(
                            nodeId,
                            text = name,
                            name = Filename.Ok name,
                            kind = Special kind,
                            updateTime = NodeUpdateTime.now ())
                    ApplyResult.Changed
                        { state with
                              graph = Graph.addDetachedNode node state.graph }
        | Op.SetName(nodeId, oldName, newName) ->
            Graph.setName nodeId oldName newName state.graph
            |> fromGraphResult state
        | Op.SetDocumentState(nodeId, oldState, newState) ->
            Graph.setDocumentState nodeId oldState newState state.graph
            |> fromGraphResult state
        | Op.SetUpdateTime(nodeId, _oldTime, newTime) ->
            match Map.tryFind nodeId state.graph.nodes with
            | None -> ApplyResult.Invalid(state, "node not found")
            | Some node ->
                let stamped = NodeUpdateTime.withStamp newTime node
                if stamped.updateTime = node.updateTime then
                    ApplyResult.Unchanged state
                else
                    ApplyResult.Changed
                        { state with
                            graph =
                                { state.graph with
                                    nodes = Map.add nodeId stamped state.graph.nodes } }

    let apply (op: Op) (state: State) : ApplyResult =
        if isBlockedByInaccessibleDocument op state.graph then
            ApplyResult.Invalid(state, unparsedDocumentError)
        else
            applyAllowed op state

    let private undoAllowed (op: Op) (state: State) : ApplyResult =
        match op with
        | Op.NewNode(nodeId, _) ->
            let nodes = state.graph.nodes |> Map.remove nodeId
            ApplyResult.Changed
                { state with graph = Graph.fromNodes state.graph.root nodes }
        | Op.SetText(nodeId, oldText, newText) ->
            Graph.setText nodeId newText oldText state.graph
            |> fromGraphResult state
        | Op.SetClasses(nodeId, oldClasses, newClasses) ->
            Graph.setClasses nodeId newClasses oldClasses state.graph
            |> fromGraphResult state
        | Op.Replace(parentId, index, oldChildren, newChildren) ->
            // Inverse: swap old/new to restore
            Graph.replace parentId index newChildren oldChildren state.graph
            |> fromGraphResult state
        | Op.NewSpecialNode(nodeId, _, _) ->
            let nodes = state.graph.nodes |> Map.remove nodeId
            ApplyResult.Changed
                { state with graph = Graph.fromNodes state.graph.root nodes }
        | Op.SetName(nodeId, oldName, newName) ->
            Graph.setName nodeId newName oldName state.graph
            |> fromGraphResult state
        | Op.SetDocumentState(nodeId, oldState, newState) ->
            Graph.setDocumentState nodeId newState oldState state.graph
            |> fromGraphResult state
        | Op.SetUpdateTime(nodeId, oldTime, _newTime) ->
            match Map.tryFind nodeId state.graph.nodes with
            | None -> ApplyResult.Invalid(state, "node not found")
            | Some node ->
                let restored = NodeUpdateTime.withStamp oldTime node
                if restored.updateTime = node.updateTime then
                    ApplyResult.Unchanged state
                else
                    ApplyResult.Changed
                        { state with
                            graph =
                                { state.graph with
                                    nodes = Map.add nodeId restored state.graph.nodes } }

    let undo (op: Op) (state: State) : ApplyResult =
        if isBlockedByInaccessibleDocument op state.graph then
            ApplyResult.Invalid(state, unparsedDocumentError)
        else
            undoAllowed op state


[<RequireQualifiedAccess>]
module Change =
    let addOp (op: Op) (change: Change) : Change =
        { change with ops = change.ops @ [ op ] }

    /// Construct the inverse of a change: reversed op list, each op with old/new swapped.
    /// Change.undo(invert c) re-applies c's effect (valid for SetText and Replace).
    /// NewNode has no DeleteNode counterpart, so its inversion is imperfect; undo-of-undo
    /// for splits will return ApplyResult.Invalid and leave state unchanged.
    let invert (change: Change) : Change =
        let invertOp op =
            match op with
            | Op.NewNode(id, text)                   -> Op.NewNode(id, text)
            | Op.SetText(id, old, new_)              -> Op.SetText(id, new_, old)
            | Op.SetClasses(id, oldCls, newCls)      -> Op.SetClasses(id, newCls, oldCls)
            | Op.Replace(pid, i, olds, news)         -> Op.Replace(pid, i, news, olds)
            | Op.NewSpecialNode(id, kind, name)      -> Op.NewSpecialNode(id, kind, name)
            | Op.SetName(id, old, new_)              -> Op.SetName(id, new_, old)
            | Op.SetDocumentState(id, old, new_)     -> Op.SetDocumentState(id, new_, old)
            | Op.SetUpdateTime(id, old, new_)        -> Op.SetUpdateTime(id, new_, old)
        { change with
            changeId = System.Guid.NewGuid()
            ops = change.ops |> List.rev |> List.map invertOp }

    let apply (change: Change) (state: State) : ApplyResult =
        let step (accState, hasChanged) op =
            match Op.apply op accState with
            | ApplyResult.Invalid _ as err -> Error err
            | ApplyResult.Unchanged s' -> Ok(s', hasChanged)
            | ApplyResult.Changed s' -> Ok(s', true)

        let result =
            change.ops
            |> List.fold
                (fun acc op ->
                    match acc with
                    | Error err -> Error err
                    | Ok (s, changed) -> step (s, changed) op)
                (Ok(state, false))

        match result with
        | Error (ApplyResult.Invalid(_, message)) ->
            ApplyResult.Invalid(state, message)
        | Error err -> err
        | Ok (s, false) -> ApplyResult.Unchanged s
        | Ok (s, true) -> ApplyResult.Changed s

    let undo (change: Change) (state: State) : ApplyResult =
        let step (accState, hasChanged) op =
            match Op.undo op accState with
            | ApplyResult.Invalid _ as err -> Error err
            | ApplyResult.Unchanged s' -> Ok(s', hasChanged)
            | ApplyResult.Changed s' -> Ok(s', true)

        let result =
            change.ops
            |> List.rev
            |> List.fold
                (fun acc op ->
                    match acc with
                    | Error err -> Error err
                    | Ok (s, changed) -> step (s, changed) op)
                (Ok(state, false))

        match result with
        | Error (ApplyResult.Invalid(_, message)) ->
            ApplyResult.Invalid(state, message)
        | Error err -> err
        | Ok (s, false) -> ApplyResult.Unchanged s
        | Ok (s, true) -> ApplyResult.Changed s



[<RequireQualifiedAccess>]
module History =
    let private validateOwnershipSemantics
        (graph: Graph)
        (childIdsScope: Set<NodeId> option)
        : Result<unit, string * NodeId> =
        let allChildren =
            graph.nodes
            |> Map.toList
            |> List.collect (fun (parentId, node) ->
                node.children |> List.map (fun child -> parentId, child))

        let allChildIds =
            allChildren |> List.map (fun (_, child) -> child.id) |> Set.ofList

        let ownerByChildId =
            allChildren
            |> List.choose (fun (parentId, child) ->
                match Node.childOwnership graph parentId child with
                | Ownership.Owner -> Some(child.id, parentId)
                | Ownership.Ref -> None)
            |> List.groupBy fst
            |> List.map (fun (childId, pairs) -> childId, (pairs |> List.map snd))
            |> Map.ofList

        let childIdsToCheck =
            match childIdsScope with
            | None -> allChildIds
            | Some ids -> Set.intersect ids allChildIds

        let locatedChildDetail (id: NodeId) : string =
            let text =
                match Map.tryFind id graph.nodes with
                | Some n when n.text.Length <= 80 -> n.text
                | Some n -> n.text.Substring(0, 80) + "..."
                | None -> ""
            let tail = NodeId.GuidTail8 id.Value
            $"text='{text}' id={tail}"

        // Prefer Owner parent from Loaded lists; else Node.owner (resident claim).
        let ownerParentOf (childId: NodeId) : NodeId option =
            match Map.tryFind childId ownerByChildId with
            | Some (parentId :: _) -> Some parentId
            | Some []
            | None ->
                Map.tryFind childId graph.nodes
                |> Option.map (fun n -> n.owner)

        // Proven missing only when a non-ROOT claimed owner is Loaded without
        // an Owner edge. ROOT (Create/appendChildren default) is incomplete under
        // selective load: the real Owner parent may be Unloaded elsewhere.
        let isProvenMissingOwner (childId: NodeId) : bool =
            match Map.tryFind childId ownerByChildId with
            | Some _ -> false
            | None ->
                match Map.tryFind childId graph.nodes with
                | None -> false
                | Some childNode when childNode.owner = graph.root -> false
                | Some childNode ->
                    match Map.tryFind childNode.owner graph.nodes with
                    | None -> false
                    | Some { childrenStatus = Unloaded } -> false
                    | Some _ -> true

        let childIdsMissingOwner =
            childIdsToCheck
            |> Seq.filter isProvenMissingOwner
            |> Seq.toList

        if not childIdsMissingOwner.IsEmpty then
            let childId = List.head childIdsMissingOwner
            Error (
                $"invalid ownership semantics: missing owner occurrence [{locatedChildDetail childId}]",
                childId)
        else
            let childIdsWithMultipleOwners =
                childIdsToCheck
                |> Seq.filter (fun childId ->
                    match Map.tryFind childId ownerByChildId with
                    | None -> false
                    | Some owners -> owners.Length <> 1)
                |> Seq.toList

            if not childIdsWithMultipleOwners.IsEmpty then
                let childId = List.head childIdsWithMultipleOwners
                let owners = ownerByChildId.[childId]
                let ownersStr =
                    owners
                    |> List.map (fun id -> NodeId.GuidTail8 id.Value)
                    |> String.concat ","
                Error (
                    $"invalid ownership semantics: expected exactly one owner occurrence [{locatedChildDetail childId} owners={ownersStr}]",
                    childId)
            else
                // true = reaches root; false = cycle/broken; None = incomplete hop
                let rec reachesRootWithoutCycle
                    (currentId: NodeId)
                    (visited: Set<NodeId>)
                    : bool option =
                    if currentId = graph.root then
                        Some true
                    elif Set.contains currentId visited then
                        Some false
                    else
                        match Map.tryFind currentId graph.nodes with
                        | None -> None
                        | Some _ ->
                            match ownerParentOf currentId with
                            | None -> None
                            | Some parentId ->
                                match Map.tryFind parentId graph.nodes with
                                | None when parentId <> graph.root -> None
                                | _ ->
                                    reachesRootWithoutCycle
                                        parentId
                                        (Set.add currentId visited)

                let ownerChainIds (startId: NodeId) : NodeId list =
                    let rec loop currentId visited acc =
                        if Set.contains currentId visited then
                            List.rev (currentId :: acc)
                        else
                            let nextAcc = currentId :: acc
                            let nextVisited = Set.add currentId visited
                            if currentId = graph.root then
                                List.rev nextAcc
                            else
                                match ownerParentOf currentId with
                                | Some parentId ->
                                    loop parentId nextVisited nextAcc
                                | None -> List.rev nextAcc
                    loop startId Set.empty []

                let formatOwnerChain (ids: NodeId list) : string =
                    ids
                    |> List.map (fun id -> NodeId.GuidTail8 id.Value)
                    |> String.concat " -> "

                let brokenOwnerChainChild =
                    childIdsToCheck
                    |> Seq.tryFind (fun childId ->
                        match ownerParentOf childId with
                        | None -> false
                        | Some ownerParent ->
                            reachesRootWithoutCycle ownerParent Set.empty = Some false)

                match brokenOwnerChainChild with
                | Some childId ->
                    let chain = formatOwnerChain (ownerChainIds childId)
                    Error (
                        $"invalid ownership semantics: owner chain does not reach root [{locatedChildDetail childId} chain={chain}]",
                        childId)
                | None ->
                    match childIdsScope with
                    | Some _ -> Ok ()
                    | None ->
                        let invalidPlacementChild =
                            allChildren
                            |> Seq.tryPick (fun (parentId, child) ->
                                match
                                    Node.childOwnership graph parentId child,
                                    Map.tryFind child.id graph.nodes
                                with
                                | Ownership.Owner,
                                  Some { kind = Special (File | Directory) }
                                    when not (Graph.isSystemDirectoryNode child.id) ->
                                    if not (
                                        GraphQuery.containerOrDescendant graph parentId) then
                                        Some child.id
                                    else
                                        None
                                | _ -> None)

                        match invalidPlacementChild with
                        | Some childId ->
                            Error (
                                "invalid ownership semantics: File and Directory nodes must have a Workspace or Directory owner ancestor (not under a File)",
                                childId)
                        | None ->
                            match GraphQuery.tryFindArtifactNameDuplicate graph with
                            | Some dupId ->
                                let name =
                                    Map.tryFind dupId graph.nodes
                                    |> Option.bind (fun n -> Filename.tryValue n.name)
                                    |> Option.defaultValue "?"
                                Error (
                                    $"invalid ownership semantics: duplicate name '{name}' in artifact directory",
                                    dupId)
                            | None -> Ok ()

    let empty: History =
        { past = []
          future = []
          nextId = 0 }

    let newChange (history: History) : Change =
        { id = history.nextId
          changeId = System.Guid.NewGuid()
          ops = [] }

    let validateOwnershipLocated (graph: Graph) : Result<unit, string * NodeId> =
        validateOwnershipSemantics graph None

    let validateOwnership (graph: Graph) : Result<unit, string> =
        match validateOwnershipLocated graph with
        | Ok () -> Ok ()
        | Error (msg, _) -> Error msg

    let private opChangesGraphShape =
        function
        | Op.Replace _ | Op.NewNode _ | Op.NewSpecialNode _ -> true
        | Op.SetText _ | Op.SetClasses _ | Op.SetName _ | Op.SetDocumentState _
        | Op.SetUpdateTime _ -> false

    let private invalidOwnedFileDirectoryPlacement
        (graph: Graph)
        (parentId: NodeId)
        (newChildren: ChildNode list)
        : bool
        =
        GraphQuery.invalidOwnedFileDirectoryPlacement graph parentId newChildren

    let private validateOwnershipForChange (graph: Graph) (change: Change) : Result<unit, string> =
        let shapeOps = change.ops |> List.filter opChangesGraphShape

        if List.isEmpty shapeOps then
            Ok ()
        else
            let childIds =
                shapeOps
                |> List.collect (Op.involvedNodeIds graph)
                |> Set.ofList

            match validateOwnershipSemantics graph (Some childIds) with
            | Error (msg, _) -> Error msg
            | Ok () ->
                shapeOps
                |> List.tryPick (fun op ->
                    match op with
                    | Op.Replace(parentId, _, _, newChildren) ->
                        if invalidOwnedFileDirectoryPlacement graph parentId newChildren then
                            Some
                                "invalid ownership semantics: File and Directory nodes must have a Workspace or Directory owner ancestor (not under a File)"
                        elif
                            newChildren
                            |> List.exists (fun c ->
                                Node.childOwnership graph parentId c = Ownership.Owner)
                            && GraphQuery.artifactNameConflict graph parentId newChildren
                        then
                            let name =
                                newChildren
                                |> List.tryPick (fun c ->
                                    if Node.childOwnership graph parentId c
                                       <> Ownership.Owner then
                                        None
                                    else
                                        Map.tryFind c.id graph.nodes
                                        |> Option.bind (fun n ->
                                            Filename.tryValue n.name))
                                |> Option.defaultValue "?"
                            Some
                                $"invalid ownership semantics: duplicate name '{name}' in artifact directory"
                        else
                            None
                    | _ -> None)
                |> Option.map Error
                |> Option.defaultValue (Ok ())

    let addChange (change: Change) (history: History) : History =
        let nextId = max history.nextId (change.id + 1)
        // Emacs stack model: instead of discarding the future on a new change, fold it back
        // into past as inverse changes. Subsequent undos will re-apply those inverses,
        // giving "undo the undo" (redo-via-undo) without a separate redo stack clearing.
        let requeued = history.future |> List.map Change.invert
        { history with
              past = change :: requeued @ history.past
              future = []
              nextId = nextId }

    /// Apply a server-trusted change: ops + history, no ownership check.
    /// Poll/tail replay trusts the server log; local edits use applyChange instead.
    let applyChangeTrusted (change: Change) (state: State) : ApplyResult =
        match Change.apply change state with
        | ApplyResult.Invalid _ as err -> err
        | ApplyResult.Unchanged s -> ApplyResult.Unchanged s
        | ApplyResult.Changed s ->
            let history' = addChange change s.history
            ApplyResult.Changed { s with history = history' }

    let applyChange (change: Change) (state: State) : ApplyResult =
        match applyChangeTrusted change state with
        | ApplyResult.Invalid _ as err -> err
        | ApplyResult.Unchanged s -> ApplyResult.Unchanged s
        | ApplyResult.Changed s ->
            match validateOwnershipForChange s.graph change with
            | Error msg -> ApplyResult.Invalid(state, msg)
            | Ok () -> ApplyResult.Changed s

    let undo (state: State) : ApplyResult =
        match state.history.past with
        | [] -> ApplyResult.Unchanged state
        | change :: restPast ->
            match Change.undo change state with
            | ApplyResult.Invalid _ as err -> err
            | ApplyResult.Unchanged s -> ApplyResult.Unchanged s
            | ApplyResult.Changed s ->
                let history' =
                    { s.history with
                        past = restPast
                        future = change :: s.history.future }

                ApplyResult.Changed { s with history = history' }

    let redo (state: State) : ApplyResult =
        match state.history.future with
        | [] -> ApplyResult.Unchanged state
        | change :: restFuture ->
            match Change.apply change state with
            | ApplyResult.Invalid _ as err -> err
            | ApplyResult.Unchanged s -> ApplyResult.Unchanged s
            | ApplyResult.Changed s ->
                let history' =
                    { s.history with
                        past = change :: s.history.past
                        future = restFuture }

                ApplyResult.Changed { s with history = history' }

    let private changedResult
        (actionName: string)
        (materialized: Change)
        (result: ApplyResult)
        : Result<State * Change, string> =
        match result with
        | ApplyResult.Changed state -> Ok(state, materialized)
        | ApplyResult.Unchanged _ -> Error(actionName + " did not change state")
        | ApplyResult.Invalid(_, message) -> Error message

    let applyAction
        (action: ChangeRequest)
        (state: State)
        : Result<State * Change, string> =
        match action with
        | ChangeRequest.Change change ->
            applyChange change state
            |> changedResult "Change" change
        | ChangeRequest.Undo(id, changeId) ->
            match state.history.past with
            | [] -> Error "Undo requires a past change"
            | change :: _ ->
                let materialized =
                    { Change.invert change with
                        id = id
                        changeId = changeId }
                undo state
                |> changedResult "Undo" materialized
        | ChangeRequest.Redo(id, changeId) ->
            match state.history.future with
            | [] -> Error "Redo requires a future change"
            | change :: _ ->
                let materialized =
                    { change with
                        id = id
                        changeId = changeId }
                redo state
                |> changedResult "Redo" materialized

/// After DocumentPersistence stamps artifact roots, emit ops for the change log / poll tail.
[<RequireQualifiedAccess>]
module PersistStamp =

    let opsBetween (before: Graph) (after: Graph) : Op list =
        after.nodes
        |> Map.toList
        |> List.choose (fun (id, afterNode) ->
            let newTime = NodeUpdateTime.toDbPrecision afterNode.updateTime
            match Map.tryFind id before.nodes with
            | Some beforeNode ->
                let oldTime = NodeUpdateTime.toDbPrecision beforeNode.updateTime
                if oldTime = newTime then
                    None
                else
                    Some(Op.SetUpdateTime(id, oldTime, newTime))
            | None ->
                if newTime = NodeUpdateTime.missing then
                    None
                else
                    Some(Op.SetUpdateTime(id, NodeUpdateTime.missing, newTime)))

    let appendToChange (change: Change) (stampOps: Op list) : Change =
        if stampOps.IsEmpty then
            change
        else
            { change with ops = change.ops @ stampOps }

    let appendToLast (changes: Change list) (stampOps: Op list) : Change list =
        if stampOps.IsEmpty || changes.IsEmpty then
            changes
        else
            match List.rev changes with
            | [] -> changes
            | last :: rest ->
                List.rev (appendToChange last stampOps :: rest)

    /// Apply stamp ops to a graph (submitter ack path; ignores history).
    let applyToGraph (stampOps: Op list) (graph: Graph) : Graph =
        if stampOps.IsEmpty then
            graph
        else
            let state =
                { graph = graph
                  history = History.empty
                  revision = Revision.Zero }
            let change =
                { id = 0
                  changeId = System.Guid.Empty
                  ops = stampOps }
            match Change.apply change state with
            | ApplyResult.Changed s
            | ApplyResult.Unchanged s -> s.graph
            | ApplyResult.Invalid _ -> graph

