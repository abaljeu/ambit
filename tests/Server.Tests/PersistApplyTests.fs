module Gambol.Server.Tests.PersistApplyTests

open System
open System.IO
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error error ->
        Assert.Fail($"{label}: {error}")
        Unchecked.defaultof<_>

let private addRootChild text =
    let childId = NodeId.New()
    childId,
    [ Op.NewNode(childId, text)
      Op.Replace(Graph.rootId, [], [ ChildNode.owner childId ]) ]

let private wireEvent submissionId ops : Ev =
    { id = EventIdFixtures.storedId 99
      submissionId = submissionId
      authority = Authority "Wire"
      commandName = "persist-apply"
      body = EventBody.Change ops }

let private eventsSince (handlers: PersistHandlers) after =
    requireOk "getEventsSince" (handlers.getEventsSince after)

let private applyThenAppend (handlers: PersistHandlers) event =
    let accepted = requireOk "applyEvent" (handlers.applyEvent event false)
    Assert.Empty(eventsSince handlers (EventId.zero))
    requireOk "appendEvent" (handlers.appendEvent event)
    let stored = Assert.Single(eventsSince handlers (EventId.zero))
    Assert.Equal(event.submissionId, stored.submissionId)
    accepted

let private fileNodeWithText () =
    let fileId = NodeId.New()
    let node =
        Node.Create(
            fileId,
            text = "hello",
            name = Filename.create "note.txt",
            kind = Special File)
    fileId, node

let private editFileText eventId fileId oldText newText : Ev =
    { id = EventIdFixtures.storedId eventId
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = "Edit"
      body = EventBody.Change [ Op.SetText(fileId, oldText, newText) ] }

[<Fact>]
let ``FileAgent applyEvent marks a File Node at the later Edit id`` () =
    let fileId, node = fileNodeWithText ()
    let agent = FileAgent.create (newTempDir ())
    let handlers = (FileAgent.persist agent).handlers
    let seeded =
        match handlers.getState () with
        | Error error ->
            Assert.Fail($"getState: {error}")
            Unchecked.defaultof<_>
        | Ok state ->
            Graph.addDetachedNode node state.graph
    handlers.replaceGraph (Notice.NodePersisting fileId) seeded
    requireOk
        "first edit"
        (handlers.applyEvent (editFileText 4 fileId "hello" "first") false)
    |> ignore
    requireOk
        "second edit"
        (handlers.applyEvent (editFileText 5 fileId "first" "second") false)
    |> ignore
    let state = requireOk "getState" (handlers.getState ())
    let live = state.graph.nodes.[fileId]
    Assert.Equal(PersistState.Unpersisted, live.documentState.persistState)
    Assert.Equal(EventIdFixtures.storedId 5, live.documentState.changedVersion)
    Assert.Equal("second", live.text)

[<Fact>]
let ``FileAgent applyEvent applies Ev Ops without leftover Change`` () =
    let childId, ops = addRootChild "persist-apply-file"
    let event = wireEvent (Guid.NewGuid()) ops
    let agent = FileAgent.create (newTempDir ())
    let handlers = (FileAgent.persist agent).handlers
    let accepted = applyThenAppend handlers event
    let state = requireOk "getState" (handlers.getState ())
    Assert.Equal("persist-apply-file", state.graph.nodes.[childId].text)
    Assert.Equal(event.submissionId, Assert.Single(accepted.events).submissionId)

[<Fact>]
let ``DbAgent applyEvent applies Ev Ops without leftover Change`` () =
    let childId, ops = addRootChild "persist-apply-db"
    let event = wireEvent (Guid.NewGuid()) ops
    let initial =
        { graph = Graph.create (); eventId = EventId.zero }
    let agent = DbAgent.createForTest initial (fun _ -> Ok [])
    let handlers = (DbAgent.persist agent).handlers
    let accepted = applyThenAppend handlers event
    let state = requireOk "getState" (handlers.getState ())
    Assert.Equal("persist-apply-db", state.graph.nodes.[childId].text)
    Assert.Equal(event.submissionId, Assert.Single(accepted.events).submissionId)

