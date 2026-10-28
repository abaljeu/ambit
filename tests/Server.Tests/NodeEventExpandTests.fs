module Gambol.Server.Tests.NodeEventExpandTests

open System
open System.IO
open System.Threading.Tasks
open Microsoft.AspNetCore.Http.HttpResults
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

module Encode = Thoth.Json.Newtonsoft.Encode

let private outlineText = "outline body" + Environment.NewLine

let private readOnlyPostMessage =
    "Database persistence is unavailable; file fallback is read-only."

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private postGraph (host: MailboxHost) (ops: Op list) =
    let event = GraphOnlyChangePost.mint "seed" ops
    CoreMailboxApi.postGraphOnly host testCaller event
    |> Async.RunSynchronously
    |> requireOk "seed"
    |> ignore

let private stateOf (host: MailboxHost) =
    CoreMailboxApi.getState host
    |> Async.RunSynchronously
    |> requireOk "state"

let private changeOf (ops: Op list) : Ev =
    { id = EventId.zero
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = ""
      body = EventBody.Change ops }

let private postChange (handle: CoreChangesDoor) (ops: Op list) = task {
    let body =
        Encode.toString 0 (
            ApiResponseSerialization.encodeChangeRequest
                { events = [ changeOf ops ]
                  want = [] })
    let! result =
        Api.postEvents handle 10 20 body |> Async.StartAsTask
    match box result with
    | :? ContentHttpResult -> ()
    | other ->
        Assert.Fail($"postEvents failed: {other.GetType().Name}")
}

let private orderedEvents (host: MailboxHost) =
    let _ = stateOf host
    CoreMailboxApi.eventsSince host EventId.zero
    |> Async.RunSynchronously
    |> fun log ->
        log.events
        |> List.sortBy (fun event -> EventId.value event.id)

let private hasCase (host: MailboxHost) pred =
    orderedEvents host
    |> List.exists (fun event ->
        match event.body with
        | EventBody.Notice notice -> pred notice
        | _ -> false)

let private documentState host id =
    Node.documentStateEnum (stateOf host).graph.nodes.[id]

let private waitUntil pred timeoutMs = task {
    let start = DateTime.UtcNow
    while
        not (pred ())
        && (DateTime.UtcNow - start).TotalMilliseconds < float timeoutMs do
        do! Task.Delay 10
    return pred ()
}

let private isNotice notice = function
    | { body = EventBody.Notice found } -> found = notice
    | _ -> false

let private hasChange = function
    | { body = EventBody.Change _ } -> true
    | _ -> false

let private parsingIndex ordered nodeId =
    ordered
    |> List.findIndex (function
        | { body = EventBody.Notice (Notice.NodeParsing id) } ->
            id = nodeId
        | _ -> false)

let private assertParsingThenParsed host nodeId expectChange =
    let ordered = orderedEvents host
    let after = List.skip (parsingIndex ordered nodeId + 1) ordered
    let untilParsed =
        after
        |> List.takeWhile (function
            | { body = EventBody.Notice (Notice.NodeParsed id) } ->
                id <> nodeId
            | _ -> true)
    Assert.True(after.Length > untilParsed.Length)
    Assert.Equal(expectChange, List.exists hasChange untilParsed)

let private assertParsingThenFailed host nodeId message =
    let ordered = orderedEvents host
    let parsingAt = parsingIndex ordered nodeId
    let failedAt =
        ordered
        |> List.findIndex (function
            | { body = EventBody.Notice (Notice.ParseFailed (id, found)) } ->
                id = nodeId && found = message
            | _ -> false)
    let afterParsing = List.skip (parsingAt + 1) ordered
    let between = List.take (failedAt - parsingAt - 1) afterParsing
    Assert.True(parsingAt < failedAt)
    Assert.False(List.exists hasChange between)
    Assert.False(
        afterParsing
        |> List.exists (function
            | { body = EventBody.Notice (Notice.NodeParsed id) } ->
                id = nodeId
            | _ -> false))

