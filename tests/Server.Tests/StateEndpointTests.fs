module Gambol.Server.Tests.StateEndpointTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc.Testing
open Microsoft.Extensions.Configuration
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend
open SpecialNodeTestHelpers

module Encode = Thoth.Json.Newtonsoft.Encode
module Decode = Thoth.Json.Newtonsoft.Decode

let private testFile = "gambol"

let private decode decoder json =
    match Decode.fromString decoder json with
    | Ok v -> v
    | Error err -> failwith $"Decode failed: {err}"

let private decodeRevision json =
    Thoth.Json.Core.Decode.object (fun get ->
        get.Required.Field "revision" Serialization.decodeRevision)
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
        response.changes |> List.map (fun change -> change.changeId)

let private decodeSuccessRevision json =
    (decodeSuccess json).revision

let private decodeErrorField json =
    Thoth.Json.Core.Decode.object (fun get ->
        get.Required.Field "error" Thoth.Json.Core.Decode.string)
    |> decode <| json

let private decodeGraph json =
    Thoth.Json.Core.Decode.object (fun get ->
        get.Required.Field "graph" Serialization.decodeGraph)
    |> decode <| json

/// GET /ambit/state, assert 200 + JSON content type, return body string.
let private getStateJson (client: HttpClient) (_file: string) = task {
    let! resp = client.GetAsync("/ambit/state")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    Assert.Equal("application/json", resp.Content.Headers.ContentType.MediaType)
    return! resp.Content.ReadAsStringAsync()
}

/// Legacy total-load escape hatch for tests that need the canonical graph via HTTP.
let private getStateJsonFull (client: HttpClient) (_file: string) = task {
    let! resp = client.GetAsync("/ambit/state?scope=full")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    Assert.Equal("application/json", resp.Content.Headers.ContentType.MediaType)
    return! resp.Content.ReadAsStringAsync()
}

let private encodeChangeBatchBody (changes: Change list) =
    Encode.toString 0 (Serialization.encodeChangeBatch { changes = changes })

/// POST /ambit/changes with a change and return the raw response.
let private postChange (client: HttpClient) (_file: string) (change: Change) = task {
    let body = encodeChangeBatchBody [ change ]
    let content = new StringContent(body, Encoding.UTF8, "application/json")
    return! client.PostAsync("/ambit/changes", content)
}

let private postChanges (client: HttpClient) (_file: string) (changes: Change list) = task {
    let body = encodeChangeBatchBody changes
    let content = new StringContent(body, Encoding.UTF8, "application/json")
    return! client.PostAsync("/ambit/changes", content)
}

let private ownedChild (id: NodeId) : ChildNode list =
    ChildNode.owners [ id ]

let private assertExactPrefix (submitted: Change) (confirmed: Change) =
    Assert.Equal(submitted.changeId, confirmed.changeId)
    Assert.True(
        confirmed.ops.Length >= submitted.ops.Length,
        "confirmation must keep submitted ops as a prefix")
    Assert.Equal<Op list>(
        submitted.ops,
        List.take submitted.ops.Length confirmed.ops)
    confirmed.ops
    |> List.skip submitted.ops.Length
    |> List.iter (fun op ->
        match op with
        | Op.SetUpdateTime _ -> ()
        | _ -> failwith "stamp enrichment must be SetUpdateTime-only")

/// Build a change (base revision `rev`) that adds one child under root; returns change + child id.
let private changeAddChild (rootId: NodeId) (rev: int) (childText: string) : Change * NodeId =
    let childId = NodeId.New()
    let c =
        { id = rev
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, childText)
              Op.Replace(rootId, 0, [], ownedChild childId) ] }
    c, childId

/// Read a file that may be held open by a FileAgent (shared read).
let private readFileShared (path: string) =
    use fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
    use reader = new StreamReader(fs)
    reader.ReadToEnd()

let private writeDocumentFiles (tempDir: string) (state: State) =
    DocumentPersistence.writeAllDocuments tempDir state.graph
    |> function
        | Ok _ -> ()
        | Error err -> failwith err
    match Bookkeeping.writeRevision tempDir state.revision.Value with
    | Ok () -> ()
    | Error err -> failwith err
    File.WriteAllText(Bookkeeping.logPath tempDir, "")

let private stateWithChild (text: string) =
    let initialState =
        { graph = Graph.create ()
          history = History.empty
          revision = Revision 0 }

    let change, _ = changeAddChild Graph.rootId 0 text

    match History.applyChange change initialState with
    | ApplyResult.Changed st -> { st with revision = Revision 1 }
    | ApplyResult.Unchanged _ -> failwith "Expected file bootstrap change to apply"
    | ApplyResult.Invalid (_, err) -> failwith $"Expected valid bootstrap change: {err}"

// ---- Backend parameterisation ----

/// Both backends under test. MemberData requires this to be public.
let backends : obj[][] = [| [| box BackendKind.File |]; [| box BackendKind.Db |] |]

