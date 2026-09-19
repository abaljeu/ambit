module EventTests

open System
open Gambol.Shared
open Gambol.Shared
open Xunit

let private browser = Authority "Browser"

let private event commandName body : Ev =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = browser
      commandName = commandName
      body = body }

let private startRequest () : ActorStart =
    let zoom = NodeId.New()
    { zoomId = zoom
      focusId = NodeId.New()
      commandId = NodeId.New()
      graphIds = [ zoom ]
      eventId = EventIdFixtures.storedId 4 }

let private textState () : State * NodeId =
    let graph, ids =
        ModelBuilder.createNodes [ "old" ] (Graph.create ())
    { graph = graph; eventId = EventId.zero }, List.head ids

let private assertStored (id: EventId) =
    Assert.True(EventId.isAccepted id)

let private assertPast stored candidate =
    Assert.True(
        EventId.value candidate > EventId.value stored)

[<Fact>]
let ``EventId Zero is the draft id`` () =
    Assert.Equal(EventId.zero, EventId.fromJson 0)
    Assert.Equal(0, EventId.value EventId.zero)
    Assert.Equal(0, EventId.toJson EventId.zero)
    Assert.Equal("0", EventId.display EventId.zero)

[<Fact>]
let ``EventId stored Int is a positive Int`` () =
    Assert.Equal(5, EventId.value (EventId.fromJson 5))
    Assert.Equal(5, EventId.toJson (EventId.fromJson 5))
    Assert.Equal("5", EventId.display (EventId.fromJson 5))
    Assert.True(EventId.isAccepted (EventId.fromJson 5))

[<Fact>]
let ``EventId next of Zero is Zero`` () =
    Assert.Equal(EventId.zero, EventId.next EventId.zero)

[<Fact>]
let ``EventId next of a stored Int is the next positive Int`` () =
    Assert.Equal(EventId.fromJson 6, EventId.next (EventId.fromJson 5))

[<Fact>]
let ``EventId fromJson round-trips a stored Int`` () =
    Assert.Equal(
        EventId.fromJson 7,
        EventId.fromJson (EventId.toJson (EventId.fromJson 7)))

[<Fact>]
let ``empty nextId is a stored Int not next of Zero`` () =
    assertStored EventLog.empty.nextId
    Assert.NotEqual(EventId.next EventId.zero, EventLog.empty.nextId)

[<Fact>]
let ``append since tryFind`` () =
    let log1 = EventLog.append (event "" (EventBody.Change [])) EventLog.empty
    let firstId = Ev.id log1.events.Head
    assertStored firstId
    Assert.Equal(EventLog.empty.nextId, firstId)
    Assert.NotEqual(firstId, log1.nextId)
    let log2 = EventLog.append (event "" (EventBody.Change [])) log1
    let log3 = EventLog.append (event "" (EventBody.Change [])) log2
    let ids = log3.events |> List.map Ev.id
    Assert.Equal(3, ids |> List.distinct |> List.length)
    ids |> List.iter assertStored
    ids |> List.iter (fun id -> Assert.NotEqual(id, log3.nextId))
    let tail = EventLog.since EventId.zero log3
    Assert.Equal(3, tail.events.Length)
    Assert.True((ids = (tail.events |> List.map Ev.id)))
    match EventLog.tryFind firstId log3 with
    | None -> failwith "expected stamped Event"
    | Some found -> Assert.Equal(firstId, Ev.id found)

[<Fact>]
let ``restore dedupe`` () =
    let submissionId = Guid.NewGuid()
    let first =
        { id = EventId.zero
          submissionId = submissionId
          authority = browser
          commandName = "First"
          body = EventBody.Change [] }
    let duplicate =
        { first with
            id = EventLog.empty.nextId
            commandName = "Dup" }
    let assigned =
        EventLog.empty
        |> EventLog.append (event "x" (EventBody.Change []))
        |> EventLog.append (event "y" (EventBody.Change []))
    let second =
        { event "Second" (EventBody.Change []) with
            id = Ev.id assigned.events.Head }
    let log = EventLog.restore [ first; duplicate; second ] EventLog.empty
    Assert.Equal("Second", log.events.Head.commandName)
    Assert.Equal(Ev.id second, Ev.id log.events.Head)
    Assert.Equal(2, log.events.Length)
    Assert.Equal(EventId.zero, Ev.id log.events.[1])
    Assert.Equal("First", log.events.[1].commandName)
    let stored = EventLog.all log
    Assert.Equal(1, stored.events.Length)
    Assert.Equal(Ev.id second, Ev.id stored.events.Head)
    stored.events
    |> List.map Ev.id
    |> List.iter (fun id ->
        assertStored id
        assertPast id (EventLog.nextId log))

