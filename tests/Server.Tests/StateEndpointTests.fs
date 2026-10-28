module Gambol.Server.Tests.StateEndpointTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend
open SpecialNodeTestHelpers

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private decode decoder json =
    match Decode.fromString decoder json with
    | Ok v -> v
    | Error err -> failwith $"Decode failed: {err}"

let private decodeEventId json =
    Thoth.Json.Core.Decode.object (fun get ->
        get.Required.Field "eventId" EventJson.decodeEventId)
    |> decode <| json

let private decodeSuccess json =
    match
        Decode.fromString
            ApiResponseSerialization.decodeChangeSuccessResponseDecoder
            json
    with
    | Ok response -> response
    | Error err -> failwith $"Decode success response failed: {err}"

let private decodeAckChangeIds json =
    decodeSuccess json
    |> fun response ->
        changesOnly response.events
        |> List.map (fun event -> event.submissionId)

let private decodeSuccessRevision json =
    (decodeSuccess json).eventId

let private decodeSuccessExternalChanges json =
    (decodeSuccess json).externalChanges

let private assertAmbConflictFirstChild
    (graph: Graph)
    (parentId: NodeId)
    (expectedText: string)
    =
    let kids = Graph.children graph parentId
    Assert.NotEmpty(kids)
    let first = kids.[0]
    Assert.Equal(Ownership.Owner, Node.childOwnership graph parentId first)
    let child = graph.nodes.[first.id]
    Assert.Equal(expectedText, child.text)
    Assert.True(CssClass.contains "amb-conflict" child.cssClasses)

let private decodeErrorField json =
    Thoth.Json.Core.Decode.object (fun get ->
        get.Required.Field "error" Thoth.Json.Core.Decode.string)
    |> decode <| json

let private decodeGraph json =
    Thoth.Json.Core.Decode.object (fun get ->
        get.Required.Field "graph" Serialization.decodeGraph)
    |> decode <| json

let private decodePoll json =
    decode
        ApiResponseSerialization.decodeChangeSuccessResponseDecoder
        json

/// GET /ambit/state, assert 200 + JSON content type, return body string.
let private getStateJson (client: HttpClient) = task {
    let! resp = client.GetAsync("/ambit/state")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    Assert.Equal("application/json", resp.Content.Headers.ContentType.MediaType)
    return! resp.Content.ReadAsStringAsync()
}

/// Legacy total-load escape hatch for tests that need the canonical graph.
let private getStateJsonFull (client: HttpClient) = task {
    let! resp = client.GetAsync("/ambit/state?scope=full")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    Assert.Equal("application/json", resp.Content.Headers.ContentType.MediaType)
    return! resp.Content.ReadAsStringAsync()
}

let private readState (client: HttpClient) = task {
    let! json = getStateJson client
    return decodeEventId json, decodeGraph json
}

let private readFullState (client: HttpClient) = task {
    let! json = getStateJsonFull client
    return decodeEventId json, decodeGraph json
}

let private stateRoot (client: HttpClient) = task {
    let! _, graph = readState client
    return graph.root
}

let private encodeChangeRequestBody (events: Ev list) =
    Encode.toString 0 (
        ApiResponseSerialization.encodeChangeRequest
            { events = events; want = [] })

let private postPoll (client: HttpClient) eventId = task {
    let body =
        Encode.toString 0 (
            ApiResponseSerialization.encodePollRequest
                { eventId = eventId; want = [] })
    use content = new StringContent(body, Encoding.UTF8, "application/json")
    return! client.PostAsync("/ambit/poll", content)
}

let private countPersistProgress (events: Ev list) =
    events
    |> List.fold
        (fun (changes, persisted) event ->
            match event.body with
            | EventBody.Change _ -> changes + 1, persisted
            | EventBody.Notice (Notice.NodePersisted _) ->
                changes, persisted + 1
            | _ -> changes, persisted)
        (0, 0)

let private readPoll (client: HttpClient) = task {
    let! response = postPoll client EventId.zero
    Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    let! json = response.Content.ReadAsStringAsync()
    return decodePoll json
}

/// Notices match, the tip is still, and persisted covers every Change
/// unless this batch shares one write.
let private persistTipSettled
    requireEach
    (first: ChangeSuccessResponse)
    (second: ChangeSuccessResponse)
    =
    let changes, persisted = countPersistProgress second.events
    let enough =
        if requireEach then persisted >= changes
        else changes = 0 || persisted >= 1
    enough && second.eventId = first.eventId

let private waitForPersist requireEach (client: HttpClient) = task {
    let deadline = DateTime.UtcNow.AddSeconds 20.0
    let rec poll () = task {
        let! first = readPoll client
        do! Task.Delay 40
        let! second = readPoll client
        if persistTipSettled requireEach first second then
            return second.eventId
        elif DateTime.UtcNow >= deadline then
            let changes, persisted = countPersistProgress second.events
            let message =
                $"persist did not settle: changes {changes}, "
                + $"persisted {persisted}"
            return failwith message
        else
            do! Task.Delay 20
            return! poll ()
    }
    return! poll ()
}

/// One persisted notice per Change already on this client's log.
let private waitUntilPersistSettled client =
    waitForPersist true client

/// A batch may share one write. Wait until that write is logged.
let private waitUntilPersistNoted client =
    waitForPersist false client

let private assertAckWithinSettled
    (baseline: EventId)
    (returned: EventId)
    (settled: EventId)
    =
    Assert.True(
        EventId.value returned > EventId.value baseline,
        $"ack revision {EventId.value returned} baseline {EventId.value baseline}")
    Assert.True(
        EventId.value returned <= EventId.value settled,
        $"ack revision {EventId.value returned} settled {EventId.value settled}")

