module EventTests

open System
open Gambol.Shared
open Gambol.Shared.Events
open Xunit

let private browser = Authority "Browser"

let private event commandName body : Event =
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
      revision = EventId 4 }

let private textState () : State * NodeId =
    let graph, ids =
        ModelBuilder.createNodes [ "old" ] (Graph.create ())
    { graph = graph; revision = Revision.Zero }, List.head ids

[<Fact>]
let ``append since tryFind`` () =
    let log1 = EventLog.append (event "" (EventBody.Change [])) EventLog.empty
    let log2 = EventLog.append (event "" (EventBody.Change [])) log1
    let log3 = EventLog.append (event "" (EventBody.Change [])) log2
    Assert.Equal(EventId 2, Event.id log3.events.Head)
    let tail = EventLog.since EventId.zero log3
    Assert.Equal(2, tail.events.Length)
    Assert.Equal(EventId 2, Event.id tail.events.Head)
    Assert.Equal(EventId 1, Event.id tail.events.[1])
    match EventLog.tryFind (EventId 1) log3 with
    | None -> failwith "expected EventId 1"
    | Some found -> Assert.Equal(EventId 1, Event.id found)

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
            id = EventId 1
            commandName = "Dup" }
    let second =
        { event "Second" (EventBody.Change []) with id = EventId 2 }
    let log = EventLog.restore [ first; duplicate; second ] EventLog.empty
    Assert.Equal(EventId.zero, EventLog.nextId log)
    Assert.Equal(EventId 2, Event.id log.events.Head)
    Assert.Equal("Second", log.events.Head.commandName)
    let restored = EventLog.since (EventId -1) log
    Assert.Equal(2, restored.events.Length)
    Assert.Equal(EventId 2, Event.id restored.events.Head)
    Assert.Equal("Second", restored.events.Head.commandName)
    Assert.Equal(EventId.zero, Event.id restored.events.[1])
    Assert.Equal("First", restored.events.[1].commandName)

[<Fact>]
let ``restore keeps source nextId`` () =
    let persist = { event "P" (EventBody.Change []) with id = EventId 9 }
    let log = { EventLog.empty with nextId = EventId 3 }
    let restored = EventLog.restore [ persist ] log
    Assert.Equal(EventId 3, EventLog.nextId restored)
    Assert.Equal(EventId 9, Event.id restored.events.Head)

let private actorStart commandName : Event =
    event commandName (EventBody.ActorStart(startRequest ()))

let private actorStop commandName : Event =
    event commandName (EventBody.ActorStop(NodeId.New(), ActorSucceeded))

