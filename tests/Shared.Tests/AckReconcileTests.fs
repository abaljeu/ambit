module AckReconcileTests

open System
open Xunit
open Gambol.Shared
open Gambol.Shared
open Gambol.Shared.ViewModel

let private stampTime =
    DateTime(2026, 8, 16, 12, 0, 0, DateTimeKind.Utc)

let private clientState graph revision history : ClientSyncState =
    ClientSyncState.create graph revision history

let private sending items =
    { SyncInfo.initial with
        pending = items
        syncState = Sending 1 }

let private confirm (item: Ev) suffix : Ev =
    { item with
        body = EventBody.Change (SpecialNodeTestHelpers.eventOps item @ suffix) }

let private stamp nodeId =
    Op.SetUpdateTime(nodeId, NodeUpdateTime.missing, stampTime)

let private textChange id nodeId oldText newText : Ev =
    { id = id
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = ""
      body = EventBody.Change [ Op.SetText(nodeId, oldText, newText) ] }

let private seededEdit () =
    let graph0 = Graph.create ()
    let graph1, nodeId = Graph.newNode "before" graph0
    let change = textChange EventId.zero nodeId "before" "after"
    let state0 = clientState graph1 (EventId.zero) (ClientHistory.clear ())
    match SyncLogic.applyLocalEvent { change with commandName = "Edit node" } state0 with
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
            (EventIdFixtures.storedId 1)
            state
            (sending [ pending ])
    let next, syncInfo, effects, suffixOps = expectApplied result
    Assert.Equal(state.history, next.history)
    Assert.Equal(EventIdFixtures.storedId 1, next.eventId)
    Assert.Equal(stampTime, next.graph.nodes.[nodeId].updateTime)
    Assert.Equal("after", next.graph.nodes.[nodeId].text)
    Assert.Empty(syncInfo.pending)
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
                (EventIdFixtures.storedId 1)
                afterUndo
                (sending [ pending ])
        let next, syncInfo, _, _ = expectApplied result
        Assert.Equal(afterUndo.history, next.history)
        Assert.Equal("before", next.graph.nodes.[nodeId].text)
        Assert.Empty(syncInfo.pending)
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
                    (EventIdFixtures.storedId 1)
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
                    (EventIdFixtures.storedId 3)
                    afterRedo
                    (sending submitted)
            let next, syncInfo, _, _ = expectApplied result
            Assert.Equal(afterRedo.history, next.history)
            Assert.Equal("after", next.graph.nodes.[nodeId].text)
            Assert.Empty(syncInfo.pending)
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
            (EventIdFixtures.storedId 1)
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
        { first with
            id = EventId.zero
            submissionId = Guid.NewGuid()
            body = EventBody.Change [ Op.SetText(NodeId.New(), "x", "y") ] }
    let pending = [ first; later ]
    let result =
        SyncLogic.reconcileAck
            [ first ]
            [ confirm first [] ]
            (EventIdFixtures.storedId 1)
            state
            (sending pending)
    let _, syncInfo, effects, _ = expectApplied result
    Assert.Equal<Ev list>([ later ], syncInfo.pending)
    Assert.Equal(Sending 1, syncInfo.syncState)
    match effects with
    | [ SubmitPendingBatch (baseEventId, events) ] ->
        Assert.Equal(EventIdFixtures.storedId 1, baseEventId)
        Assert.Equal<Ev list>([ later ], events)
    | _ -> failwith "expected remainder SubmitPendingBatch"

[<Fact>]
let ``late duplicate response is ignored when identities are retired`` () =
    let _, state, pending = seededEdit ()
    let acked =
        { state with eventId = EventIdFixtures.storedId 1 }
    let result =
        SyncLogic.reconcileAck
            [ pending ]
            [ confirm pending [ stamp (NodeId.New()) ] ]
            (EventIdFixtures.storedId 1)
            acked
            SyncInfo.initial
    match result with
    | AckReconcile.Ignored ->
        Assert.Equal(EventIdFixtures.storedId 1, acked.eventId)
        Assert.Equal(state.history, acked.history)
    | other -> failwithf "expected Ignored, got %A" other