/// Run a test body against a fresh client for the given backend.
/// For Db: resets the test database before creating the client.
let private withClient (backend: BackendKind) (f: HttpClient -> Task<unit>) = task {
    match backend with
    | BackendKind.File ->
        use client = createFileClient ()
        return! f client
    | BackendKind.Db ->
        let connStr = requireDbConnStr ()
        do! resetTestDatabase connStr
        use client = createDbClient connStr
        return! f client
}

[<Fact>]
let ``DB mode without connection serves read-only file fallback`` () = task {
    let tempDir = newTempDir ()
    use client = createDbModeWithoutConnectionClientForDir tempDir
    let! resp = client.GetAsync("/ambit/state")
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

    let! body = resp.Content.ReadAsStringAsync()
    Assert.Equal(Revision 0, decodeRevision body)

    let rootId = (decodeGraph body).root
    let change, _ = changeAddChild rootId 0 "startup-file-fallback"
    let! postResp = postChange client testFile change
    Assert.Equal(HttpStatusCode.BadRequest, postResp.StatusCode)
    let! errorBody = postResp.Content.ReadAsStringAsync()
    Assert.Contains("read-only", decodeErrorField errorBody)

    let ambPath = Path.Combine(tempDir, ".amb")

    if File.Exists ambPath then
        Assert.DoesNotContain("startup-file-fallback", File.ReadAllText ambPath)
}

// ---- GET /ambit/state tests (parameterised) ----

[<Theory; MemberData(nameof backends)>]
let ``GET state returns revision 0 for fresh server`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json = getStateJson client testFile
        Assert.Equal(Revision 0, decodeRevision json)
    })

[<Theory; MemberData(nameof backends)>]
let ``GET state returns valid graph with root node`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json = getStateJson client testFile
        let graph = decodeGraph json
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

// ---- POST /ambit/changes tests (parameterised) ----

[<Theory; MemberData(nameof backends)>]
let ``POST Change and inverse Changes return complete confirmations in request order``
    (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let change, childId = changeAddChild rootId 0 "history-action"
        let undo = Change.inverse (Revision 1) (Guid.NewGuid()) change
        let redo = Change.inverse (Revision 2) (Guid.NewGuid()) undo
        let! response = postChanges client testFile [ change; undo; redo ]
        Assert.Equal(HttpStatusCode.OK, response.StatusCode)
        let! ackJson = response.Content.ReadAsStringAsync()
        let post = decodeSuccess ackJson
        Assert.Equal(Revision 3, post.revision)
        Assert.True(post.buildEpochSec > 0)
        Assert.True(post.pageBuildEpochSec > 0)
        Assert.True(post.isReady)
        Assert.False(post.externalChanges)
        Assert.Equal<Guid list>(
            [ change.changeId; undo.changeId; redo.changeId ],
            post.changes |> List.map (fun confirmed -> confirmed.changeId))
        List.iter2 assertExactPrefix [ change; undo; redo ] post.changes
        let! stateJson = getStateJson client testFile
        Assert.Equal("history-action", (decodeGraph stateJson).nodes.[childId].text)
        let! pollResponse = client.GetAsync("/ambit/poll?rev=0")
        let! pollJson = pollResponse.Content.ReadAsStringAsync()
        let poll =
            decode
                ApiResponseSerialization.decodeChangeSuccessResponseDecoder
                pollJson
        Assert.Equal(Revision 3, poll.revision)
        Assert.True(poll.buildEpochSec > 0)
        Assert.True(poll.pageBuildEpochSec > 0)
        Assert.True(poll.isReady)
        Assert.True(poll.externalChanges)
        Assert.Equal<Change list>(post.changes, poll.changes)
    })

[<Fact>]
let ``file backend large paste inverse total response is measured`` () = task {
    use client = createFileClient ()
    let children =
        List.init 2000 (fun _ -> ChildNode.owner (NodeId.New()))
    let paste =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ for i, child in List.indexed children ->
                Op.NewNode(child.id, "line " + string i)
              yield Op.Replace(Graph.workspacesId, 0, [], children) ] }
    let! pasteResp = postChange client testFile paste
    Assert.Equal(HttpStatusCode.OK, pasteResp.StatusCode)
    let inverse = Change.inverse (Revision 1) (Guid.NewGuid()) paste
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let! inverseResp = postChange client testFile inverse
    sw.Stop()
    Assert.Equal(HttpStatusCode.OK, inverseResp.StatusCode)
    printfn
        "2,000-Node paste inverse File-backend total response: %.3f ms"
        sw.Elapsed.TotalMilliseconds
}

