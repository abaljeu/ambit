module Gambol.Shared.Tests.SyncPlannerTests

open System
open Xunit
open Gambol.Shared
open Gambol.Shared.ViewModel

let private mkChange id =
    { id = id
      changeId = Guid.NewGuid()
      ops = [] }

let private asPending change = PendingChange.ofChange change

let private withKind recordId kind change : PendingChange =
    { change = change
      transition =
        Some
            { recordId = recordId
              submittedChangeId = change.changeId
              kind = kind } }

[<Fact>]
let ``tryStartSubmit returns SubmitPendingBatch effect when queue is ready`` () =
    let c = mkChange 7
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ asPending c ] }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (Revision 9) syncInfo
    Assert.Equal(Sending 1, nextInfo.syncState)
    match effects with
    | [ SubmitPendingBatch (baseRevision, changes) ] ->
        Assert.Equal(9, baseRevision)
        Assert.Equal<PendingChange list>([ asPending c ], changes)
    | _ ->
        failwith "Expected single SubmitPendingBatch effect"

[<Fact>]
let ``tryStartSubmit returns no effects when already sending`` () =
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ mkChange 1 |> asPending ]
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
            pendingChanges = [ c1; c2; c3 ] |> List.map asPending
            syncState = Sending 1 }
    let nextInfo, pending, effects =
        SyncPlanner.ackBatch [ c1.changeId; c2.changeId ] (Revision 3) syncInfo
    Assert.Single(pending) |> ignore
    Assert.Equal(c3.changeId, pending.Head.change.changeId)
    Assert.Equal(Sending 1, nextInfo.syncState)
    match effects with
    | [ SubmitPendingBatch (baseRevision, changes) ] ->
        Assert.Equal(3, baseRevision)
        Assert.Equal<PendingChange list>([ asPending c3 ], changes)
    | _ ->
        failwith "Expected single SubmitPendingBatch effect for remaining queue"

[<Fact>]
let ``ackBatch with last item returns Idle and no effects`` () =
    let c = mkChange 0
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ asPending c ]
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
        { SyncInfo.initial with pendingChanges = [ mkChange 0 |> asPending ] }
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
let ``tryStartLoad emits LoadServer when idle with empty queue`` () =
    let targetId = NodeId.New()
    let targets = [ { targetId = targetId; includeWorkspace = true } ]
    let si, effects =
        SyncPlanner.tryStartLoad (Revision 5) targets SyncInfo.initial
    Assert.Equal(Loading, si.syncState)
    match effects with
    | [ LoadServer (rev, loadTargets) ] ->
        Assert.Equal(5, rev)
        Assert.Equal(1, loadTargets.Length)
        Assert.Equal(targetId, loadTargets.[0].targetId)
        Assert.True(loadTargets.[0].includeWorkspace)
    | _ -> failwith "Expected single LoadServer effect"