let private ackSettled
    (client: HttpClient)
    (baseline: EventId)
    (response: HttpResponseMessage)
    =
    task {
        let! postBody = response.Content.ReadAsStringAsync()
        let returned = decodeSuccessRevision postBody
        let! settled = waitUntilPersistSettled client
        assertAckWithinSettled baseline returned settled
        return postBody, settled
    }

let private assertAckForSubmission
    (client: HttpClient)
    (baseline: EventId)
    (response: HttpResponseMessage)
    (submissionId: Guid)
    =
    task {
        let! postBody, settled = ackSettled client baseline response
        Assert.Equal<Guid list>([ submissionId ], decodeAckChangeIds postBody)
        return postBody, settled
    }

/// POST /ambit/events with a change and return the raw response.
let private postChange (client: HttpClient) (change: Ev) = task {
    let body = encodeChangeRequestBody [ change ]
    let content = new StringContent(body, Encoding.UTF8, "application/json")
    return! client.PostAsync("/ambit/events", content)
}

let private postChanges (client: HttpClient) (changes: Ev list) = task {
    let body = encodeChangeRequestBody changes
    let content = new StringContent(body, Encoding.UTF8, "application/json")
    return! client.PostAsync("/ambit/events", content)
}

let private postChangeOk (client: HttpClient) (change: Ev) = task {
    let! response = postChange client change
    Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    return response
}

let private ownedChild (id: NodeId) : ChildNode list =
    ChildNode.owners [ id ]

let private assertExactPrefix (submitted: Ev) (confirmed: Ev) =
    Assert.Equal(submitted.submissionId, confirmed.submissionId)
    Assert.True(
        (eventOps confirmed).Length >= (eventOps submitted).Length,
        "confirmation must keep submitted ops as a prefix")
    Assert.Equal<Op list>(
        (eventOps submitted),
        List.take (eventOps submitted).Length (eventOps confirmed))
    (eventOps confirmed)
    |> List.skip (eventOps submitted).Length
    |> List.iter (fun op ->
        match op with
        | Op.SetUpdateTime _ -> ()
        | _ -> failwith "stamp enrichment must be SetUpdateTime")

let private browserChange ops =
    SpecialNodeTestHelpers.changeEventZero "" ops

let private browserChangeId submissionId ops =
    SpecialNodeTestHelpers.changeEvent "" EventId.zero submissionId ops

let private changeAddChild (rootId: NodeId) (childText: string) : Ev * NodeId =
    let childId = NodeId.New()
    let change =
        browserChange
            [ Op.NewNode(childId, childText)
              Op.Replace(rootId, [], ownedChild childId) ]
    change, childId

/// Read a file that may be held open by a FileAgent (shared read).
let private readFileShared (path: string) =
    use fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
    use reader = new StreamReader(fs)
    reader.ReadToEnd()

let private writeDocumentFiles (tempDir: string) (state: State) =
    DocumentPersistWrite.writeAllDocuments tempDir state.graph
    |> function
        | Ok _ -> ()
        | Error err -> failwith err
    match Bookkeeping.writeEventId tempDir state.eventId with
    | Ok () -> ()
    | Error err -> failwith err
    File.WriteAllText(EventLogFile.eventsPath tempDir, "")

let private stateWithChild (text: string) =
    let initialState =
        { graph = Graph.create ()
          eventId = EventId.zero }
    let change, _ = changeAddChild Graph.rootId text
    match applyChange change initialState with
    | ApplyResult.Changed st -> { st with eventId = EventId.fromJson 1 }
    | ApplyResult.Unchanged _ -> failwith "Expected file bootstrap change to apply"
    | ApplyResult.Invalid (_, err) -> failwith $"Expected valid bootstrap change: {err}"

let private withFreshConnection (body: string -> Task<unit>) = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    return! body connStr
}

let private withDbClient (body: string -> HttpClient -> Task<unit>) =
    withFreshConnection (fun connStr -> task {
        use client = createDbClient connStr
        return! body connStr client
    })

/// Fresh Database HTTP client for state and Change facts.
let private withClient (body: HttpClient -> Task<unit>) =
    withDbClient (fun _ client -> body client)

let private isNodePersisted (event: Ev) =
    match event.body with
    | EventBody.Notice (Notice.NodePersisted _) -> true
    | _ -> false

let private submissionIdsOf (rows: Database.EventRow list) =
    rows
    |> List.choose (fun row ->
        match EventLogFile.decodeEvent row.payload with
        | Ok event -> Some event.submissionId
        | Error _ -> None)

let private usingAdmittedFile tempDir body = task {
    let host, handle = createAdmittedFile tempDir
    try
        return! body host handle
    finally
        CoreMailboxApi.dispose host
}

let private withNewFile body = task {
    let tempDir = newTempDir ()
    return! usingAdmittedFile tempDir (fun host handle ->
        body tempDir host handle)
}

let private postEventsOk (handle: CoreChangesDoor) (events: Ev list) = task {
    let! result = CoreDoor.postEvents handle events |> Async.StartAsTask
    match result with
    | Error err -> Assert.Fail(err)
    | Ok _ -> ()
}

let private waitForPersisted host minimum =
    waitForNotices
        (fun () ->
            CoreMailboxApi.getEventsSince host EventId.zero
            |> Async.StartAsTask)
        (fun events ->
            events |> List.filter isNodePersisted |> List.length >= minimum)