[<Theory; MemberData(nameof backends)>]
let ``POST explicit Undo JSON is rejected`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let body =
            """{"changes":[{"action":"undo","id":0,"changeId":"00000000-0000-0000-0000-000000000001"}]}"""
        use content = new StringContent(body, Encoding.UTF8, "application/json")
        let! response = client.PostAsync("/ambit/changes", content)
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode)
        let! stateJson = getStateJson client testFile
        Assert.Equal(Revision 0, decodeRevision stateJson)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST unchanged submission is rejected`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let noOp =
            { id = 0
              changeId = Guid.NewGuid()
              ops = [] }
        let! response = postChange client testFile noOp
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode)
        let! body = response.Content.ReadAsStringAsync()
        Assert.Contains("Unchanged", body)
        let! stateJson = getStateJson client testFile
        Assert.Equal(Revision 0, decodeRevision stateJson)
    })

[<Fact>]
let ``POST changes accepts X-Gambol-Client header`` () = task {
    use client = createFileClient ()
    let! json0 = getStateJson client testFile
    let rootId = (decodeGraph json0).root
    let change, _ = changeAddChild rootId 0 "hinted"
    let body = encodeChangeBatchBody [ change ]
    use content = new StringContent(body, Encoding.UTF8, "application/json")
    use req = new HttpRequestMessage(HttpMethod.Post, "/ambit/changes")
    req.Content <- content
    req.Headers.TryAddWithoutValidation(
        ClientIdentity.HeaderName,
        "Win32; Mozilla/5.0 (test)")
    |> ignore
    let! resp = client.SendAsync(req)
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
}