let private startParseOn writable dataDir host =
    let push, consumer = ParseStack.create ()
    let parseHandle = CoreDoor.create writable host testCaller
    ParseThread.start
        { dataDir = dataDir
          consumer = consumer
          push = push
          getGraph = ParseThread.graphFromHost host
          postOps = ParseThread.postParseOps parseHandle
          markUnparsed = ignore
          finishParse =
            fun nodeId ->
                CoreMailboxApi.postNotice host (Notice.NodeParsed nodeId) }
    push

let private startParse dataDir host =
    startParseOn true dataDir host

let private admitHome dataDir =
    let host, handle = createAdmittedFile dataDir
    let wsId, wsOps =
        FileNodeOps.planCreateWorkspace (stateOf host).graph "home"
    postGraph host wsOps
    host, handle, wsId

let private withHome body = task {
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
        do! body dataDir host handle wsId
    finally
        CoreMailboxApi.dispose host
}

let private addOwned host parent plan name =
    let id, ops = plan (stateOf host).graph parent name
    postGraph host ops
    id

let private addFile host parent name =
    addOwned host parent FileNodeOps.planCreateOwnedFile name

let private addDirectory host parent name =
    addOwned host parent FileNodeOps.planCreateOwnedDirectory name

let private setState host id oldState newState =
    let notice =
        match oldState, newState with
        | Unparsed, Current -> Notice.NodeParsed id
        | Unparsed, PullingFile -> Notice.NodeUploading id
        | Current, Unparsed -> Notice.NodeUnparsed id
        | foundOld, foundNew ->
            failwith $"no notice sets {foundOld} to {foundNew}"
    CoreMailboxApi.postNotice host notice
    Assert.Equal(newState, documentState host id)

let private received host id =
    hasCase host (function
        | Notice.NodeReceived found -> found = id
        | _ -> false)

let private loggedUnparsed host id =
    hasCase host (function
        | Notice.NodeUnparsed found -> found = id
        | _ -> false)

let private homePath (dataDir: string) (rel: string) =
    Path.Combine(
        dataDir,
        "home",
        rel.Replace('/', Path.DirectorySeparatorChar))

let private placeFile (dataDir: string) (rel: string) (text: string) =
    let full = homePath dataDir rel
    Directory.CreateDirectory(Path.GetDirectoryName full) |> ignore
    File.WriteAllText(full, text)

let private ensureHomeDir dataDir rel =
    Directory.CreateDirectory(homePath dataDir rel) |> ignore

let private reconcile host handle dataDir changes =
    LazyLoadReconciliationServer.reconcileReceivedFiles
        { handle = handle
          dataDir = dataDir
          workspaceLabel = "home" }
        changes
    |> Async.StartAsTask

let private pullReceived host handle dataDir changes = task {
    let! pulled = reconcile host handle dataDir changes
    requireOk "pull" pulled |> ignore
}

let private readOnlyHandle host =
    CoreDoor.create false host testCaller

let private parsedMsg host id =
    hasCase host (function
        | Notice.NodeParsed found -> found = id
        | _ -> false)

let private anyNodeMsg host =
    hasCase host (function
        | Notice.NodeUploading _
        | Notice.NodeReceived _
        | Notice.NodeParsed _ -> true
        | _ -> false)

let private assertLogged host id newState present =
    Assert.Equal(newState, documentState host id)
    Assert.True(present)

let private assertReceivedUnparsed host id =
    Assert.True(loggedUnparsed host id)
    assertLogged host id Unparsed (received host id)

let private waitParsed host id = task {
    let! parsed =
        waitUntil
            (fun () ->
                parsedMsg host id
                && documentState host id = Current)
            2000
    Assert.True(parsed)
}

let private waitParseFailed host id = task {
    let! failed =
        waitUntil
            (fun () ->
                hasCase host (function
                    | Notice.ParseFailed (found, message) ->
                        found = id && message = readOnlyPostMessage
                    | _ -> false))
            2000
    Assert.True(failed)
}

