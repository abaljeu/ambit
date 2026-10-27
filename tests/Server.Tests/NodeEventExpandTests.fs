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

let private hasCase (host: MailboxHost) pred =
    let _ = stateOf host
    let log =
        CoreMailboxApi.eventsSince host EventId.zero
        |> Async.RunSynchronously
    log.events
    |> List.exists (fun event ->
        match event.body with
        | EventBody.Notice notice -> pred notice
        | _ -> false)

let private documentState host id =
    Node.documentStateEnum (stateOf host).graph.nodes.[id]

let private setsUnparsed id = function
    | Op.SetDocumentState(found, _, Unparsed) when found = id -> true
    | _ -> false

let private containsSet (ops: Op list) id newState =
    ops
    |> List.exists (function
        | Op.SetDocumentState(found, _, next) ->
            found = id && next = newState
        | _ -> false)

let private changeOps (event: Ev) =
    match event.body with
    | EventBody.Change ops -> ops
    | _ -> []

let private fileNamed (host: MailboxHost) parentId name =
    Graph.children (stateOf host).graph parentId
    |> List.pick (fun child ->
        let node = (stateOf host).graph.nodes.[child.id]
        match Filename.tryValue node.name with
        | Some found when found = name -> Some child.id
        | _ -> None)

let private waitUntil pred timeoutMs = task {
    let start = DateTime.UtcNow
    while
        not (pred ())
        && (DateTime.UtcNow - start).TotalMilliseconds < float timeoutMs do
        do! Task.Delay 10
    return pred ()
}

let private startParse dataDir host =
    let push, consumer = ParseStack.create ()
    let parseHandle = CoreDoor.create true host testCaller
    ParseThread.start
        { dataDir = dataDir
          consumer = consumer
          push = push
          getGraph = ParseThread.graphFromHost host
          postOps = ParseThread.postParseOps parseHandle
          markUnparsed = ignore
          beginParse =
            fun nodeId ->
                CoreMailboxApi.postNotice host (Notice.NodeParsing nodeId)
          finishParse =
            fun nodeId ->
                CoreMailboxApi.postNotice host (Notice.NodeParsed nodeId) }
    push

let private admitHome dataDir =
    let host, handle = createAdmittedFile dataDir
    let wsId, wsOps =
        FileNodeOps.planCreateWorkspace (stateOf host).graph "home"
    postGraph host wsOps
    host, handle, wsId

let private addOwned host parent plan name =
    let id, ops = plan (stateOf host).graph parent name
    postGraph host ops
    id

let private addFile host parent name =
    addOwned host parent FileNodeOps.planCreateOwnedFile name

let private addDirectory host parent name =
    addOwned host parent FileNodeOps.planCreateOwnedDirectory name

let private setState host id oldState newState =
    postGraph host [ Op.SetDocumentState(id, oldState, newState) ]

let private uploading host id =
    hasCase host (function
        | Notice.NodeUploading found -> found = id
        | _ -> false)

let private received host id =
    hasCase host (function
        | Notice.NodeReceived found -> found = id
        | _ -> false)

let private loggedUnparsed host id =
    hasCase host (function
        | Notice.NodeUnparsed found -> found = id
        | _ -> false)

let private placeFile (dataDir: string) (rel: string) (text: string) =
    let full =
        Path.Combine(
            dataDir,
            "home",
            rel.Replace('/', Path.DirectorySeparatorChar))
    Directory.CreateDirectory(Path.GetDirectoryName full) |> ignore
    File.WriteAllText(full, text)

let private pullReceived host handle dataDir changes = task {
    let! pulled =
        LazyLoadReconciliationServer.reconcileReceivedFiles
            { handle = handle
              dataDir = dataDir
              workspaceLabel = "home" }
            changes
        |> Async.StartAsTask
    requireOk "pull" pulled |> ignore
}

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

let private wroteSet host id newState =
    let log =
        CoreMailboxApi.eventsSince host EventId.zero
        |> Async.RunSynchronously
    log.events
    |> List.exists (fun event ->
        match event.body with
        | EventBody.Change ops -> containsSet ops id newState
        | _ -> false)

let private assertBeside host id newState ops =
    Assert.True(containsSet ops id newState)
    Assert.True(wroteSet host id newState)
    Assert.Equal(newState, documentState host id)

