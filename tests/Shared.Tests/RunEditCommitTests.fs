module RunEditCommitTests

open Gambol.Shared
open Gambol.Shared.ViewModel
open VmTestHelpers
open Xunit

/// Shared `afterEditCommit` then Commands.execRunOp tryStart arm.
/// No Client test project. execRunOp must call this Shared gate.
let private afterEditCommitThenTryStart
    (commit: VM -> VM * Effect list)
    (model: VM)
    : VM * Effect list =
    RunEditCommit.afterEditCommit commit model (fun committed commitEffects ->
        match committed.selectedNodes with
        | None -> committed, commitEffects
        | Some sel ->
            let focusId =
                ViewModelSelection.focusedNodeId committed.graph sel
            match
                CommandRequest.tryStart
                    committed.graph
                    committed.siteMap
                    committed.zoomRoot
                    focusId
                    committed.eventId with
            | Ok request ->
                let syncInfo, queued =
                    RunLaunch.queueStart
                        request
                        committed.eventId
                        committed.syncInfo
                        commitEffects
                { committed with syncInfo = syncInfo }, queued
            | Error msg ->
                { committed with
                    lastCmdResult =
                        Some (CmdLastResult.Error (Some "Run", msg)) },
                commitEffects)

let private owned = ChildNode.owners

let private requireOk label r =
    match r with
    | Ok v -> v
    | Error e -> failwith $"{label}: {e}"

let private ownerChain
    (texts: string list)
    : Graph * SiteMap * NodeId list =
    let g0 = Graph.create ()
    let g1, ids = ModelBuilder.createNodes texts g0
    let graph =
        ids
        |> List.fold
            (fun (graph, parentId) childId ->
                let next =
                    Graph.replace parentId 0 [] (owned [ childId ]) graph
                    |> requireOk "ownerChain"
                next, childId)
            (g1, g1.root)
        |> fst
    let zoomId = ids.[0]
    let siteMap, _ = buildSiteMapFrom graph zoomId (Sid 0)
    graph, siteMap, ids

let private editingCommandModel () : VM =
    let graph, _, ids = ownerChain [ "?test hello" ]
    let focusId = ids.[0]
    let model = emptyModel graph
    match ViewModelSelection.singleSelection graph model.siteMap focusId with
    | None -> failwith "expected command selection"
    | Some sel ->
        { model with
            selectedNodes = Some sel
            zoomRoot = focusId
            mode = Editing ("old", EditCaret.Utf16Index 0) }

/// Same Error path as Client `commitTextEdit` after SetText CAS fail.
let private commitFailingSetTextCas (model: VM) : VM * Effect list =
    match model.mode, model.selectedNodes with
    | Editing (originalText, _), Some sel ->
        let editingId =
            ViewModelSelection.focusedNodeId model.graph sel
        match
            GraphMutate.setText
                true editingId originalText "?test hello X" model.graph with
        | Ok _ -> failwith "expected old text does not match"
        | Error msg ->
            ViewModelMoveOps.withMoveError
                msg { model with mode = Selecting }, []
    | _ -> failwith "expected Editing selection"

[<Fact>]
let ``failed Editing SetText CAS does not queue ActorStart`` () =
    let model = editingCommandModel ()
    let ran, effects =
        afterEditCommitThenTryStart commitFailingSetTextCas model
    Assert.Empty(effects)
    Assert.Empty(ran.syncInfo.pending)
    match ran.lastCmdResult with
    | Some (CmdLastResult.Error (None, msg)) ->
        Assert.Equal("old text does not match", msg)
    | other -> failwith $"expected commit Error, got %A{other}"

[<Fact>]
let ``failed Editing SetText with same prior Error does not queue ActorStart`` () =
    let stale =
        Some (CmdLastResult.Error (None, "old text does not match"))
    let model = { editingCommandModel () with lastCmdResult = stale }
    let ran, effects =
        afterEditCommitThenTryStart commitFailingSetTextCas model
    Assert.Empty(effects)
    Assert.Empty(ran.syncInfo.pending)
    match ran.lastCmdResult with
    | Some (CmdLastResult.Error (None, msg)) ->
        Assert.Equal("old text does not match", msg)
    | other -> failwith $"expected commit Error, got %A{other}"

[<Fact>]
let ``Selecting may launch after a stale Error`` () =
    let after =
        Some (CmdLastResult.Error (None, "old text does not match"))
    Assert.True(RunEditCommit.mayLaunchAfterEditCommit false after)

[<Fact>]
let ``successful Editing commit may SubmitCommand`` () =
    Assert.True(
        RunEditCommit.mayLaunchAfterEditCommit
            true (Some (CmdLastResult.Ok (Some "Edit node"))))