let private parsedNotices host id =
    orderedEvents host
    |> List.filter (function
        | { body = EventBody.Notice (Notice.NodeParsed found) } ->
            found = id
        | _ -> false)

let private childIdNamed host parent name =
    let graph = (stateOf host).graph
    Graph.children graph parent
    |> List.find (fun child ->
        Filename.tryValue graph.nodes.[child.id].name = Some name)
    |> fun child -> child.id

let private onlyChild host parentId =
    let graph = (stateOf host).graph
    graph.nodes.[(Graph.children graph parentId).Head.id]

let private assertOutlineParsed host docsId =
    assertParsingThenParsed host docsId true
    Assert.Equal("outline body", (onlyChild host docsId).text)
    Assert.Equal(Current, documentState host docsId)

let private noticeAt ordered notice =
    List.findIndex (isNotice notice) ordered

let private parseChangeAt ordered =
    ordered
    |> List.findIndex (function
        | { commandName = "Parse"; body = EventBody.Change _ } -> true
        | _ -> false)

let private parsedAfterChange ordered changeAt nodeId =
    ordered
    |> List.mapi (fun index event -> index, event)
    |> List.find (fun (index, event) ->
        index > changeAt
        && match event.body with
           | EventBody.Notice (Notice.NodeParsed id) -> id = nodeId
           | _ -> false)
    |> fst

let private parseOps ordered =
    ordered
    |> List.collect (function
        | { commandName = "Parse"; body = EventBody.Change ops } -> ops
        | _ -> [])

let private parseChangesFollow parsingAt ordered =
    ordered
    |> List.mapi (fun index event -> index, event)
    |> List.forall (fun (index, event) ->
        match event with
        | { commandName = "Parse"; body = EventBody.Change _ } ->
            index > parsingAt
        | _ -> true)

let private assertReconcileParseOrder host docsId subId =
    let ordered = orderedEvents host
    let changeAt = parseChangeAt ordered
    let parsingAt = noticeAt ordered (Notice.NodeParsing docsId)
    let parsedAt = parsedAfterChange ordered changeAt docsId
    Assert.Contains(
        parseOps ordered,
        fun op ->
            match op with
            | Op.NewSpecialNode(_, _, "extra.txt") -> true
            | _ -> false)
    Assert.True(parseChangesFollow parsingAt ordered)
    Assert.True(noticeAt ordered (Notice.NodeUnparsed docsId) < parsingAt)
    Assert.True(parsingAt < changeAt)
    Assert.True(changeAt < noticeAt ordered (Notice.NodeUnparsed subId))
    Assert.True(noticeAt ordered (Notice.NodeUnparsed subId) < parsedAt)
    Assert.False(
        ordered
        |> List.skip (parsedAt + 1)
        |> List.exists (isNotice (Notice.NodeUnparsed docsId)))
    Assert.Equal(Current, documentState host docsId)
    Assert.Equal(Unparsed, documentState host subId)

let private parsingIds host =
    orderedEvents host
    |> List.choose (function
        | { body = EventBody.Notice (Notice.NodeParsing id) } -> Some id
        | _ -> None)

[<Fact>]
let ``git changed file posts NodeReceived and NodeUnparsed`` () =
    withHome (fun dataDir host handle wsId -> task {
        let fileId = addFile host wsId "note.txt"
        setState host fileId Unparsed Current
        placeFile dataDir "note.txt" "body"
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Modified "note.txt" ]
        assertReceivedUnparsed host fileId
    })