let private addNestedWorkspace (client: HttpClient) = task {
    let wsId = NodeId.New()
    let innerId = NodeId.New()
    let createWorkspace =
        browserChange
            [ Op.NewSpecialNode(wsId, Workspace, "home")
              Op.Replace(Graph.workspacesId, [], ownedChild wsId)
              Op.SetDocumentState(wsId, Unparsed, Current) ]
    let! _ = postChangeOk client createWorkspace
    let createInner =
        browserChange
            [ Op.NewNode(innerId, "inside-ws")
              Op.Replace(wsId, [], ownedChild innerId) ]
    let! _ = postChangeOk client createInner
    return wsId, innerId
}

[<Fact>]
let ``unavailable Database serves read-only file fallback and ignores Persistence:Mode`` () = task {
    let tempDir = newTempDir ()
    use client = createDbModeWithoutConnectionClientForDir tempDir
    let! resp = client.GetAsync("/ambit/state")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

    let! body = resp.Content.ReadAsStringAsync()
    Assert.Equal(EventId.fromJson 0, decodeEventId body)

    let rootId = (decodeGraph body).root
    let change, _ = changeAddChild rootId "startup-file-fallback"
    let! postResp = postChange client change
    Assert.Equal(HttpStatusCode.BadRequest, postResp.StatusCode)
    let! errorBody = postResp.Content.ReadAsStringAsync()
    Assert.Contains("read-only", decodeErrorField errorBody)

    let ambPath = Path.Combine(tempDir, ".amb")
    if File.Exists ambPath then
        Assert.DoesNotContain("startup-file-fallback", File.ReadAllText ambPath)
}

[<Fact>]
let ``GET state returns revision 0 for fresh server`` () =
    withClient (fun client -> task {
        let! eventId, _ = readState client
        Assert.Equal(EventId.fromJson 0, eventId)
    })

[<Fact>]
let ``GET state returns valid graph with root node`` () =
    withClient (fun client -> task {
        let! _, graph = readState client
        Assert.Equal(0, userNodeCount graph)
        Assert.True(graph.nodes.ContainsKey graph.root)
        let root = graph.nodes.[graph.root]
        Assert.Equal("ROOT", root.text)
        Assert.Empty(userRootChildren graph)
    })

[<Fact>]
let ``user css is served from canonical SYSTEM path`` () = task {
    let tempDir = newTempDir ()
    let systemDir = Path.Combine(tempDir, "SYSTEM")
    Directory.CreateDirectory(systemDir) |> ignore
    File.WriteAllText(Path.Combine(tempDir, "user.css"), "legacy")
    File.WriteAllText(Path.Combine(systemDir, "user.css"), "canonical")
    use client = createClientForDir tempDir

    let! response = client.GetAsync("/ambit/user.css")
    Assert.Equal(HttpStatusCode.OK, response.StatusCode)
    let! css = response.Content.ReadAsStringAsync()
    Assert.Equal("canonical", css)
}

[<Fact>]
let ``POST Change and inverse Changes return complete confirmations in request order``
    () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let change, childId = changeAddChild rootId "history-action"
        let undo = inverseEvent EventId.zero (Guid.NewGuid()) change
        let redo = inverseEvent EventId.zero (Guid.NewGuid()) undo
        let! baseline = waitUntilPersistSettled client
        let! response = postChanges client [ change; undo; redo ]
        Assert.Equal(HttpStatusCode.OK, response.StatusCode)
        let! ackJson = response.Content.ReadAsStringAsync()
        let post = decodeSuccess ackJson
        let! settled = waitUntilPersistNoted client
        Assert.True(EventId.value settled >= 3)
        assertAckWithinSettled baseline post.eventId settled
        Assert.True(post.buildEpochSec > 0)
        Assert.True(post.pageBuildEpochSec > 0)
        Assert.True(post.isReady)
        Assert.False(post.externalChanges)
        Assert.Equal<Guid list>(
            [ change.submissionId; undo.submissionId; redo.submissionId ],
            changesOnly post.events
            |> List.map (fun confirmed -> confirmed.submissionId))
        List.iter2 assertExactPrefix [ change; undo; redo ] (changesOnly post.events)
        let! _, graph = readState client
        Assert.Equal("history-action", graph.nodes.[childId].text)
        let! poll = readPoll client
        Assert.Equal(settled, poll.eventId)
        Assert.True(poll.buildEpochSec > 0)
        Assert.True(poll.pageBuildEpochSec > 0)
        Assert.True(poll.isReady)
        Assert.True(poll.externalChanges)
        // Poll is EventLog.since: newest-head. ACK keeps request order.
        Assert.Equal<Ev list>(
            List.rev (changesOnly post.events),
            changesOnly poll.events)
    })

