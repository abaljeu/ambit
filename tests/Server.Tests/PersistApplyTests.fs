module Gambol.Server.Tests.PersistApplyTests

open System
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

/// Spec §2.6: Undo and Redo set Unpersisted and do not write a version.
/// commandName stays the original Edit so a command-name check would still stamp.
let private persistedAt version text =
    let fileId = NodeId.New()
    let node =
        Node.Create(
            fileId,
            text = text,
            name = Filename.create "note.txt",
            kind = Special File)
        |> Node.withUnpersistedAt version
        |> Node.withPersistState PersistState.Persisted
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

let private assertKeptVersion state fileId text version =
    let live = state.graph.nodes.[fileId]
    Assert.Equal(text, live.text)
    Assert.Equal(PersistState.Unpersisted, live.documentState.persistState)
    Assert.Equal(version, live.documentState.changedVersion)

let private applyKept handlers graphOnly event fileId text version =
    requireOk "applyEvent" (handlers.applyEvent event graphOnly) |> ignore
    let state = requireOk "getState" (handlers.getState ())
    assertKeptVersion state fileId text version

let private fileHandlers () =
    let agent = FileAgent.create (newTempDir ())
    (FileAgent.persist agent).handlers

let private dbHandlers () =
    let initial =
        { graph = Graph.create (); eventId = EventId.zero }
    let agent = DbAgent.createForTest initial (fun _ -> Ok [])
    (DbAgent.persist agent).handlers

/// Apply one Undo or Redo whose commandName is still the original Edit.
let private runAction handlers graphOnly before after bodyFor =
    let version = EventIdFixtures.storedId 4
    let fileId, node = persistedAt version before
    seedPersisted handlers fileId node
    let event = editAction 9 (bodyFor version fileId)
    applyKept handlers graphOnly event fileId after version

[<Fact>]
let ``Undo and Redo leave Unpersisted and keep changedVersion`` () =
    let undo version fileId =
        EventBody.Undo(
            version,
            [ Op.SetText(fileId, "edited", "hello") ])
    let redo version fileId =
        EventBody.Redo(
            version,
            [ Op.SetText(fileId, "hello", "edited") ])
    runAction (fileHandlers ()) false "edited" "hello" undo
    runAction (fileHandlers ()) true "hello" "edited" redo
    runAction (dbHandlers ()) false "edited" "hello" undo
    runAction (dbHandlers ()) true "hello" "edited" redo