[<Fact(Skip = "pending documentState/Load spec")>]
let ``git load reconcile leaves an Unparsed parent Unparsed without NodeParsed`` () =
    withHome (fun dataDir host handle wsId -> task {
        let docsId = addDirectory host wsId "docs"
        Assert.Equal(Unparsed, documentState host docsId)
        placeFile dataDir "docs/extra.txt" "extra"
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Added "docs/extra.txt" ]
        let extraId = childIdNamed host docsId "extra.txt"
        Assert.Empty(parsedNotices host docsId)
        Assert.Empty(parsedNotices host wsId)
        Assert.Empty(parsedNotices host extraId)
        Assert.Equal(Unparsed, documentState host docsId)
        Assert.Equal(Unparsed, documentState host wsId)
        Assert.Equal(Unparsed, documentState host extraId)
    })

[<Fact>]
let ``git Load reconcile posts NodeUnparsed then the Change then NodeParsed`` () =
    withHome (fun dataDir host handle wsId -> task {
        let docsId = addDirectory host wsId "docs"
        setState host docsId Unparsed Current
        let subId = addDirectory host docsId "sub"
        setState host subId Unparsed Current
        placeFile dataDir "docs/.amb" outlineText
        placeFile dataDir "docs/sub/extra.txt" "extra"
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Modified "docs/.amb"
                  LazyLoadReconciliation.Added "docs/sub/extra.txt" ]
        assertReconcileParseOrder host docsId subId
    })

[<Fact>]
let ``git Load reconcile posts no NodeUnparsed for a PullingFile container`` () =
    withHome (fun dataDir host handle wsId -> task {
        let docsId = addDirectory host wsId "docs"
        setState host docsId Unparsed Current
        CoreMailboxApi.postNotice host (Notice.NodeUploading docsId)
        Assert.Equal(PullingFile, documentState host docsId)
        placeFile dataDir "docs/extra.txt" "extra"
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Added "docs/extra.txt" ]
        Assert.False(loggedUnparsed host docsId)
        Assert.Equal(PullingFile, documentState host docsId)
    })

[<Fact>]
let ``git load reconcile posts NodeParsing before and NodeParsed after the dir-info parse`` () =
    withHome (fun dataDir host handle wsId -> task {
        let docsId = addDirectory host wsId "docs"
        setState host docsId Unparsed Current
        placeFile dataDir "docs/.amb" outlineText
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Modified "docs/.amb" ]
        assertOutlineParsed host docsId
    })

[<Fact>]
let ``A git Load reconcile whose Change fails posts ParseFailed for each parsedDirIds entry`` () =
    withHome (fun dataDir host _ wsId -> task {
        let docsId = addDirectory host wsId "docs"
        setState host docsId Unparsed Current
        let subId = addDirectory host docsId "sub"
        setState host subId Unparsed Current
        placeFile dataDir "docs/.amb" outlineText
        placeFile dataDir "docs/sub/.amb" ("sub body" + Environment.NewLine)
        let! pulled =
            reconcile
                host
                (readOnlyHandle host)
                dataDir
                [ LazyLoadReconciliation.Modified "docs/.amb"
                  LazyLoadReconciliation.Modified "docs/sub/.amb" ]
        match pulled with
        | Ok _ -> Assert.Fail("expected the Change post to fail")
        | Error message ->
            Assert.Equal(readOnlyPostMessage, message)
        let ids = parsingIds host
        Assert.Equal<NodeId list>(
            List.sort [ docsId; subId ],
            List.sort ids)
        ids
        |> List.iter (fun id ->
            assertParsingThenFailed host id readOnlyPostMessage
            Assert.Equal(Unparsed, documentState host id))
    })

[<Fact(Skip = "pending documentState/Load spec")>]
let ``git load reconcile parses an Unparsed directory file and NodeParsed sets Current`` () =
    withHome (fun dataDir host handle wsId -> task {
        let docsId = addDirectory host wsId "docs"
        Assert.Equal(Unparsed, documentState host docsId)
        placeFile dataDir "docs/.amb" outlineText
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Modified "docs/.amb" ]
        assertOutlineParsed host docsId
    })