[<Fact>]
let ``tryStartLoad returns no effects when already loading`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Loading }
    let targets =
        [ { targetId = NodeId.New(); includeWorkspace = false } ]
    let si, effects =
        SyncPlanner.tryStartLoad (Revision 5) targets syncInfo
    Assert.Equal(Loading, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartPoll returns no effects when loading`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Loading }
    let si, effects = SyncPlanner.tryStartPoll (Revision 5) syncInfo
    Assert.Equal(Loading, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartSubmit returns no effects when loading`` () =
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ mkChange 1 |> asPending ]
            syncState = Loading }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (Revision 1) syncInfo
    Assert.Equal(Loading, nextInfo.syncState)
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
            pendingChanges = [ mkChange 14706 |> asPending ]
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
            pendingChanges = [ asPending c ]
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
            pendingChanges = [ mkChange 1 |> asPending ]
            syncState = Uploading }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (Revision 1) syncInfo
    Assert.Equal(Uploading, nextInfo.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartPoll returns no effects when parsing`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Parsing }
    let si, effects = SyncPlanner.tryStartPoll (Revision 5) syncInfo
    Assert.Equal(Parsing, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartSubmit returns no effects when parsing`` () =
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ mkChange 1 |> asPending ]
            syncState = Parsing }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (Revision 1) syncInfo
    Assert.Equal(Parsing, nextInfo.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartLoad returns no effects when parsing`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Parsing }
    let targets =
        [ { targetId = NodeId.New(); includeWorkspace = false } ]
    let si, effects =
        SyncPlanner.tryStartLoad (Revision 5) targets syncInfo
    Assert.Equal(Parsing, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``mixed C Undo Redo delta chain preserves identities and rewrites revisions`` () =
    let change = mkChange 99
    let undo = mkChange 99
    let redo = mkChange 99
    let items =
        [ withKind 4 PendingKind.Normal change
          withKind 4 PendingKind.Undo undo
          withKind 4 PendingKind.Redo redo ]
    let chained = SyncBatch.toPendingDeltaChain 7 items
    Assert.Equal<int list>(
        [ 7; 8; 9 ],
        chained |> List.map (fun item -> item.change.id))
    Assert.Equal<Guid list>(
        [ change.changeId; undo.changeId; redo.changeId ],
        chained |> List.map (fun item -> item.change.changeId))
    Assert.Equal<PendingKind option list>(
        [ Some PendingKind.Normal; Some PendingKind.Undo; Some PendingKind.Redo ],
        chained
        |> List.map (fun item ->
            item.transition |> Option.map (fun t -> t.kind)))
    let wire = SyncBatch.toWireBatch 7 items
    Assert.Equal<ChangeRequest list>(
        [ ChangeRequest.Change { change with id = 7 }
          ChangeRequest.Undo(8, undo.changeId)
          ChangeRequest.Redo(9, redo.changeId) ],
        wire)

[<Fact>]
let ``applyAndEnqueueLocalAction applies Undo optimistically and queues the Change`` () =
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
    let undo = ChangeRequest.Undo(1, Guid.NewGuid())
    match
        SyncPlanner.applyAndEnqueueLocalAction
            undo
            changed
            SyncInfo.initial
    with
    | Error error -> failwith error
    | Ok (next, syncInfo, effects) ->
        Assert.Equal(node.text, next.graph.nodes.[node.id].text)
        let queued = Assert.Single(syncInfo.pendingChanges)
        Assert.Equal(ChangeRequest.actionId undo, queued.change.changeId)
        let queuedKind = queued.transition |> Option.map (fun t -> t.kind)
        Assert.Equal(Some PendingKind.Undo, queuedKind)
        Assert.False(queued.change.ops.IsEmpty)
        Assert.Contains(SavePendingQueue syncInfo.pendingChanges, effects)

[<Fact>]
let ``later queued actions do not alter the SubmitPendingBatch list`` () =
    let first = mkChange 1 |> asPending
    let later = mkChange 2 |> asPending
    let syncInfo =
        { SyncInfo.initial with pendingChanges = [ first ] }
    let sending, effects = SyncPlanner.tryStartSubmit (Revision 4) syncInfo
    match effects with
    | [ SubmitPendingBatch (_, submitted) ] ->
        let grown =
            { sending with pendingChanges = sending.pendingChanges @ [ later ] }
        Assert.Equal<PendingChange list>([ first ], submitted)
        Assert.Equal<PendingChange list>([ first; later ], grown.pendingChanges)
        let _, laterEffects = SyncPlanner.tryStartSubmit (Revision 4) grown
        Assert.Empty(laterEffects)
    | _ ->
        failwith "Expected single SubmitPendingBatch effect"

[<Fact>]
let ``retry list stays the submitted snapshot after later actions append`` () =
    let submitted = [ mkChange 1 |> asPending ]
    let later = mkChange 2 |> asPending
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = submitted @ [ later ]
            syncState = WaitingToRetry(1, 4, submitted) }
    let nextInfo, effects = SyncPlanner.retryWaiting false syncInfo
    Assert.Equal(Sending 2, nextInfo.syncState)
    match effects with
    | [ SubmitPendingBatch (baseRevision, retryList) ] ->
        Assert.Equal(4, baseRevision)
        Assert.Equal<PendingChange list>(submitted, retryList)
        Assert.NotEqual<PendingChange list>(syncInfo.pendingChanges, retryList)
    | _ ->
        failwith "Expected retry of the WaitingToRetry snapshot"