[<Theory; MemberData(nameof backends)>]
let ``POST changes SetText changes child text and bumps revision`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let change0, childId = changeAddChild rootId 0 ""
        let! r0 = postChange client testFile change0
        Assert.Equal(HttpStatusCode.OK, r0.StatusCode)

        let change = { id = 1; changeId = Guid.NewGuid(); ops = [ Op.SetText(childId, "", "hello") ] }
        let! resp = postChange client testFile change
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

        let! postBody = resp.Content.ReadAsStringAsync()
        Assert.Equal(Revision 2, decodeSuccessRevision postBody)
        Assert.Equal<Guid list>([ change.changeId ], decodeAckChangeIds postBody)

        let! json = getStateJson client testFile
        let graph = decodeGraph json
        Assert.Equal("hello", graph.nodes.[childId].text)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST changes NewNode+Replace adds child to root`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let childId = NodeId.New()

        let change =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(childId, "child")
                  Op.Replace(rootId, 0, [], [ ChildNode.owner childId ]) ] }

        let! resp = postChange client testFile change
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

        let! postBody = resp.Content.ReadAsStringAsync()
        Assert.Equal(Revision 1, decodeSuccessRevision postBody)
        Assert.Equal<Guid list>([ change.changeId ], decodeAckChangeIds postBody)

        let! json = getStateJson client testFile
        let graph = decodeGraph json
        Assert.Equal(1, userNodeCount graph)
        Assert.Equal<ChildNode list>([ ChildNode.owner childId ], userRootChildren graph)
        Assert.Equal("child", graph.nodes.[childId].text)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST changes with invalid JSON returns 400`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! _ = getStateJson client testFile
        let content = new StringContent("not json", Encoding.UTF8, "application/json")
        let! resp = client.PostAsync("/ambit/changes", content)
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST changes with bad op returns 400`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! _ = getStateJson client testFile
        let bogusId = NodeId.New()
        let change = { id = 0; changeId = Guid.NewGuid(); ops = [ Op.SetText(bogusId, "wrong", "new") ] }
        let! resp = postChange client testFile change
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode)
        let! body = resp.Content.ReadAsStringAsync()
        let err = decodeErrorField body
        Assert.False(String.IsNullOrWhiteSpace err)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST changes twice bumps revision to 2`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let change1, childId = changeAddChild rootId 0 "first"
        let! resp1 = postChange client testFile change1
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode)

        let change2 = { id = 1; changeId = Guid.NewGuid(); ops = [ Op.SetText(childId, "first", "second") ] }
        let! resp2 = postChange client testFile change2
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode)

        let! postBody2 = resp2.Content.ReadAsStringAsync()
        Assert.Equal(Revision 2, decodeSuccessRevision postBody2)

        let! json = getStateJson client testFile
        let g = decodeGraph json
        Assert.Equal("second", g.nodes.[childId].text)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST changes batch with two changes bumps revision to 2`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let change1, childId = changeAddChild rootId 0 "first"
        let change2 =
            { id = 1
              changeId = Guid.NewGuid()
              ops = [ Op.SetText(childId, "first", "second") ] }

        let! resp = postChanges client testFile [ change1; change2 ]
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

        let! postBody = resp.Content.ReadAsStringAsync()
        Assert.Equal(Revision 2, decodeSuccessRevision postBody)
        Assert.Equal<Guid list>(
            [ change1.changeId; change2.changeId ],
            decodeAckChangeIds postBody)

        let! json = getStateJson client testFile
        let g = decodeGraph json
        Assert.Equal("second", g.nodes.[childId].text)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST changes batch with bad second change leaves state unchanged`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let change1, childId = changeAddChild rootId 0 "first"
        let bad =
            { id = 1
              changeId = Guid.NewGuid()
              ops = [ Op.SetText(NodeId.New(), "old", "new") ] }

        let! resp = postChanges client testFile [ change1; bad ]
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode)

        let! json = getStateJson client testFile
        Assert.Equal(Revision 0, decodeRevision json)
        Assert.False((decodeGraph json).nodes.ContainsKey childId)
        let! pollResponse = client.GetAsync("/ambit/poll?rev=0")
        let! pollJson = pollResponse.Content.ReadAsStringAsync()
        let poll =
            decode
                ApiResponseSerialization.decodeChangeSuccessResponseDecoder
                pollJson
        Assert.False(poll.externalChanges)
        Assert.Empty(poll.changes)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST changes persists in GET state`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let c0, childId = changeAddChild rootId 0 ""
        let! _ = postChange client testFile c0
        let! _ =
            postChange client testFile
                { id = 1; changeId = Guid.NewGuid(); ops = [ Op.SetText(childId, "", "persisted") ] }

        let! json = getStateJson client testFile
        Assert.Equal(Revision 2, decodeRevision json)
        let g = decodeGraph json
        Assert.Equal("persisted", g.nodes.[childId].text)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST same changeId twice is idempotent`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let cid = Guid.NewGuid()
        let childId = NodeId.New()
        let change =
            { id = 0
              changeId = cid
              ops =
                [ Op.NewNode(childId, "once")
                  Op.Replace(rootId, 0, [], ownedChild childId) ] }

        let! r1 = postChange client testFile change
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode)
        let! b1 = r1.Content.ReadAsStringAsync()
        Assert.DoesNotContain("graph", b1, StringComparison.Ordinal)
        Assert.Equal(Revision 1, decodeSuccessRevision b1)
        Assert.Equal<Guid list>([ cid ], decodeAckChangeIds b1)

        let! r2 = postChange client testFile change
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode)
        let! b2 = r2.Content.ReadAsStringAsync()
        Assert.DoesNotContain("graph", b2, StringComparison.Ordinal)
        Assert.Equal(Revision 1, decodeSuccessRevision b2)
        Assert.Equal<Guid list>([ cid ], decodeAckChangeIds b2)
        Assert.Equal<Change list>(
            (decodeSuccess b1).changes,
            (decodeSuccess b2).changes)

        let! json = getStateJson client testFile
        Assert.Equal(Revision 1, decodeRevision json)
        Assert.Equal("once", (decodeGraph json).nodes.[childId].text)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST with stale base revision and valid SetText succeeds`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let setup, childId = changeAddChild rootId 0 ""
        let! r0 = postChange client testFile setup
        Assert.Equal(HttpStatusCode.OK, r0.StatusCode)

        let stale =
            { id = 5
              changeId = Guid.NewGuid()
              ops = [ Op.SetText(childId, "", "x") ] }
        let! resp = postChange client testFile stale
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

        let! postBody = resp.Content.ReadAsStringAsync()
        Assert.Equal(Revision 2, decodeSuccessRevision postBody)
        Assert.Equal<Guid list>([ stale.changeId ], decodeAckChangeIds postBody)

        let! json = getStateJson client testFile
        Assert.Equal(Revision 2, decodeRevision json)
        Assert.Equal("x", (decodeGraph json).nodes.[childId].text)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST unrelated attribute edits with stale revision both succeed``
    (backend: BackendKind)
    =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let setupX, nodeX = changeAddChild rootId 0 "x0"
        let! rX = postChange client testFile setupX
        Assert.Equal(HttpStatusCode.OK, rX.StatusCode)

        let nodeY = NodeId.New()
        let setupY =
            { id = 1
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(nodeY, "y0")
                  Op.Replace(rootId, 1, [], ownedChild nodeY) ] }
        let! rY = postChange client testFile setupY
        Assert.Equal(HttpStatusCode.OK, rY.StatusCode)

        let changeA =
            { id = 2
              changeId = Guid.NewGuid()
              ops = [ Op.SetText(nodeY, "y0", "yA") ] }
        let! rA = postChange client testFile changeA
        Assert.Equal(HttpStatusCode.OK, rA.StatusCode)

        let changeB =
            { id = 2
              changeId = Guid.NewGuid()
              ops = [ Op.SetText(nodeX, "x0", "xB") ] }
        let! rB = postChange client testFile changeB
        Assert.Equal(HttpStatusCode.OK, rB.StatusCode)

        let! postBody = rB.Content.ReadAsStringAsync()
        Assert.Equal(Revision 4, decodeSuccessRevision postBody)
        Assert.Equal<Guid list>([ changeB.changeId ], decodeAckChangeIds postBody)

        let! json = getStateJson client testFile
        let g = decodeGraph json
        Assert.Equal(Revision 4, decodeRevision json)
        Assert.Equal("xB", g.nodes.[nodeX].text)
        Assert.Equal("yA", g.nodes.[nodeY].text)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST attribute collision with stale oldText returns 400`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let setup, nodeX = changeAddChild rootId 0 "x0"
        let! r0 = postChange client testFile setup
        Assert.Equal(HttpStatusCode.OK, r0.StatusCode)

        let changeA =
            { id = 1
              changeId = Guid.NewGuid()
              ops = [ Op.SetText(nodeX, "x0", "xA") ] }
        let! rA = postChange client testFile changeA
        Assert.Equal(HttpStatusCode.OK, rA.StatusCode)

        let changeB =
            { id = 1
              changeId = Guid.NewGuid()
              ops = [ Op.SetText(nodeX, "x0", "xB") ] }
        let! rB = postChange client testFile changeB
        Assert.Equal(HttpStatusCode.BadRequest, rB.StatusCode)
        let! errBody = rB.Content.ReadAsStringAsync()
        Assert.Contains("old text does not match", decodeErrorField errBody)

        let! json = getStateJson client testFile
        Assert.Equal(Revision 2, decodeRevision json)
        Assert.Equal("xA", (decodeGraph json).nodes.[nodeX].text)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST unrelated structural edits with stale revision both succeed``
    (backend: BackendKind)
    =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let parentP1 = NodeId.New()
        let parentP2 = NodeId.New()
        let setupParents =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(parentP1, "p1")
                  Op.NewNode(parentP2, "p2")
                  Op.Replace(
                      rootId,
                      0,
                      [],
                      [ ChildNode.owner parentP1; ChildNode.owner parentP2 ]) ] }
        let! r0 = postChange client testFile setupParents
        Assert.Equal(HttpStatusCode.OK, r0.StatusCode)

        let childA = NodeId.New()
        let changeA =
            { id = 1
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(childA, "a")
                  Op.Replace(parentP1, 0, [], ownedChild childA) ] }
        let! rA = postChange client testFile changeA
        Assert.Equal(HttpStatusCode.OK, rA.StatusCode)

        let childB = NodeId.New()
        let changeB =
            { id = 1
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(childB, "b")
                  Op.Replace(parentP2, 0, [], ownedChild childB) ] }
        let! rB = postChange client testFile changeB
        Assert.Equal(HttpStatusCode.OK, rB.StatusCode)

        let! postBody = rB.Content.ReadAsStringAsync()
        Assert.Equal(Revision 3, decodeSuccessRevision postBody)
        Assert.Equal<Guid list>([ changeB.changeId ], decodeAckChangeIds postBody)

        let! json = getStateJson client testFile
        let g = decodeGraph json
        Assert.Equal(Revision 3, decodeRevision json)
        Assert.Equal<ChildNode list>(
            ownedChild childA,
            g.nodes.[parentP1].children)
        Assert.Equal<ChildNode list>(
            ownedChild childB,
            g.nodes.[parentP2].children)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST same-parent structural collision with stale span returns 400``
    (backend: BackendKind)
    =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let parentP = NodeId.New()
        let child0 = NodeId.New()
        let setup =
            { id = 0
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(parentP, "p")
                  Op.NewNode(child0, "c0")
                  Op.Replace(rootId, 0, [], ownedChild parentP)
                  Op.Replace(parentP, 0, [], ownedChild child0) ] }
        let! r0 = postChange client testFile setup
        Assert.Equal(HttpStatusCode.OK, r0.StatusCode)

        let childA = NodeId.New()
        let changeA =
            { id = 1
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(childA, "a")
                  Op.Replace(
                      parentP,
                      0,
                      ownedChild child0,
                      ownedChild childA) ] }
        let! rA = postChange client testFile changeA
        Assert.Equal(HttpStatusCode.OK, rA.StatusCode)

        let childB = NodeId.New()
        let changeB =
            { id = 1
              changeId = Guid.NewGuid()
              ops =
                [ Op.NewNode(childB, "b")
                  Op.Replace(
                      parentP,
                      0,
                      ownedChild child0,
                      ownedChild childB) ] }
        let! rB = postChange client testFile changeB
        Assert.Equal(HttpStatusCode.BadRequest, rB.StatusCode)
        let! errBody = rB.Content.ReadAsStringAsync()
        Assert.Contains("old span does not match", decodeErrorField errBody)

        let! json = getStateJson client testFile
        let g = decodeGraph json
        Assert.Equal(Revision 2, decodeRevision json)
        Assert.Equal<ChildNode list>(
            ownedChild childA,
            g.nodes.[parentP].children)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST duplicate changeId with stale revision stays idempotent``
    (backend: BackendKind)
    =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let cid = Guid.NewGuid()
        let childId = NodeId.New()
        let change =
            { id = 0
              changeId = cid
              ops =
                [ Op.NewNode(childId, "once")
                  Op.Replace(rootId, 0, [], ownedChild childId) ] }

        let! r1 = postChange client testFile change
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode)

        let other, otherId = changeAddChild rootId 1 "other"
        let! rOther = postChange client testFile other
        Assert.Equal(HttpStatusCode.OK, rOther.StatusCode)

        let! r2 = postChange client testFile change
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode)
        let! b2 = r2.Content.ReadAsStringAsync()
        Assert.Equal(Revision 2, decodeSuccessRevision b2)
        Assert.Equal<Guid list>([ cid ], decodeAckChangeIds b2)

        let! json = getStateJson client testFile
        let g = decodeGraph json
        Assert.Equal(Revision 2, decodeRevision json)
        Assert.Equal("once", g.nodes.[childId].text)
        Assert.Equal("other", g.nodes.[otherId].text)
    })

// ---- Change log + persistence tests (file backend only) ----

/// Submit a NewNode+Replace that adds a child with the given text under root.
let private addChild (client: HttpClient) (file: string) (rootId: NodeId) (rev: Revision) (text: string) = task {
    let change, childId = changeAddChild rootId rev.Value text
    let! resp = postChange client file change
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
    return childId
}

[<Fact>]
let ``POST changes creates log file`` () = task {
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir
    let! json0 = getStateJson client testFile
    let rootId = (decodeGraph json0).root

    let! _ = addChild client testFile rootId (Revision 0) "logged"

    let logPath = Bookkeeping.logPath tempDir
    Assert.True(File.Exists(logPath), "Log file should exist after first change")
    let content = readFileShared logPath
    Assert.Contains("logged", content)
}

[<Fact>]
let ``Snapshot writes amb artifacts asynchronously after change`` () = task {
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir
    let! json0 = getStateJson client testFile
    let rootId = (decodeGraph json0).root

    let! _ = addChild client testFile rootId (Revision 0) "snapped"

    do! Task.Delay(500)

    let ambPath = Path.Combine(tempDir, ".amb")
    Assert.True(File.Exists ambPath, ".amb snapshot should exist")
    let content = File.ReadAllText ambPath
    Assert.Contains("snapped", content)

    let metaPath = Bookkeeping.metaPath tempDir
    Assert.True(File.Exists metaPath, "Meta file should exist")
    let rev = Int32.Parse(File.ReadAllText(metaPath).Trim())
    Assert.Equal(1, rev)
}

[<Fact>]
let ``Log contains valid change data after POST`` () = task {
    let tempDir = newTempDir ()
    use client = createClientForDir tempDir
    let! json0 = getStateJson client testFile
    let rootId = (decodeGraph json0).root

    let! _ = addChild client testFile rootId (Revision 0) "logged-entry"

    let logPath = Bookkeeping.logPath tempDir
    Assert.True(File.Exists(logPath))
    let content = readFileShared logPath
    Assert.Contains("logged-entry", content)
    Assert.True(content.StartsWith("00000000"), "Log entry should have 8-digit padded change id prefix")
}

// ---- DB restart tests (DB backend only) ----

[<Fact>]
let ``DB is present in db mode`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    use client1 = createDbClient connStr
    let! json0 = getStateJson client1 testFile
    let rootId = (decodeGraph json0).root
    let change, _ = changeAddChild rootId 0 "probe"
    let! r1 = postChange client1 testFile change
    Assert.Equal(HttpStatusCode.OK, r1.StatusCode)
    let! rows = Database.getChangesAfterCheckpointRevision connStr 0 |> Async.AwaitTask
    Assert.Equal(1, rows.Length)
}