[<Fact>]
let ``rejected ACK leaves graph revision History and pending unchanged`` () =
    let nodeId, state, pending = seededEdit ()
    let confirmed =
        [ { pending with
              body =
                EventBody.Change
                    (SpecialNodeTestHelpers.eventOps pending
                     @ [ Op.SetText(nodeId, "after", "nope") ]) } ]
    let syncInfo = sending [ pending ]
    let result =
        SyncLogic.reconcileAck [ pending ] confirmed (EventIdFixtures.storedId 1) state syncInfo
    expectRejected "forbidden-suffix" result
    Assert.Equal(EventId.zero, state.eventId)
    Assert.Equal("after", state.graph.nodes.[nodeId].text)
    Assert.Equal<Ev list>([ pending ], syncInfo.pending)

[<Fact>]
let ``workspace singleton ACK uses the same seam as the queue`` () =
    let nodeId, state, pending = seededEdit ()
    let submitted = pending
    let result =
        SyncLogic.reconcileAck
            [ submitted ]
            [ confirm submitted [ stamp nodeId ] ]
            (EventIdFixtures.storedId 1)
            state
            (sending [ submitted ])
    let next, syncInfo, _, _ = expectApplied result
    Assert.Equal(state.history, next.history)
    Assert.Equal(stampTime, next.graph.nodes.[nodeId].updateTime)
    Assert.Empty(syncInfo.pending)

[<Fact>]
let ``async workspace late duplicate is ignored without applying suffixes twice`` () =
    let nodeId, state, pending = seededEdit ()
    let submitted = pending
    let first =
        SyncLogic.reconcileAck
            [ submitted ]
            [ confirm submitted [ stamp nodeId ] ]
            (EventIdFixtures.storedId 1)
            state
            (sending [ submitted ])
    let after, _, _, _ = expectApplied first
    let duplicate =
        SyncLogic.reconcileAck
            [ submitted ]
            [ confirm submitted [ stamp nodeId ] ]
            (EventIdFixtures.storedId 1)
            after
            SyncInfo.initial
    match duplicate with
    | AckReconcile.Ignored -> Assert.Equal(stampTime, after.graph.nodes.[nodeId].updateTime)
    | other -> failwithf "expected Ignored, got %A" other

[<Fact>]
let ``missing confirmation is rejected atomically`` () =
    let _, state, pending = seededEdit ()
    let result =
        SyncLogic.reconcileAck
            [ pending ]
            []
            (EventIdFixtures.storedId 1)
            state
            (sending [ pending ])
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
                (EventIdFixtures.storedId 2)
                afterUndo
                (sending submitted)
        expectRejected "reordered" result
    | other -> failwithf "expected Undo, got %A" other

[<Fact>]
let ``unmatched confirmation is rejected atomically`` () =
    let _, state, pending = seededEdit ()
    let other = { pending with submissionId = Guid.NewGuid() }
    let result =
        SyncLogic.reconcileAck
            [ pending ]
            [ other ]
            (EventIdFixtures.storedId 1)
            state
            (sending [ pending ])
    expectRejected "unmatched" result

[<Fact>]
let ``changed-prefix confirmation is rejected atomically`` () =
    let nodeId, state, pending = seededEdit ()
    let confirmed =
        [ { pending with
              body = EventBody.Change [ Op.SetText(nodeId, "before", "other") ] } ]
    let result =
        SyncLogic.reconcileAck
            [ pending ]
            confirmed
            (EventIdFixtures.storedId 1)
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
                (EventIdFixtures.storedId 2)
                afterUndo
                (sending [ second ])
        expectRejected "partial-overlap" result
    | other -> failwithf "expected Undo, got %A" other

[<Fact>]
let ``forward-Revision late response is rejected atomically`` () =
    let _, state, pending = seededEdit ()
    let acked = { state with eventId = EventIdFixtures.storedId 1 }
    let result =
        SyncLogic.reconcileAck
            [ pending ]
            [ confirm pending [] ]
            (EventIdFixtures.storedId 4)
            acked
            SyncInfo.initial
    expectRejected "forward-event" result

[<Fact>]
let ``forbidden-suffix confirmation is rejected atomically`` () =
    let nodeId, state, pending = seededEdit ()
    let confirmed =
        [ { pending with
              body =
                EventBody.Change
                    (SpecialNodeTestHelpers.eventOps pending
                     @ [ Op.SetText(nodeId, "after", "nope") ]) } ]
    let result =
        SyncLogic.reconcileAck
            [ pending ]
            confirmed
            (EventIdFixtures.storedId 1)
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
            (EventIdFixtures.storedId 1)
            state
            syncInfo
    let next, nextSync, effects, suffixOps = expectApplied result
    Assert.Equal(state.graph, next.graph)
    Assert.Equal(state.eventId, next.eventId)
    Assert.Equal(state.history, next.history)
    Assert.Empty(nextSync.pending)
    Assert.Equal(Idle, nextSync.syncState)
    Assert.Equal(Some "before", nextSync.catchUp |> Option.map (fun c -> c.graph.nodes.[nodeId].text))
    Assert.Empty(suffixOps)
    Assert.Empty(effects)

