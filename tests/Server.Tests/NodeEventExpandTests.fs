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
    MailboxHost.loggedNotices host |> List.exists pred

let private documentState host id =
    Node.documentStateEnum (stateOf host).graph.nodes.[id]

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
                && wroteSet host id Current)
            2000
    Assert.True(parsed)
}

[<Fact>]
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

[<Fact>]
let ``git changed file posts NodeReceived beside SetDocumentState`` () = task {
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
        Assert.True(wroteSet host fileId Unparsed)
        assertLogged host fileId Unparsed (received host fileId)
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
        Assert.True(wroteSet host fileId Unparsed)
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
        Assert.True(wroteSet host fileId Unparsed)
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
        Assert.True(wroteSet host fileId Unparsed)
        assertLogged host fileId Unparsed (received host fileId)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``incoming file posts NodeReceived beside SetDocumentState`` () = task {
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

[<Fact>]
let ``import logs NodeParsed beside SetDocumentState`` () = task {
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    try
        let fileId = addFile host wsId "note.txt"
        let package =
            ImportText.buildPackage "note.txt" "alpha"
            |> requireOk "package"
        let ops =
            ImportText.buildImportChange
                (stateOf host).graph
                fileId
                []
                package
                (Guid.NewGuid())
            |> changeOps
        Assert.False(parsedMsg host fileId)
        do! postChange handle ops
        assertBeside host fileId Current ops
        Assert.True(parsedMsg host fileId)
    finally
        CoreMailboxApi.dispose host
}

[<Fact>]
let ``planParseFile logs NodeParsed beside SetDocumentState`` () = task {
    let dataDir = newTempDir ()
    let host, handle, wsId = admitHome dataDir
    let push = startParse dataDir host
    try
        let fileId = addFile host wsId "note.txt"
        let disk = Path.Combine(dataDir, "home", "note.txt")
        Directory.CreateDirectory(Path.GetDirectoryName disk) |> ignore
        File.WriteAllText(disk, "HELLO\n")
        push fileId
        do! waitParsed host fileId
        let otherId = addFile host wsId "other.txt"
        let body =
            "{\"fileId\":\""
            + string otherId.Value
            + "\",\"text\":\"BODY\\n\"}"
        let! posted =
            Api.postParseFile handle dataDir body |> Async.StartAsTask
        match box posted with
        | :? ContentHttpResult -> ()
        | other ->
            Assert.Fail($"postParseFile failed: {other.GetType().Name}")
        Assert.True(wroteSet host otherId Current)
        assertLogged host otherId Current (parsedMsg host otherId)
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
            push dirId
            do! waitParsed host dirId
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