let private assertLogged host id newState present =
    Assert.Equal(newState, documentState host id)
    Assert.True(present)

let private waitParsed host id = task {
    let! parsed =
        waitUntil
            (fun () ->
                parsedMsg host id
                && documentState host id = Current
                && not (wroteSet host id Current))
            2000
    Assert.True(parsed)
}

[<Fact(Skip = "128/129: upload posts NodeUploading beside SetDocumentState pending")>]
let ``upload posts NodeUploading beside SetDocumentState`` () = task {
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
        let planned =
            WorkspaceUploadStructure.planStubOps
                (stateOf host).graph
                "home"
                [ { relative = "note.txt"; isDirectory = false } ]
            |> requireOk "stubs"
        Assert.False(anyNodeMsg host)
        do! postChange handle planned
        let noteId = fileNamed host wsId "note.txt"
        assertBeside host noteId PullingFile planned
        Assert.True(uploading host noteId)
        let currentId = addFile host wsId "current.txt"
        setState host currentId Unparsed Current
        let plainId = addFile host wsId "plain.txt"
        let marks =
            [ Op.SetDocumentState(currentId, Current, PullingFile)
              Op.SetDocumentState(plainId, Unparsed, PullingFile) ]
        do! postChange handle marks
        assertBeside host currentId PullingFile marks
        Assert.True(uploading host currentId)
        assertBeside host plainId PullingFile marks
        Assert.True(uploading host plainId)
    finally
        CoreMailboxApi.dispose host
}