[<Fact>]
let ``git load reconcile notices a renamed directory file and does not rename it`` () =
    withHome (fun dataDir host handle wsId -> task {
        let docsId = addDirectory host wsId "docs"
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Renamed("docs/.amb", "moved/.amb") ]
        Assert.Equal(Current, documentState host docsId)
        Assert.Equal(
            Some "docs",
            Filename.tryValue (stateOf host).graph.nodes.[docsId].name)
        Assert.True(parsedMsg host docsId)
        Assert.False(
            hasCase host (function
                | Notice.NodeParsing id -> id = docsId
                | _ -> false))
    })

[<Fact>]
let ``modified directory file with no text posts NodeUnparsed`` () =
    withHome (fun dataDir host handle wsId -> task {
        let docsId = addDirectory host wsId "docs"
        setState host wsId Unparsed Current
        setState host docsId Unparsed Current
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Modified ".amb"
                  LazyLoadReconciliation.Modified "docs/.amb" ]
        Assert.True(loggedUnparsed host wsId)
        Assert.True(loggedUnparsed host docsId)
        Assert.Equal(Unparsed, documentState host wsId)
        Assert.Equal(Unparsed, documentState host docsId)
    })

[<Fact>]
let ``git load reconcile posts NodeUnparsed for a modified file`` () =
    withHome (fun dataDir host handle wsId -> task {
        let fileId = addFile host wsId "note.txt"
        setState host wsId Unparsed Current
        setState host fileId Unparsed Current
        let renamedId = addFile host wsId "old.txt"
        setState host renamedId Unparsed Current
        placeFile dataDir "note.txt" "body"
        let changes = [
            LazyLoadReconciliation.Modified "note.txt"
            LazyLoadReconciliation.Renamed("old.txt", "renamed.txt")
        ]
        let planned =
            LazyLoadReconciliationReport.planReceivedFiles
                (stateOf host).graph
                "home"
                changes
                Map.empty
            |> requireOk "plan"
        Assert.Equal<NodeId list>(
            [ fileId; renamedId ],
            planned.unparsedIds)
        do! pullReceived host handle dataDir changes
        Assert.True(loggedUnparsed host fileId)
        Assert.True(loggedUnparsed host renamedId)
    })

[<Fact>]
let ``git load reconcile reports a modified kind conflict and posts NodeUnparsed for the other file`` () =
    withHome (fun dataDir host handle wsId -> task {
        let _docsId = addDirectory host wsId "docs"
        let fileId = addFile host wsId "note.txt"
        setState host fileId Unparsed Current
        let changes = [
            LazyLoadReconciliation.Modified "docs"
            LazyLoadReconciliation.Modified "note.txt"
        ]
        let! pulled = reconcile host handle dataDir changes
        let failures = requireOk "pull" pulled
        Assert.True(loggedUnparsed host fileId)
        Assert.Contains(
            failures,
            fun failure ->
                failure.path = "docs"
                && failure.message.Contains("kind conflict"))
        let! loaded =
            GithubTransportActor.finishLoadPull
                { handle = handle
                  dataDir = dataDir
                  workspaceLabel = "home" }
                wsId
                changes
            |> Async.StartAsTask
        requireOk "load" loaded |> ignore
    })

[<Fact>]
let ``reconcile emits NodeReceived`` () =
    withHome (fun dataDir host handle wsId -> task {
        let fileId = addFile host wsId "note.txt"
        setState host fileId Unparsed PullingFile
        placeFile dataDir "note.txt" "body"
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Modified "note.txt" ]
        let node = (stateOf host).graph.nodes.[fileId]
        Assert.True(received host fileId)
        Assert.Equal(Unparsed, documentState host fileId)
        Assert.False(node.documentState.pullingFile)
        Assert.Equal(
            PersistState.Persisted,
            node.documentState.persistState)
    })

