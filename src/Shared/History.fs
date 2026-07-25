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
            parentId
            :: ((oldChildren @ newChildren)
                |> List.choose (fun child ->
                    if child.ref = Ownership.Owner then Some child.id else None))
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

        let ownedAreDocumentRoots children =
            children
            |> List.filter (fun child -> child.ref = Ownership.Owner)
            |> List.forall (fun child ->
                DocumentPartition.isDocumentRootNode graph child.id)

        match op with
        | Op.Replace(parentId, _, oldChildren, newChildren) ->
            let stubAttachUnderShell =
                isUnparsedTreeShell parentId
                && ownedAreDocumentRoots oldChildren
                && ownedAreDocumentRoots newChildren
            let parentBlocked =
                if isCurrentDocumentRoot graph parentId then false
                elif stubAttachUnderShell then false
                else nodeBlocked parentId
            // Document roots may move as opaque units; their Unparsed state
            // must not block sibling reorder / reparent under a Current parent.
            let childBlocked =
                (oldChildren @ newChildren)
                |> List.exists (fun child ->
                    child.ref = Ownership.Owner
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
                          graph =
                              Graph.fromNodes
                                  state.graph.root
                                  (state.graph.nodes |> Map.add nodeId node) }
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
                    child.ref = Ownership.Owner
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
                              graph =
                                  Graph.fromNodes
                                      state.graph.root
                                      (state.graph.nodes |> Map.add nodeId node) }
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
                match child.ref with
                | Ownership.Owner -> Some(child.id, parentId)
                | Ownership.Ref -> None)
            |> List.groupBy fst
            |> List.map (fun (childId, pairs) -> childId, (pairs |> List.map snd))
            |> Map.ofList

        let childIdsToCheck =
            match childIdsScope with
            | None -> allChildIds
            | Some ids -> Set.intersect ids allChildIds

        let childIdsMissingOwner =
            childIdsToCheck
            |> Seq.filter (fun childId -> not (Map.containsKey childId ownerByChildId))
            |> Seq.toList

        if not childIdsMissingOwner.IsEmpty then
            Error (
                "invalid ownership semantics: missing owner occurrence",
                List.head childIdsMissingOwner)
        else
            let childIdsWithMultipleOwners =
                childIdsToCheck
                |> Seq.filter (fun childId ->
                    match Map.tryFind childId ownerByChildId with
                    | None -> false
                    | Some owners -> owners.Length <> 1)
                |> Seq.toList

            if not childIdsWithMultipleOwners.IsEmpty then
                Error (
                    "invalid ownership semantics: expected exactly one owner occurrence",
                    List.head childIdsWithMultipleOwners)
            else
                let ownerParentOf childId = ownerByChildId.[childId] |> List.head

                let rec reachesRootWithoutCycle (currentId: NodeId) (visited: Set<NodeId>) =
                    if currentId = graph.root then
                        true
                    elif Set.contains currentId visited then
                        false
                    elif ownerByChildId |> Map.containsKey currentId then
                        let parentId = ownerParentOf currentId
                        reachesRootWithoutCycle parentId (Set.add currentId visited)
                    else
                        false

                let brokenOwnerChainChild =
                    childIdsToCheck
                    |> Seq.tryFind (fun childId ->
                        let ownerParent = ownerParentOf childId
                        not (reachesRootWithoutCycle ownerParent Set.empty))

                match brokenOwnerChainChild with
                | Some childId ->
                    Error (
                        "invalid ownership semantics: owner chain does not reach root",
                        childId)
                | None ->
                    match childIdsScope with
                    | Some _ -> Ok ()
                    | None ->
                        let invalidPlacementChild =
                            allChildren
                            |> Seq.tryPick (fun (parentId, child) ->
                                match child.ref, Map.tryFind child.id graph.nodes with
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
                                Error (
                                    "invalid ownership semantics: duplicate name in artifact directory",
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
                            newChildren |> List.exists (fun c -> c.ref = Ownership.Owner)
                            && GraphQuery.artifactNameConflict graph parentId newChildren
                        then
                            Some
                                "invalid ownership semantics: duplicate name in artifact directory"
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

