module ActorLiveTests

open System
open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

let private actorStartEvent eventId focusId : Ev =
    { id = eventId
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = "Start"
      body =
        EventBody.ActorStart
            { zoomId = focusId
              focusId = focusId
              commandId = NodeId.New()
              graphIds = [ focusId ]
              eventId = eventId } }

let private actorStopEvent eventId focusId result : Ev =
    { id = eventId
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = "Stop"
      body = EventBody.ActorStop(focusId, result) }

[<Fact>]
let ``ActorStart adds and ActorStop removes a live Focus`` () =
    let focusId = NodeId.New()
    let started = actorStartEvent (EventIdFixtures.storedId 1) focusId
    let live = ActorLive.applyEvents [ started ] Set.empty
    Assert.True(Set.contains focusId live)
    let stopped =
        actorStopEvent
            (EventIdFixtures.storedId 2) focusId ActorSucceeded
    Assert.False(
        Set.contains focusId (ActorLive.applyEvents [ stopped ] live))

[<Fact>]
let ``command response ActorStart is live after applyServerTail`` () =
    let focusId = NodeId.New()
    let started = actorStartEvent (EventIdFixtures.storedId 6) focusId
    let state =
        ClientSyncState.create
            (Graph.create ())
            (EventIdFixtures.storedId 5)
            (ClientHistory.clear ())
    match SyncLogic.applyServerTail [ started ] state with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok after ->
        Assert.True(Set.contains focusId after.actorLiveFocusIds)
        Assert.Equal(
            Some (CmdLastResult.Detail (Some "Run", "AI started.")),
            ActorLive.lastCmdResult [ started ])

[<Fact>]
let ``ActorStop ActorFailed sets AI Actor failed lastCmdResult`` () =
    let focusId = NodeId.New()
    let events =
        [ actorStartEvent (EventIdFixtures.storedId 1) focusId
          actorStopEvent
            (EventIdFixtures.storedId 2) focusId (ActorFailed "") ]
    Assert.Equal(
        Some (CmdLastResult.Error (Some "AI", "Actor failed.")),
        ActorLive.lastCmdResult events)
    let named =
        [ actorStopEvent
            (EventIdFixtures.storedId 5)
            focusId
            (ActorFailed
                "Could not send message to Cursor: unauthorized") ]
    Assert.Equal(
        Some (
            CmdLastResult.Error (
                Some "AI",
                "Could not send message to Cursor: unauthorized")),
        ActorLive.lastCmdResult named)
    let succeeded =
        [ actorStopEvent
            (EventIdFixtures.storedId 3) focusId ActorSucceeded ]
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "AI", "Actor succeeded.")),
        ActorLive.lastCmdResult succeeded)
    let cancelled =
        [ actorStopEvent
            (EventIdFixtures.storedId 4) focusId ActorCancelled ]
    Assert.Equal(
        Some (CmdLastResult.Error (Some "AI", "Actor cancelled.")),
        ActorLive.lastCmdResult cancelled)
    Assert.Equal(
        "Run: AI started.",
        CmdLastResult.toDisplay
            (CmdLastResult.Detail (Some "Run", "AI started.")))

[<Fact>]
let ``cancelEffect sends SubmitCancel only while the Focus is live`` () =
    let focusId = NodeId.New()
    let otherId = NodeId.New()
    let live = Set.singleton focusId
    Assert.True(ActorLive.offersCancel focusId live)
    Assert.False(ActorLive.offersCancel otherId live)
    Assert.Equal(
        Some (SubmitCancel focusId),
        ActorLive.cancelEffect focusId live)
    Assert.Equal(None, ActorLive.cancelEffect otherId live)
    Assert.Equal(None, ActorLive.cancelEffect focusId Set.empty)

[<Fact>]
let ``focusIdsFromLockPresent reads GetState overlay`` () =
    let focusId = NodeId.New()
    let graph0 = Graph.create ()
    let node = Node.Create(focusId, text = "focus", lockPresent = true)
    let graph =
        Graph.fromNodes graph0.root (Map.add focusId node graph0.nodes)
    Assert.True(
        Set.contains focusId (ActorLive.focusIdsFromLockPresent graph))
    Assert.True(Set.isEmpty (ActorLive.focusIdsFromLockPresent graph0))