[<Fact>]
let ``renamed file posts NodeReceived`` () =
    withHome (fun dataDir host handle wsId -> task {
        let fileId = addFile host wsId "note.txt"
        setState host wsId Unparsed Current
        setState host fileId Unparsed Current
        let planned =
            LazyLoadReconciliationReport.planReceivedFiles
                (stateOf host).graph
                "home"
                [ LazyLoadReconciliation.Renamed("note.txt", "renamed.txt") ]
                Map.empty
            |> requireOk "plan"
        Assert.Contains(fileId, planned.renamedReceivedIds)
        placeFile dataDir "note.txt" "body"
        File.Move(
            homePath dataDir "note.txt",
            homePath dataDir "renamed.txt")
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Renamed("note.txt", "renamed.txt") ]
        assertReceivedUnparsed host fileId
    })

[<Fact>]
let ``moved file posts NodeReceived`` () =
    withHome (fun dataDir host handle wsId -> task {
        let docsId = addDirectory host wsId "docs"
        let otherId = addDirectory host wsId "other"
        setState host wsId Unparsed Current
        setState host docsId Unparsed Current
        setState host otherId Unparsed Current
        let fileId = addFile host docsId "note.txt"
        setState host fileId Unparsed Current
        placeFile dataDir "docs/note.txt" "body"
        ensureHomeDir dataDir "other"
        File.Move(
            homePath dataDir "docs/note.txt",
            homePath dataDir "other/note.txt")
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Renamed(
                    "docs/note.txt", "other/note.txt") ]
        assertReceivedUnparsed host fileId
    })

[<Fact>]
let ``renamed and modified file posts NodeReceived`` () =
    withHome (fun dataDir host handle wsId -> task {
        let fileId = addFile host wsId "note.txt"
        setState host wsId Unparsed Current
        setState host fileId Unparsed Current
        placeFile dataDir "renamed.txt" "changed body"
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Renamed("note.txt", "renamed.txt") ]
        assertReceivedUnparsed host fileId
    })

[<Fact(Skip = "pending documentState/Load spec")>]
let ``planParseFile posts NodeParsing, the Change, then NodeParsed`` () =
    withHome (fun dataDir host _ wsId -> task {
        let push = startParse dataDir host
        let fileId = addFile host wsId "note.txt"
        placeFile dataDir "note.txt" "HELLO\n"
        push fileId
        do! waitParsed host fileId
        assertParsingThenParsed host fileId true
        Assert.Equal("HELLO", (onlyChild host fileId).text)
        Assert.Equal(Current, documentState host fileId)
    })

[<Fact(Skip = "pending documentState/Load spec")>]
let ``A file parse whose Change fails posts NodeParsing, then ParseFailed, and no NodeParsed`` () =
    withHome (fun dataDir host _ wsId -> task {
        let push = startParseOn false dataDir host
        let fileId = addFile host wsId "note.txt"
        let before = (stateOf host).graph.nodes.[fileId].documentState
        placeFile dataDir "note.txt" "HELLO\n"
        push fileId
        do! waitParseFailed host fileId
        assertParsingThenFailed host fileId readOnlyPostMessage
        Assert.Equal(
            before,
            (stateOf host).graph.nodes.[fileId].documentState)
        Assert.Empty(Graph.children (stateOf host).graph fileId)
    })

[<Fact(Skip = "pending documentState/Load spec")>]
let ``A directory parse whose Change fails posts NodeParsing, then ParseFailed, and no NodeParsed`` () =
    withHome (fun dataDir host _ wsId -> task {
        let push = startParseOn false dataDir host
        let dirId = addDirectory host wsId "docs"
        let before = (stateOf host).graph.nodes.[dirId].documentState
        placeFile dataDir "docs/extra.txt" "EXTRA\n"
        push dirId
        do! waitParseFailed host dirId
        assertParsingThenFailed host dirId readOnlyPostMessage
        Assert.Equal(
            before,
            (stateOf host).graph.nodes.[dirId].documentState)
        let graph = (stateOf host).graph
        let names =
            Graph.children graph dirId
            |> List.map (fun child ->
                Filename.tryValue graph.nodes.[child.id].name)
        Assert.DoesNotContain(Some "extra.txt", names)
    })

