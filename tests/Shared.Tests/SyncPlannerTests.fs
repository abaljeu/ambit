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
let ``tryStartSubmit returns head intent when queue is ready`` () =
    let c = mkChange 7
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ c ] }
    let nextInfo, intent = SyncPlanner.tryStartSubmit (Revision 9) syncInfo
    Assert.True(nextInfo.submitInFlight)
    Assert.Equal(Sending 1, nextInfo.syncState)
    match intent with
    | SubmitHead (baseRevision, head) ->
        Assert.Equal(9, baseRevision)
        Assert.Equal(c.changeId, head.changeId)
    | _ ->
        failwith "Expected SubmitHead intent"

[<Fact>]
let ``tryStartSubmit returns NoSubmit when already in flight`` () =
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ mkChange 1 ]
            submitInFlight = true }
    let nextInfo, intent = SyncPlanner.tryStartSubmit (Revision 1) syncInfo
    Assert.True(nextInfo.submitInFlight)
    Assert.Equal(NoSubmit, intent)

[<Fact>]
let ``ackSubmit dequeues head and schedules next`` () =
    let c1 = mkChange 0
    let c2 = mkChange 0
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ c1; c2 ]
            submitInFlight = true
            syncState = Sending 1 }
    let nextInfo, pending, intent =
        SyncPlanner.ackSubmit c1.changeId (Revision 3) syncInfo
    Assert.Single(pending) |> ignore
    Assert.Equal(c2.changeId, pending.Head.changeId)
    Assert.True(nextInfo.submitInFlight)
    Assert.Equal(Sending 1, nextInfo.syncState)
    match intent with
    | SubmitHead (baseRevision, head) ->
        Assert.Equal(3, baseRevision)
        Assert.Equal(c2.changeId, head.changeId)
    | _ ->
        failwith "Expected SubmitHead for remaining queue"

[<Fact>]
let ``ackSubmit with last item returns Idle and NoSubmit`` () =
    let c = mkChange 0
    let syncInfo =
        { SyncInfo.initial with
            pendingChanges = [ c ]
            submitInFlight = true
            syncState = Sending 1 }
    let nextInfo, pending, intent =
        SyncPlanner.ackSubmit c.changeId (Revision 1) syncInfo
    Assert.Empty(pending)
    Assert.False(nextInfo.submitInFlight)
    Assert.Equal(Idle, nextInfo.syncState)
    Assert.Equal(NoSubmit, intent)