[<Fact>]
let ``restore advances nextId past persisted Event ids`` () =
    let one =
        EventLog.append (event "a" (EventBody.Change [])) EventLog.empty
    let ahead =
        one
        |> EventLog.append (event "b" (EventBody.Change []))
        |> EventLog.append (event "c" (EventBody.Change []))
    let persist =
        { event "P" (EventBody.Change []) with
            id = Ev.id ahead.events.Head }
    let log = { EventLog.empty with nextId = one.nextId }
    let restored = EventLog.restore [ persist ] log
    Assert.Equal(persist.id, Ev.id restored.events.Head)
    assertPast persist.id (EventLog.nextId restored)

[<Fact>]
let ``advancePast raises nextId past the given Event id`` () =
    let ahead =
        EventLog.empty
        |> EventLog.append (event "a" (EventBody.Change []))
        |> EventLog.append (event "b" (EventBody.Change []))
        |> EventLog.append (event "c" (EventBody.Change []))
    let stored = Ev.id ahead.events.Head
    let log = EventLog.advancePast stored EventLog.empty
    assertPast stored log.nextId
    let earlier = EventLog.empty.nextId
    let caughtUp = EventLog.advancePast earlier log
    Assert.Equal(log.nextId, caughtUp.nextId)

[<Fact>]
let ``adoptNewestHead keeps newest-head and nextId past max`` () =
    let newest =
        { event "Stop" (EventBody.ActorStop(NodeId.New(), ActorSucceeded))
            with id = EventId.fromJson 3 }
    let middle =
        { event "Edit" (EventBody.Change []) with id = EventId.fromJson 2 }
    let oldest =
        { event "Start" (EventBody.ActorStart(startRequest ()))
            with id = EventId.fromJson 1 }
    let log = EventLog.adoptNewestHead [ newest; middle; oldest ]
    Assert.Equal(EventId.fromJson 3, Ev.id log.events.Head)
    Assert.Equal("Stop", log.events.Head.commandName)
    Assert.Equal(EventId.fromJson 4, EventLog.nextId log)
    Assert.Equal(EventId.fromJson 3, EventLog.tip log)

let private recoverChange childText eventId : Ev * NodeId =
    let childId = NodeId.New()
    let ops =
        [ Op.NewNode(childId, childText)
          Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ]
    { event "" (EventBody.Change ops) with
        id = EventId.fromJson eventId },
    childId

[<Fact>]
let ``recover Log greater than Graph applies until concurrent`` () =
    let change, childId = recoverChange "recovered" 2
    let start =
        { event "" (EventBody.ActorStart(startRequest ()))
            with id = EventId.fromJson 1 }
    let stop =
        { event "" (EventBody.ActorStop(NodeId.New(), ActorSucceeded))
            with id = EventId.fromJson 3 }
    let log = EventLog.adoptNewestHead [ stop; change; start ]
    let before = { graph = Graph.create (); eventId = EventId.zero }
    let recovered, kept = EventLog.recover before log
    Assert.Equal("recovered", recovered.graph.nodes.[childId].text)
    Assert.Equal(EventId.fromJson 3, recovered.eventId)
    Assert.Equal(log.events.Length, kept.events.Length)

[<Fact>]
let ``recover empty EventLog keeps Graph checkpoint EventId`` () =
    let graph, _ =
        ModelBuilder.createNodes [ "kept" ] (Graph.create ())
    let before =
        { graph = graph; eventId = EventId.fromJson 4 }
    let recovered, dropped = EventLog.recover before EventLog.empty
    Assert.Equal(EventId.fromJson 4, recovered.eventId)
    Assert.Equal(before.graph, recovered.graph)
    Assert.Empty(dropped.events)
    Assert.Equal(EventId.fromJson 5, dropped.nextId)