[<Fact>]
let ``amended confirmation echo routes through external ACK not Reject`` () =
    let nodeId, state, pending = seededEdit ()
    let amended =
        [ { pending with
              body =
                EventBody.Change [ Op.SetText(nodeId, "before", "server-amended") ] } ]
    let result =
        SyncLogic.reconcileExternalAck
            [ pending ]
            (EventIdFixtures.storedId 1)
            state
            (sending [ pending ])
    match result with
    | AckReconcile.Applied _ -> ()
    | AckReconcile.Rejected msg ->
        failwithf "expected external Applied, got Rejected: %s" msg
    | AckReconcile.Ignored -> failwith "expected Applied, got Ignored"
    Assert.False(SyncLogic.isConfirmationEcho [ pending ] amended)

[<Fact>]
let ``Notice in the reply keeps the confirmation echo`` () =
    let nodeId, state, pending = seededEdit ()
    let notice =
        { pending with
            id = EventIdFixtures.storedId 2
            submissionId = Guid.NewGuid()
            authority = Authority "Browser"
            commandName = "Edit node"
            body =
                EventBody.Notice(
                    Notice.NodePersisted(nodeId, EventId.zero, DateTime.UtcNow)) }
    let confirmed = [ confirm pending []; notice ]
    Assert.True(SyncLogic.isConfirmationEcho [ pending ] confirmed)
    let applied =
        SyncLogic.reconcileAck
            [ pending ]
            confirmed
            (EventIdFixtures.storedId 2)
            state
            (sending [ pending ])
    let _, syncInfo, _, _ = expectApplied applied
    Assert.True(syncInfo.catchUp.IsNone)
    let foreign =
        { confirm pending [] with
            submissionId = Guid.NewGuid()
            body = EventBody.Change [ Op.SetText(nodeId, "before", "other") ] }
    Assert.False(SyncLogic.isConfirmationEcho [ pending ] [ foreign; notice ])
    match
        SyncLogic.reconcileExternalAck
            [ pending ]
            (EventIdFixtures.storedId 3)
            state
            (sending [ pending ])
    with
    | AckReconcile.Applied (_, caught, _, _) ->
        Assert.True(caught.catchUp.IsSome)
    | other -> failwithf "expected catch-up Applied, got %A" other

[<Fact>]
let ``Undo ACK sets changedVersion to the Undo event id`` () =
    let fileId = NodeId.New()
    let persisted = EventIdFixtures.storedId 2
    let node =
        Node.Create(
            fileId,
            text = "hello",
            name = Filename.create "note.txt",
            kind = Special File)
    let seeded =
        { node with
            documentState =
                { node.documentState with
                    persistedVersion = persisted
                    persistState = PersistState.Persisted } }
    let graph = Graph.addDetachedNode seeded (Graph.create ())
    let change =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = "Edit"
          body = EventBody.Change [ Op.SetText(fileId, "hello", "edited") ] }
    let start =
        clientState graph EventId.zero (ClientHistory.clear ())
    match SyncLogic.applyLocalEvent change start with
    | Error msg -> failwith msg
    | Ok (afterEdit, _) ->
        match SyncLogic.applyLocalUndo (Guid.NewGuid()) afterEdit with
        | Some (Ok (afterUndo, pending)) ->
            let undoId = EventIdFixtures.storedId 9
            let confirmed = { pending with id = undoId }
            let result =
                SyncLogic.reconcileAck
                    [ pending ]
                    [ confirmed ]
                    undoId
                    afterUndo
                    (sending [ pending ])
            let next, _, _, _ = expectApplied result
            let live = next.graph.nodes.[fileId]
            Assert.Equal("hello", live.text)
            Assert.Equal(
                PersistState.Unpersisted,
                live.documentState.persistState)
            Assert.Equal(undoId, live.documentState.changedVersion)
            Assert.Equal(persisted, live.documentState.persistedVersion)
        | other -> failwithf "expected Undo, got %A" other