[<Fact>]
let ``Edit reply with only a persist Notice has no external changes`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let change, _ = changeAddChild rootId "persist-notice"
        let! response = postChangeOk client change
        let! ackJson = response.Content.ReadAsStringAsync()
        let post = decodeSuccess ackJson
        Assert.False(post.externalChanges)
        Assert.Contains(change.submissionId, decodeAckChangeIds ackJson)
        let! events =
            waitForNotices
                (fun () -> task {
                    let! poll = readPoll client
                    return poll.events
                })
                (List.exists isNodePersisted)
        Assert.True(List.exists isNodePersisted events)
    })

[<Fact>]
let ``large paste inverse total response is measured`` () =
    withClient (fun client -> task {
        let children =
            List.init 2000 (fun _ -> ChildNode.owner (NodeId.New()))
        let paste =
            browserChange
                [ for i, child in List.indexed children ->
                    Op.NewNode(child.id, "line " + string i)
                  yield Op.Replace(Graph.workspacesId, [], children) ]
        let! _ = postChangeOk client paste
        let inverse = inverseEvent EventId.zero (Guid.NewGuid()) paste
        let sw = System.Diagnostics.Stopwatch.StartNew()
        let! _ = postChangeOk client inverse
        sw.Stop()
        printfn
            "2,000-Node paste inverse total response: %.3f ms"
            sw.Elapsed.TotalMilliseconds
    })

[<Fact>]
let ``POST explicit Undo JSON is rejected`` () =
    withClient (fun client -> task {
        let body =
            "{\"events\":[{\"id\":0,"
            + "\"submissionId\":\"00000000-0000-0000-0000-000000000001\","
            + "\"authority\":\"\",\"commandName\":\"\","
            + "\"body\":{\"Change\":[]}}]}"
        use content = new StringContent(body, Encoding.UTF8, "application/json")
        let! response = client.PostAsync("/ambit/events", content)
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode)
        let! eventId, _ = readState client
        Assert.Equal(EventId.fromJson 0, eventId)
    })

[<Fact>]
let ``POST unchanged submission is rejected`` () =
    withClient (fun client -> task {
        let noOp = browserChange []
        let! response = postChange client noOp
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode)
        let! body = response.Content.ReadAsStringAsync()
        Assert.Contains("Unchanged", body)
        let! eventId, _ = readState client
        Assert.Equal(EventId.fromJson 0, eventId)
    })

[<Fact>]
let ``POST changes accepts X-Gambol-Client header`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let change, _ = changeAddChild rootId "hinted"
        let body = encodeChangeRequestBody [ change ]
        use content = new StringContent(body, Encoding.UTF8, "application/json")
        use req = new HttpRequestMessage(HttpMethod.Post, "/ambit/events")
        req.Content <- content
        req.Headers.TryAddWithoutValidation(
            ClientIdentity.HeaderName,
            "Win32; Mozilla/5.0 (test)")
        |> ignore
        let! resp = client.SendAsync(req)
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    })

[<Fact>]
let ``POST changes SetText changes child text and bumps revision`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let change0, childId = changeAddChild rootId ""
        let! _ = postChangeOk client change0
        let! baseline = waitUntilPersistSettled client
        let change = browserChange [ Op.SetText(childId, "", "hello") ]
        let! resp = postChangeOk client change
        let! _ = assertAckForSubmission client baseline resp change.submissionId
        let! _, graph = readState client
        Assert.Equal("hello", graph.nodes.[childId].text)
    })

[<Fact>]
let ``POST changes NewNode+Replace adds child to root`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let childId = NodeId.New()
        let change =
            browserChange
                [ Op.NewNode(childId, "child")
                  Op.Replace(rootId, [], [ ChildNode.owner childId ]) ]
        let! resp = postChangeOk client change
        let! _ =
            assertAckForSubmission client EventId.zero resp change.submissionId
        let! _, graph = readState client
        Assert.Equal(1, userNodeCount graph)
        Assert.Equal<ChildNode list>(
            [ ChildNode.owner childId ],
            userRootChildren graph)
        Assert.Equal("child", graph.nodes.[childId].text)
    })

[<Fact>]
let ``POST changes with invalid JSON returns 400`` () =
    withClient (fun client -> task {
        let! _ = getStateJson client
        let content = new StringContent("not json", Encoding.UTF8, "application/json")
        let! resp = client.PostAsync("/ambit/events", content)
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode)
    })

[<Fact>]
let ``POST changes with bad op returns 400`` () =
    withClient (fun client -> task {
        let! _ = getStateJson client
        let bogusId = NodeId.New()
        let change = browserChange [ Op.SetText(bogusId, "wrong", "new") ]
        let! resp = postChange client change
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode)
        let! body = resp.Content.ReadAsStringAsync()
        let err = decodeErrorField body
        Assert.False(String.IsNullOrWhiteSpace err)
    })

[<Fact>]
let ``POST changes twice bumps revision to 2`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let change1, childId = changeAddChild rootId "first"
        let! _ = postChangeOk client change1
        let! baseline = waitUntilPersistSettled client
        let change2 = browserChange [ Op.SetText(childId, "first", "second") ]
        let! resp2 = postChangeOk client change2
        let! _, _ = ackSettled client baseline resp2
        let! _, graph = readState client
        Assert.Equal("second", graph.nodes.[childId].text)
    })

[<Fact>]
let ``POST changes batch with two changes bumps revision to 2`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let change1, childId = changeAddChild rootId "first"
        let change2 = browserChange [ Op.SetText(childId, "first", "second") ]
        let! baseline = waitUntilPersistSettled client
        let! resp = postChanges client [ change1; change2 ]
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
        let! postBody = resp.Content.ReadAsStringAsync()
        let returned = decodeSuccessRevision postBody
        let! settled = waitUntilPersistNoted client
        Assert.True(EventId.value settled >= 2)
        assertAckWithinSettled baseline returned settled
        Assert.Equal<Guid list>(
            [ change1.submissionId; change2.submissionId ],
            decodeAckChangeIds postBody)
        let! _, graph = readState client
        Assert.Equal("second", graph.nodes.[childId].text)
    })