/// Same apply path as Client `commitTextEdit`, using live node text.
let private commitLiveEdit (liveText: string) (model: VM) : VM * Effect list =
    match model.mode, model.selectedNodes with
    | Editing _, Some sel ->
        let editingId =
            ViewModelSelection.focusedNodeId model.graph sel
        let ops = RunEditCommit.commitTextOps editingId liveText model.graph
        let state = { graph = model.graph; eventId = model.eventId }
        match ChangeValidation.applyOps ops state with
        | ApplyResult.Invalid (_, msg) ->
            ViewModelMoveOps.withMoveError
                msg { model with mode = Selecting }, []
        | ApplyResult.Changed next
        | ApplyResult.Unchanged next ->
            ViewModelMoveOps.withLastCmdOk
                { model with graph = next.graph; mode = Selecting }, []
    | _ -> model, []

[<Fact>]
let ``edit then Run queues ActorStart behind the edit in one batch`` () =
    let model = editingCommandModel ()
    let focusId =
        match model.selectedNodes with
        | None -> failwith "expected command selection"
        | Some sel ->
            ViewModelSelection.focusedNodeId model.graph sel
    let live = "?test hello now"
    let commit (model: VM) =
        let ops = RunEditCommit.commitTextOps focusId live model.graph
        let event = ClientHistory.mintChange "Edit node" ops
        match
            ChangeValidation.applyOps
                ops { graph = model.graph; eventId = model.eventId }
        with
        | ApplyResult.Changed next ->
            let sync, effects =
                SyncPlanner.enqueuePending
                    event model.eventId model.syncInfo
            { model with
                graph = next.graph
                mode = Selecting
                syncInfo = sync },
            effects
        | other -> failwith $"expected Changed, got %A{other}"
    let ran, effects = afterEditCommitThenTryStart commit model
    match ran.syncInfo.pending with
    | [ edit; start ] ->
        match edit.body, start.body with
        | EventBody.Change _, EventBody.ActorStart request ->
            Assert.Equal(focusId, request.commandId)
        | _ -> failwith "expected Change then ActorStart"
    | other -> failwith $"expected two pending, got %A{other}"
    let batches =
        effects
        |> List.choose (function
            | SubmitPendingBatch (_, events) -> Some events
            | _ -> None)
    Assert.Equal<Ev list list>([ ran.syncInfo.pending ], batches)

[<Fact>]
let ``Cancel queues behind an edit and does not post a side channel`` () =
    let model = editingCommandModel ()
    let focusId =
        match model.selectedNodes with
        | None -> failwith "expected command selection"
        | Some sel ->
            ViewModelSelection.focusedNodeId model.graph sel
    let edit = ClientHistory.mintChange "Edit node" []
    let sync, editEffects =
        SyncPlanner.enqueuePending edit model.eventId model.syncInfo
    let queued, effects =
        RunLaunch.queueCancel focusId model.eventId sync editEffects
    match queued.pending with
    | [ _; cancel ] ->
        match cancel.body with
        | EventBody.Cancel id -> Assert.Equal(focusId, id)
        | other -> failwith $"expected Cancel, got %A{other}"
    | other -> failwith $"expected two pending, got %A{other}"
    let batches =
        effects
        |> List.choose (function
            | SubmitPendingBatch (_, events) -> Some events
            | _ -> None)
    Assert.Equal<Ev list list>([ queued.pending ], batches)

[<Fact>]
let ``stale Editing snapshot commits live text and queues ActorStart`` () =
    let model = editingCommandModel ()
    let focusId =
        match model.selectedNodes with
        | None -> failwith "expected command selection"
        | Some sel ->
            ViewModelSelection.focusedNodeId model.graph sel
    let live = "?test hello now"
    match RunEditCommit.commitTextOps focusId live model.graph with
    | [ Op.SetText(_, oldText, newText) ] ->
        Assert.Equal("?test hello", oldText)
        Assert.Equal(live, newText)
    | other -> failwith $"expected one SetText, got %A{other}"
    let ran, effects =
        afterEditCommitThenTryStart (commitLiveEdit live) model
    Assert.Equal(live, ran.graph.nodes.[focusId].text)
    Assert.True(
        ran.syncInfo.pending
        |> List.exists (function
            | { body = EventBody.ActorStart _ } -> true
            | _ -> false))
    match ran.lastCmdResult with
    | Some (CmdLastResult.Error (_, msg)) ->
        failwith $"expected launch, got {msg}"
    | _ -> ()
