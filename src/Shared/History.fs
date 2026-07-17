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

    let private involvedNodeIds (graph: Graph) (op: Op) : NodeId list =
        match op with
        | Op.NewNode(nodeId, _)
        | Op.NewSpecialNode(nodeId, _, _) ->
            if Map.containsKey nodeId graph.nodes then [ nodeId ] else []
        | Op.SetText(nodeId, _, _)
        | Op.SetClasses(nodeId, _, _)
        | Op.SetName(nodeId, _, _) -> [ nodeId ]
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

    /// Unparsed membership blocks edits. Exception: Replace whose parent is already a
    /// Current document root — needed when parsing a nested File while an enclosing
    /// Directory/Workspace is Unparsed (e.g. after `.amb` modification). Upload-built
    /// directories stay Current; this is not a substitute for that invariant.
    /// Only Owner children are re-checked, so removing an Unparsed stub from a Current
    /// parent remains blocked.
    let private isBlockedByUnparsedDocument (op: Op) (graph: Graph) : bool =
        let nodeBlocked nodeId =
            DocumentPartition.isMemberOfUnparsedDocument graph nodeId

        match op with
        | Op.Replace(parentId, _, oldChildren, newChildren) ->
            let parentBlocked =
                if isCurrentDocumentRoot graph parentId then false
                else nodeBlocked parentId
            let childBlocked =
                (oldChildren @ newChildren)
                |> List.exists (fun child ->
                    child.ref = Ownership.Owner && nodeBlocked child.id)
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
            if nodeId = Graph.rootId || nodeId = Graph.trashId || nodeId = Graph.workspacesId then
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

    let apply (op: Op) (state: State) : ApplyResult =
        if isBlockedByUnparsedDocument op state.graph then
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

    let undo (op: Op) (state: State) : ApplyResult =
        if isBlockedByUnparsedDocument op state.graph then
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
    let private validateOwnershipSemantics (graph: Graph) : Result<unit, string> =
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

        let childIdsMissingOwner =
            allChildIds
            |> Seq.filter (fun childId -> not (Map.containsKey childId ownerByChildId))
            |> Seq.toList

        if not childIdsMissingOwner.IsEmpty then
            Error "invalid ownership semantics: missing owner occurrence"
        else
            let childIdsWithMultipleOwners =
                ownerByChildId
                |> Map.toSeq
                |> Seq.filter (fun (_, owners) -> owners.Length <> 1)
                |> Seq.toList

            if not childIdsWithMultipleOwners.IsEmpty then
                Error "invalid ownership semantics: expected exactly one owner occurrence"
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

                let hasBrokenOwnerChain =
                    allChildIds
                    |> Seq.exists (fun childId ->
                        let ownerParent = ownerParentOf childId
                        not (reachesRootWithoutCycle ownerParent Set.empty))

                if hasBrokenOwnerChain then
                    Error "invalid ownership semantics: owner chain does not reach root"
                else
                    let hasInvalidFileDirectoryPlacement =
                        allChildren
                        |> Seq.exists (fun (parentId, child) ->
                            match child.ref, Map.tryFind child.id graph.nodes with
                            | Ownership.Owner, Some { kind = Special (File | Directory) }
                                when child.id <> Graph.trashId ->
                                not (GraphQuery.canOwn graph parentId child.id)
                            | _ -> false)

                    if hasInvalidFileDirectoryPlacement then
                        Error
                            "invalid ownership semantics: File and Directory nodes must have a Workspace or Directory owner ancestor (not under a File)"
                    elif GraphQuery.hasArtifactNameDuplicates graph then
                        Error
                            "invalid ownership semantics: duplicate name in artifact directory"
                    else
                        Ok ()

    let empty: History =
        { past = []
          future = []
          nextId = 0 }

    let newChange (history: History) : Change =
        { id = history.nextId
          changeId = System.Guid.NewGuid()
          ops = [] }

    let validateOwnership (graph: Graph) : Result<unit, string> =
        validateOwnershipSemantics graph

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

    /// Apply a server-trusted change: ops + history, no per-step ownership check.
    /// Callers replaying validated tails must run validateOwnership once on the final graph.
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
            match validateOwnership s.graph with
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