[<Fact>]
let ``CoreEventDispatch persist apply does not copy Ev to leftover Change`` () =
    let childId, ops = addRootChild "no-change-copy"
    let event = { wireEvent (Guid.NewGuid()) ops with id = EventId.zero }
    let persist = FileAgent.persist (FileAgent.create (newTempDir ()))
    let host =
        CoreMailbox.host
            (CoreActorPool.create ())
            persist
            admittedCredentials
    try
        let stored =
            CoreMailboxApi.postEvent host testCaller event
            |> Async.RunSynchronously
            |> requireOk "postEvent"
        let state =
            CoreMailboxApi.getState host
            |> Async.RunSynchronously
            |> requireOk "getState"
        Assert.Equal("no-change-copy", state.graph.nodes.[childId].text)
        Assert.Equal(event.submissionId, stored.submissionId)
        match Ev.ops stored with
        | Some storedOps ->
            Assert.Equal<Op list>(ops, List.take ops.Length storedOps)
        | None -> Assert.Fail("expected Change EventBody Ops")
        let logged =
            CoreMailboxApi.getEventsSince host (EventId.zero)
            |> Async.RunSynchronously
        Assert.Equal(
            event.submissionId,
            Assert.Single(changesOnly logged).submissionId)
    finally
        CoreMailboxApi.dispose host

let private withPersistedVersions changed persisted node =
    { node with
        documentState =
            { node.documentState with
                persistState = PersistState.Persisted
                changedVersion = changed
                persistedVersion = persisted } }

let private fileAt text persisted =
    let fileId = NodeId.New()
    let node =
        Node.Create(
            fileId,
            text = text,
            name = Filename.create "note.txt",
            kind = Special File)
        |> withPersistedVersions EventId.zero persisted
    fileId, node

let private seedPersisted handlers fileId node =
    let state = requireOk "seed getState" (handlers.getState ())
    handlers.replaceGraph
        (Notice.NodePersisting fileId)
        (Graph.addDetachedNode node state.graph)

let private editAction id body : Ev =
    { id = EventIdFixtures.storedId id
      submissionId = Guid.NewGuid()
      authority = Authority "Browser"
      commandName = "Edit"
      body = body }

let private fileHandlers () =
    let agent = FileAgent.create (newTempDir ())
    (FileAgent.persist agent).handlers

let private applyAction handlers event =
    requireOk "applyEvent" (handlers.applyEvent event false) |> ignore
    requireOk "getState" (handlers.getState ())

let private assertVersions state fileId text changed persisted =
    let live = state.graph.nodes.[fileId]
    Assert.Equal(text, live.text)
    Assert.Equal(PersistState.Unpersisted, live.documentState.persistState)
    Assert.Equal(changed, live.documentState.changedVersion)
    Assert.Equal(persisted, live.documentState.persistedVersion)

/// Edit at V, then Undo or Redo at a later id. persistedVersion stays.
let private editThenAction handlers actionId bodyFor =
    let version = EventIdFixtures.storedId 4
    let persisted = EventIdFixtures.storedId 2
    let fileId, node = fileAt "hello" persisted
    seedPersisted handlers fileId node
    let edit =
        editAction 4 (EventBody.Change [ Op.SetText(fileId, "hello", "edited") ])
    applyAction handlers edit |> ignore
    let action = editAction actionId (bodyFor version fileId)
    let state = applyAction handlers action
    fileId, persisted, state

[<Fact>]
let ``Undo sets changedVersion to the Undo event and leaves persistedVersion`` () =
    let undo version fileId =
        EventBody.Undo(version, [ Op.SetText(fileId, "edited", "hello") ])
    let fileId, persisted, state =
        editThenAction (fileHandlers ()) 9 undo
    assertVersions
        state fileId "hello" (EventIdFixtures.storedId 9) persisted

[<Fact>]
let ``Redo sets changedVersion to the Redo event and leaves persistedVersion`` () =
    let handlers = fileHandlers ()
    let version = EventIdFixtures.storedId 4
    let persisted = EventIdFixtures.storedId 2
    let fileId, node = fileAt "hello" persisted
    seedPersisted handlers fileId node
    applyAction handlers (
        editAction 4 (EventBody.Change [ Op.SetText(fileId, "hello", "edited") ]))
    |> ignore
    applyAction handlers (
        editAction 9 (
            EventBody.Undo(version, [ Op.SetText(fileId, "edited", "hello") ])))
    |> ignore
    let state =
        applyAction handlers (
            editAction 12 (
                EventBody.Redo(
                    EventIdFixtures.storedId 9,
                    [ Op.SetText(fileId, "hello", "edited") ])))
    assertVersions
        state fileId "edited" (EventIdFixtures.storedId 12) persisted

