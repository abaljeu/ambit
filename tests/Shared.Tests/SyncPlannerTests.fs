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
let ``tryStartSubmit returns SubmitChange effect when queue is ready`` () =
    let c = mkChange 7
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ c ] }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (Revision 9) syncInfo
    Assert.Equal(Sending 1, nextInfo.syncState)
    match effects with
    | [ SubmitChange (baseRevision, head) ] ->
        Assert.Equal(9, baseRevision)
        Assert.Equal(c.changeId, head.changeId)
    | _ ->
        failwith "Expected single SubmitChange effect"

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
let ``ackSubmit dequeues head and schedules next`` () =
    let c1 = mkChange 0
    let c2 = mkChange 0
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ c1; c2 ]
            syncState = Sending 1 }
    let nextInfo, pending, effects =
        SyncPlanner.ackSubmit c1.changeId (Revision 3) syncInfo
    Assert.Single(pending) |> ignore
    Assert.Equal(c2.changeId, pending.Head.changeId)
    Assert.Equal(Sending 1, nextInfo.syncState)
    match effects with
    | [ SubmitChange (baseRevision, head) ] ->
        Assert.Equal(3, baseRevision)
        Assert.Equal(c2.changeId, head.changeId)
    | _ ->
        failwith "Expected single SubmitChange effect for remaining queue"

[<Fact>]
let ``ackSubmit with last item returns Idle and no effects`` () =
    let c = mkChange 0
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ c ]
            syncState = Sending 1 }
    let nextInfo, pending, effects =
        SyncPlanner.ackSubmit c.changeId (Revision 1) syncInfo
    Assert.Empty(pending)
    Assert.Equal(Idle, nextInfo.syncState)
    Assert.Empty(effects)

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
