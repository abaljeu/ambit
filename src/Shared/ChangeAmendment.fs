namespace Gambol.Shared

/// Server-side amendment for recoverable field compare-and-swap collisions.
[<RequireQualifiedAccess>]
module ChangeAmendment =

    [<Literal>]
    let private ambConflictClass = "amb-conflict"

    [<Literal>]
    let private oldTextMismatch = "old text does not match"

    [<Literal>]
    let private oldNameMismatch = "old name does not match"

    [<Literal>]
    let private oldClassesMismatch = "old classes do not match"

    [<Literal>]
    let private oldSpanMismatch = "old span does not match"

    let private ambConflictClasses =
        CssClass.ofList [ ambConflictClass ]

    let private isRecoverableCas (message: string) =
        message = oldTextMismatch
        || message = oldNameMismatch
        || message = oldClassesMismatch
        || message = oldSpanMismatch

    let private classSet (classes: CssClasses) =
        classes |> CssClass.toList |> Set.ofList

    let private classesFromSet (items: Set<string>) =
        items |> Set.toList |> CssClass.ofList

    /// Merge class edits against the Actor's common prior when another Change landed first.
    let private mergeClassesFromPrior
        (prior: CssClasses)
        (current: CssClasses)
        (postedNew: CssClasses)
        : CssClasses
        =
        let priorSet = classSet prior
        let postedNewSet = classSet postedNew
        let currentSet = classSet current
        let removes = Set.difference priorSet postedNewSet
        let adds = Set.difference postedNewSet priorSet
        Set.union adds (Set.difference currentSet removes) |> classesFromSet

    let private ambConflictChildOps (graph: Graph) (parentId: NodeId) (conflictText: string) =
        let childId = NodeId.New()
        let current =
            match Map.tryFind parentId graph.nodes with
            | None -> []
            | Some _ -> GraphChildren.get graph parentId
        [ Op.NewNode(childId, conflictText)
          Op.SetClasses(childId, CssClass.empty, ambConflictClasses)
          ChildListWire.replace parentId current [ ChildNode.owner childId ] ]

    let private tryAmendSetText (graph: Graph) (nodeId: NodeId) (message: string) (newText: string) =
        if message <> oldTextMismatch then
            Error message
        else
            match Map.tryFind nodeId graph.nodes with
            | None -> Error "node not found"
            | Some node when node.text = newText -> Ok []
            | Some _ -> Ok (ambConflictChildOps graph nodeId newText)

    let private tryAmendSetName
        (graph: Graph)
        (nodeId: NodeId)
        (message: string)
        (newName: string)
        =
        if message <> oldNameMismatch then
            Error message
        else
            match Map.tryFind nodeId graph.nodes with
            | None -> Error "node not found"
            | Some node ->
                match Filename.tryValue node.name with
                | Some current when current = newName -> Ok []
                | _ -> Ok (ambConflictChildOps graph nodeId newName)

    let private tryAmendSetClasses
        (graph: Graph)
        (nodeId: NodeId)
        (prior: CssClasses)
        (postedNew: CssClasses)
        (message: string)
        =
        if message <> oldClassesMismatch then
            Error message
        else
            match Map.tryFind nodeId graph.nodes with
            | None -> Error "node not found"
            | Some node ->
                let merged =
                    mergeClassesFromPrior prior node.cssClasses postedNew
                if merged = node.cssClasses then
                    Ok []
                else
                    Ok [ Op.SetClasses(nodeId, node.cssClasses, merged) ]

    let private tryAmendReplace
        (graph: Graph)
        (parentId: NodeId)
        (anchor: ChildNode list)
        (newList: ChildNode list)
        (message: string)
        =
        if message <> oldSpanMismatch then
            Error message
        else
            match Map.tryFind parentId graph.nodes with
            | None -> Error "node not found"
            | Some _ ->
                let current = GraphChildren.get graph parentId
                let target = ChildListMerge.resolve anchor current newList
                if target = current then
                    Ok []
                else
                    Ok [ ChildListWire.replace parentId current target ]

    let private tryAmendOp (graph: Graph) (op: Op) (message: string) : Result<Op list, string> =
        match op with
        | Op.SetText(nodeId, _, newText) ->
            tryAmendSetText graph nodeId message newText
        | Op.SetName(nodeId, _, newName) ->
            tryAmendSetName graph nodeId message newName
        | Op.SetClasses(nodeId, prior, postedNew) ->
            tryAmendSetClasses graph nodeId prior postedNew message
        | Op.Replace(parentId, anchor, newList) ->
            tryAmendReplace graph parentId anchor newList message
        | _ -> Error message

    let private buildAmendedOps (ops: Op list) (state: State) : Result<Op list, string> =
        let rec applyReplacement replacement current acc rest =
            match replacement with
            | [] -> foldOps rest current acc
            | op :: tail ->
                match Op.apply op current with
                | ApplyResult.Invalid (_, msg) -> Error msg
                | ApplyResult.Unchanged next -> applyReplacement tail next acc rest
                | ApplyResult.Changed next -> applyReplacement tail next (acc @ [ op ]) rest

        and foldOps remaining current acc =
            match remaining with
            | [] -> Ok acc
            | op :: rest ->
                match Op.apply op current with
                | ApplyResult.Invalid (_, msg) when isRecoverableCas msg ->
                    match tryAmendOp current.graph op msg with
                    | Error err -> Error err
                    | Ok [] -> foldOps rest current acc
                    | Ok replacement -> applyReplacement replacement current acc rest
                | ApplyResult.Invalid (_, msg) -> Error msg
                | ApplyResult.Unchanged next -> foldOps rest next (acc @ [ op ])
                | ApplyResult.Changed next -> foldOps rest next (acc @ [ op ])

        foldOps ops state []

    let private applyMarked
        (guardUnparsed: bool)
        (changedVersion: EventId option)
        (ops: Op list)
        (state: State)
        : ApplyResult * bool * Op list
        =
        match
            ChangeValidation.applyOpsAllowing
                guardUnparsed changedVersion ops state
        with
        | (ApplyResult.Changed _ | ApplyResult.Unchanged _) as ok ->
            ok, false, ops
        | ApplyResult.Invalid (_, msg) when isRecoverableCas msg ->
            match buildAmendedOps ops state with
            | Error err -> ApplyResult.Invalid(state, err), false, ops
            | Ok amended when amended = ops ->
                ApplyResult.Invalid(state, msg), false, ops
            | Ok amended ->
                match
                    ChangeValidation.applyOpsAllowing
                        guardUnparsed changedVersion amended state
                with
                | ApplyResult.Invalid _ as err -> err, false, ops
                | ApplyResult.Unchanged _ as unchanged ->
                    unchanged, true, amended
                | ApplyResult.Changed _ as changed -> changed, true, amended
        | ApplyResult.Invalid _ as err ->
            err, false, ops

    /// Apply Ops, amending recoverable field CAS failures instead of rejecting.
    let applyOps (ops: Op list) (state: State) : ApplyResult * bool * Op list =
        applyMarked true (Some EventId.zero) ops state

    /// Disk parse (`commandName` Parse) applies without writing PersistState.
    /// The normal-change unparsed gate still applies. Mailbox postGraphOnly
    /// uses applyForGraphOnly.
    let applyForCommand
        (changedVersion: EventId)
        (commandName: string)
        (ops: Op list)
        (state: State)
        : ApplyResult * bool * Op list
        =
        applyMarked
            true
            (Op.persistMarkVersion commandName changedVersion)
            ops
            state

    /// Mailbox postGraphOnly. A document-root suffix attach may land on an
    /// Unparsed shell. Other unparsed content edits stay blocked.
    let applyForGraphOnly
        (changedVersion: EventId)
        (commandName: string)
        (ops: Op list)
        (state: State)
        : ApplyResult * bool * Op list
        =
        applyMarked
            false
            (Op.persistMarkVersion commandName changedVersion)
            ops
            state

    let private leadNotice (notice: Notice) : Ev =
        { id = EventId.zero
          submissionId = System.Guid.Empty
          authority = Authority "Parse"
          commandName = "Parse"
          body = EventBody.Notice notice }

    let private applyOneLead
        (origin: State)
        (current: ApplyResult)
        (notice: Notice)
        =
        match current with
        | ApplyResult.Invalid _ as err -> err
        | ApplyResult.Unchanged state
        | ApplyResult.Changed state ->
            match Ev.apply (leadNotice notice) state with
            | ApplyResult.Invalid (_, msg) ->
                ApplyResult.Invalid(origin, msg)
            | ApplyResult.Unchanged _ -> current
            | ApplyResult.Changed next -> ApplyResult.Changed next

    let private applyLeadNotices (notices: Notice list) (state: State) =
        List.fold (applyOneLead state) (ApplyResult.Unchanged state) notices

    let private applyReady
        (graphOnly: bool)
        (eventId: EventId)
        (event: Ev)
        (ops: Op list)
        (ready: State)
        =
        applyMarked
            (not graphOnly)
            (Op.persistMarkVersion event.commandName eventId)
            ops
            ready

    /// One apply for FileAgent and DbAgent. Pass the posted event id.
    /// Undo and Redo mark at that id the same way a Change does.
    /// A graph-only Parse infers a lead NodeParsed before the ops.
    let applyPosted
        (graphOnly: bool)
        (eventId: EventId)
        (event: Ev)
        (ops: Op list)
        (state: State)
        : ApplyResult * bool * Op list =
        let notices =
            if graphOnly then
                DocumentOpImpact.inferNotices
                    state.graph
                    event.commandName
                    ops
            else
                []
        match applyLeadNotices notices state with
        | ApplyResult.Invalid (_, msg) ->
            ApplyResult.Invalid(state, msg), false, ops
        | ApplyResult.Unchanged ready ->
            applyReady graphOnly eventId event ops ready
        | ApplyResult.Changed ready ->
            match applyReady graphOnly eventId event ops ready with
            | ApplyResult.Invalid (_, msg), _, _ ->
                ApplyResult.Invalid(state, msg), false, ops
            | ApplyResult.Unchanged _, amended, applied ->
                ApplyResult.Changed ready, amended, applied
            | changed, amended, applied ->
                changed, amended, applied