[<Fact>]
let ``DB rows persist after agent cache reset`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    use client1 = createDbClient connStr
    let! json0 = getStateJson client1 testFile
    let rootId = (decodeGraph json0).root
    let change, _ = changeAddChild rootId 0 "probe"
    let! r1 = postChange client1 testFile change
    Assert.Equal(HttpStatusCode.OK, r1.StatusCode)
    DatabaseSetup.resetAgentCacheForTest ()
    let! rows = Database.getChangesAfterCheckpointRevision connStr 0 |> Async.AwaitTask
    Assert.Equal(1, rows.Length)
}

[<Fact>]
let ``DB rows survive second server startup without DB reset`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    use client1 = createDbClient connStr
    let! json0 = getStateJson client1 testFile
    let rootId = (decodeGraph json0).root
    let change, _ = changeAddChild rootId 0 "probe"
    let! r1 = postChange client1 testFile change
    Assert.Equal(HttpStatusCode.OK, r1.StatusCode)
    let! rowsBefore = Database.getChangesAfterCheckpointRevision connStr 0 |> Async.AwaitTask
    Assert.Equal(1, rowsBefore.Length)
    use client2 = createDbClientNoReset connStr
    let! rowsAfter = Database.getChangesAfterCheckpointRevision connStr 0 |> Async.AwaitTask
    Assert.Equal(1, rowsAfter.Length)
}

