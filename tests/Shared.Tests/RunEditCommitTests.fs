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
                committed, commitEffects @ [ SubmitCommand request ]
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
                editingId originalText "?test hello X" model.graph with
        | Ok _ -> failwith "expected old text does not match"
        | Error msg ->
            ViewModelMoveOps.withMoveError
                msg { model with mode = Selecting }, []
    | _ -> failwith "expected Editing selection"

[<Fact>]
let ``failed Editing SetText CAS does not SubmitCommand`` () =
    let model = editingCommandModel ()
    let ran, effects =
        afterEditCommitThenTryStart commitFailingSetTextCas model
    Assert.Empty(effects)
    Assert.False(
        effects
        |> List.exists (function
            | SubmitCommand _ -> true
            | _ -> false))
    match ran.lastCmdResult with
    | Some (CmdLastResult.Error (None, msg)) ->
        Assert.Equal("old text does not match", msg)
    | other -> failwith $"expected commit Error, got %A{other}"

[<Fact>]
let ``failed Editing SetText with same prior Error does not SubmitCommand`` () =
    let stale =
        Some (CmdLastResult.Error (None, "old text does not match"))
    let model = { editingCommandModel () with lastCmdResult = stale }
    let ran, effects =
        afterEditCommitThenTryStart commitFailingSetTextCas model
    Assert.Empty(effects)
    Assert.False(
        effects
        |> List.exists (function
            | SubmitCommand _ -> true
            | _ -> false))
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
