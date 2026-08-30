module AckReconcileTests

open System
open Xunit
open Gambol.Shared
open Gambol.Shared.ViewModel

let private stampTime =
    DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc)

let private clientState graph revision history : ClientSyncState =
    { graph = graph
      revision = revision
      history = history }

let private sending items =
    { SyncInfo.initial with
        pendingChanges = items
        syncState = Sending 1 }

let private confirm (item: PendingChange) suffix : Change =
    { item.change with ops = item.change.ops @ suffix }

let private stamp nodeId =
    Op.SetUpdateTime(nodeId, NodeUpdateTime.missing, stampTime)

let private textChange id nodeId oldText newText : Change =
    { id = id
      changeId = Guid.NewGuid()
      ops = [ Op.SetText(nodeId, oldText, newText) ] }

let private seededEdit () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange 0 nodeId "before" "after"
    let state0 = clientState graph1 (Revision 0) (ClientHistory.clear ())
    match SyncLogic.applyLocalChange "Edit node" change state0 with
    | Error msg -> failwith msg
    | Ok (state, pending) -> nodeId, state, pending

let private expectApplied result =
    match result with
    | AckReconcile.Applied (state, syncInfo, effects, suffixOps) ->
        state, syncInfo, effects, suffixOps
    | AckReconcile.Ignored -> failwith "expected Applied, got Ignored"
    | AckReconcile.Rejected msg -> failwithf "expected Applied, got Rejected: %s" msg

let private expectRejected needle result =
    match result with
    | AckReconcile.Rejected msg ->
        Assert.Contains(needle, msg)
    | other -> failwithf "expected Rejected, got %A" other

[<Fact>]
let ``Normal ACK projects SetUpdateTime suffix and does not change History`` () =
    let nodeId, state, pending = seededEdit ()
    let suffix = [ stamp nodeId ]
    let confirmed = [ confirm pending suffix ]
    let result =
        SyncLogic.reconcileAck
            [ pending ]
            confirmed
            (Revision 1)
            state
            (sending [ pending ])
    let next, syncInfo, effects, suffixOps = expectApplied result
    Assert.Equal(state.history, next.history)
    Assert.Equal(Revision 1, next.revision)
    Assert.Equal(stampTime, next.graph.nodes.[nodeId].updateTime)
    Assert.Equal("after", next.graph.nodes.[nodeId].text)
    Assert.Empty(syncInfo.pendingChanges)
    Assert.Equal(Idle, syncInfo.syncState)
    Assert.Empty(effects)
    Assert.Equal<Op list>(suffix, suffixOps)

[<Fact>]
let ``Undo ACK retires the Undo transition without changing History`` () =
    let nodeId, afterEdit, _ = seededEdit ()
    match SyncLogic.applyLocalUndo (Guid.NewGuid()) afterEdit with
    | Some (Ok (afterUndo, pending)) ->
        let result =
            SyncLogic.reconcileAck
                [ pending ]
                [ confirm pending [] ]
                (Revision 1)
                afterUndo
                (sending [ pending ])
        let next, syncInfo, _, _ = expectApplied result
        Assert.Equal(afterUndo.history, next.history)
        Assert.Equal("before", next.graph.nodes.[nodeId].text)
        Assert.Empty(syncInfo.pendingChanges)
    | other -> failwithf "expected Undo, got %A" other

[<Fact>]
let ``Redo ACK retires the Redo transition without changing History`` () =
    let nodeId, afterEdit, _ = seededEdit ()
    match SyncLogic.applyLocalUndo (Guid.NewGuid()) afterEdit with
    | Some (Ok (afterUndo, _)) ->
        match SyncLogic.applyLocalRedo (Guid.NewGuid()) afterUndo with
        | Some (Ok (afterRedo, pending)) ->
            let result =
                SyncLogic.reconcileAck
                    [ pending ]
                    [ confirm pending [] ]
                    (Revision 1)
                    afterRedo
                    (sending [ pending ])
            let next, _, _, _ = expectApplied result
            Assert.Equal(afterRedo.history, next.history)
            Assert.Equal("after", next.graph.nodes.[nodeId].text)
        | other -> failwithf "expected Redo, got %A" other
    | other -> failwithf "expected Undo, got %A" other

[<Fact>]
let ``same-batch C Undo Redo ACK retires the prefix together`` () =
    let nodeId, afterEdit, normal = seededEdit ()
    match SyncLogic.applyLocalUndo (Guid.NewGuid()) afterEdit with
    | Some (Ok (afterUndo, undoItem)) ->
        match SyncLogic.applyLocalRedo (Guid.NewGuid()) afterUndo with
        | Some (Ok (afterRedo, redoItem)) ->
            let submitted = [ normal; undoItem; redoItem ]
            let confirmed = submitted |> List.map (fun item -> confirm item [])
            let result =
                SyncLogic.reconcileAck
                    submitted
                    confirmed
                    (Revision 3)
                    afterRedo
                    (sending submitted)
            let next, syncInfo, _, _ = expectApplied result
            Assert.Equal(afterRedo.history, next.history)
            Assert.Equal("after", next.graph.nodes.[nodeId].text)
            Assert.Empty(syncInfo.pendingChanges)
        | other -> failwithf "expected Redo, got %A" other
    | other -> failwithf "expected Undo, got %A" other