[<Fact>]
let ``DB authority does not import files when database is empty`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let tempDir = newTempDir ()

    writeDocumentFiles tempDir (stateWithChild "from-file-bootstrap")

    use client = createDbClientForDir connStr tempDir
    let! json = getStateJson client testFile
    Assert.Equal(Revision 0, decodeRevision json)
    Assert.DoesNotContain((0, "from-file-bootstrap"), userTreeShape (decodeGraph json))
}

[<Fact>]
let ``file mode startup imports files when database is empty`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    let tempDir = newTempDir ()

    writeDocumentFiles tempDir (stateWithChild "from-file-bootstrap")

    use client = createFileModeWithDbClientForDir connStr tempDir
    let! json = getStateJson client testFile
    Assert.Equal(Revision 1, decodeRevision json)
    Assert.Contains((0, "from-file-bootstrap"), userTreeShape (decodeGraph json))
}

[<Fact>]
let ``DB restart preserves NodeIds`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    use client1 = createDbClient connStr
    let! json0 = getStateJson client1 testFile
    let rootId = (decodeGraph json0).root
    let change, childId = changeAddChild rootId 0 "db-restart-child"
    let! r1 = postChange client1 testFile change
    Assert.Equal(HttpStatusCode.OK, r1.StatusCode)
    use client2 = createDbClientNoReset connStr
    let! json2 = getStateJson client2 testFile
    let graph2 = decodeGraph json2
    Assert.Equal(Revision 1, decodeRevision json2)
    Assert.True(graph2.nodes.ContainsKey childId, "NodeId must survive restart")
    Assert.Equal("db-restart-child", graph2.nodes.[childId].text)
}

[<Fact>]
let ``DB restart does not replay log when projection is cleared`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    use client1 = createDbClient connStr
    let! json0 = getStateJson client1 testFile
    let rootId = (decodeGraph json0).root
    let change1, childId1 = changeAddChild rootId 0 "first"
    let! r1 = postChange client1 testFile change1
    Assert.Equal(HttpStatusCode.OK, r1.StatusCode)
    let change2, childId2 = changeAddChild rootId 1 "second"
    let! r2 = postChange client1 testFile change2
    Assert.Equal(HttpStatusCode.OK, r2.StatusCode)
    let! logRows = Database.getChangesAfterCheckpointRevision connStr 0 |> Async.AwaitTask
    Assert.Equal(2, logRows.Length)
    // Wipe projection tables only; the Change log is not a startup journal.
    use conn = new Npgsql.NpgsqlConnection(connStr)
    do! conn.OpenAsync()
    use cmd = conn.CreateCommand()
    cmd.CommandText <- "TRUNCATE node_children, nodes, graph RESTART IDENTITY CASCADE;"
    let! _ = cmd.ExecuteNonQueryAsync()
    use client2 = createDbClientNoReset connStr
    let! json2 = getStateJson client2 testFile
    Assert.Equal(Revision 0, decodeRevision json2)
    let graph2 = decodeGraph json2
    Assert.False(graph2.nodes.ContainsKey childId1)
    Assert.False(graph2.nodes.ContainsKey childId2)
}