[<Fact>]
let ``POST changes batch with bad second change keeps earlier items`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let change1, childId = changeAddChild rootId "first"
        let bad =
            browserChange [ Op.SetText(NodeId.New(), "old", "new") ]
        let! resp = postChanges client [ change1; bad ]
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode)
        let! settled = waitUntilPersistSettled client
        let! eventId, graph = readState client
        Assert.Equal(settled, eventId)
        Assert.True(graph.nodes.ContainsKey childId)
        Assert.Equal("first", graph.nodes.[childId].text)
        let! poll = readPoll client
        Assert.True(poll.externalChanges)
        Assert.Equal<Guid list>(
            [ change1.submissionId ],
            changesOnly poll.events
            |> List.map (fun event -> event.submissionId))
    })

[<Fact>]
let ``POST changes persists in GET state`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let c0, childId = changeAddChild rootId ""
        let! _ = postChange client c0
        let! _ = waitUntilPersistSettled client
        let! _ =
            postChange client (
                browserChange [ Op.SetText(childId, "", "persisted") ])
        let! settled = waitUntilPersistSettled client
        let! eventId, graph = readState client
        Assert.Equal(settled, eventId)
        Assert.Equal("persisted", graph.nodes.[childId].text)
    })

[<Fact>]
let ``POST same submissionId twice is idempotent`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let cid = Guid.NewGuid()
        let childId = NodeId.New()
        let change =
            browserChangeId cid
                [ Op.NewNode(childId, "once")
                  Op.Replace(rootId, [], ownedChild childId) ]
        let! r1 = postChangeOk client change
        let! b1 = r1.Content.ReadAsStringAsync()
        Assert.DoesNotContain("graph", b1, StringComparison.Ordinal)
        Assert.Equal<Guid list>([ cid ], decodeAckChangeIds b1)
        let! r2 = postChangeOk client change
        let! b2 = r2.Content.ReadAsStringAsync()
        Assert.DoesNotContain("graph", b2, StringComparison.Ordinal)
        Assert.Equal<Guid list>([ cid ], decodeAckChangeIds b2)
        Assert.Equal<Ev list>(
            changesOnly ((decodeSuccess b1).events),
            changesOnly ((decodeSuccess b2).events))
        let returned1 = decodeSuccessRevision b1
        let returned2 = decodeSuccessRevision b2
        let! settled = waitUntilPersistSettled client
        assertAckWithinSettled EventId.zero returned1 settled
        assertAckWithinSettled EventId.zero returned2 settled
        let! eventId, graph = readState client
        Assert.Equal(settled, eventId)
        Assert.Equal("once", graph.nodes.[childId].text)
    })

[<Fact>]
let ``POST with stale base revision and valid SetText succeeds`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let setup, childId = changeAddChild rootId ""
        let! _ = postChangeOk client setup
        let! baseline = waitUntilPersistSettled client
        let stale = browserChange [ Op.SetText(childId, "", "x") ]
        let! resp = postChangeOk client stale
        let! _, settled =
            assertAckForSubmission client baseline resp stale.submissionId
        let! eventId, graph = readState client
        Assert.Equal(settled, eventId)
        Assert.Equal("x", graph.nodes.[childId].text)
    })

[<Fact>]
let ``POST unrelated attribute edits with stale revision both succeed`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let setupX, nodeX = changeAddChild rootId "x0"
        let! _ = postChangeOk client setupX
        let! _ = waitUntilPersistSettled client
        let! _, graph1 = readState client
        let rootChildren = Graph.children graph1 rootId
        let nodeY = NodeId.New()
        let setupY =
            browserChange
                [ Op.NewNode(nodeY, "y0")
                  Op.Replace(
                      rootId,
                      rootChildren,
                      (List.take 1 rootChildren)
                      @ (ownedChild nodeY)
                      @ (List.skip 1 rootChildren)) ]
        let! _ = postChangeOk client setupY
        let! _ = waitUntilPersistSettled client
        let changeA = browserChange [ Op.SetText(nodeY, "y0", "yA") ]
        let! _ = postChangeOk client changeA
        let! baseline = waitUntilPersistSettled client
        let changeB = browserChange [ Op.SetText(nodeX, "x0", "xB") ]
        let! rB = postChangeOk client changeB
        let! _, settled =
            assertAckForSubmission client baseline rB changeB.submissionId
        let! eventId, graph = readState client
        Assert.Equal(settled, eventId)
        Assert.Equal("xB", graph.nodes.[nodeX].text)
        Assert.Equal("yA", graph.nodes.[nodeY].text)
    })

[<Fact>]
let ``POST concurrent stale text Changes amend second as amb-conflict child`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let setup, nodeX = changeAddChild rootId "x0"
        let! _ = postChangeOk client setup
        let! _ = waitUntilPersistSettled client
        let changeA = browserChange [ Op.SetText(nodeX, "x0", "xA") ]
        let! _ = postChangeOk client changeA
        let! baseline = waitUntilPersistSettled client
        let changeB = browserChange [ Op.SetText(nodeX, "x0", "xB") ]
        let! rB = postChange client changeB
        let! rBText = rB.Content.ReadAsStringAsync()
        Assert.True(rB.StatusCode = HttpStatusCode.OK, rBText)
        let revision = decodeSuccessRevision rBText
        let! settled = waitUntilPersistSettled client
        Assert.True(EventId.value settled >= 6)
        assertAckWithinSettled baseline revision settled
        Assert.Equal<Guid list>([ changeB.submissionId ], decodeAckChangeIds rBText)
        Assert.True(decodeSuccessExternalChanges rBText)
        let! eventId, graph = readFullState client
        Assert.Equal(settled, eventId)
        Assert.Equal("xA", graph.nodes.[nodeX].text)
        assertAmbConflictFirstChild graph nodeX "xB"
    })