[<Fact>]
let ``recover Graph greater than Log drops log and keeps Graph EventId`` () =
    let change, _ = recoverChange "lag" 2
    let log = EventLog.adoptNewestHead [ change ]
    let graph, _ =
        ModelBuilder.createNodes [ "ahead" ] (Graph.create ())
    let before =
        { graph = graph; eventId = EventId.fromJson 5 }
    let recovered, dropped = EventLog.recover before log
    Assert.Equal(EventId.fromJson 5, recovered.eventId)
    Assert.Equal(before.graph, recovered.graph)
    Assert.Empty(dropped.events)
    Assert.Equal(EventId.fromJson 6, dropped.nextId)

[<Fact>]
let ``recover equal Log and Graph is noop`` () =
    let change, childId = recoverChange "same" 2
    let log = EventLog.adoptNewestHead [ change ]
    let applied, _ =
        EventLog.recover
            { graph = Graph.create (); eventId = EventId.zero }
            log
    let recovered, kept = EventLog.recover applied log
    Assert.Equal(applied, recovered)
    Assert.Equal<Ev list>(log.events, kept.events)
    Assert.Equal("same", recovered.graph.nodes.[childId].text)
    Assert.Equal(EventId.fromJson 2, recovered.eventId)

[<Fact>]
let ``recover moves EventId only by apply`` () =
    let change, childId = recoverChange "applied" 2
    let log = EventLog.adoptNewestHead [ change ]
    let graph, _ =
        ModelBuilder.createNodes [ "ahead" ] (Graph.create ())
    let ahead =
        { graph = graph; eventId = EventId.fromJson 5 }
    let droppedState, _ = EventLog.recover ahead log
    Assert.Equal(ahead.eventId, droppedState.eventId)
    let behind = { graph = Graph.create (); eventId = EventId.zero }
    let applied, _ = EventLog.recover behind log
    Assert.Equal(EventId.fromJson 2, applied.eventId)
    Assert.Equal("applied", applied.graph.nodes.[childId].text)

let private actorStart commandName : Ev =
    event commandName (EventBody.ActorStart(startRequest ()))

let private actorStop commandName : Ev =
    event commandName (EventBody.ActorStop(NodeId.New(), ActorSucceeded))

let private changeNamed commandName eventId : Ev =
    { event commandName (EventBody.Change []) with
        id = EventIdFixtures.storedId eventId }

[<Fact>]
let ``undo skips ActorStart/ActorStop and inverts the next Action`` () =
    let changeEv = changeNamed "Edit node" 5
    let recorded =
        ClientHistory.clear ()
        |> ClientHistory.recordEvent "Edit node" changeEv
        |> ClientHistory.recordEvent "Start" (actorStart "Start")
        |> ClientHistory.recordEvent "Stop" (actorStop "Stop")
    let undoEvent, undone =
        match ClientHistory.undoEvent recorded with
        | None -> failwith "expected Undo of Edit node"
        | Some pair -> pair
    match undoEvent.body with
    | EventBody.Undo(target, _) -> Assert.Equal(Ev.id changeEv, target)
    | _ -> failwith "expected Undo body"
    Assert.Equal("Edit node", undoEvent.commandName)
    Assert.Equal(None, ClientHistory.undoEvent undone)
    Assert.Equal(Some "Edit node", ClientHistory.tryPeekRedoName undone)
    Assert.Equal(None, ClientHistory.tryPeekUndoName undone)

[<Fact>]
let ``redo skips ActorStart/ActorStop and inverts the next Action`` () =
    let changeEv = changeNamed "Edit node" 5
    let recorded =
        ClientHistory.clear ()
        |> ClientHistory.recordEvent "Edit node" changeEv
        |> ClientHistory.recordEvent "Start" (actorStart "Start")
        |> ClientHistory.recordEvent "Stop" (actorStop "Stop")
    let undoEvent, undone =
        match ClientHistory.undoEvent recorded with
        | None -> failwith "expected Undo"
        | Some pair -> pair
    let redoEvent, redone =
        match ClientHistory.redoEvent undone with
        | None -> failwith "expected Redo of the Undo Action"
        | Some pair -> pair
    match redoEvent.body with
    | EventBody.Redo(target, _) -> Assert.Equal(Ev.id undoEvent, target)
    | _ -> failwith "expected Redo body"
    Assert.Equal("Edit node", redoEvent.commandName)
    Assert.Equal(None, ClientHistory.redoEvent redone)
    Assert.Equal(Some "Edit node", ClientHistory.tryPeekUndoName redone)
    match ClientHistory.undoEvent redone with
    | None -> failwith "expected Undo after Redo still skips Actors"
    | Some (secondUndo, afterSecond) ->
        match secondUndo.body with
        | EventBody.Undo(target, _) ->
            Assert.Equal(Ev.id redoEvent, target)
        | _ -> failwith "expected Undo body"
        Assert.Equal(None, ClientHistory.undoEvent afterSecond)