[<Fact>]
let ``partial residency skips a stamp on an Absent node`` () =
    let _, state, pending = seededEdit ()
    let absentId = NodeId.New()
    let suffix = [ stamp absentId ]
    let result =
        SyncLogic.reconcileAck
            [ pending ]
            [ confirm pending suffix ]
            (Revision 1)
            state
            (sending [ pending ])
    let next, _, _, suffixOps = expectApplied result
    Assert.False(Map.containsKey absentId next.graph.nodes)
    Assert.Equal(state.history, next.history)
    Assert.Equal<Op list>(suffix, suffixOps)

[<Fact>]
let ``retry ACK removes only the submitted prefix and resubmits the remainder`` () =
    let _, state, first = seededEdit ()
    let later =
        { first.change with
            id = 1
            changeId = Guid.NewGuid()
            ops = [ Op.SetText(NodeId.New(), "x", "y") ] }
        |> PendingChange.ofChange
    let pending = [ first; later ]
    let result =
        SyncLogic.reconcileAck
            [ first ]
            [ confirm first [] ]
            (Revision 1)
            state
            (sending pending)
    let _, syncInfo, effects, _ = expectApplied result
    Assert.Equal<PendingChange list>([ later ], syncInfo.pendingChanges)
    Assert.Equal(Sending 1, syncInfo.syncState)
    match effects with
    | [ SubmitPendingBatch (baseRevision, changes) ] ->
        Assert.Equal(1, baseRevision)
        Assert.Equal<PendingChange list>([ later ], changes)
    | _ -> failwith "expected remainder SubmitPendingBatch"

[<Fact>]
let ``late duplicate response is ignored when identities are retired`` () =
    let _, state, pending = seededEdit ()
    let acked =
        { state with revision = Revision 1 }
    let result =
        SyncLogic.reconcileAck
            [ pending ]
            [ confirm pending [ stamp (NodeId.New()) ] ]
            (Revision 1)
            acked
            SyncInfo.initial
    match result with
    | AckReconcile.Ignored ->
        Assert.Equal(Revision 1, acked.revision)
        Assert.Equal(state.history, acked.history)
    | other -> failwithf "expected Ignored, got %A" other

[<Fact>]
let ``rejected ACK leaves graph revision History and pending unchanged`` () =
    let nodeId, state, pending = seededEdit ()
    let confirmed =
        [ { pending.change with
              ops = pending.change.ops @ [ Op.SetText(nodeId, "after", "nope") ] } ]
    let syncInfo = sending [ pending ]
    let result =
        SyncLogic.reconcileAck [ pending ] confirmed (Revision 1) state syncInfo
    expectRejected "forbidden-suffix" result
    Assert.Equal(Revision 0, state.revision)
    Assert.Equal("after", state.graph.nodes.[nodeId].text)
    Assert.Equal<PendingChange list>([ pending ], syncInfo.pendingChanges)

[<Fact>]
let ``workspace singleton ACK uses the same seam as the queue`` () =
    let nodeId, state, pending = seededEdit ()
    let submitted = PendingChange.workspaceSingleton 4 pending.change
    let result =
        SyncLogic.reconcileAck
            [ submitted ]
            [ confirm submitted [ stamp nodeId ] ]
            (Revision 1)
            state
            (sending [ submitted ])
    let next, syncInfo, _, _ = expectApplied result
    Assert.Equal(state.history, next.history)
    Assert.Equal(stampTime, next.graph.nodes.[nodeId].updateTime)
    Assert.Empty(syncInfo.pendingChanges)

[<Fact>]
let ``async workspace late duplicate is ignored without applying suffixes twice`` () =
    let nodeId, state, pending = seededEdit ()
    let submitted = PendingChange.workspaceSingleton 4 pending.change
    let first =
        SyncLogic.reconcileAck
            [ submitted ]
            [ confirm submitted [ stamp nodeId ] ]
            (Revision 1)
            state
            (sending [ submitted ])
    let after, _, _, _ = expectApplied first
    let duplicate =
        SyncLogic.reconcileAck
            [ submitted ]
            [ confirm submitted [ stamp nodeId ] ]
            (Revision 1)
            after
            SyncInfo.initial
    match duplicate with
    | AckReconcile.Ignored -> Assert.Equal(stampTime, after.graph.nodes.[nodeId].updateTime)
    | other -> failwithf "expected Ignored, got %A" other