let private postBody host commandName body =
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = commandName
          body = body }
    CoreMailboxApi.postEvent host testCaller event |> Async.StartAsTask

let private stateOf host = task {
    let! state = CoreMailboxApi.getState host |> Async.StartAsTask
    return requireOk "getState" state
}

let private artifactText dataDir graph fileId =
    let path =
        DocumentPersistPath.resolveArtifactPath dataDir graph fileId
        |> requireOk "path"
    if File.Exists path then File.ReadAllText path else ""

let private waitUntil label ready = task {
    let start = DateTime.UtcNow
    let rec poll () = task {
        let! ok = ready ()
        if ok || (DateTime.UtcNow - start).TotalSeconds >= 4.0 then
            Assert.True(ok, label)
        else
            do! Task.Delay 20
            return! poll ()
    }
    do! poll ()
}

[<Fact>]
let ``Undo pushes the document root and the persist thread writes the undone graph`` () =
    task {
        let dataDir = newTempDir ()
        let host = admittedHostFile (FileAgent.create dataDir)
        try
            let! initial = stateOf host
            let workspaceId, workspaceOps =
                FileNodeOps.planCreateWorkspace initial.graph "home"
            let! workspace =
                postBody host "Create workspace" (EventBody.Change workspaceOps)
            requireOk "workspace" workspace |> ignore
            let! afterWorkspace = stateOf host
            let fileId, fileOps =
                FileNodeOps.planCreateOwnedFile
                    afterWorkspace.graph workspaceId "note.txt"
            let! file =
                postBody host "Create file" (EventBody.Change fileOps)
            requireOk "file" file |> ignore
            CoreMailboxApi.postNotice host (Notice.NodeParsed fileId)
            do!
                waitUntil "parsed file" (fun () -> task {
                    let! state = stateOf host
                    return
                        state.graph.nodes.[fileId].parseState
                        = ParseState.Parsed
                })
            let! afterFile = stateOf host
            let childId = NodeId.New()
            let oldChildren = GraphChildren.get afterFile.graph fileId
            let addChild =
                EventBody.Change [
                    Op.NewNode(childId, "edited")
                    Op.Replace(
                        fileId,
                        oldChildren,
                        oldChildren @ [ ChildNode.owner childId ])
                ]
            let! edited = postBody host "Edit" addChild
            let storedEdit = requireOk "edit" edited
            do!
                waitUntil "edited file" (fun () -> task {
                    let! state = stateOf host
                    let text = artifactText dataDir state.graph fileId
                    return text.Contains "edited"
                })
            let! undone =
                postBody
                    host
                    "Edit"
                    (EventBody.Undo(storedEdit.id, []))
            let storedUndo = requireOk "undo" undone
            do!
                waitUntil "undone file" (fun () -> task {
                    let! state = stateOf host
                    let text = artifactText dataDir state.graph fileId
                    let unlinked =
                        GraphChildren.get state.graph fileId
                        |> List.forall (fun child -> child.id <> childId)
                    return unlinked && not (text.Contains "edited")
                })
            let! final = stateOf host
            let! events =
                CoreMailboxApi.getEventsSince host EventId.zero
                |> Async.StartAsTask
            let undoAt =
                events
                |> List.findIndex (fun event ->
                    event.submissionId = storedUndo.submissionId)
            let wroteFile =
                events
                |> List.skip (undoAt + 1)
                |> List.exists (fun event ->
                    match event.body with
                    | EventBody.Notice (Notice.NodePersisted (id, _, _)) ->
                        id = fileId
                    | _ -> false)
            Assert.True(wroteFile, "persist thread wrote the file after Undo")
            Assert.Equal(
                storedUndo.id,
                final.graph.nodes.[fileId].documentState.changedVersion)
        finally
            CoreMailboxApi.dispose host
    }
