module Gambol.Shared.Tests.SyncPlannerTests

open System
open Xunit
open Gambol.Shared
open Gambol.Shared.ViewModel

let private mkChange id =
    { id = id
      changeId = Guid.NewGuid()
      ops = [] }

let private asAction change = HistoryAction.Change change

[<Fact>]
let ``tryStartSubmit returns SubmitPendingBatch effect when queue is ready`` () =
    let c = mkChange 7
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ asAction c ] }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (Revision 9) syncInfo
    Assert.Equal(Sending 1, nextInfo.syncState)
    match effects with
    | [ SubmitPendingBatch (baseRevision, changes) ] ->
        Assert.Equal(9, baseRevision)
        Assert.Equal<HistoryAction list>([ asAction c ], changes)
    | _ ->
        failwith "Expected single SubmitPendingBatch effect"

[<Fact>]
let ``tryStartSubmit returns no effects when already sending`` () =
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ mkChange 1 |> asAction ]
            syncState = Sending 1 }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (Revision 1) syncInfo
    Assert.Equal(Sending 1, nextInfo.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``ackBatch dequeues acknowledged changes and schedules remainder`` () =
    let c1 = mkChange 0
    let c2 = mkChange 0
    let c3 = mkChange 0
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ c1; c2; c3 ] |> List.map asAction
            syncState = Sending 1 }
    let nextInfo, pending, effects =
        SyncPlanner.ackBatch [ c1.changeId; c2.changeId ] (Revision 3) syncInfo
    Assert.Single(pending) |> ignore
    Assert.Equal(c3.changeId, HistoryAction.actionId pending.Head)
    Assert.Equal(Sending 1, nextInfo.syncState)
    match effects with
    | [ SubmitPendingBatch (baseRevision, changes) ] ->
        Assert.Equal(3, baseRevision)
        Assert.Equal<HistoryAction list>([ asAction c3 ], changes)
    | _ ->
        failwith "Expected single SubmitPendingBatch effect for remaining queue"

[<Fact>]
let ``ackBatch with last item returns Idle and no effects`` () =
    let c = mkChange 0
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ asAction c ]
            syncState = Sending 1 }
    let nextInfo, pending, effects =
        SyncPlanner.ackBatch [ c.changeId ] (Revision 1) syncInfo
    Assert.Empty(pending)
    Assert.Equal(Idle, nextInfo.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``toDeltaChain rewrites stale queued ids to contiguous revisions`` () =
    let c1 = mkChange 637
    let c2 = mkChange 637
    let c3 = mkChange 637
    let chained = Gambol.Shared.SyncBatch.toDeltaChain 637 [ c1; c2; c3 ]
    Assert.Equal<int list>([ 637; 638; 639 ], chained |> List.map (fun c -> c.id))
    Assert.Equal<Guid list>(
        [ c1.changeId; c2.changeId; c3.changeId ],
        chained |> List.map (fun c -> c.changeId))

[<Fact>]
let ``toDeltaChain keeps empty batch empty`` () =
    let chained = Gambol.Shared.SyncBatch.toDeltaChain 12 []
    Assert.Empty(chained)

[<Fact>]
let ``tryStartPoll emits PollServer when idle with empty queue`` () =
    let si, effects = SyncPlanner.tryStartPoll (Revision 5) SyncInfo.initial
    Assert.Equal(Polling, si.syncState)
    match effects with
    | [ PollServer rev ] -> Assert.Equal(5, rev)
    | _ -> failwith "Expected single PollServer effect"

[<Fact>]
let ``tryStartPoll returns no effects when queue is non-empty`` () =
    let syncInfo =
        { SyncInfo.initial with pendingChanges = [ mkChange 0 |> asAction ] }
    let si, effects = SyncPlanner.tryStartPoll (Revision 5) syncInfo
    Assert.Equal(Idle, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartPoll returns no effects when already sending`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Sending 1 }
    let si, effects = SyncPlanner.tryStartPoll (Revision 5) syncInfo
    Assert.Equal(Sending 1, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartPoll returns no effects when uploading`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Uploading }
    let si, effects = SyncPlanner.tryStartPoll (Revision 5) syncInfo
    Assert.Equal(Uploading, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``queued workspace Upload waits while a change submit is in flight`` () =
    let scope =
        { label = "home"
          relative = "notes/today.md"
          kind = SyncScopeKind.File }
    let request = QueuedWorkspacePush(scope, Some(NodeId.New()))
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ mkChange 14706 |> asAction ]
            syncState = Sending 1 }
        |> SyncInfo.queueRequest request
    let si, effects = SyncPlanner.tryReleaseQueued syncInfo
    Assert.Equal<QueuedRequest list>([ request ], si.queuedRequests)
    Assert.Empty(effects)