[<Fact(Skip = "128/129: upload emits NodeUploading pending")>]
let ``upload emits NodeUploading`` () = task {
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
        let planned =
            WorkspaceUploadStructure.planStubOps
                (stateOf host).graph
                "home"
                [ { relative = "note.txt"; isDirectory = false } ]
            |> requireOk "stubs"
        do! postChange handle planned
        let noteId = fileNamed host wsId "note.txt"
        let node = (stateOf host).graph.nodes.[noteId]
        Assert.True(uploading host noteId)
        Assert.True(node.documentState.pullingFile)
        Assert.Equal(PullingFile, documentState host noteId)
        Assert.Equal(
            PersistState.Unpersisted,
            node.documentState.persistState)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``git changed file posts NodeReceived and NodeUnparsed`` () = task {
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
        let fileId = addFile host wsId "note.txt"
        setState host fileId Unparsed Current
        let home = Path.Combine(dataDir, "home")
        Directory.CreateDirectory(home) |> ignore
        File.WriteAllText(Path.Combine(home, "note.txt"), "body")
        let! pulled =
            LazyLoadReconciliationServer.reconcileReceivedFiles
                { handle = handle
                  dataDir = dataDir
                  workspaceLabel = "home" }
                [ LazyLoadReconciliation.Modified "note.txt" ]
            |> Async.StartAsTask
        requireOk "pull" pulled |> ignore
        Assert.False(wroteSet host fileId Unparsed)
        Assert.True(loggedUnparsed host fileId)
        assertLogged host fileId Unparsed (received host fileId)
    finally
        CoreMailboxApi.dispose host
}

let private parsedNotices host id =
    let _ = stateOf host
    let log =
        CoreMailboxApi.eventsSince host EventId.zero
        |> Async.RunSynchronously
    log.events
    |> List.choose (fun event ->
        match event.body with
        | EventBody.Notice (Notice.NodeParsed found) when found = id ->
            Some event
        | _ -> None)

[<Fact>]
let ``git load reconcile leaves an Unparsed parent Unparsed without NodeParsed`` () =
    task {
        let dataDir = newTempDir ()
        let host, handle, wsId = admitHome dataDir
        try
            let docsId = addDirectory host wsId "docs"
            Assert.Equal(Unparsed, documentState host docsId)
            let home = Path.Combine(dataDir, "home")
            let docs = Path.Combine(home, "docs")
            Directory.CreateDirectory(docs) |> ignore
            File.WriteAllText(Path.Combine(docs, "extra.txt"), "extra")
            do!
                pullReceived
                    host
                    handle
                    dataDir
                    [ LazyLoadReconciliation.Added "docs/extra.txt" ]
            let graph = (stateOf host).graph
            let extraId =
                Graph.children graph docsId
                |> List.find (fun child ->
                    Filename.tryValue graph.nodes.[child.id].name = Some "extra.txt")
                |> fun child -> child.id
            Assert.Empty(parsedNotices host docsId)
            Assert.Empty(parsedNotices host wsId)
            Assert.Empty(parsedNotices host extraId)
            Assert.Equal(Unparsed, documentState host docsId)
            Assert.Equal(Unparsed, documentState host wsId)
            Assert.Equal(Unparsed, documentState host extraId)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``git Load reconcile posts NodeUnparsed then the Change then NodeParsed`` () =
    task {
        let dataDir = newTempDir ()
        let host, handle, wsId = admitHome dataDir
        try
            let docsId = addDirectory host wsId "docs"
            setState host docsId Unparsed Current
            let subId = addDirectory host docsId "sub"
            setState host subId Unparsed Current
            placeFile
                dataDir
                "docs/.amb"
                ("outline body" + Environment.NewLine)
            placeFile dataDir "docs/sub/extra.txt" "extra"
            do!
                pullReceived
                    host
                    handle
                    dataDir
                    [ LazyLoadReconciliation.Modified "docs/.amb"
                      LazyLoadReconciliation.Added "docs/sub/extra.txt" ]
            let ordered =
                (stateOf host) |> ignore
                CoreMailboxApi.eventsSince host EventId.zero
                |> Async.RunSynchronously
                |> fun log ->
                    log.events
                    |> List.sortBy (fun event -> EventId.value event.id)
            let indexOf notice =
                ordered
                |> List.findIndex (function
                    | { body = EventBody.Notice found } -> found = notice
                    | _ -> false)
            let changeAt =
                ordered
                |> List.findIndex (function
                    | { commandName = "Parse"; body = EventBody.Change _ } ->
                        true
                    | _ -> false)
            let unparsedAt = indexOf (Notice.NodeUnparsed docsId)
            let parsingAt = indexOf (Notice.NodeParsing docsId)
            let childAt = indexOf (Notice.NodeUnparsed subId)
            let parsedAt =
                ordered
                |> List.mapi (fun i event -> i, event)
                |> List.find (fun (i, event) ->
                    i > changeAt
                    && match event.body with
                       | EventBody.Notice (Notice.NodeParsed id) ->
                           id = docsId
                       | _ -> false)
                |> fst
            let parseOps =
                ordered
                |> List.collect (fun event ->
                    match event with
                    | { commandName = "Parse"; body = EventBody.Change ops } ->
                        ops
                    | _ -> [])
            Assert.Contains(
                parseOps,
                fun op ->
                    match op with
                    | Op.NewSpecialNode(_, _, "extra.txt") -> true
                    | _ -> false)
            Assert.True(
                ordered
                |> List.mapi (fun i event -> i, event)
                |> List.forall (fun (i, event) ->
                    match event with
                    | { commandName = "Parse"; body = EventBody.Change _ } ->
                        i > parsingAt
                    | _ -> true))
            Assert.True(unparsedAt < parsingAt)
            Assert.True(parsingAt < changeAt)
            Assert.True(changeAt < childAt)
            Assert.True(childAt < parsedAt)
            Assert.False(
                ordered
                |> List.skip (parsedAt + 1)
                |> List.exists (function
                    | { body = EventBody.Notice (Notice.NodeUnparsed id) } ->
                        id = docsId
                    | _ -> false))
            Assert.Equal(Current, documentState host docsId)
            Assert.Equal(Unparsed, documentState host subId)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``git Load reconcile posts no NodeUnparsed for a PullingFile container`` () =
    task {
        let dataDir = newTempDir ()
        let host, handle, wsId = admitHome dataDir
        try
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
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``git load reconcile posts NodeParsing before and NodeParsed after the dir-info parse`` () =
    task {
        let dataDir = newTempDir ()
        let host, handle, wsId = admitHome dataDir
        try
            let docsId = addDirectory host wsId "docs"
            setState host docsId Unparsed Current
            placeFile
                dataDir
                "docs/.amb"
                ("outline body" + Environment.NewLine)
            do!
                pullReceived
                    host
                    handle
                    dataDir
                    [ LazyLoadReconciliation.Modified "docs/.amb" ]
            let ordered =
                (stateOf host) |> ignore
                CoreMailboxApi.eventsSince host EventId.zero
                |> Async.RunSynchronously
                |> fun log ->
                    log.events
                    |> List.sortBy (fun event -> EventId.value event.id)
            let parsingAt =
                ordered
                |> List.findIndex (function
                    | { body = EventBody.Notice (Notice.NodeParsing id) } ->
                        id = docsId
                    | _ -> false)
            let after = List.skip (parsingAt + 1) ordered
            let untilParsed =
                after
                |> List.takeWhile (function
                    | { body = EventBody.Notice (Notice.NodeParsed id) } ->
                        id <> docsId
                    | _ -> true)
            let parsedFollows =
                after.Length > untilParsed.Length
            let changeBetween =
                untilParsed
                |> List.exists (function
                    | { body = EventBody.Change _ } -> true
                    | _ -> false)
            Assert.True(parsedFollows)
            Assert.True(changeBetween)
            let graph = (stateOf host).graph
            let childId = (Graph.children graph docsId).Head.id
            Assert.Equal("outline body", graph.nodes.[childId].text)
            Assert.Equal(Current, documentState host docsId)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``git load reconcile parses an Unparsed directory file and NodeParsed sets Current`` () =
    task {
        let dataDir = newTempDir ()
        let host, handle, wsId = admitHome dataDir
        try
            let docsId = addDirectory host wsId "docs"
            Assert.Equal(Unparsed, documentState host docsId)
            placeFile
                dataDir
                "docs/.amb"
                ("outline body" + Environment.NewLine)
            do!
                pullReceived
                    host
                    handle
                    dataDir
                    [ LazyLoadReconciliation.Modified "docs/.amb" ]
            let ordered =
                (stateOf host) |> ignore
                CoreMailboxApi.eventsSince host EventId.zero
                |> Async.RunSynchronously
                |> fun log ->
                    log.events
                    |> List.sortBy (fun event -> EventId.value event.id)
            let parsingAt =
                ordered
                |> List.findIndex (function
                    | { body = EventBody.Notice (Notice.NodeParsing id) } ->
                        id = docsId
                    | _ -> false)
            let after = List.skip (parsingAt + 1) ordered
            let untilParsed =
                after
                |> List.takeWhile (function
                    | { body = EventBody.Notice (Notice.NodeParsed id) } ->
                        id <> docsId
                    | _ -> true)
            Assert.True(after.Length > untilParsed.Length)
            Assert.True(
                untilParsed
                |> List.exists (function
                    | { body = EventBody.Change _ } -> true
                    | _ -> false))
            let graph = (stateOf host).graph
            let childId = (Graph.children graph docsId).Head.id
            Assert.Equal("outline body", graph.nodes.[childId].text)
            Assert.Equal(Current, documentState host docsId)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``git load reconcile notices a renamed directory file and does not rename it`` () =
    task {
        let dataDir = newTempDir ()
        let host, handle, wsId = admitHome dataDir
        try
            let docsId = addDirectory host wsId "docs"
            do!
                pullReceived
                    host
                    handle
                    dataDir
                    [ LazyLoadReconciliation.Renamed(
                        "docs/.amb",
                        "moved/.amb") ]
            Assert.Equal(Current, documentState host docsId)
            Assert.Equal(
                Some "docs",
                Filename.tryValue (stateOf host).graph.nodes.[docsId].name)
            Assert.True(
                hasCase host (function
                    | Notice.NodeParsed id -> id = docsId
                    | _ -> false))
            Assert.False(
                hasCase host (function
                    | Notice.NodeParsing id -> id = docsId
                    | _ -> false))
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``modified directory file with no text posts NodeUnparsed`` () = task {
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
        let docsId = addDirectory host wsId "docs"
        setState host wsId Unparsed Current
        setState host docsId Unparsed Current
        let! pulled =
            LazyLoadReconciliationServer.reconcileReceivedFiles
                { handle = handle
                  dataDir = dataDir
                  workspaceLabel = "home" }
                [ LazyLoadReconciliation.Modified ".amb"
                  LazyLoadReconciliation.Modified "docs/.amb" ]
            |> Async.StartAsTask
        requireOk "pull" pulled |> ignore
        Assert.True(loggedUnparsed host wsId)
        Assert.True(loggedUnparsed host docsId)
        Assert.Equal(Unparsed, documentState host wsId)
        Assert.Equal(Unparsed, documentState host docsId)
        Assert.False(wroteSet host wsId Unparsed)
        Assert.False(wroteSet host docsId Unparsed)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``git load reconcile posts NodeUnparsed for a modified file`` () = task {
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
        let fileId = addFile host wsId "note.txt"
        setState host wsId Unparsed Current
        setState host fileId Unparsed Current
        let renamedId = addFile host wsId "old.txt"
        setState host renamedId Unparsed Current
        let home = Path.Combine(dataDir, "home")
        Directory.CreateDirectory(home) |> ignore
        File.WriteAllText(Path.Combine(home, "note.txt"), "body")
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
        let! pulled =
            LazyLoadReconciliationServer.reconcileReceivedFiles
                { handle = handle
                  dataDir = dataDir
                  workspaceLabel = "home" }
                changes
            |> Async.StartAsTask
        requireOk "pull" pulled |> ignore
        Assert.True(loggedUnparsed host fileId)
        Assert.True(loggedUnparsed host renamedId)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``git load reconcile reports a modified kind conflict and posts NodeUnparsed for the other file`` () =
    task {
        let dataDir = newTempDir ()
        let host, handle, wsId = admitHome dataDir
        try
            let _docsId = addDirectory host wsId "docs"
            let fileId = addFile host wsId "note.txt"
            setState host fileId Unparsed Current
            let changes = [
                LazyLoadReconciliation.Modified "docs"
                LazyLoadReconciliation.Modified "note.txt"
            ]
            let! pulled =
                LazyLoadReconciliationServer.reconcileReceivedFiles
                    { handle = handle
                      dataDir = dataDir
                      workspaceLabel = "home" }
                    changes
                |> Async.StartAsTask
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
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``reconcile emits NodeReceived`` () = task {
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
        let fileId = addFile host wsId "note.txt"
        setState host fileId Unparsed PullingFile
        let home = Path.Combine(dataDir, "home")
        Directory.CreateDirectory(home) |> ignore
        File.WriteAllText(Path.Combine(home, "note.txt"), "body")
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
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``renamed file posts NodeReceived`` () = task {
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
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
        Assert.DoesNotContain(planned.ops, setsUnparsed fileId)
        placeFile dataDir "note.txt" "body"
        File.Move(
            Path.Combine(dataDir, "home", "note.txt"),
            Path.Combine(dataDir, "home", "renamed.txt"))
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Renamed("note.txt", "renamed.txt") ]
        Assert.False(wroteSet host fileId Unparsed)
        Assert.True(loggedUnparsed host fileId)
        assertLogged host fileId Unparsed (received host fileId)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``moved file posts NodeReceived`` () = task {
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
        let docsId = addDirectory host wsId "docs"
        let otherId = addDirectory host wsId "other"
        setState host wsId Unparsed Current
        setState host docsId Unparsed Current
        setState host otherId Unparsed Current
        let fileId = addFile host docsId "note.txt"
        setState host fileId Unparsed Current
        placeFile dataDir "docs/note.txt" "body"
        Directory.CreateDirectory(
            Path.Combine(dataDir, "home", "other"))
        |> ignore
        File.Move(
            Path.Combine(dataDir, "home", "docs", "note.txt"),
            Path.Combine(dataDir, "home", "other", "note.txt"))
        do!
            pullReceived
                host
                handle
                dataDir
                [ LazyLoadReconciliation.Renamed(
                    "docs/note.txt", "other/note.txt") ]
        Assert.False(wroteSet host fileId Unparsed)
        Assert.True(loggedUnparsed host fileId)
        assertLogged host fileId Unparsed (received host fileId)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``renamed and modified file posts NodeReceived`` () = task {
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
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
        Assert.False(wroteSet host fileId Unparsed)
        Assert.True(loggedUnparsed host fileId)
        assertLogged host fileId Unparsed (received host fileId)
    finally
        CoreMailboxApi.dispose host
}

[<SkippableFact>]
let ``incoming file posts NodeReceived beside SetDocumentState`` () = task {
    Skip.If(
        true,
        "127 — Server file present drops its SetDocumentState")
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
        let presentId = addFile host wsId "present.txt"
        setState host presentId Unparsed PullingFile
        let otherId = addFile host wsId "other.txt"
        setState host otherId Unparsed PullingFile
        let ops =
            WorkspaceUploadStructure.planServerFilePresentOps
                (stateOf host).graph
                "home"
                [ "present.txt"; "missing.txt" ]
        Assert.False(received host presentId)
        do! postChange handle ops
        assertBeside host presentId Unparsed ops
        Assert.True(received host presentId)
        Assert.Equal(PullingFile, documentState host otherId)
        Assert.False(received host otherId)
    finally
        CoreMailboxApi.dispose host
}

[<SkippableFact>]
let ``incoming file emits NodeReceived`` () = task {
    Skip.If(
        true,
        "127 — Server file present drops its SetDocumentState")
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
        let presentId = addFile host wsId "present.txt"
        setState host presentId Unparsed PullingFile
        let ops =
            WorkspaceUploadStructure.planServerFilePresentOps
                (stateOf host).graph
                "home"
                [ "present.txt" ]
        do! postChange handle ops
        let node = (stateOf host).graph.nodes.[presentId]
        Assert.True(received host presentId)
        Assert.Equal(Unparsed, documentState host presentId)
        Assert.False(node.documentState.pullingFile)
        Assert.Equal(
            PersistState.Persisted,
            node.documentState.persistState)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``planParseFile posts NodeParsing, the Change, then NodeParsed`` () = task {
    let dataDir = newTempDir ()
    let host, _, wsId = admitHome dataDir
    let push = startParse dataDir host
    try
        let fileId = addFile host wsId "note.txt"
        let disk = Path.Combine(dataDir, "home", "note.txt")
        Directory.CreateDirectory(Path.GetDirectoryName disk) |> ignore
        File.WriteAllText(disk, "HELLO\n")
        push fileId
        do! waitParsed host fileId
        let ordered =
            CoreMailboxApi.eventsSince host EventId.zero
            |> Async.RunSynchronously
            |> fun log ->
                log.events
                |> List.sortBy (fun event -> EventId.value event.id)
        let parsingAt =
            ordered
            |> List.findIndex (function
                | { body = EventBody.Notice (Notice.NodeParsing id) } ->
                    id = fileId
                | _ -> false)
        let after = List.skip (parsingAt + 1) ordered
        let untilParsed =
            after
            |> List.takeWhile (function
                | { body = EventBody.Notice (Notice.NodeParsed id) } ->
                    id <> fileId
                | _ -> true)
        Assert.True(after.Length > untilParsed.Length)
        Assert.True(
            untilParsed
            |> List.exists (function
                | { body = EventBody.Change _ } -> true
                | _ -> false))
        let graph = (stateOf host).graph
        let childId = (Graph.children graph fileId).Head.id
        Assert.Equal("HELLO", graph.nodes.[childId].text)
        Assert.Equal(Current, documentState host fileId)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``planDirectoryReconcile logs NodeParsed beside SetDocumentState`` () =
    task {
        let dataDir = newTempDir ()
        let host, _, wsId = admitHome dataDir
        let push = startParse dataDir host
        try
            let dirId = addDirectory host wsId "docs"
            Directory.CreateDirectory(
                Path.Combine(dataDir, "home", "docs"))
            |> ignore
            Assert.False(parsedMsg host dirId)
            Assert.False(wroteSet host dirId Current)
            push dirId
            let! parsed =
                waitUntil
                    (fun () ->
                        parsedMsg host dirId
                        && documentState host dirId = Current)
                    2000
            Assert.True(parsed)
            Assert.False(wroteSet host dirId Current)
            CoreMailboxApi.postNotice host (Notice.NodeParsed dirId)
            Assert.Equal(Current, documentState host dirId)
            Assert.True(parsedMsg host dirId)
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``unparsed directory with a normal child drops a missing member`` () =
    task {
        let dataDir = newTempDir ()
        let host, _, wsId = admitHome dataDir
        let push = startParse dataDir host
        try
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
            Directory.CreateDirectory(
                Path.Combine(dataDir, "home", "docs"))
            |> ignore
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
        finally
            CoreMailboxApi.dispose host
    }

[<Fact>]
let ``ParseFinished is NodeParsed`` () = task {
    let dataDir = newTempDir ()
    let host, _, wsId = admitHome dataDir
    try
        let fileId = addFile host wsId "note.txt"
        CoreMailboxApi.postNotice host (Notice.NodeParsed fileId)
        assertLogged host fileId Current (parsedMsg host fileId)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``non document state change posts no Node Notice`` () = task {
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
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
    finally
        CoreMailboxApi.dispose host
}
