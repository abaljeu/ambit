namespace Gambol.Shared

[<RequireQualifiedAccess>]
type Op =
    | NewNode of nodeId: NodeId * text: string
    | SetText of nodeId: NodeId * oldText: string * newText: string
    | SetClasses of nodeId: NodeId * oldClasses: CssClasses * newClasses: CssClasses
    | Replace of
        parentId: NodeId *
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

type EventId =
    private
    | Zero // the Ev is a draft
    | Int of int // The Ev is an event; accepted, uniquely numbered, 
        // and has been stored, and applied in the system

    member this.Value =
        match this with
        | Zero -> 0
        | Int n -> n

[<RequireQualifiedAccess>]
module EventId =
    let zero = Zero
    let next (id: EventId) =
        match id with
        | Int n -> Int(n + 1)
        | Zero -> Zero
    let max (a: EventId) (b: EventId) =
        if a.Value >= b.Value then a else b
    let value (id: EventId) = id.Value
    let display (id: EventId) = string id.Value
    let fromJson n =
        if n > 0 then Int n
        else Zero
    let toJson (id: EventId) = id.Value

type Authority = Authority of string

type ActorResult =
    | ActorSucceeded
    | ActorFailed

type ActorStart =
    { zoomId: NodeId
      focusId: NodeId
      commandId: NodeId
      graphIds: NodeId list
      eventId: EventId }

[<RequireQualifiedAccess>]
type EventBody =
    | Change of ops: Op list
    | Undo of target: EventId * ops: Op list
    | Redo of target: EventId * ops: Op list
    | ActorStart of ActorStart
    | ActorStop of focusId: NodeId * result: ActorResult

type Ev =
    { id: EventId
      submissionId: System.Guid
      authority: Authority
      commandName: string
      body: EventBody }

type State =
    { graph: Graph
      eventId: EventId }


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
    /// artifact-name checks for that parent run separately in validateOwnershipForOps.
    let involvedNodeIds (graph: Graph) (op: Op) : NodeId list =
        match op with
        | Op.NewNode(nodeId, _)
        | Op.NewSpecialNode(nodeId, _, _) ->
            if Map.containsKey nodeId graph.nodes then [ nodeId ] else []
        | Op.SetText(nodeId, _, _)
        | Op.SetClasses(nodeId, _, _)
        | Op.SetName(nodeId, _, _)
        | Op.SetUpdateTime(nodeId, _, _) -> [ nodeId ]
        | Op.Replace(parentId, oldChildren, newChildren) ->
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
        | Op.SetUpdateTime _ ->
            // Download stamp alignment and persist tails only touch mtime metadata;
            // they must not require the document to be parsed first.
            false
        | Op.Replace(parentId, oldChildren, newChildren) ->
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
        | Op.Replace(parentId, oldChildren, newChildren) ->
            match Graph.replace parentId 0 oldChildren newChildren state.graph with
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

    let applyAll (ops: Op list) (state: State) : ApplyResult =
        let step (accState, hasChanged) op =
            match apply op accState with
            | ApplyResult.Invalid _ as err -> Error err
            | ApplyResult.Unchanged s' -> Ok(s', hasChanged)
            | ApplyResult.Changed s' -> Ok(s', true)

        let result =
            ops
            |> List.fold
                (fun acc op ->
                    match acc with
                    | Error err -> Error err
                    | Ok(s, changed) -> step (s, changed) op)
                (Ok(state, false))

        match result with
        | Error(ApplyResult.Invalid(_, message)) ->
            ApplyResult.Invalid(state, message)
        | Error err -> err
        | Ok(s, false) -> ApplyResult.Unchanged s
        | Ok(s, true) -> ApplyResult.Changed s

    let invert (op: Op) : Op =
        match op with
        | Op.NewNode(id, text) -> Op.NewNode(id, text)
        | Op.SetText(id, old, new_) -> Op.SetText(id, new_, old)
        | Op.SetClasses(id, old, new_) -> Op.SetClasses(id, new_, old)
        | Op.Replace(parentId, oldChildren, newChildren) ->
            Op.Replace(parentId, newChildren, oldChildren)
        | Op.NewSpecialNode(id, kind, name) -> Op.NewSpecialNode(id, kind, name)
        | Op.SetName(id, old, new_) -> Op.SetName(id, new_, old)
        | Op.SetDocumentState(id, old, new_) ->
            Op.SetDocumentState(id, new_, old)
        | Op.SetUpdateTime(id, old, new_) -> Op.SetUpdateTime(id, new_, old)

    let invertAll (ops: Op list) : Op list =
        let retainReversible =
            function
            | Op.NewNode _
            | Op.NewSpecialNode _ -> None
            | op -> Some(invert op)

        ops |> List.rev |> List.choose retainReversible

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
        | Op.Replace(parentId, oldChildren, newChildren) ->
            // Inverse: swap old/new to restore
            Graph.replace parentId 0 newChildren oldChildren state.graph
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
module Ev =
    let id (event: Ev) : EventId = event.id

    let authority (event: Ev) : Authority = event.authority

    let ops (event: Ev) : Op list option =
        match event.body with
        | EventBody.Change ops
        | EventBody.Undo(_, ops)
        | EventBody.Redo(_, ops) -> Some ops
        | EventBody.ActorStart _
        | EventBody.ActorStop _ -> None

    let isAction (event: Ev) : bool = ops event |> Option.isSome

    let target (event: Ev) : EventId option =
        match event.body with
        | EventBody.Undo(target, _)
        | EventBody.Redo(target, _) -> Some target
        | EventBody.Change _
        | EventBody.ActorStart _
        | EventBody.ActorStop _ -> None

    let inverseOps (event: Ev) : Op list option =
        ops event |> Option.map Op.invertAll

    let fromJson
        eventId
        submissionId
        authority
        commandName
        body
        : Ev =
        { id = EventId.fromJson eventId
          submissionId = submissionId
          authority = authority
          commandName = commandName
          body = body }

    let toJson (event: Ev) =
        EventId.toJson event.id,
        event.submissionId,
        event.authority,
        event.commandName,
        event.body

    let apply (event: Ev) (state: State) : ApplyResult =
        match ops event with
        | None -> ApplyResult.Unchanged state
        | Some opList -> Op.applyAll opList state