[<Fact>]
let ``tryPeekUndoName and tryPeekRedoName skip Actor events`` () =
    let changeEv = changeNamed "Edit node" 5
    let recorded =
        ClientHistory.clear ()
        |> ClientHistory.recordEvent "Edit node" changeEv
        |> ClientHistory.recordEvent "Start" (actorStart "Start")
        |> ClientHistory.recordEvent "Stop" (actorStop "Stop")
    Assert.Equal(Some "Edit node", ClientHistory.tryPeekUndoName recorded)
    Assert.Equal(None, ClientHistory.tryPeekRedoName recorded)
    match ClientHistory.undoEvent recorded with
    | None -> failwith "expected Undo"
    | Some (_, undone) ->
        Assert.Equal(None, ClientHistory.tryPeekUndoName undone)
        Assert.Equal(Some "Edit node", ClientHistory.tryPeekRedoName undone)

[<Fact>]
let ``tryPeek finds Action under Actors after Change-shaped record`` () =
    let source =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [] }
    let changeOnly =
        ClientHistory.clear ()
        |> ClientHistory.record { source with commandName = "Cut" }
    Assert.Equal(Some "Cut", ClientHistory.tryPeekUndoName changeOnly)
    let actorsOnly =
        changeOnly
        |> ClientHistory.recordEvent "Start" (actorStart "Start")
        |> ClientHistory.recordEvent "Stop" (actorStop "Stop")
    Assert.Equal(Some "Cut", ClientHistory.tryPeekUndoName actorsOnly)
    let withCut =
        ClientHistory.clear ()
        |> ClientHistory.record { source with commandName = "Cut" }
    let actionUnderActors =
        withCut
        |> ClientHistory.recordEvent "Edit node" (changeNamed "Edit node" 5)
        |> ClientHistory.recordEvent "Start" (actorStart "Start")
    Assert.Equal(Some "Edit node", ClientHistory.tryPeekUndoName actionUnderActors)
    match ClientHistory.undo (Guid.NewGuid()) changeOnly with
    | None -> failwith "expected Change Undo"
    | Some (_, undoneChange) ->
        Assert.Equal(None, ClientHistory.tryPeekUndoName undoneChange)
        Assert.Equal(Some "Cut", ClientHistory.tryPeekRedoName undoneChange)

[<Fact>]
let ``ClientHistory.record fold`` () =
    let first = event "First" (EventBody.Change [])
    let second = event "Second" (EventBody.Change [])
    let recorded =
        ClientHistory.clear ()
        |> ClientHistory.recordEvent "First" first
    let afterUndo =
        match ClientHistory.undoEvent recorded with
        | None -> failwith "expected first Undo"
        | Some (_, history) -> history
    let withSecond = ClientHistory.recordEvent "Second" second afterUndo
    Assert.Equal(Some "Second", ClientHistory.tryPeekUndoName withSecond)
    Assert.Equal(None, ClientHistory.tryPeekRedoName withSecond)
    match ClientHistory.undoEvent withSecond with
    | None -> failwith "expected Second Undo"
    | Some (_, afterSecond) ->
        Assert.Equal(Some "First", ClientHistory.tryPeekUndoName afterSecond)
        match ClientHistory.undoEvent afterSecond with
        | None -> failwith "expected folded First Undo"
        | Some (_, emptyPast) ->
            Assert.Equal(None, ClientHistory.tryPeekUndoName emptyPast)

