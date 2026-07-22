module Gambol.Shared.Tests.SyncPlannerTests

open System
open Xunit
open Gambol.Shared
open Gambol.Shared.ViewModel

let private mkChange id =
    { id = id
      changeId = Guid.NewGuid()
      ops = [] }

[<Fact>]
let ``tryStartSubmit returns SubmitPendingBatch effect when queue is ready`` () =
    let c = mkChange 7
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ c ] }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (Revision 9) syncInfo
    Assert.Equal(Sending 1, nextInfo.syncState)
    match effects with
    | [ SubmitPendingBatch (baseRevision, changes) ] ->
        Assert.Equal(9, baseRevision)
        Assert.Equal<Change list>([ c ], changes)
    | _ ->
        failwith "Expected single SubmitPendingBatch effect"

[<Fact>]
let ``tryStartSubmit returns no effects when already sending`` () =
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ mkChange 1 ]
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
            pendingChanges = [ c1; c2; c3 ]
            syncState = Sending 1 }
    let nextInfo, pending, effects =
        SyncPlanner.ackBatch [ c1.changeId; c2.changeId ] (Revision 3) syncInfo
    Assert.Single(pending) |> ignore
    Assert.Equal(c3.changeId, pending.Head.changeId)
    Assert.Equal(Sending 1, nextInfo.syncState)
    match effects with
    | [ SubmitPendingBatch (baseRevision, changes) ] ->
        Assert.Equal(3, baseRevision)
        Assert.Equal<Change list>([ c3 ], changes)
    | _ ->
        failwith "Expected single SubmitPendingBatch effect for remaining queue"

[<Fact>]
let ``ackBatch with last item returns Idle and no effects`` () =
    let c = mkChange 0
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ c ]
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
        { SyncInfo.initial with pendingChanges = [ mkChange 0 ] }
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
let ``tryStartSubmit returns no effects when uploading`` () =
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ mkChange 1 ]
            syncState = Uploading }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (Revision 1) syncInfo
    Assert.Equal(Uploading, nextInfo.syncState)
    Assert.Empty(effects)