[<Fact>]
let ``missing confirmation is rejected atomically`` () =
    let _, state, pending = seededEdit ()
    let result =
        SyncLogic.reconcileAck [ pending ] [] (Revision 1) state (sending [ pending ])
    expectRejected "missing" result

[<Fact>]
let ``reordered confirmation is rejected atomically`` () =
    let _, afterEdit, first = seededEdit ()
    match SyncLogic.applyLocalUndo (Guid.NewGuid()) afterEdit with
    | Some (Ok (afterUndo, second)) ->
        let submitted = [ first; second ]
        let confirmed = [ confirm second []; confirm first [] ]
        let result =
            SyncLogic.reconcileAck
                submitted
                confirmed
                (Revision 2)
                afterUndo
                (sending submitted)
        expectRejected "reordered" result
    | other -> failwithf "expected Undo, got %A" other

[<Fact>]
let ``unmatched confirmation is rejected atomically`` () =
    let _, state, pending = seededEdit ()
    let other = { pending.change with changeId = Guid.NewGuid() }
    let result =
        SyncLogic.reconcileAck
            [ pending ]
            [ other ]
            (Revision 1)
            state
            (sending [ pending ])
    expectRejected "unmatched" result

[<Fact>]
let ``changed-prefix confirmation is rejected atomically`` () =
    let nodeId, state, pending = seededEdit ()
    let confirmed =
        [ { pending.change with ops = [ Op.SetText(nodeId, "before", "other") ] } ]
    let result =
        SyncLogic.reconcileAck
            [ pending ]
            confirmed
            (Revision 1)
            state
            (sending [ pending ])
    expectRejected "changed-prefix" result

[<Fact>]
let ``partial-overlap confirmation is rejected atomically`` () =
    let _, afterEdit, first = seededEdit ()
    match SyncLogic.applyLocalUndo (Guid.NewGuid()) afterEdit with
    | Some (Ok (afterUndo, second)) ->
        let submitted = [ first; second ]
        let result =
            SyncLogic.reconcileAck
                submitted
                (submitted |> List.map (fun item -> confirm item []))
                (Revision 2)
                afterUndo
                (sending [ second ])
        expectRejected "partial-overlap" result
    | other -> failwithf "expected Undo, got %A" other

[<Fact>]
let ``forward-Revision late response is rejected atomically`` () =
    let _, state, pending = seededEdit ()
    let acked = { state with revision = Revision 1 }
    let result =
        SyncLogic.reconcileAck
            [ pending ]
            [ confirm pending [] ]
            (Revision 4)
            acked
            SyncInfo.initial
    expectRejected "forward-Revision" result

[<Fact>]
let ``forbidden-suffix confirmation is rejected atomically`` () =
    let nodeId, state, pending = seededEdit ()
    let confirmed =
        [ { pending.change with
              ops = pending.change.ops @ [ Op.SetText(nodeId, "after", "nope") ] } ]
    let result =
        SyncLogic.reconcileAck
            [ pending ]
            confirmed
            (Revision 1)
            state
            (sending [ pending ])
    expectRejected "forbidden-suffix" result

[<Fact>]
let ``externalChanges ACK notes catch-up without rejecting or changing graph`` () =
    let nodeId, state, pending = seededEdit ()
    let syncInfo = sending [ pending ]
    let result =
        SyncLogic.reconcileExternalAck
            [ pending ]
            (Revision 1)
            state
            syncInfo
    let next, nextSync, effects, suffixOps = expectApplied result
    Assert.Equal(state.graph, next.graph)
    Assert.Equal(state.revision, next.revision)
    Assert.Equal(state.history, next.history)
    Assert.Empty(nextSync.pendingChanges)
    Assert.Equal(Idle, nextSync.syncState)
    Assert.Equal(Some "before", nextSync.catchUp |> Option.map (fun c -> c.graph.nodes.[nodeId].text))
    Assert.Empty(suffixOps)
    Assert.Empty(effects)

[<Fact>]
let ``amended confirmation echo routes through external ACK not Reject`` () =
    let nodeId, state, pending = seededEdit ()
    let amended =
        [ { pending.change with
              ops = [ Op.SetText(nodeId, "before", "server-amended") ] } ]
    let result =
        SyncLogic.reconcileExternalAck
            [ pending ]
            (Revision 1)
            state
            (sending [ pending ])
    match result with
    | AckReconcile.Applied _ -> ()
    | AckReconcile.Rejected msg ->
        failwithf "expected external Applied, got Rejected: %s" msg
    | AckReconcile.Ignored -> failwith "expected Applied, got Ignored"
    Assert.False(SyncLogic.isConfirmationEcho [ pending ] amended)
