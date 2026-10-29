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
    /// Server disk mtime after persist. `oldTime` is for undo; apply ignores mismatch.
    | SetUpdateTime of nodeId: NodeId * oldTime: System.DateTime * newTime: System.DateTime


type Authority = Authority of string

type ActorResult =
    | ActorSucceeded
    | ActorFailed of string
    | ActorCancelled
    | ActorQuery of ids: NodeId list

type ActorStart =
    { zoomId: NodeId
      focusId: NodeId
      commandId: NodeId
      graphIds: NodeId list
      eventId: EventId }

[<RequireQualifiedAccess>]
type LoadSavePath =
    | Git
    | Desk

[<RequireQualifiedAccess>]
type LoadSavePrePick =
    | Plain
    | Git
    | Desk

[<RequireQualifiedAccess>]
type LoadSaveOperation =
    | Load
    | Save

type LoadSaveCommandRequest =
    { operation: LoadSaveOperation
      prePick: LoadSavePrePick
      holdStack: bool
      start: ActorStart }

[<RequireQualifiedAccess>]
type Notice =
    | NodeParsed of nodeId: NodeId
    | NodePersisted of
        nodeId: NodeId *
        persistedVersion: EventId *
        updateTime: System.DateTime
    | NodeUnparsed of nodeId: NodeId
    | NodeUploading of nodeId: NodeId
    | NodeReceived of nodeId: NodeId
    | NodeParsing of nodeId: NodeId
    | NodePersisting of nodeId: NodeId
    | NodeUnpersisted of nodeId: NodeId
    | PersistFailed of nodeId: NodeId * message: string
    | ParseFailed of nodeId: NodeId * message: string

type PeerActorName = PeerActorName of string

[<RequireQualifiedAccess>]
type ChangeOrigin =
    | Edit
    | Parse

[<RequireQualifiedAccess>]
type Order =
    | Change of ops: Op list * origin: ChangeOrigin
    | Undo of target: EventId
    | Redo of target: EventId
    | StartActor of ActorStart
    | StartPeerActor of
        peerName: PeerActorName *
        request: ActorStart
    | LoadSave of
        path: LoadSavePath *
        peerName: PeerActorName *
        request: LoadSaveCommandRequest
    | Load of nodeId: NodeId
    | Cancel of focusId: NodeId

