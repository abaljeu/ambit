module BootCacheTests

open System
open Gambol.Shared
open Gambol.Shared
open BootCacheTestHelpers
open Xunit

module Enc = Thoth.Json.Newtonsoft.Encode
module Dec = Thoth.Json.Newtonsoft.Decode

let private zoomId =
    NodeId(Guid.Parse "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")

let private emptyGraphJson =
    "{\"root\":\"00000000-0000-0000-0000-000000000000\",\"nodes\":[]}"

let private sampleStateJson =
    "{\"eventId\":4,\"graph\":" + emptyGraphJson + ",\"ready\":true}"

let private sampleSnapshot : BootCache.SnapshotRecord =
    BootCache.snapshotRecord
        "ambit"
        "root"
        sampleStateJson
        (EventIdFixtures.storedId 4)
        true
        "2026-08-27T14:00:00Z"
        ""

[<Fact>]
let ``database and store names match the cache-first design`` () =
    Assert.Equal("gambol-boot-cache-v1", BootCache.databaseName)
    Assert.Equal("snapshots", BootCache.snapshotStore)
    Assert.Equal("changes", BootCache.changeStore)
    Assert.Equal(1, BootCache.codecVersion)

[<Fact>]
let ``scopeKey is root when there is no Zoom widen`` () =
    Assert.Equal("root", BootCache.scopeKey None)

[<Fact>]
let ``scopeKey includes the Zoom guid when widen is present`` () =
    Assert.Equal(
        "root|zoom:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
        BootCache.scopeKey (Some zoomId))

[<Fact>]
let ``snapshotRecord stores /state body and metadata without Graph parse`` () =
    Assert.Equal(1, sampleSnapshot.codecVersion)
    Assert.Equal("ambit", sampleSnapshot.file)
    Assert.Equal("root", sampleSnapshot.scopeKey)
    Assert.Equal(EventIdFixtures.storedId 4, sampleSnapshot.eventId)
    Assert.True sampleSnapshot.isReady
    Assert.Contains("\"eventId\":4", sampleSnapshot.stateJson)
    Assert.Equal("2026-08-27T14:00:00Z", sampleSnapshot.writtenAt)

[<Fact>]
let ``snapshot envelope round-trips through JSON`` () =
    let json = Enc.toString 0 (BootCache.encodeSnapshot sampleSnapshot)
    match Dec.fromString BootCache.decodeSnapshot json with
    | Error err -> failwith err
    | Ok decoded -> Assert.Equal(sampleSnapshot, decoded)

[<Fact>]
let ``validateSnapshot accepts matching file scope and codec`` () =
    match BootCache.validateSnapshot "ambit" "root" sampleSnapshot with
    | Error err -> failwith err
    | Ok () -> ()

[<Fact>]
let ``validateSnapshot rejects codec file and scope mismatches`` () =
    let codec =
        BootCache.validateSnapshot "ambit" "root" { sampleSnapshot with codecVersion = 0 }
    let file =
        BootCache.validateSnapshot "other" "root" sampleSnapshot
    let scope =
        BootCache.validateSnapshot "ambit" "root|zoom:nope" sampleSnapshot
    Assert.Equal(Error "codec", codec)
    Assert.Equal(Error "file", file)
    Assert.Equal(Error "scope", scope)

[<Fact>]
let ``event log item round-trips through Ev JSON`` () =
    let event = mkEvent 4
    let json = Enc.toString 0 (BootCache.encodeEvent event)
    match Dec.fromString BootCache.decodeEvent json with
    | Error err -> failwith err
    | Ok decoded -> Assert.Equal(event, decoded)

[<Fact>]
let ``eventsAfter keeps ids greater than snapshot Revision and sorts`` () =
    let e2 = mkEvent 2
    let e4 = mkEvent 4
    let e5 = mkEvent 5
    let kept = BootCache.eventsAfter (EventIdFixtures.storedId 3) [ e5; e2; e4 ]
    Assert.Equal<EventId list>(
        [ e4.id; e5.id ],
        kept |> List.map (fun event -> event.id))