[<Fact>]
let ``POST concurrent stale name Changes amend second as amb-conflict child`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let nodeId = NodeId.New()
        let setup =
            browserChange
                [ Op.NewNode(nodeId, "body")
                  Op.SetName(nodeId, "", "alpha")
                  Op.Replace(rootId, [], ownedChild nodeId) ]
        let! _ = postChangeOk client setup
        let changeA = browserChange [ Op.SetName(nodeId, "alpha", "nameA") ]
        let! _ = postChangeOk client changeA
        let changeB = browserChange [ Op.SetName(nodeId, "alpha", "nameB") ]
        let! rB = postChangeOk client changeB
        let! postBody = rB.Content.ReadAsStringAsync()
        Assert.True(decodeSuccessExternalChanges postBody)
        let! _, graph = readFullState client
        Assert.Equal(
            "nameA",
            Filename.tryValue graph.nodes.[nodeId].name |> Option.get)
        assertAmbConflictFirstChild graph nodeId "nameB"
    })

[<Fact>]
let ``POST concurrent stale class Changes merge set delta and succeed`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let nodeId = NodeId.New()
        let prior = CssClass.ofList [ "a"; "b" ]
        let setup =
            browserChange
                [ Op.NewNode(nodeId, "tagged")
                  Op.SetClasses(nodeId, CssClass.empty, prior)
                  Op.Replace(rootId, [], ownedChild nodeId) ]
        let! _ = postChangeOk client setup
        let changeA =
            browserChange
                [ Op.SetClasses(nodeId, prior, CssClass.ofList [ "b" ]) ]
        let! _ = postChangeOk client changeA
        let changeB =
            browserChange
                [ Op.SetClasses(
                    nodeId,
                    prior,
                    CssClass.ofList [ "a"; "b"; "c" ]) ]
        let! rB = postChangeOk client changeB
        let! postBody = rB.Content.ReadAsStringAsync()
        Assert.True(decodeSuccessExternalChanges postBody)
        let! _, graph = readState client
        let classes =
            graph.nodes.[nodeId].cssClasses |> CssClass.toList |> Set.ofList
        Assert.Equal<Set<string>>(Set.ofList [ "b"; "c" ], classes)
    })

[<Fact>]
let ``POST unrelated structural edits with stale revision both succeed`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let parentP1 = NodeId.New()
        let parentP2 = NodeId.New()
        let setupParents =
            browserChange
                [ Op.NewNode(parentP1, "p1")
                  Op.NewNode(parentP2, "p2")
                  Op.Replace(
                      rootId,
                      [],
                      [ ChildNode.owner parentP1
                        ChildNode.owner parentP2 ]) ]
        let! _ = postChangeOk client setupParents
        let! _ = waitUntilPersistSettled client
        let childA = NodeId.New()
        let changeA =
            browserChange
                [ Op.NewNode(childA, "a")
                  Op.Replace(parentP1, [], ownedChild childA) ]
        let! _ = postChangeOk client changeA
        let! baseline = waitUntilPersistSettled client
        let childB = NodeId.New()
        let changeB =
            browserChange
                [ Op.NewNode(childB, "b")
                  Op.Replace(parentP2, [], ownedChild childB) ]
        let! rB = postChangeOk client changeB
        let! _, settled =
            assertAckForSubmission client baseline rB changeB.submissionId
        let! eventId, graph = readFullState client
        Assert.Equal(settled, eventId)
        Assert.Equal<ChildNode list>(ownedChild childA, Graph.children graph parentP1)
        Assert.Equal<ChildNode list>(ownedChild childB, Graph.children graph parentP2)
    })

[<Fact>]
let ``POST same-parent structural collision amends and succeeds`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let parentP = NodeId.New()
        let child0 = NodeId.New()
        let setup =
            browserChange
                [ Op.NewNode(parentP, "p")
                  Op.NewNode(child0, "c0")
                  Op.Replace(rootId, [], ownedChild parentP)
                  Op.Replace(parentP, [], ownedChild child0) ]
        let! _ = postChangeOk client setup
        let! _ = waitUntilPersistSettled client
        let childA = NodeId.New()
        let changeA =
            browserChange
                [ Op.NewNode(childA, "a")
                  Op.Replace(parentP, ownedChild child0, ownedChild childA) ]
        let! _ = postChangeOk client changeA
        let! _ = waitUntilPersistSettled client
        let childB = NodeId.New()
        let changeB =
            browserChange
                [ Op.NewNode(childB, "b")
                  Op.Replace(parentP, ownedChild child0, ownedChild childB) ]
        let! rB = postChangeOk client changeB
        let! postBody = rB.Content.ReadAsStringAsync()
        Assert.True(decodeSuccessExternalChanges postBody)
        let! settled = waitUntilPersistSettled client
        let! eventId, graph = readFullState client
        Assert.Equal(settled, eventId)
        let children = Graph.children graph parentP
        Assert.Equal(2, children.Length)
        Assert.Contains(ownedChild childA |> List.head, children)
        Assert.Contains(ownedChild childB |> List.head, children)
    })