[<RequireQualifiedAccess>]
type EventBody =
    | Change of ops: Op list
    | Undo of target: EventId * ops: Op list
    | Redo of target: EventId * ops: Op list
    | ActorStart of ActorStart
    | ActorStop of focusId: NodeId * result: ActorResult
    | Load of nodeId: NodeId
    | Cancel of focusId: NodeId
    | Notice of Notice

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

    let private isCurrentDocumentRoot (graph: Graph) (nodeId: NodeId) : bool =
        match Map.tryFind nodeId graph.nodes with
        | Some node ->
            DocumentPartition.isDocumentRootNode graph nodeId
            && Node.documentStateEnum node = Current
        | None -> false

    let private ownedAreDocumentRoots graph parentId children =
        children
        |> List.forall (fun child ->
            Node.childOwnership graph parentId child <> Ownership.Owner
            || DocumentPartition.isDocumentRootNode graph child.id)

    let private replaceIsBlocked graph parentId oldChildren newChildren =
        let blocked id =
            DocumentPartition.isMemberOfInaccessibleDocument graph id
        let shell =
            match Map.tryFind parentId graph.nodes with
            | Some { kind = Special(Directory | Workspace); documentState = ds }
                when DocumentState.toLegacy ds = Unparsed -> true
            | _ -> false
        let stub =
            shell
            && ownedAreDocumentRoots graph parentId oldChildren
            && ownedAreDocumentRoots graph parentId newChildren
        let parentBlocked =
            not (isCurrentDocumentRoot graph parentId)
            && not stub
            && blocked parentId
        let touched =
            oldChildren @ newChildren
            |> List.exists (fun child ->
                Node.childOwnership graph parentId child = Ownership.Owner
                && blocked child.id
                && not (DocumentPartition.isDocumentRootNode graph child.id))
        parentBlocked || touched

    /// Inaccessible document membership blocks content edits. Structural Replace is allowed
    /// when relocating an inaccessible document root as an opaque unit under a
    /// Current parent (Move Up/Down, Move Selection to Start/End, indent).
    /// Replace that mutates inside an Unparsed document remains blocked, with
    /// two exceptions: Replace under a Current document root (nested parse while
    /// an enclosing Directory/Workspace is Unparsed), and attaching/detaching
    /// document-root stubs under an Unparsed Directory/Workspace shell.
    let private isBlockedByInaccessibleDocument (op: Op) (graph: Graph) : bool =
        match op with
        | Op.SetUpdateTime _ ->
            // Download stamp alignment and persist tails only touch mtime metadata;
            // they must not require the document to be parsed first.
            false
        | Op.NewSpecialNode _ ->
            // Create and undo introduce or remove the special node. They are not
            // an edit inside an Unparsed document.
            false
        | Op.Replace(parentId, oldChildren, newChildren) ->
            replaceIsBlocked graph parentId oldChildren newChildren
        | _ ->
            involvedNodeIds graph op
            |> List.distinct
            |> List.exists (fun id ->
                DocumentPartition.isMemberOfInaccessibleDocument graph id)

    let private applyAllowed
        (changedVersion: EventId option)
        (op: Op)
        (state: State)
        : ApplyResult
        =
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
            GraphMutate.setText changedVersion nodeId oldText newText state.graph
            |> fromGraphResult state
        | Op.SetClasses(nodeId, oldClasses, newClasses) ->
            GraphMutate.setClasses
                changedVersion nodeId oldClasses newClasses state.graph
            |> fromGraphResult state
        | Op.Replace(parentId, oldChildren, newChildren) ->
            match
                GraphMutate.replace
                    changedVersion parentId 0 oldChildren newChildren state.graph
            with
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
                            documentState = Current,
                            updateTime = NodeUpdateTime.now ())
                    ApplyResult.Changed
                        { state with
                              graph = Graph.addDetachedNode node state.graph }
        | Op.SetName(nodeId, oldName, newName) ->
            GraphMutate.setName changedVersion nodeId oldName newName state.graph
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

    /// Disk parse posts commandName "Parse" and must not write PersistState.
    let internal marksOwningPersist (commandName: string) =
        commandName <> "Parse"
    let internal persistMarkVersion
        (commandName: string)
        (eventId: EventId)
        : EventId option =
        if marksOwningPersist commandName then Some eventId else None
    let internal guardsUnparsedDocument (commandName: string) =
        commandName <> "Parse"

    /// Graph-only post passes guardUnparsed false. Outline ops may edit an
    /// Unparsed document. Every other apply keeps the strict gate.
    let private blockedForApply guardUnparsed op graph =
        guardUnparsed && isBlockedByInaccessibleDocument op graph

    let internal applyAllowing guardUnparsed changedVersion op state =
        if blockedForApply guardUnparsed op state.graph then
            ApplyResult.Invalid(state, unparsedDocumentError)
        else
            applyAllowed changedVersion op state

    let apply (op: Op) (state: State) : ApplyResult =
        applyAllowing true (Some EventId.zero) op state

    let internal applyAllAllowing guardUnparsed changedVersion ops state =
        let step (accState, hasChanged) op =
            match applyAllowing guardUnparsed changedVersion op accState with
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

    let applyAll (ops: Op list) (state: State) : ApplyResult =
        applyAllAllowing true (Some EventId.zero) ops state

    let invert (op: Op) : Op =
        match op with
        | Op.NewNode(id, text) -> Op.NewNode(id, text)
        | Op.SetText(id, old, new_) -> Op.SetText(id, new_, old)
        | Op.SetClasses(id, old, new_) -> Op.SetClasses(id, new_, old)
        | Op.Replace(parentId, oldChildren, newChildren) ->
            Op.Replace(parentId, newChildren, oldChildren)
        | Op.NewSpecialNode(id, kind, name) -> Op.NewSpecialNode(id, kind, name)
        | Op.SetName(id, old, new_) -> Op.SetName(id, new_, old)
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
            let childMap = state.graph.childMap |> Map.remove nodeId
            ApplyResult.Changed
                { state with
                    graph = Graph.fromNodes state.graph.root nodes childMap }
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
            let childMap = state.graph.childMap |> Map.remove nodeId
            ApplyResult.Changed
                { state with
                    graph = Graph.fromNodes state.graph.root nodes childMap }
        | Op.SetName(nodeId, oldName, newName) ->
            Graph.setName nodeId newName oldName state.graph
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
        | EventBody.ActorStart _ | EventBody.Cancel _
        | EventBody.Load _
        | EventBody.ActorStop _ | EventBody.Notice _ -> None

    let withOps (event: Ev) (ops: Op list) : Ev =
        match event.body with
        | EventBody.Change _ ->
            { event with body = EventBody.Change ops }
        | EventBody.Undo(target, _) ->
            { event with body = EventBody.Undo(target, ops) }
        | EventBody.Redo(target, _) ->
            { event with body = EventBody.Redo(target, ops) }
        | EventBody.ActorStart _ | EventBody.Cancel _
        | EventBody.Load _
        | EventBody.ActorStop _ | EventBody.Notice _ -> event

    let isAction (event: Ev) : bool = ops event |> Option.isSome

    let target (event: Ev) : EventId option =
        match event.body with
        | EventBody.Undo(target, _)
        | EventBody.Redo(target, _) -> Some target
        | EventBody.Change _
        | EventBody.ActorStart _ | EventBody.Cancel _
        | EventBody.Load _
        | EventBody.ActorStop _ | EventBody.Notice _ -> None

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

    /// Enum a Notice writes. Graph.setDocumentState applies it.
    let internal documentStateFromNotice (notice: Notice) =
        match notice with
        | Notice.NodeUploading nodeId -> Some(nodeId, PullingFile)
        | Notice.NodeReceived nodeId
        | Notice.NodeUnparsed nodeId -> Some(nodeId, Unparsed)
        | Notice.NodeParsed nodeId -> Some(nodeId, Current)
        | Notice.NodeParsing _
        | Notice.NodePersisting _
        | Notice.NodeUnpersisted _
        | Notice.PersistFailed _
        | Notice.ParseFailed _
        | Notice.NodePersisted _ -> None

    let private applyNotice (notice: Notice) (state: State) : ApplyResult =
        match documentStateFromNotice notice with
        | None -> ApplyResult.Unchanged state
        | Some(nodeId, newState) ->
            match Graph.setDocumentState nodeId newState state.graph with
            | Ok graph -> ApplyResult.Changed { state with graph = graph }
            | Error msg -> ApplyResult.Invalid(state, msg)

    let apply (event: Ev) (state: State) : ApplyResult =
        match event.body with
        | EventBody.Notice notice -> applyNotice notice state
        | _ ->
            match ops event with
            | None -> ApplyResult.Unchanged state
            | Some opList ->
                Op.applyAllAllowing
                    (Op.guardsUnparsedDocument event.commandName)
                    (Op.persistMarkVersion event.commandName event.id)
                    opList
                    state
