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

let private decodeAckChangeIds json =
    Thoth.Json.Core.Decode.object (fun get ->
        get.Required.Field "ackedChangeIds" (Thoth.Json.Core.Decode.list Thoth.Json.Core.Decode.guid))
    |> decode <| json

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
    [ { ref = Ownership.Owner; id = id } ]

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
    File.WriteAllText(Path.Combine(tempDir, $"{testFile}.meta"), string state.revision.Value)
    File.WriteAllText(Path.Combine(tempDir, $"{testFile}.log"), "")

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
        Assert.Equal(Revision 2, decodeRevision postBody)
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
                  Op.Replace(rootId, 0, [], [ { ref = Ownership.Owner; id = childId } ]) ] }

        let! resp = postChange client testFile change
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode)

        let! postBody = resp.Content.ReadAsStringAsync()
        Assert.Equal(Revision 1, decodeRevision postBody)
        Assert.Equal<Guid list>([ change.changeId ], decodeAckChangeIds postBody)

        let! json = getStateJson client testFile
        let graph = decodeGraph json
        Assert.Equal(1, userNodeCount graph)
        Assert.Equal<ChildNode list>([ { ref = Ownership.Owner; id = childId } ], userRootChildren graph)
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
        Assert.Equal(Revision 2, decodeRevision postBody2)

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
        Assert.Equal(Revision 2, decodeRevision postBody)
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
        Assert.Equal(Revision 1, decodeRevision b1)
        Assert.Equal<Guid list>([ cid ], decodeAckChangeIds b1)

        let! r2 = postChange client testFile change
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode)
        let! b2 = r2.Content.ReadAsStringAsync()
        Assert.DoesNotContain("graph", b2, StringComparison.Ordinal)
        Assert.Equal(Revision 1, decodeRevision b2)
        Assert.Equal<Guid list>([ cid ], decodeAckChangeIds b2)

        let! json = getStateJson client testFile
        Assert.Equal(Revision 1, decodeRevision json)
        Assert.Equal("once", (decodeGraph json).nodes.[childId].text)
    })

[<Theory; MemberData(nameof backends)>]
let ``POST with wrong base revision returns 400`` (backend: BackendKind) =
    withClient backend (fun client -> task {
        let! json0 = getStateJson client testFile
        let rootId = (decodeGraph json0).root
        let change =
            { id = 5
              changeId = Guid.NewGuid()
              ops = [ Op.SetText(rootId, "", "x") ] }

        let! resp = postChange client testFile change
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode)
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

    let logPath = Path.Combine(tempDir, $"{testFile}.log")
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

    let metaPath = Path.Combine(tempDir, testFile + ".meta")
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

    let logPath = Path.Combine(tempDir, $"{testFile}.log")
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
let ``DB restart replays log when projection is cleared`` () = task {
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
    // Wipe projection tables only (not changes) to force full log replay on restart
    use conn = new Npgsql.NpgsqlConnection(connStr)
    do! conn.OpenAsync()
    use cmd = conn.CreateCommand()
    cmd.CommandText <- "TRUNCATE node_children, nodes, graph RESTART IDENTITY CASCADE;"
    let! _ = cmd.ExecuteNonQueryAsync()
    use client2 = createDbClientNoReset connStr
    let! json2 = getStateJson client2 testFile
    Assert.Equal(Revision 2, decodeRevision json2)
    let graph2 = decodeGraph json2
    Assert.True(graph2.nodes.ContainsKey childId1, "first child must survive restart")
    Assert.True(graph2.nodes.ContainsKey childId2, "second child must survive restart")
    Assert.Equal("first", graph2.nodes.[childId1].text)
    Assert.Equal("second", graph2.nodes.[childId2].text)
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
    Assert.Equal(Revision 1, decodeRevision b1)
    Assert.Equal<Guid list>([ change.changeId ], decodeAckChangeIds b1)

    use client2 = createDbClientNoReset connStr
    let! r2 = postChange client2 testFile change
    Assert.Equal(HttpStatusCode.OK, r2.StatusCode)
    let! b2 = r2.Content.ReadAsStringAsync()
    Assert.Equal(Revision 1, decodeRevision b2)
    Assert.Equal<Guid list>([ change.changeId ], decodeAckChangeIds b2)

    let! json2 = getStateJson client2 testFile
    Assert.Equal(Revision 1, decodeRevision json2)
    let graph2 = decodeGraph json2
    Assert.True(graph2.nodes.ContainsKey childId, "duplicate change must not create a new node")
    Assert.Equal("once-after-restart", graph2.nodes.[childId].text)
    Assert.Equal<ChildNode list>(ownedChild childId, userRootChildren graph2)
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
