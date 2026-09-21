module ActorLiveTests

open System
open Gambol.Shared
open Gambol.Shared.ViewModel
open Xunit

let private owned = ChildNode.owners

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err -> failwith $"{label}: {err}"

let private actorStartAt eventId focusId zoomId : Ev =
    { id = eventId
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = "Start"
      body =
        EventBody.ActorStart
            { zoomId = zoomId
              focusId = focusId
              commandId = NodeId.New()
              graphIds = [ focusId ]
              eventId = eventId } }

let private actorStartEvent eventId focusId : Ev =
    actorStartAt eventId focusId focusId

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
let ``cancel response ActorStop Cancelled drops live after applyServerTail`` () =
    let focusId = NodeId.New()
    let started = actorStartEvent (EventIdFixtures.storedId 6) focusId
    let state =
        ClientSyncState.create
            (Graph.create ())
            (EventIdFixtures.storedId 5)
            (ClientHistory.clear ())
    match SyncLogic.applyServerTail [ started ] state with
    | Error msg -> failwith $"Expected Ok, got Error: {msg}"
    | Ok live ->
        Assert.True(Set.contains focusId live.actorLiveFocusIds)
        let cancelled =
            actorStopEvent
                (EventIdFixtures.storedId 7) focusId ActorCancelled
        match SyncLogic.applyServerTail [ cancelled ] live with
        | Error msg -> failwith $"Expected Ok, got Error: {msg}"
        | Ok after ->
            Assert.False(Set.contains focusId after.actorLiveFocusIds)
            Assert.Equal(
                Some (CmdLastResult.Error (None, "Actor cancelled.")),
                ActorLive.lastCmdResult
                    (Graph.create ()) focusId [ cancelled ])

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
            Some (CmdLastResult.Detail (Some "Run", "Actor started.")),
            ActorLive.lastCmdResult (Graph.create ()) focusId [ started ])

[<Fact>]
let ``ActorStop without Command uses generic lastCmdResult`` () =
    let focusId = NodeId.New()
    let events =
        [ actorStartEvent (EventIdFixtures.storedId 1) focusId
          actorStopEvent
            (EventIdFixtures.storedId 2) focusId (ActorFailed "") ]
    Assert.Equal(
        Some (CmdLastResult.Error (None, "Actor failed.")),
        ActorLive.lastCmdResult (Graph.create ()) focusId events)
    let named =
        [ actorStopEvent
            (EventIdFixtures.storedId 5)
            focusId
            (ActorFailed
                "Could not send message to Cursor: unauthorized") ]
    Assert.Equal(
        Some (
            CmdLastResult.Error (
                None,
                "Could not send message to Cursor: unauthorized")),
        ActorLive.lastCmdResult (Graph.create ()) focusId named)
    let succeeded =
        [ actorStopEvent
            (EventIdFixtures.storedId 3) focusId ActorSucceeded ]
    Assert.Equal(
        Some (CmdLastResult.Detail (None, "Actor succeeded.")),
        ActorLive.lastCmdResult (Graph.create ()) focusId succeeded)
    let cancelled =
        [ actorStopEvent
            (EventIdFixtures.storedId 4) focusId ActorCancelled ]
    Assert.Equal(
        Some (CmdLastResult.Error (None, "Actor cancelled.")),
        ActorLive.lastCmdResult (Graph.create ()) focusId cancelled)
    Assert.Equal(
        "Run: Actor started.",
        CmdLastResult.toDisplay
            (CmdLastResult.Detail (Some "Run", "Actor started.")))

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

let private graphWithCommand (commandText: string) : Graph * NodeId =
    let g0 = Graph.create ()
    let g1, ids = ModelBuilder.createNodes [ commandText ] g0
    let commandId = ids.[0]
    let graph =
        Graph.replace g1.root 0 [] (owned [ commandId ]) g1
        |> requireOk "graphWithCommand.root"
    graph, commandId

let private graphWithCommandChild
    (commandText: string)
    (childText: string)
    : Graph * NodeId * NodeId =
    let g0 = Graph.create ()
    let g1, commandIds = ModelBuilder.createNodes [ commandText ] g0
    let commandId = commandIds.[0]
    let g2, childIds = ModelBuilder.createNodes [ childText ] g1
    let childId = childIds.[0]
    let g3 =
        Graph.replace g2.root 0 [] (owned [ commandId ]) g2
        |> requireOk "graphWithCommandChild.root"
    let graph =
        Graph.replace commandId 0 [] (owned [ childId ]) g3
        |> requireOk "graphWithCommandChild.command"
    graph, commandId, childId

[<Fact>]
let ``?test start and stop use TitleCase Test not AI`` () =
    let graph, commandId = graphWithCommand "?test hello"
    let started =
        actorStartAt (EventIdFixtures.storedId 1) commandId commandId
    let startResult =
        ActorLive.lastCmdResult graph commandId [ started ]
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "Run", "Test started.")),
        startResult)
    Assert.Equal(
        "Run: Test started.",
        CmdLastResult.toDisplay
            (CmdLastResult.Detail (Some "Run", "Test started.")))
    let stopped =
        actorStopEvent
            (EventIdFixtures.storedId 2) commandId ActorSucceeded
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "Test", "Actor succeeded.")),
        ActorLive.lastCmdResult graph commandId [ stopped ])
    let failed =
        actorStopEvent
            (EventIdFixtures.storedId 3) commandId (ActorFailed "")
    Assert.Equal(
        Some (CmdLastResult.Error (Some "Test", "Actor failed.")),
        ActorLive.lastCmdResult graph commandId [ failed ])
    let cancelled =
        actorStopEvent
            (EventIdFixtures.storedId 4) commandId ActorCancelled
    Assert.Equal(
        Some (CmdLastResult.Error (Some "Test", "Actor cancelled.")),
        ActorLive.lastCmdResult graph commandId [ cancelled ])