[<Fact>]
let ``eventsAfter drops the snapshot Revision itself`` () =
    Assert.Empty(BootCache.eventsAfter (EventIdFixtures.storedId 3) [ mkEvent 3 ])

[<Fact>]
let ``acceptedForLog prefers confirmed Events when the server assigned ids`` () =
    let confirmed = [ mkEvent 9 ]
    let submitted = [ (mkEvent 8) ]
    let accepted = BootCache.acceptedForLog confirmed submitted
    Assert.Equal(confirmed.Head.id, accepted.Head.id)

[<Fact>]
let ``acceptedForLog uses submitted Events when confirmed is empty`` () =
    let submitted = [ (mkEvent 8) ]
    let accepted = BootCache.acceptedForLog [] submitted
    Assert.Equal(submitted.Head.id, accepted.Head.id)

let private noteSnapshot () =
    let graph, noteId = Graph.newNode "hello" (Graph.create ())
    { graph = graph
      eventId = EventIdFixtures.storedId 5
      isReady = true
      seedLiveFocusIds = Set.empty },
    noteId

let private decodeState text =
    Dec.fromString ApiResponseSerialization.decodeStateResponseDecoder text

let private stateJson (response: StateResponse) =
    Enc.toString 0 (ApiResponseSerialization.encodeStateResponse response)

let private recordFor (response: StateResponse) =
    BootCache.snapshotRecord
        "ambit"
        "root"
        (stateJson response)
        response.eventId
        response.isReady
        "2026-08-27T14:00:00Z"
        ""

[<Fact>]
let ``decideBootRead fetches /state when the feature flag is off`` () =
    let response, _ = noteSnapshot ()
    match
        BootCache.decideBootRead
            false
            "ambit"
            "root"
            (Some (recordFor response))
            []
            decodeState
    with
    | BootCache.BootRead.FetchState "disabled" -> ()
    | other -> failwithf "%A" other

[<Fact>]
let ``decideBootRead fetches /state on miss and metadata mismatch`` () =
    let response, _ = noteSnapshot ()
    let snap = recordFor response
    let decode = decodeState
    match BootCache.decideBootRead true "ambit" "root" None [] decode with
    | BootCache.BootRead.FetchState "miss" -> ()
    | other -> failwithf "%A" other
    match
        BootCache.decideBootRead true "other" "root" (Some snap) [] decode
    with
    | BootCache.BootRead.FetchState "file" -> ()
    | other -> failwithf "%A" other

let private wait decode elapsed returned record =
    BootCache.decideBootReadWait
        elapsed returned true "ambit" "root" record [] decode

[<Fact>]
let ``decideBootReadWait keeps waiting before the cache-read timeout`` () =
    match wait decodeState 0 false None with
    | BootCache.BootReadWait.KeepWaiting -> ()
    | other -> failwithf "%A" other
    match
        wait decodeState (BootCache.cacheReadTimeoutMs - 1) false None
    with
    | BootCache.BootReadWait.KeepWaiting -> ()
    | other -> failwithf "%A" other

[<Fact>]
let ``decideBootReadWait fetches /state when IndexedDB never returns`` () =
    match
        wait decodeState BootCache.cacheReadTimeoutMs false None
    with
    | BootCache.BootReadWait.Done (BootCache.BootRead.FetchState "timeout") ->
        ()
    | other -> failwithf "%A" other

[<Fact>]
let ``decideBootReadWait fetches /state as soon as IndexedDB reports a miss`` () =
    match wait decodeState 0 true None with
    | BootCache.BootReadWait.Done (BootCache.BootRead.FetchState "miss") ->
        ()
    | other -> failwithf "%A" other