[<Fact>]
let ``same recordId C Undo Redo remain one SubmitPendingBatch`` () =
    let items =
        [ mkChange 0 |> withKind 7 PendingKind.Normal
          mkChange 0 |> withKind 7 PendingKind.Undo
          mkChange 0 |> withKind 7 PendingKind.Redo ]
    let syncInfo = { SyncInfo.initial with pendingChanges = items }
    let _, effects = SyncPlanner.tryStartSubmit (Revision 3) syncInfo
    match effects with
    | [ SubmitPendingBatch (_, submitted) ] ->
        Assert.Equal<PendingChange list>(items, submitted)
    | _ ->
        failwith "Expected the full same-recordId batch"

[<Fact>]
let ``ackBatch remainder with the same recordId still submits together`` () =
    let first = mkChange 0 |> withKind 7 PendingKind.Normal
    let remainder =
        [ mkChange 0 |> withKind 7 PendingKind.Undo
          mkChange 0 |> withKind 7 PendingKind.Redo ]
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = first :: remainder
            syncState = Sending 1 }
    let _, pending, effects =
        SyncPlanner.ackBatch [ first.change.changeId ] (Revision 1) syncInfo
    Assert.Equal<PendingChange list>(remainder, pending)
    match effects with
    | [ SubmitPendingBatch (_, submitted) ] ->
        Assert.Equal<PendingChange list>(remainder, submitted)
    | _ ->
        failwith "Expected remainder batch with the same recordId"

[<Fact>]
let ``restorePending strips transition and does not record History`` () =
    let state0 = ModelBuilder.createState12 ()
    let root = state0.graph.nodes.[state0.graph.root]
    let node = state0.graph.nodes.[root.children.Head.id]
    let stale =
        { id = 0
          changeId = Guid.NewGuid()
          ops = [ Op.SetText(node.id, node.text, "stale") ] }
        |> asPending
    let change =
        { id = 1
          changeId = Guid.NewGuid()
          ops = [ Op.SetText(node.id, node.text, "restored") ] }
    let saved = [ stale; change |> withKind 3 PendingKind.Undo ]
    let snapshot =
        { state0 with
            history = History.empty
            revision = Revision 1 }
    let next, restored =
        SyncPlanner.restorePending (Revision 1) saved snapshot
    let queued = Assert.Single(restored)
    Assert.Equal(None, queued.transition)
    Assert.Equal(change.changeId, queued.change.changeId)
    Assert.Equal("restored", next.graph.nodes.[node.id].text)
    Assert.Equal(History.empty, next.history)

[<Fact>]
let ``workspace singleton lineage is the exact item used before the request`` () =
    let change = mkChange 12
    let submitted = PendingChange.workspaceSingleton 5 change
    match submitted.transition with
    | Some transition ->
        Assert.Equal(5, transition.recordId)
        Assert.Equal(change.changeId, transition.submittedChangeId)
        Assert.Equal(PendingKind.Normal, transition.kind)
    | None ->
        failwith "Expected workspace PendingTransition"
    let chained = SyncBatch.toPendingDeltaChain 12 [ submitted ]
    let wire = SyncBatch.toWireBatch 12 [ submitted ]
    Assert.Equal(submitted.change.changeId, chained.Head.change.changeId)
    Assert.Equal(submitted.transition, chained.Head.transition)
    Assert.Equal<ChangeRequest list>(
        [ ChangeRequest.Change { change with id = 12 } ],
        wire)
    let effect =
        ContinuePostUploadStructure(
            submitted,
            { label = "home"
              relative = "notes.md"
              kind = SyncScopeKind.File },
            None)
    match effect with
    | ContinuePostUploadStructure (item, _, _) ->
        Assert.Equal(submitted, item)
    | _ ->
        failwith "Expected ContinuePostUploadStructure to carry the singleton"