[<Fact>]
let ``queued file Upload preserves its scope until the change queue drains`` () =
    let c = mkChange 14706
    let request =
        QueuedWorkspacePush(
            { label = "home"
              relative = "notes/today.md"
              kind = SyncScopeKind.File },
            Some(NodeId.New()))
    let queued =
        { SyncInfo.initial with
            pendingChanges = [ asAction c ]
            syncState = Sending 1 }
        |> SyncInfo.queueRequest request
    let acked, _, _ = SyncPlanner.ackBatch [ c.changeId ] (Revision 14707) queued
    let si, effects = SyncPlanner.tryReleaseQueued acked
    Assert.Empty(si.queuedRequests)
    Assert.Equal<Effect list>([ RunQueuedRequest request ], effects)

[<Fact>]
let ``queued Uploads release one request at a time`` () =
    let first =
        QueuedWorkspacePush(
            { label = "home"
              relative = "first.md"
              kind = SyncScopeKind.File },
            Some(NodeId.New()))
    let second =
        QueuedWorkspacePush(
            { label = "home"
              relative = "second.md"
              kind = SyncScopeKind.File },
            Some(NodeId.New()))
    let queued =
        SyncInfo.initial
        |> SyncInfo.queueRequest first
        |> SyncInfo.queueRequest second
    let si, effects = SyncPlanner.tryReleaseQueued queued
    Assert.Equal<QueuedRequest list>([ second ], si.queuedRequests)
    Assert.Equal<Effect list>([ RunQueuedRequest first ], effects)

[<Fact>]
let ``queued Upload is released after a poll settles`` () =
    let request =
        QueuedWorkspacePush(
            { label = "home"
              relative = ""
              kind = SyncScopeKind.Workspace },
            None)
    let syncInfo =
        { SyncInfo.initial with syncState = Polling }
        |> SyncInfo.queueRequest request
    Assert.Empty(snd (SyncPlanner.tryReleaseQueued syncInfo))
    let settled =
        { syncInfo with syncState = Idle } |> SyncPlanner.tryReleaseQueued
    Assert.Equal<Effect list>([ RunQueuedRequest request ], snd settled)

[<Fact>]
let ``pressing Load twice while it waits queues one request`` () =
    let request = QueuedLoad
    let syncInfo =
        { SyncInfo.initial with syncState = Polling }
        |> SyncInfo.queueRequest request
        |> SyncInfo.queueRequest request
    Assert.Equal<QueuedRequest list>([ request ], syncInfo.queuedRequests)

[<Fact>]
let ``QueuedLoad releases after sync settles`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Polling }
        |> SyncInfo.queueRequest QueuedLoad
    Assert.Empty(snd (SyncPlanner.tryReleaseQueued syncInfo))
    let settled =
        { syncInfo with syncState = Idle } |> SyncPlanner.tryReleaseQueued
    Assert.Equal<Effect list>([ RunQueuedRequest QueuedLoad ], snd settled)

[<Fact>]
let ``tryReleaseQueued is inert with nothing queued`` () =
    let si, effects = SyncPlanner.tryReleaseQueued SyncInfo.initial
    Assert.Equal(SyncInfo.initial, si)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartSubmit returns no effects when uploading`` () =
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ mkChange 1 |> asAction ]
            syncState = Uploading }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (Revision 1) syncInfo
    Assert.Equal(Uploading, nextInfo.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``mixed action delta chain preserves identities and rewrites revisions`` () =
    let change = mkChange 99
    let undoId = Guid.NewGuid()
    let redoId = Guid.NewGuid()
    let actions =
        [ HistoryAction.Change change
          HistoryAction.Undo(99, undoId)
          HistoryAction.Redo(99, redoId) ]
    let chained = SyncBatch.toActionDeltaChain 7 actions
    Assert.Equal<int list>(
        [ 7; 8; 9 ],
        chained |> List.map HistoryAction.baseRevision)
    Assert.Equal<Guid list>(
        [ change.changeId; undoId; redoId ],
        chained |> List.map HistoryAction.actionId)

[<Fact>]
let ``applyAndEnqueueLocalAction applies Undo optimistically and queues intent`` () =
    let state0 = ModelBuilder.createState12 ()
    let root = state0.graph.nodes.[state0.graph.root]
    let node = state0.graph.nodes.[root.children.Head.id]
    let change =
        { id = 0
          changeId = Guid.NewGuid()
          ops = [ Op.SetText(node.id, node.text, "changed") ] }
    let changed =
        History.applyChange change state0
        |> function
            | ApplyResult.Changed state -> state
            | _ -> failwith "expected initial change"
    let undo = HistoryAction.Undo(1, Guid.NewGuid())
    match
        SyncPlanner.applyAndEnqueueLocalAction
            undo
            changed
            SyncInfo.initial
    with
    | Error error -> failwith error
    | Ok (next, syncInfo, effects) ->
        Assert.Equal(node.text, next.graph.nodes.[node.id].text)
        Assert.Equal<HistoryAction list>([ undo ], syncInfo.pendingChanges)
        Assert.Contains(SavePendingQueue [ undo ], effects)