[<Fact>]
let ``POST duplicate submissionId with stale revision stays idempotent`` () =
    withClient (fun client -> task {
        let! rootId = stateRoot client
        let cid = Guid.NewGuid()
        let childId = NodeId.New()
        let change =
            browserChangeId cid
                [ Op.NewNode(childId, "once")
                  Op.Replace(rootId, [], ownedChild childId) ]
        let! _ = postChangeOk client change
        let! _ = waitUntilPersistSettled client
        let other, otherId = changeAddChild rootId "other"
        let! _ = postChangeOk client other
        let! settled = waitUntilPersistSettled client
        let! r2 = postChangeOk client change
        let! b2 = r2.Content.ReadAsStringAsync()
        Assert.Equal(settled, decodeSuccessRevision b2)
        Assert.Equal<Guid list>([ cid ], decodeAckChangeIds b2)
        let! eventId, graph = readState client
        Assert.Equal(settled, eventId)
        Assert.Equal("once", graph.nodes.[childId].text)
        Assert.Equal("other", graph.nodes.[otherId].text)
    })

[<Fact>]
let ``POST changes creates log file`` () =
    withNewFile (fun tempDir _host handle -> task {
        let change, _ = changeAddChild Graph.rootId "logged"
        do! postEventsOk handle [ change ]
        let logPath = EventLogFile.eventsPath tempDir
        Assert.True(
            File.Exists(logPath),
            "Events file should exist after first change")
        let content = readFileShared logPath
        Assert.Contains("logged", content)
    })

[<Fact>]
let ``Snapshot writes amb artifacts asynchronously after change`` () =
    withNewFile (fun tempDir _host handle -> task {
        let change, _ = changeAddChild Graph.rootId "snapped"
        do! postEventsOk handle [ change ]
        do! Task.Delay(500)
        let ambPath = Path.Combine(tempDir, ".amb")
        Assert.True(File.Exists ambPath, ".amb snapshot should exist")
        let content = File.ReadAllText ambPath
        Assert.Contains("snapped", content)
    })

[<Fact>]
let ``Log contains valid change data after POST`` () =
    withNewFile (fun tempDir _host handle -> task {
        let change, _ = changeAddChild Graph.rootId "logged-entry"
        do! postEventsOk handle [ change ]
        let logPath = EventLogFile.eventsPath tempDir
        Assert.True(File.Exists logPath)
        let content = readFileShared logPath
        Assert.Contains("logged-entry", content)
        Assert.True(
            content.StartsWith("00000001"),
            "Log entry should have 8-digit padded id prefix")
    })

[<Fact>]
let ``DB is present in db mode`` () =
    withDbClient (fun connStr client -> task {
        let! rootId = stateRoot client
        let change, _ = changeAddChild rootId "probe"
        let! _ = postChangeOk client change
        let! _ = waitUntilPersistSettled client
        let! rows = Database.getEventsAfter connStr 0 |> Async.AwaitTask
        Assert.Contains(change.submissionId, submissionIdsOf rows)
    })

[<Fact>]
let ``DB rows persist after agent cache reset`` () =
    withDbClient (fun connStr client -> task {
        let! rootId = stateRoot client
        let change, _ = changeAddChild rootId "probe"
        let! _ = postChangeOk client change
        let! _ = waitUntilPersistSettled client
        DatabaseSetup.resetAgentCacheForTest ()
        let! rows = Database.getEventsAfter connStr 0 |> Async.AwaitTask
        Assert.Contains(change.submissionId, submissionIdsOf rows)
    })

[<Fact>]
let ``DB rows survive second server startup without DB reset`` () =
    withDbClient (fun connStr client1 -> task {
        let! rootId = stateRoot client1
        let change, _ = changeAddChild rootId "probe"
        let! _ = postChangeOk client1 change
        let! _ = waitUntilPersistSettled client1
        let! rowsBefore = Database.getEventsAfter connStr 0 |> Async.AwaitTask
        use client2 = createDbClientNoReset connStr
        let! rowsAfter = Database.getEventsAfter connStr 0 |> Async.AwaitTask
        Assert.Equal<Database.EventRow list>(rowsBefore, rowsAfter)
    })

[<Fact>]
let ``DB authority does not import files when database is empty`` () =
    withFreshConnection (fun connStr -> task {
        let tempDir = newTempDir ()
        writeDocumentFiles tempDir (stateWithChild "from-file-bootstrap")
        use client = createDbClientForDir connStr tempDir
        let! eventId, graph = readState client
        Assert.Equal(EventId.fromJson 0, eventId)
        Assert.DoesNotContain((0, "from-file-bootstrap"), userTreeShape graph)
    })

[<Fact>]
let ``legacy Persistence:Mode file still uses Database persistence`` () =
    withFreshConnection (fun connStr -> task {
        let tempDir = newTempDir ()
        writeDocumentFiles tempDir (stateWithChild "from-file-bootstrap")
        use client = createFileModeWithDbClientForDir connStr tempDir
        let! eventId, graph = readState client
        Assert.Equal(EventId.fromJson 0, eventId)
        Assert.DoesNotContain((0, "from-file-bootstrap"), userTreeShape graph)
        let change, _ = changeAddChild graph.root "legacy-mode-ignored"
        let! _ = postChangeOk client change
        return ()
    })

[<Fact>]
let ``DB restart preserves NodeIds`` () =
    withDbClient (fun connStr client1 -> task {
        let! rootId = stateRoot client1
        let change, childId = changeAddChild rootId "db-restart-child"
        let! _ = postChangeOk client1 change
        let! settled = waitUntilPersistSettled client1
        use client2 = createDbClientNoReset connStr
        let! eventId, graph = readState client2
        Assert.Equal(settled, eventId)
        Assert.True(graph.nodes.ContainsKey childId, "NodeId must survive restart")
        Assert.Equal("db-restart-child", graph.nodes.[childId].text)
    })