[<Fact>]
let ``?test ancestor Command labels Focus child start and stop`` () =
    let graph, commandId, childId =
        graphWithCommandChild "?test hello" "note"
    let started =
        actorStartAt (EventIdFixtures.storedId 7) childId commandId
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "Run", "Test started.")),
        ActorLive.lastCmdResult graph commandId [ started ])
    let stopped =
        actorStopEvent
            (EventIdFixtures.storedId 8) childId ActorSucceeded
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "Test", "Actor succeeded.")),
        ActorLive.lastCmdResult graph commandId [ stopped ])

[<Fact>]
let ``?ai start and stop use TitleCase Ai not hardcoded AI`` () =
    let graph, commandId = graphWithCommand "?ai later"
    let started =
        actorStartAt (EventIdFixtures.storedId 9) commandId commandId
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "Run", "Ai started.")),
        ActorLive.lastCmdResult graph commandId [ started ])
    let stopped =
        actorStopEvent
            (EventIdFixtures.storedId 10) commandId ActorSucceeded
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "Ai", "Actor succeeded.")),
        ActorLive.lastCmdResult graph commandId [ stopped ])

[<Fact>]
let ``no Command on owner path uses generic wording not AI`` () =
    let graph, nodeId = graphWithCommand "hello"
    let started =
        actorStartAt (EventIdFixtures.storedId 11) nodeId nodeId
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "Run", "Actor started.")),
        ActorLive.lastCmdResult graph nodeId [ started ])
    let stopped =
        actorStopEvent
            (EventIdFixtures.storedId 12) nodeId ActorSucceeded
    Assert.Equal(
        Some (CmdLastResult.Detail (None, "Actor succeeded.")),
        ActorLive.lastCmdResult graph nodeId [ stopped ])
    let failed =
        actorStopEvent
            (EventIdFixtures.storedId 13) nodeId (ActorFailed "")
    Assert.Equal(
        Some (CmdLastResult.Error (None, "Actor failed.")),
        ActorLive.lastCmdResult graph nodeId [ failed ])

let private graphWithCommandMidChild
    (commandText: string)
    (midText: string)
    (childText: string)
    : Graph * NodeId * NodeId * NodeId =
    let g0 = Graph.create ()
    let g1, commandIds = ModelBuilder.createNodes [ commandText ] g0
    let commandId = commandIds.[0]
    let g2, midIds = ModelBuilder.createNodes [ midText ] g1
    let midId = midIds.[0]
    let g3, childIds = ModelBuilder.createNodes [ childText ] g2
    let childId = childIds.[0]
    let g4 =
        Graph.replace g3.root 0 [] (owned [ commandId ]) g3
        |> requireOk "graphWithCommandMidChild.root"
    let g5 =
        Graph.replace commandId 0 [] (owned [ midId ]) g4
        |> requireOk "graphWithCommandMidChild.command"
    let graph =
        Graph.replace midId 0 [] (owned [ childId ]) g5
        |> requireOk "graphWithCommandMidChild.mid"
    graph, commandId, midId, childId

[<Fact>]
let ``equals Command uses TitleCase name token not a product name`` () =
    let graph, nodeId = graphWithCommand "count=1+2"
    let started =
        actorStartAt (EventIdFixtures.storedId 19) nodeId nodeId
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "Run", "Count started.")),
        ActorLive.lastCmdResult graph nodeId [ started ])
    let stopped =
        actorStopEvent
            (EventIdFixtures.storedId 20) nodeId ActorSucceeded
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "Count", "Actor succeeded.")),
        ActorLive.lastCmdResult graph nodeId [ stopped ])

[<Fact>]
let ``equals ancestor stops scan and does not use ? above`` () =
    let graph, commandId, equalsId, childId =
        graphWithCommandMidChild "?test hello" "count=1+2" "note"
    let started =
        actorStartAt (EventIdFixtures.storedId 14) childId commandId
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "Run", "Count started.")),
        ActorLive.lastCmdResult graph commandId [ started ])
    let stopped =
        actorStopEvent
            (EventIdFixtures.storedId 15) childId ActorSucceeded
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "Count", "Actor succeeded.")),
        ActorLive.lastCmdResult graph commandId [ stopped ])
    let onEquals =
        actorStartAt (EventIdFixtures.storedId 16) equalsId commandId
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "Run", "Count started.")),
        ActorLive.lastCmdResult graph commandId [ onEquals ])

[<Fact>]
let ``equals form with no name token uses generic not ? ancestor`` () =
    let graph, commandId, _, childId =
        graphWithCommandMidChild "?ai later" "=1+2" "note"
    let started =
        actorStartAt (EventIdFixtures.storedId 17) childId commandId
    Assert.Equal(
        Some (CmdLastResult.Detail (Some "Run", "Actor started.")),
        ActorLive.lastCmdResult graph commandId [ started ])
    let stopped =
        actorStopEvent
            (EventIdFixtures.storedId 18) childId ActorSucceeded
    Assert.Equal(
        Some (CmdLastResult.Detail (None, "Actor succeeded.")),
        ActorLive.lastCmdResult graph commandId [ stopped ])