let private changeNamed commandName eventId : Event =
    { event commandName (EventBody.Change []) with id = EventId eventId }

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
    | EventBody.Undo(target, _) -> Assert.Equal(EventId 5, target)
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
    | EventBody.Redo(target, _) -> Assert.Equal(Event.id undoEvent, target)
    | _ -> failwith "expected Redo body"
    Assert.Equal("Edit node", redoEvent.commandName)
    Assert.Equal(None, ClientHistory.redoEvent redone)
    Assert.Equal(Some "Edit node", ClientHistory.tryPeekUndoName redone)
    match ClientHistory.undoEvent redone with
    | None -> failwith "expected Undo after Redo still skips Actors"
    | Some (secondUndo, afterSecond) ->
        match secondUndo.body with
        | EventBody.Undo(target, _) ->
            Assert.Equal(Event.id redoEvent, target)
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
        { id = 0
          changeId = Guid.NewGuid()
          ops = [] }
    let changeOnly, _ =
        ClientHistory.clear () |> ClientHistory.record "Cut" source
    Assert.Equal(Some "Cut", ClientHistory.tryPeekUndoName changeOnly)
    let actorsOnly =
        changeOnly
        |> ClientHistory.recordEvent "Start" (actorStart "Start")
        |> ClientHistory.recordEvent "Stop" (actorStop "Stop")
    Assert.Equal(Some "Cut", ClientHistory.tryPeekUndoName actorsOnly)
    let withCut, _ =
        ClientHistory.clear () |> ClientHistory.record "Cut" source
    let actionUnderActors =
        withCut
        |> ClientHistory.recordEvent "Edit node" (changeNamed "Edit node" 5)
        |> ClientHistory.recordEvent "Start" (actorStart "Start")
    Assert.Equal(Some "Edit node", ClientHistory.tryPeekUndoName actionUnderActors)
    match ClientHistory.undo (Revision 1) (Guid.NewGuid()) changeOnly with
    | None -> failwith "expected Change Undo"
    | Some (_, _, undoneChange, _) ->
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
        { event "Edit node" (EventBody.Change ops) with id = EventId 5 }
    let recorded =
        ClientHistory.clear ()
        |> ClientHistory.recordEvent "Edit node" changeEvent
    let undoEvent, undone =
        match ClientHistory.undoEvent recorded with
        | None -> failwith "expected Undo"
        | Some pair -> pair
    match undoEvent.body with
    | EventBody.Undo(target, inverseOps) ->
        Assert.Equal(EventId 5, target)
        Assert.Equal<Op list>([ Op.SetText(nodeId, "new", "old") ], inverseOps)
    | _ -> failwith "expected Undo body"
    let redoEvent, _ =
        match ClientHistory.redoEvent undone with
        | None -> failwith "expected Redo"
        | Some pair -> pair
    match redoEvent.body with
    | EventBody.Redo(target, _) -> Assert.Equal(Event.id undoEvent, target)
    | _ -> failwith "expected Redo body"

[<Fact>]
let ``Actor bodies do not apply`` () =
    let state = { graph = Graph.create (); revision = Revision.Zero }
    let start = event "" (EventBody.ActorStart(startRequest ()))
    let stop =
        event "" (EventBody.ActorStop(NodeId.New(), ActorSucceeded))
    match Event.apply start state with
    | ApplyResult.Unchanged after -> Assert.Equal(state.graph, after.graph)
    | _ -> failwith "ActorStart must not change the Graph"
    match Event.apply stop state with
    | ApplyResult.Unchanged after -> Assert.Equal(state.graph, after.graph)
    | _ -> failwith "ActorStop must not change the Graph"

[<Fact>]
let ``Undo Redo carried Ops`` () =
    let state, nodeId = textState ()
    let undoOps = [ Op.SetText(nodeId, "old", "undone") ]
    let redoOps = [ Op.SetText(nodeId, "undone", "redone") ]
    let undoEv = event "" (EventBody.Undo(EventId 99, undoOps))
    let redoEv = event "" (EventBody.Redo(EventId 99, redoOps))
    match Event.apply undoEv state with
    | ApplyResult.Changed afterUndo ->
        Assert.Equal("undone", afterUndo.graph.nodes.[nodeId].text)
        match Event.apply redoEv afterUndo with
        | ApplyResult.Changed afterRedo ->
            Assert.Equal("redone", afterRedo.graph.nodes.[nodeId].text)
        | other -> failwithf "expected Redo apply Changed, got %A" other
    | other -> failwithf "expected Undo apply Changed, got %A" other

[<Fact>]
let ``Every Event carries Authority`` () =
    let start = startRequest ()
    let bodies =
        [ EventBody.Change []
          EventBody.Undo(EventId 1, [])
          EventBody.Redo(EventId 1, [])
          EventBody.ActorStart start
          EventBody.ActorStop(start.focusId, ActorFailed) ]
    bodies
    |> List.iter (fun body ->
        let ev = event "" body
        Assert.Equal(browser, Event.authority ev))

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
let ``commandName is on the Event`` () =
    let ev = event "Cut" (EventBody.Change [])
    Assert.Equal("Cut", ev.commandName)
    let recorded =
        ClientHistory.clear () |> ClientHistory.recordEvent "Cut" ev
    match ClientHistory.undoEvent recorded with
    | None -> failwith "expected Undo"
    | Some (undoEvent, _) -> Assert.Equal("Cut", undoEvent.commandName)