[<Fact>]
let ``DB restart replays log when projection is cleared`` () =
    withDbClient (fun connStr client1 -> task {
        let! rootId = stateRoot client1
        let change1, childId1 = changeAddChild rootId "first"
        let! _ = postChangeOk client1 change1
        let! baseline = waitUntilPersistSettled client1
        let change2, childId2 = changeAddChild rootId "second"
        let! r2 = postChangeOk client1 change2
        let! _, settled = ackSettled client1 baseline r2
        let! logRows = Database.getEventsAfter connStr 0 |> Async.AwaitTask
        let storedIds = submissionIdsOf logRows
        Assert.Contains(change1.submissionId, storedIds)
        Assert.Contains(change2.submissionId, storedIds)
        // Wipe projection only; EventLog persist is the restart authority.
        use conn = new Npgsql.NpgsqlConnection(connStr)
        do! conn.OpenAsync()
        use cmd = conn.CreateCommand()
        cmd.CommandText <-
            "TRUNCATE node_children, nodes, graph RESTART IDENTITY CASCADE;"
        let! _ = cmd.ExecuteNonQueryAsync()
        use client2 = createDbClientNoReset connStr
        let! eventId, graph = readState client2
        Assert.Equal(settled, eventId)
        Assert.True(graph.nodes.ContainsKey childId1)
        Assert.True(graph.nodes.ContainsKey childId2)
    })

[<Fact>]
let ``DB restart keeps duplicate submissionId idempotent`` () =
    withDbClient (fun connStr client1 -> task {
        let! rootId = stateRoot client1
        let change, childId = changeAddChild rootId "once-after-restart"
        let! r1 = postChangeOk client1 change
        let! b1 = r1.Content.ReadAsStringAsync()
        let returned = decodeSuccessRevision b1
        Assert.Equal<Guid list>([ change.submissionId ], decodeAckChangeIds b1)
        let! settled = waitUntilPersistSettled client1
        assertAckWithinSettled EventId.zero returned settled
        use client2 = createDbClientNoReset connStr
        let! r2 = postChangeOk client2 change
        let! b2 = r2.Content.ReadAsStringAsync()
        Assert.Equal(settled, decodeSuccessRevision b2)
        Assert.Equal<Guid list>([ change.submissionId ], decodeAckChangeIds b2)
        Assert.Equal<Ev list>(
            changesOnly ((decodeSuccess b1).events),
            changesOnly ((decodeSuccess b2).events))
        let! eventId, graph = readState client2
        Assert.Equal(settled, eventId)
        Assert.True(
            graph.nodes.ContainsKey childId,
            "duplicate change must not create a new node")
        Assert.Equal("once-after-restart", graph.nodes.[childId].text)
        Assert.Equal<ChildNode list>(ownedChild childId, userRootChildren graph)
    })

[<Fact>]
let ``DB restart keeps inverse Change in EventLog`` () =
    withDbClient (fun connStr client1 -> task {
        let! rootId = stateRoot client1
        let change, _ = changeAddChild rootId "restart-inverse"
        let! _ = postChangeOk client1 change
        let! _ = waitUntilPersistSettled client1
        let undo = inverseEvent EventId.zero (Guid.NewGuid()) change
        let! undoResp = postChangeOk client1 undo
        let! undoAck = undoResp.Content.ReadAsStringAsync()
        let confirmedUndo =
            Assert.Single(changesOnly (decodeSuccess undoAck).events)
        let! settled = waitUntilPersistSettled client1
        use client2 = createDbClientNoReset connStr
        let! poll = readPoll client2
        Assert.Equal(confirmedUndo, List.head (changesOnly poll.events))
        let! eventId, _ = readState client2
        Assert.Equal(settled, eventId)
    })

[<Fact>]
let ``New server uses snapshot + log replay`` () = task {
    let tempDir = newTempDir ()
    let change, firstId = changeAddChild Graph.rootId "first"
    let! noticed =
        usingAdmittedFile tempDir (fun host handle -> task {
            do! postEventsOk handle [ change ]
            let! _ = waitForPersisted host 1
            let update =
                browserChange [ Op.SetText(firstId, "first", "updated") ]
            do! postEventsOk handle [ update ]
            return! waitForPersisted host 2
        })
    do!
        usingAdmittedFile tempDir (fun _host handle -> task {
            let! stateResult = CoreDoor.getState handle |> Async.StartAsTask
            match stateResult with
            | Error err -> Assert.Fail(err)
            | Ok state ->
                let child =
                    state.graph.nodes
                    |> Map.toSeq
                    |> Seq.map snd
                    |> Seq.find (fun n -> n.text = "updated")
                Assert.Equal("updated", child.text)
                let highest =
                    noticed
                    |> List.map (fun event -> event.id)
                    |> List.fold EventId.max EventId.zero
                Assert.Equal(highest, state.eventId)
        })
}

[<Fact>]
let ``GET state default returns ROOT closure excluding nested workspace contents`` () =
    withClient (fun client -> task {
        let! wsId, innerId = addNestedWorkspace client
        let! _, graph = readState client
        Assert.True(graph.nodes.ContainsKey wsId)
        Assert.Equal(Unloaded, Graph.childrenStatus graph wsId)
        Assert.False(graph.nodes.ContainsKey innerId)
    })

[<Fact>]
let ``GET state scope full returns canonical graph for tests`` () =
    withClient (fun client -> task {
        let! wsId, innerId = addNestedWorkspace client
        let! _, graph = readFullState client
        Assert.True(graph.nodes.ContainsKey wsId)
        Assert.True(graph.nodes.ContainsKey innerId)
    })
