module BrowserNodeEventTests

open Gambol.Shared
open Xunit

let private fileNode id text legacy persist =
    Node.Create(
        id,
        text = text,
        name = Filename.create "note.txt",
        kind = Special File,
        documentState = legacy,
        persistState = persist)

let private withFile node =
    Graph.addDetachedNode node (Graph.create ())

let private client graph =
    ClientSyncState.create graph EventId.zero (ClientHistory.clear ())

let private clientWith graph history =
    ClientSyncState.create graph EventId.zero history

let private noticeEvent id notice : Ev =
    { id = id
      submissionId = System.Guid.NewGuid()
      authority = Authority ""
      commandName = ""
      body = EventBody.Notice notice }

let private applyOne (state: ClientSyncState) id notice =
    match SyncLogic.applyServerTail [ noticeEvent id notice ] state with
    | Error msg -> failwith msg
    | Ok next -> next

let private applyAll (state: ClientSyncState) events =
    match SyncLogic.applyServerTail events state with
    | Error msg -> failwith msg
    | Ok next -> next

let private live (state: ClientSyncState) id =
    state.graph.nodes.[id]

[<Fact>]
let ``client applies Node events for parse state and persist state`` () =
    let fileId = NodeId.New()
    let pullingId = NodeId.New()
    let absentId = NodeId.New()
    let file =
        fileNode fileId "note" Current PersistState.Persisted
    let pulling =
        fileNode pullingId "stub" PullingFile PersistState.Unpersisted
    let graph =
        withFile file
        |> Graph.addDetachedNode pulling
    let state0 = client graph
    let failed =
        noticeEvent
            (EventId.fromJson 1)
            (Notice.PersistFailed(fileId, "disk full"))
    let parsing =
        noticeEvent
            (EventId.fromJson 2)
            (Notice.NodeParsing fileId)
    let receivedBoth = applyAll state0 [ failed; parsing ]
    let afterParse = live receivedBoth fileId
    Assert.Equal(ParseState.Parsed, afterParse.documentState.parseState)
    Assert.Equal(PersistState.Persisted, afterParse.documentState.persistState)
    Assert.False(afterParse.documentState.pullingFile)
    Assert.Equal(
        Some "disk full",
        SyncLogic.persistFailedMessage [ failed; parsing ])
    let beforePersisting = live receivedBoth fileId
    let persisting =
        applyOne receivedBoth (EventId.fromJson 3) (Notice.NodePersisting fileId)
    let afterPersisting = live persisting fileId
    Assert.Equal(ParseState.Parsed, afterPersisting.documentState.parseState)
    Assert.Equal(
        PersistState.Persisted,
        afterPersisting.documentState.persistState)
    Assert.False(afterPersisting.documentState.pullingFile)
    Assert.Equal(beforePersisting.updateTime, afterPersisting.updateTime)
    Assert.Equal(
        beforePersisting.documentState,
        afterPersisting.documentState)
    let beforeUpload = live persisting fileId
    let uploading =
        applyOne persisting (EventId.fromJson 4) (Notice.NodeUploading fileId)
    let absent =
        applyOne uploading (EventId.fromJson 5) (Notice.NodeUploading absentId)
    Assert.False(Map.containsKey absentId absent.graph.nodes)
    let uploaded = live absent fileId
    Assert.True(uploaded.documentState.pullingFile)
    Assert.Equal(PullingFile, Node.documentStateEnum uploaded)
    Assert.Equal(
        PersistState.Unpersisted,
        uploaded.documentState.persistState)
    Assert.Equal(beforeUpload.updateTime, uploaded.updateTime)
    let received =
        applyOne absent (EventId.fromJson 6) (Notice.NodeReceived fileId)
    let arrived = live received fileId
    Assert.Equal(ParseState.Unparsed, arrived.documentState.parseState)
    Assert.False(arrived.documentState.pullingFile)
    Assert.Equal(Unparsed, Node.documentStateEnum arrived)
    Assert.Equal(PersistState.Persisted, arrived.documentState.persistState)
    let parsed =
        applyOne received (EventId.fromJson 7) (Notice.NodeParsed fileId)
    let doneParse = live parsed fileId
    Assert.Equal(ParseState.Parsed, doneParse.documentState.parseState)
    Assert.Equal(PersistState.Persisted, doneParse.documentState.persistState)
    let stayed =
        applyOne parsed (EventId.fromJson 8) (Notice.NodeParsed pullingId)
    let stillPulling = live stayed pullingId
    Assert.True(stillPulling.documentState.pullingFile)
    Assert.Equal(PullingFile, Node.documentStateEnum stillPulling)
    Assert.Equal(
        PersistState.Unpersisted,
        stillPulling.documentState.persistState)
    let unparsed =
        applyOne stayed (EventId.fromJson 9) (Notice.NodeUnparsed fileId)
    let marked = live unparsed fileId
    Assert.Equal(ParseState.Unparsed, marked.documentState.parseState)
    Assert.Equal(PersistState.Persisted, marked.documentState.persistState)
    Assert.False(marked.documentState.pullingFile)
    let reparsed =
        applyOne unparsed (EventId.fromJson 10) (Notice.NodeParsed fileId)
    let unpersisted =
        applyOne
            reparsed
            (EventId.fromJson 11)
            (Notice.NodeUnpersisted fileId)
    let dirty = live unpersisted fileId
    Assert.Equal(ParseState.Parsed, dirty.documentState.parseState)
    Assert.Equal(PersistState.Unpersisted, dirty.documentState.persistState)
    let stamped =
        applyOne
            unpersisted
            (EventId.fromJson 12)
            (Notice.NodePersisted(
                fileId,
                EventId.fromJson 1,
                System.DateTime(2026, 10, 27, 0, 0, 0, System.DateTimeKind.Utc)))
    let persisted = live stamped fileId
    Assert.Equal(ParseState.Parsed, persisted.documentState.parseState)
    Assert.Equal(PersistState.Persisted, persisted.documentState.persistState)