[<Fact>]
let ``Undo inverse Ops`` () =
    let nodeId = NodeId.New()
    let ops = [ Op.SetText(nodeId, "old", "new") ]
    let changeEvent =
        { event "Edit node" (EventBody.Change ops) with
            id = EventIdFixtures.storedId 5 }
    let recorded =
        ClientHistory.clear ()
        |> ClientHistory.recordEvent "Edit node" changeEvent
    let undoEvent, undone =
        match ClientHistory.undoEvent recorded with
        | None -> failwith "expected Undo"
        | Some pair -> pair
    match undoEvent.body with
    | EventBody.Undo(target, inverseOps) ->
        Assert.Equal(changeEvent.id, target)
        Assert.Equal<Op list>([ Op.SetText(nodeId, "new", "old") ], inverseOps)
    | _ -> failwith "expected Undo body"
    let redoEvent, _ =
        match ClientHistory.redoEvent undone with
        | None -> failwith "expected Redo"
        | Some pair -> pair
    match redoEvent.body with
    | EventBody.Redo(target, _) -> Assert.Equal(Ev.id undoEvent, target)
    | _ -> failwith "expected Redo body"

[<Fact>]
let ``Actor bodies do not apply`` () =
    let state = { graph = Graph.create (); eventId = EventId.zero }
    let start = event "" (EventBody.ActorStart(startRequest ()))
    let stop =
        event "" (EventBody.ActorStop(NodeId.New(), ActorSucceeded))
    match Ev.apply start state with
    | ApplyResult.Unchanged after -> Assert.Equal(state.graph, after.graph)
    | _ -> failwith "ActorStart must not change the Graph"
    match Ev.apply stop state with
    | ApplyResult.Unchanged after -> Assert.Equal(state.graph, after.graph)
    | _ -> failwith "ActorStop must not change the Graph"

[<Fact>]
let ``Undo Redo carried Ops`` () =
    let state, nodeId = textState ()
    let undoOps = [ Op.SetText(nodeId, "old", "undone") ]
    let redoOps = [ Op.SetText(nodeId, "undone", "redone") ]
    let undoEv =
        event "" (EventBody.Undo(EventIdFixtures.storedId 99, undoOps))
    let redoEv =
        event "" (EventBody.Redo(EventIdFixtures.storedId 99, redoOps))
    match Ev.apply undoEv state with
    | ApplyResult.Changed afterUndo ->
        Assert.Equal("undone", afterUndo.graph.nodes.[nodeId].text)
        match Ev.apply redoEv afterUndo with
        | ApplyResult.Changed afterRedo ->
            Assert.Equal("redone", afterRedo.graph.nodes.[nodeId].text)
        | other -> failwithf "expected Redo apply Changed, got %A" other
    | other -> failwithf "expected Undo apply Changed, got %A" other

[<Fact>]
let ``Every Ev carries Authority`` () =
    let start = startRequest ()
    let bodies =
        [ EventBody.Change []
          EventBody.Undo(EventIdFixtures.storedId 1, [])
          EventBody.Redo(EventIdFixtures.storedId 1, [])
          EventBody.ActorStart start
          EventBody.ActorStop(start.focusId, ActorFailed) ]
    bodies
    |> List.iter (fun body ->
        let ev = event "" body
        Assert.Equal(browser, Ev.authority ev))

[<Fact>]
let ``ActorStart body equals start request`` () =
    let request = startRequest ()
    let ev = event "" (EventBody.ActorStart request)
    match ev.body with
    | EventBody.ActorStart body -> Assert.Equal(request, body)
    | _ -> failwith "expected ActorStart"

[<Fact>]
let ``ActorStop carries ActorResult`` () =
    let focusId = NodeId.New()
    let ev = event "" (EventBody.ActorStop(focusId, ActorSucceeded))
    match ev.body with
    | EventBody.ActorStop(id, result) ->
        Assert.Equal(focusId, id)
        Assert.Equal(ActorSucceeded, result)
    | _ -> failwith "expected ActorStop"

[<Fact>]
let ``ActorStop carries ActorCancelled`` () =
    let focusId = NodeId.New()
    let ev = event "" (EventBody.ActorStop(focusId, ActorCancelled))
    match ev.body with
    | EventBody.ActorStop(id, result) ->
        Assert.Equal(focusId, id)
        Assert.Equal(ActorCancelled, result)
    | _ -> failwith "expected ActorStop"

[<Fact>]
let ``commandName is on the Ev`` () =
    let ev = event "Cut" (EventBody.Change [])
    Assert.Equal("Cut", ev.commandName)
    let recorded =
        ClientHistory.clear () |> ClientHistory.recordEvent "Cut" ev
    match ClientHistory.undoEvent recorded with
    | None -> failwith "expected Undo"
    | Some (undoEvent, _) -> Assert.Equal("Cut", undoEvent.commandName)