[<Fact(Skip = "pending documentState/Load spec")>]
let ``planDirectoryReconcile posts NodeParsing, the Change, then NodeParsed`` () =
    withHome (fun dataDir host _ wsId -> task {
        let push = startParse dataDir host
        let dirId = addDirectory host wsId "docs"
        placeFile dataDir "docs/extra.txt" "EXTRA\n"
        push dirId
        do! waitParsed host dirId
        assertParsingThenParsed host dirId true
        Assert.Equal(Some "extra.txt", (onlyChild host dirId).name |> Filename.tryValue)
    })

[<Fact(Skip = "pending documentState/Load spec")>]
let ``empty directory plan posts NodeParsing then NodeParsed`` () =
    withHome (fun dataDir host _ wsId -> task {
        let push = startParse dataDir host
        let dirId = addDirectory host wsId "docs"
        ensureHomeDir dataDir "docs"
        push dirId
        do! waitParsed host dirId
        assertParsingThenParsed host dirId false
    })

[<Fact(Skip = "pending documentState/Load spec")>]
let ``planDirectoryReconcile posts NodeParsed and leaves Current`` () =
    withHome (fun dataDir host _ wsId -> task {
        let push = startParse dataDir host
        let dirId = addDirectory host wsId "docs"
        ensureHomeDir dataDir "docs"
        Assert.False(parsedMsg host dirId)
        push dirId
        do! waitParsed host dirId
        CoreMailboxApi.postNotice host (Notice.NodeParsed dirId)
        Assert.Equal(Current, documentState host dirId)
        Assert.True(parsedMsg host dirId)
    })

[<Fact>]
let ``unparsed directory with a normal child drops a missing member`` () =
    withHome (fun dataDir host _ wsId -> task {
        let push = startParse dataDir host
        let dirId = addDirectory host wsId "docs"
        setState host dirId Unparsed Current
        let normalId = NodeId.New()
        let kids = Graph.children (stateOf host).graph dirId
        postGraph
            host
            [ Op.NewNode(normalId, "section")
              ChildListWire.append
                  dirId
                  kids
                  [ ChildNode.owner normalId ] ]
        let fileId = addFile host dirId "gone.txt"
        setState host dirId Current Unparsed
        ensureHomeDir dataDir "docs"
        push dirId
        let! dropped =
            waitUntil
                (fun () ->
                    documentState host dirId = Current
                    && Graph.children (stateOf host).graph dirId
                       |> List.forall (fun child -> child.id <> fileId))
                2000
        Assert.True(dropped)
        Assert.True(parsedMsg host dirId)
        Assert.Contains(
            normalId,
            Graph.children (stateOf host).graph dirId
            |> List.map (fun child -> child.id))
    })

[<Fact>]
let ``ParseFinished is NodeParsed`` () =
    withHome (fun _ host _ wsId -> task {
        let fileId = addFile host wsId "note.txt"
        CoreMailboxApi.postNotice host (Notice.NodeParsed fileId)
        assertLogged host fileId Current (parsedMsg host fileId)
    })

[<Fact(Skip = "pending documentState/Load spec")>]
let ``non document state change posts no Node Notice`` () =
    withHome (fun _ host handle wsId -> task {
        let fileId = addFile host wsId "note.txt"
        let oldStamp = (stateOf host).graph.nodes.[fileId].updateTime
        let newStamp = DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        do!
            postChange
                handle
                [ Op.SetUpdateTime(fileId, oldStamp, newStamp) ]
        let after = (stateOf host).graph.nodes.[fileId]
        Assert.Equal(NodeUpdateTime.toDbPrecision newStamp, after.updateTime)
        Assert.Equal(Unparsed, documentState host fileId)
        Assert.False(anyNodeMsg host)
    })