[<Fact>]
let ``decideBootRead fetches /state on decode error`` () =
    let snap =
        BootCache.snapshotRecord
            "ambit" "root" "not-json" (EventIdFixtures.storedId 5) true "t" ""
    match
        BootCache.decideBootRead
            true
            "ambit"
            "root"
            (Some snap)
            []
            decodeState
    with
    | BootCache.BootRead.FetchState "decode" -> ()
    | other -> failwithf "%A" other

[<Fact>]
let ``foldLog applies SetText and sets Revision to the last Change id`` () =
    let snapshot, noteId = noteSnapshot ()
    let event =
        { id = EventIdFixtures.storedId 6
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(noteId, "hello", "world") ] }
    match BootCache.foldLog snapshot [ event ] with
    | Error err -> failwith err
    | Ok folded ->
        Assert.Equal("world", folded.graph.nodes.[noteId].text)
        Assert.Equal(event.id, folded.eventId)

[<Fact>]
let ``decideBootRead fetches /state on fold error`` () =
    let fileId = NodeId.New()
    let fileNode =
        Node.Create(
            fileId,
            text = "file.txt",
            name = Filename.create "file.txt",
            owner = Graph.rootId,
            kind = Special File,
            documentState = Unparsed)
    let graph =
        Graph.create ()
        |> fun g -> Graph.fromNodes g.root (Map.add fileId fileNode g.nodes)
    let snapshot =
        { graph = graph
          eventId = EventIdFixtures.storedId 1
          isReady = true
          seedLiveFocusIds = Set.empty }
    let event =
        { id = EventIdFixtures.storedId 2
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(fileId, "file.txt", "changed") ] }
    let snap = recordFor snapshot
    match
        BootCache.decideBootRead
            true
            "ambit"
            "root"
            (Some snap)
            [ event ]
            decodeState
    with
    | BootCache.BootRead.FetchState "fold" -> ()
    | other -> failwithf "%A" other

[<Fact>]
let ``decideBootRead uses the folded snapshot when the cache is valid`` () =
    let snapshot, noteId = noteSnapshot ()
    let event =
        { id = EventIdFixtures.storedId 6
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(noteId, "hello", "world") ] }
    match
        BootCache.decideBootRead
            true
            "ambit"
            "root"
            (Some (recordFor snapshot))
            [ event ]
            decodeState
    with
    | BootCache.BootRead.UseCache folded ->
        Assert.Equal("world", folded.graph.nodes.[noteId].text)
        Assert.Equal(event.id, folded.eventId)
    | other -> failwithf "%A" other

[<Fact>]
let ``foldLog applies deletion and advances Revision`` () =
    let graph0 = Graph.create ()
    let graph1, ids = ModelBuilder.createNodes [ "doomed" ] graph0
    let noteId = ids.[0]
    let graph2 =
        Graph.replace graph1.root 0 [] [ ChildNode.owner noteId ] graph1
        |> ModelBuilder.requireOk "root->doomed"
    let snapshot =
        { graph = graph2
          eventId = EventIdFixtures.storedId 5
          isReady = true
          seedLiveFocusIds = Set.empty }
    Assert.True(Map.containsKey noteId snapshot.graph.nodes)
    let removeOp = Op.Replace(graph2.root, [ ChildNode.owner noteId ], [])
    let trashChildren = graph2.nodes.[Graph.trashId].children
    let addToTrashOp =
        Op.Replace(Graph.trashId, trashChildren, trashChildren @ [ ChildNode.owner noteId ])
    let event =
        { id = EventIdFixtures.storedId 6
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ removeOp; addToTrashOp ] }
    match BootCache.foldLog snapshot [ event ] with
    | Error err -> failwith err
    | Ok folded ->
        Assert.Equal(event.id, folded.eventId)
        let underRoot =
            folded.graph.nodes.[folded.graph.root].children
            |> List.exists (fun c -> c.id = noteId)
        Assert.False(underRoot, "deleted node must leave its former parent")
        let underTrash =
            folded.graph.nodes.[Graph.trashId].children
            |> List.exists (fun c -> c.id = noteId)
        Assert.True(underTrash, "MoveToTrash must place the node under TRASH")