/// Validation and apply functions for Change operations with ownership semantics.
[<RequireQualifiedAccess>]
module ChangeValidation =
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

    let private introducedChildren (oldChildren: ChildNode list) (newChildren: ChildNode list) =
        newChildren
        |> List.filter (fun nc ->
            oldChildren
            |> List.exists (fun oc -> oc.id = nc.id && oc.ref = nc.ref)
            |> not)

    let private removedChildren (oldChildren: ChildNode list) (newChildren: ChildNode list) =
        oldChildren
        |> List.filter (fun oc ->
            newChildren
            |> List.exists (fun nc -> nc.id = oc.id && nc.ref = oc.ref)
            |> not)

    let private validateOwnershipForOps (graph: Graph) (ops: Op list) : Result<unit, string> =
        let shapeOps = ops |> List.filter opChangesGraphShape

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
                    | Op.Replace(parentId, oldChildren, newChildren) ->
                        let introduced = introducedChildren oldChildren newChildren
                        if invalidOwnedFileDirectoryPlacement graph parentId introduced then
                            Some
                                "invalid ownership semantics: File and Directory nodes must have a Workspace or Directory owner ancestor (not under a File)"
                        elif
                            introduced
                            |> List.exists (fun c ->
                                Node.childOwnership graph parentId c = Ownership.Owner)
                            && GraphQuery.artifactNameConflict graph parentId introduced
                        then
                            let name =
                                introduced
                                |> List.tryPick (fun c ->
                                    Map.tryFind c.id graph.nodes
                                    |> Option.bind (fun n -> Filename.tryValue n.name))
                                |> Option.defaultValue "?"
                            Some
                                $"invalid ownership semantics: duplicate name '{name}' in artifact directory"
                        else
                            None
                    | _ -> None)
                |> Option.map Error
                |> Option.defaultValue (Ok ())

    let applyOpsTrusted (ops: Op list) (state: State) : ApplyResult =
        Op.applyAll ops state

    let applyOps (ops: Op list) (state: State) : ApplyResult =
        match applyOpsTrusted ops state with
        | ApplyResult.Invalid _ as err -> err
        | ApplyResult.Unchanged s -> ApplyResult.Unchanged s
        | ApplyResult.Changed s ->
            match validateOwnershipForOps s.graph ops with
            | Error msg -> ApplyResult.Invalid(state, msg)
            | Ok () -> ApplyResult.Changed s

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

    let appendToOps (ops: Op list) (stampOps: Op list) : Op list =
        if stampOps.IsEmpty then ops else ops @ stampOps

    let appendToEvent (event: Ev) (stampOps: Op list) : Ev =
        if stampOps.IsEmpty then
            event
        else
            match event.body with
            | EventBody.Change ops ->
                { event with body = EventBody.Change(appendToOps ops stampOps) }
            | EventBody.Undo(target, ops) ->
                { event with
                    body = EventBody.Undo(target, appendToOps ops stampOps) }
            | EventBody.Redo(target, ops) ->
                { event with
                    body = EventBody.Redo(target, appendToOps ops stampOps) }
            | EventBody.ActorStart _
            | EventBody.ActorStop _ -> event

    let appendToLastEvent (events: Ev list) (stampOps: Op list) : Ev list =
        if stampOps.IsEmpty || events.IsEmpty then
            events
        else
            match List.rev events with
            | [] -> events
            | last :: rest ->
                List.rev (appendToEvent last stampOps :: rest)