[<Fact>]
let ``DB restart keeps duplicate changeId idempotent`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    use client1 = createDbClient connStr
    let! json0 = getStateJson client1 testFile
    let rootId = (decodeGraph json0).root
    let change, childId = changeAddChild rootId 0 "once-after-restart"
    let! r1 = postChange client1 testFile change
    Assert.Equal(HttpStatusCode.OK, r1.StatusCode)
    let! b1 = r1.Content.ReadAsStringAsync()
    Assert.Equal(Revision 1, decodeSuccessRevision b1)
    Assert.Equal<Guid list>([ change.changeId ], decodeAckChangeIds b1)

    use client2 = createDbClientNoReset connStr
    let! r2 = postChange client2 testFile change
    Assert.Equal(HttpStatusCode.OK, r2.StatusCode)
    let! b2 = r2.Content.ReadAsStringAsync()
    Assert.Equal(Revision 1, decodeSuccessRevision b2)
    Assert.Equal<Guid list>([ change.changeId ], decodeAckChangeIds b2)
    Assert.Equal<Change list>(
        (decodeSuccess b1).changes,
        (decodeSuccess b2).changes)

    let! json2 = getStateJson client2 testFile
    Assert.Equal(Revision 1, decodeRevision json2)
    let graph2 = decodeGraph json2
    Assert.True(graph2.nodes.ContainsKey childId, "duplicate change must not create a new node")
    Assert.Equal("once-after-restart", graph2.nodes.[childId].text)
    Assert.Equal<ChildNode list>(ownedChild childId, userRootChildren graph2)
}

[<Fact>]
let ``file restart keeps inverse Change in ChangeLog`` () = task {
    let tempDir = newTempDir ()
    use client1 = createClientForDir tempDir
    let! json0 = getStateJson client1 testFile
    let rootId = (decodeGraph json0).root
    let change, _ = changeAddChild rootId 0 "restart-inverse"
    let! createResp = postChange client1 testFile change
    Assert.Equal(HttpStatusCode.OK, createResp.StatusCode)
    let undo = Change.inverse (Revision 1) (Guid.NewGuid()) change
    let! undoResp = postChange client1 testFile undo
    Assert.Equal(HttpStatusCode.OK, undoResp.StatusCode)
    let! undoAck = undoResp.Content.ReadAsStringAsync()
    let confirmedUndo = Assert.Single((decodeSuccess undoAck).changes)
    use client2 = createClientForDir tempDir
    let! pollResponse = client2.GetAsync("/ambit/poll?rev=0")
    let! pollJson = pollResponse.Content.ReadAsStringAsync()
    let poll =
        decode
            ApiResponseSerialization.decodeChangeSuccessResponseDecoder
            pollJson
    Assert.Equal(2, poll.changes.Length)
    Assert.Equal(confirmedUndo, poll.changes.[1])
    let! stateJson = getStateJson client2 testFile
    Assert.Equal(Revision 2, decodeRevision stateJson)
}

[<Fact>]
let ``DB restart keeps inverse Change in ChangeLog`` () = task {
    let connStr = requireDbConnStr ()
    do! resetTestDatabase connStr
    use client1 = createDbClient connStr
    let! json0 = getStateJson client1 testFile
    let rootId = (decodeGraph json0).root
    let change, _ = changeAddChild rootId 0 "restart-inverse"
    let! createResp = postChange client1 testFile change
    Assert.Equal(HttpStatusCode.OK, createResp.StatusCode)
    let undo = Change.inverse (Revision 1) (Guid.NewGuid()) change
    let! undoResp = postChange client1 testFile undo
    Assert.Equal(HttpStatusCode.OK, undoResp.StatusCode)
    let! undoAck = undoResp.Content.ReadAsStringAsync()
    let confirmedUndo = Assert.Single((decodeSuccess undoAck).changes)
    use client2 = createDbClientNoReset connStr
    let! pollResponse = client2.GetAsync("/ambit/poll?rev=0")
    let! pollJson = pollResponse.Content.ReadAsStringAsync()
    let poll =
        decode
            ApiResponseSerialization.decodeChangeSuccessResponseDecoder
            pollJson
    Assert.Equal(2, poll.changes.Length)
    Assert.Equal(confirmedUndo, poll.changes.[1])
    let! stateJson = getStateJson client2 testFile
    Assert.Equal(Revision 2, decodeRevision stateJson)
}

[<Fact>]
let ``New server uses snapshot + log replay`` () = task {
    let tempDir = newTempDir ()
    use client1 = createClientForDir tempDir
    let! json0 = getStateJson client1 testFile
    let rootId = (decodeGraph json0).root

    let! _ = addChild client1 testFile rootId (Revision 0) "first"
    do! Task.Delay(500)

    let! json1 = getStateJson client1 testFile
    let rootId2 = (decodeGraph json1).root
    let root = (decodeGraph json1).nodes.[rootId2]
    let firstChildId = root.children.[0].id
    let! _ = postChange client1 testFile { id = 1; changeId = Guid.NewGuid();
        ops = [ Op.SetText(firstChildId, "first", "updated") ] }

    use client2 = createClientForDir tempDir
    let! json = getStateJson client2 testFile
    let graph = decodeGraph json
    let child = graph.nodes |> Map.toSeq |> Seq.map snd |> Seq.find (fun n -> n.text = "updated")
    Assert.Equal("updated", child.text)
    Assert.Equal(Revision 2, decodeRevision json)
}

let private postChangeOk (client: HttpClient) (change: Change) = task {
    let! resp = postChange client testFile change
    Assert.Equal(HttpStatusCode.OK, resp.StatusCode)
}

let private addNestedWorkspaceViaPost (client: HttpClient) = task {
    let wsId = NodeId.New()
    let innerId = NodeId.New()
    let c0 =
        { id = 0
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewSpecialNode(wsId, Workspace, "home")
              Op.Replace(Graph.workspacesId, 0, [], ownedChild wsId) ] }
    do! postChangeOk client c0
    let c1 =
        { id = 1
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(innerId, "inside-ws")
              Op.Replace(wsId, 0, [], ownedChild innerId) ] }
    do! postChangeOk client c1
    return wsId, innerId
}

[<Theory; MemberData(nameof backends)>]
let ``GET state default returns ROOT closure excluding nested workspace contents`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let! wsId, innerId = addNestedWorkspaceViaPost client
        let! json = getStateJson client testFile
        let graph = decodeGraph json
        Assert.True(graph.nodes.ContainsKey wsId)
        Assert.Equal(Unloaded, graph.nodes.[wsId].childrenStatus)
        Assert.False(graph.nodes.ContainsKey innerId)
    })

[<Theory; MemberData(nameof backends)>]
let ``GET state scope full returns canonical graph for tests`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let! wsId, innerId = addNestedWorkspaceViaPost client
        let! json = getStateJsonFull client testFile
        let graph = decodeGraph json
        Assert.True(graph.nodes.ContainsKey wsId)
        Assert.True(graph.nodes.ContainsKey innerId)
    })