let private persistedFile text =
    let id = NodeId.New()
    let node = fileNode id text Current PersistState.Persisted
    id, withFile node

let private edit textFrom textTo id =
    SpecialNodeTestHelpers.changeEvent
        "Edit"
        EventId.zero
        (System.Guid.NewGuid())
        [ Op.SetText(id, textFrom, textTo) ]

[<Fact>]
let ``browser replay marks changed node Unpersisted`` () =
    let id, graph = persistedFile "hello"
    let state = client graph
    let baseline : CatchUpBaseline = { eventId = EventId.zero; graph = graph }
    let replayed =
        { edit "hello" "edited" id with id = EventId.fromJson 2 }
    match
        SyncLogic.consumeCatchUpPoll
            baseline
            [ replayed ]
            (EventId.fromJson 2)
            state
    with
    | Error msg -> failwith msg
    | Ok next ->
        let node = live next id
        Assert.Equal("edited", node.text)
        Assert.Equal(PersistState.Unpersisted, node.documentState.persistState)

[<Fact>]
let ``browser undo marks changed node Unpersisted`` () =
    let id, graph = persistedFile "edited"
    let history =
        ClientHistory.clear ()
        |> ClientHistory.record (edit "hello" "edited" id)
    match SyncLogic.applyLocalUndo (System.Guid.NewGuid()) (clientWith graph history) with
    | Some (Ok (next, _)) ->
        let node = live next id
        Assert.Equal("hello", node.text)
        Assert.Equal(PersistState.Unpersisted, node.documentState.persistState)
    | other -> failwith $"expected Undo, got {other}"

[<Fact>]
let ``browser redo marks changed node Unpersisted`` () =
    let id, graph = persistedFile "edited"
    let history =
        ClientHistory.clear ()
        |> ClientHistory.record (edit "hello" "edited" id)
    match SyncLogic.applyLocalUndo (System.Guid.NewGuid()) (clientWith graph history) with
    | Some (Ok (undone, _)) ->
        let current = live undone id
        let clean =
            Node.withPersistState PersistState.Persisted current
        let graph =
            { undone.graph with
                nodes = Map.add id clean undone.graph.nodes }
        let ready = { undone with graph = graph }
        match SyncLogic.applyLocalRedo (System.Guid.NewGuid()) ready with
        | Some (Ok (next, _)) ->
            let node = live next id
            Assert.Equal("edited", node.text)
            Assert.Equal(
                PersistState.Unpersisted,
                node.documentState.persistState)
        | other -> failwith $"expected Redo, got {other}"
    | other -> failwith $"expected Undo, got {other}"
