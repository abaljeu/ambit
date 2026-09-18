module Gambol.Shared.Tests.SyncPlannerTests

open System
open Xunit
open Gambol.Shared
open Gambol.Shared
open Gambol.Shared.ViewModel

let private mkChange _n =
    SpecialNodeTestHelpers.changeEventZero "fixture" []

[<Fact>]
let ``tryStartSubmit returns SubmitPendingBatch effect when queue is ready`` () =
    let c = mkChange 7
    let syncInfo =
        { SyncInfo.initial with
            pending = [ c ] }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (EventIdFixtures.storedId 9) syncInfo
    Assert.Equal(Sending 1, nextInfo.syncState)
    match effects with
    | [ SubmitPendingBatch (baseEventId, events) ] ->
        Assert.Equal(EventIdFixtures.storedId 9, baseEventId)
        Assert.Equal<Ev list>([ c ], events)
    | _ ->
        failwith "Expected single SubmitPendingBatch effect"

[<Fact>]
let ``tryStartSubmit returns no effects when already sending`` () =
    let syncInfo =
        { SyncInfo.initial with
            pending = [ mkChange 1 ]
            syncState = Sending 1 }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (EventIdFixtures.storedId 1) syncInfo
    Assert.Equal(Sending 1, nextInfo.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``retireSubmittedPrefix dequeues the prefix and schedules remainder`` () =
    let c1 = mkChange 0
    let c2 = mkChange 0
    let c3 = mkChange 0
    let syncInfo =
        { SyncInfo.initial with
            pending = [ c1; c2; c3 ]
            syncState = Sending 1 }
    let nextInfo, pending, effects =
        SyncPlanner.retireSubmittedPrefix 2 (EventIdFixtures.storedId 3) syncInfo
    Assert.Single(pending) |> ignore
    Assert.Equal(c3.submissionId, pending.Head.submissionId)
    Assert.Equal(Sending 1, nextInfo.syncState)
    match effects with
    | [ SubmitPendingBatch (baseEventId, events) ] ->
        Assert.Equal(EventIdFixtures.storedId 3, baseEventId)
        Assert.Equal<Ev list>([ c3 ], events)
    | _ ->
        failwith "Expected single SubmitPendingBatch effect for remaining queue"

[<Fact>]
let ``retireSubmittedPrefix of the full queue returns Idle and no effects`` () =
    let c = mkChange 0
    let syncInfo =
        { SyncInfo.initial with
            pending = [ c ]
            syncState = Sending 1 }
    let nextInfo, pending, effects =
        SyncPlanner.retireSubmittedPrefix 1 (EventIdFixtures.storedId 1) syncInfo
    Assert.Empty(pending)
    Assert.Equal(Idle, nextInfo.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``toWireBatch keeps pending EventId.zero and submissionId`` () =
    let c1 = mkChange 637
    let c2 = mkChange 637
    let events = [ c1; c2 ]
    let wire = SyncBatch.toWireBatch events
    Assert.Equal<EventId list>(
        [ EventId.zero; EventId.zero ],
        wire |> List.map (fun event -> event.id))
    Assert.Equal<Guid list>(
        [ c1.submissionId; c2.submissionId ],
        wire |> List.map (fun event -> event.submissionId))

[<Fact>]
let ``toWireBatch keeps empty batch empty`` () =
    Assert.Empty(SyncBatch.toWireBatch [])

[<Fact>]
let ``tryStartPoll uses catch-up baseline revision`` () =
    let graph0 = Graph.create ()
    let syncInfo =
        { SyncInfo.initial with
            catchUp =
                Some
                    { eventId = EventIdFixtures.storedId 3
                      graph = graph0 } }
    let si, effects = SyncPlanner.tryStartPoll (EventIdFixtures.storedId 9) syncInfo
    Assert.Equal(Polling, si.syncState)
    match effects with
    | [ PollServer eventId ] -> Assert.Equal(EventIdFixtures.storedId 3, eventId)
    | _ -> failwith "Expected PollServer from catch-up baseline"

[<Fact>]
let ``tryStartPoll emits PollServer when idle with empty queue`` () =
    let si, effects = SyncPlanner.tryStartPoll (EventIdFixtures.storedId 5) SyncInfo.initial
    Assert.Equal(Polling, si.syncState)
    match effects with
    | [ PollServer eventId ] -> Assert.Equal(EventIdFixtures.storedId 5, eventId)
    | _ -> failwith "Expected single PollServer effect"

[<Fact>]
let ``tryStartPoll returns no effects when queue is non-empty`` () =
    let syncInfo =
        { SyncInfo.initial with pending = [ mkChange 0 ] }
    let si, effects = SyncPlanner.tryStartPoll (EventIdFixtures.storedId 5) syncInfo
    Assert.Equal(Idle, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartPoll returns no effects when already sending`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Sending 1 }
    let si, effects = SyncPlanner.tryStartPoll (EventIdFixtures.storedId 5) syncInfo
    Assert.Equal(Sending 1, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartPoll returns no effects when uploading`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Uploading }
    let si, effects = SyncPlanner.tryStartPoll (EventIdFixtures.storedId 5) syncInfo
    Assert.Equal(Uploading, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartLoad emits LoadServer when idle with empty queue`` () =
    let targetId = NodeId.New()
    let targets = [ { targetId = targetId; includeWorkspace = true } ]
    let si, effects =
        SyncPlanner.tryStartLoad (EventIdFixtures.storedId 5) targets SyncInfo.initial
    Assert.Equal(Loading, si.syncState)
    match effects with
    | [ LoadServer (eventId, loadTargets) ] ->
        Assert.Equal(EventIdFixtures.storedId 5, eventId)
        Assert.Equal(1, loadTargets.Length)
        Assert.Equal(targetId, loadTargets.[0].targetId)
        Assert.True(loadTargets.[0].includeWorkspace)
    | _ -> failwith "Expected single LoadServer effect"

[<Fact>]
let ``tryStartLoad returns no effects when already loading`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Loading }
    let targets =
        [ { targetId = NodeId.New(); includeWorkspace = false } ]
    let si, effects =
        SyncPlanner.tryStartLoad (EventIdFixtures.storedId 5) targets syncInfo
    Assert.Equal(Loading, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartPoll returns no effects when loading`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Loading }
    let si, effects = SyncPlanner.tryStartPoll (EventIdFixtures.storedId 5) syncInfo
    Assert.Equal(Loading, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartSubmit returns no effects when loading`` () =
    let syncInfo =
        { SyncInfo.initial with
            pending = [ mkChange 1 ]
            syncState = Loading }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (EventIdFixtures.storedId 1) syncInfo
    Assert.Equal(Loading, nextInfo.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``queued workspace Upload waits while a change submit is in flight`` () =
    let scope =
        { label = "home"
          relative = "notes/today.md"
          kind = SyncScopeKind.File }
    let request = QueuedWorkspacePush(scope, Some(NodeId.New()))
    let syncInfo =
        { SyncInfo.initial with
            pending = [ mkChange 14706 ]
            syncState = Sending 1 }
        |> SyncInfo.queueRequest request
    let si, effects = SyncPlanner.tryReleaseQueued syncInfo
    Assert.Equal<QueuedRequest list>([ request ], si.queuedRequests)
    Assert.Empty(effects)

[<Fact>]
let ``queued file Upload preserves its scope until the change queue drains`` () =
    let c = mkChange 14706
    let request =
        QueuedWorkspacePush(
            { label = "home"
              relative = "notes/today.md"
              kind = SyncScopeKind.File },
            Some(NodeId.New()))
    let queued =
        { SyncInfo.initial with
            pending = [ c ]
            syncState = Sending 1 }
        |> SyncInfo.queueRequest request
    let acked, _, _ = SyncPlanner.retireSubmittedPrefix 1 (EventIdFixtures.storedId 14707) queued
    let si, effects = SyncPlanner.tryReleaseQueued acked
    Assert.Empty(si.queuedRequests)
    Assert.Equal<Effect list>([ RunQueuedRequest request ], effects)

[<Fact>]
let ``queued Uploads release one request at a time`` () =
    let first =
        QueuedWorkspacePush(
            { label = "home"
              relative = "first.md"
              kind = SyncScopeKind.File },
            Some(NodeId.New()))
    let second =
        QueuedWorkspacePush(
            { label = "home"
              relative = "second.md"
              kind = SyncScopeKind.File },
            Some(NodeId.New()))
    let queued =
        SyncInfo.initial
        |> SyncInfo.queueRequest first
        |> SyncInfo.queueRequest second
    let si, effects = SyncPlanner.tryReleaseQueued queued
    Assert.Equal<QueuedRequest list>([ second ], si.queuedRequests)
    Assert.Equal<Effect list>([ RunQueuedRequest first ], effects)

[<Fact>]
let ``queued Upload is released after a poll settles`` () =
    let request =
        QueuedWorkspacePush(
            { label = "home"
              relative = ""
              kind = SyncScopeKind.Workspace },
            None)
    let syncInfo =
        { SyncInfo.initial with syncState = Polling }
        |> SyncInfo.queueRequest request
    Assert.Empty(snd (SyncPlanner.tryReleaseQueued syncInfo))
    let settled =
        { syncInfo with syncState = Idle } |> SyncPlanner.tryReleaseQueued
    Assert.Equal<Effect list>([ RunQueuedRequest request ], snd settled)

[<Fact>]
let ``pressing Load twice while it waits queues one request`` () =
    let request = QueuedLoad
    let syncInfo =
        { SyncInfo.initial with syncState = Polling }
        |> SyncInfo.queueRequest request
        |> SyncInfo.queueRequest request
    Assert.Equal<QueuedRequest list>([ request ], syncInfo.queuedRequests)

[<Fact>]
let ``QueuedLoad releases after sync settles`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Polling }
        |> SyncInfo.queueRequest QueuedLoad
    Assert.Empty(snd (SyncPlanner.tryReleaseQueued syncInfo))
    let settled =
        { syncInfo with syncState = Idle } |> SyncPlanner.tryReleaseQueued
    Assert.Equal<Effect list>([ RunQueuedRequest QueuedLoad ], snd settled)

[<Fact>]
let ``tryReleaseQueued is inert with nothing queued`` () =
    let si, effects = SyncPlanner.tryReleaseQueued SyncInfo.initial
    Assert.Equal(SyncInfo.initial, si)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartSubmit returns no effects when uploading`` () =
    let syncInfo =
        { SyncInfo.initial with
            pending = [ mkChange 1 ]
            syncState = Uploading }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (EventIdFixtures.storedId 1) syncInfo
    Assert.Equal(Uploading, nextInfo.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartPoll returns no effects when parsing`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Parsing }
    let si, effects = SyncPlanner.tryStartPoll (EventIdFixtures.storedId 5) syncInfo
    Assert.Equal(Parsing, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartSubmit returns no effects when parsing`` () =
    let syncInfo =
        { SyncInfo.initial with
            pending = [ mkChange 1 ]
            syncState = Parsing }
    let nextInfo, effects = SyncPlanner.tryStartSubmit (EventIdFixtures.storedId 1) syncInfo
    Assert.Equal(Parsing, nextInfo.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``tryStartLoad returns no effects when parsing`` () =
    let syncInfo =
        { SyncInfo.initial with syncState = Parsing }
    let targets =
        [ { targetId = NodeId.New(); includeWorkspace = false } ]
    let si, effects =
        SyncPlanner.tryStartLoad (EventIdFixtures.storedId 5) targets syncInfo
    Assert.Equal(Parsing, si.syncState)
    Assert.Empty(effects)

[<Fact>]
let ``mixed C Undo Redo wire batch keeps submissionId and EventId.zero`` () =
    let change = mkChange 99
    let undo = mkChange 99
    let redo = mkChange 99
    let items = [ change; undo; redo ]
    let wire = SyncBatch.toWireBatch items
    Assert.Equal<EventId list>(
        [ EventId.zero; EventId.zero; EventId.zero ],
        wire |> List.map (fun event -> event.id))
    Assert.Equal<Guid list>(
        [ change.submissionId; undo.submissionId; redo.submissionId ],
        wire |> List.map (fun event -> event.submissionId))

[<Fact>]
let ``later queued actions do not alter the SubmitPendingBatch list`` () =
    let first = mkChange 1
    let later = mkChange 2
    let syncInfo =
        { SyncInfo.initial with pending = [ first ] }
    let sending, effects = SyncPlanner.tryStartSubmit (EventIdFixtures.storedId 4) syncInfo
    match effects with
    | [ SubmitPendingBatch (_, submitted) ] ->
        let grown =
            { sending with pending = sending.pending @ [ later ] }
        Assert.Equal<Ev list>([ first ], submitted)
        Assert.Equal<Ev list>([ first; later ], grown.pending)
        let _, laterEffects = SyncPlanner.tryStartSubmit (EventIdFixtures.storedId 4) grown
        Assert.Empty(laterEffects)
    | _ ->
        failwith "Expected single SubmitPendingBatch effect"

[<Fact>]
let ``retry list stays the submitted snapshot after later actions append`` () =
    let submitted = [ mkChange 1 ]
    let later = mkChange 2
    let syncInfo =
        { SyncInfo.initial with
            pending = submitted @ [ later ]
            syncState = WaitingToRetry(1, EventIdFixtures.storedId 4, submitted) }
    let nextInfo, effects = SyncPlanner.retryWaiting false syncInfo
    Assert.Equal(Sending 2, nextInfo.syncState)
    match effects with
    | [ SubmitPendingBatch (baseEventId, retryList) ] ->
        Assert.Equal(EventIdFixtures.storedId 4, baseEventId)
        Assert.Equal<Ev list>(submitted, retryList)
        Assert.NotEqual<Ev list>(syncInfo.pending, retryList)
    | _ ->
        failwith "Expected retry of the WaitingToRetry snapshot"

[<Fact>]
let ``C Undo Redo remain one SubmitPendingBatch`` () =
    let items = [ mkChange 0; mkChange 0; mkChange 0 ]
    let syncInfo = { SyncInfo.initial with pending = items }
    let _, effects = SyncPlanner.tryStartSubmit (EventIdFixtures.storedId 3) syncInfo
    match effects with
    | [ SubmitPendingBatch (_, submitted) ] ->
        Assert.Equal<Ev list>(items, submitted)
    | _ ->
        failwith "Expected the full pending batch"

[<Fact>]
let ``retireSubmittedPrefix remainder still submits together`` () =
    let first = mkChange 0
    let remainder = [ mkChange 0; mkChange 0 ]
    let syncInfo =
        { SyncInfo.initial with
            pending = first :: remainder
            syncState = Sending 1 }
    let _, pending, effects =
        SyncPlanner.retireSubmittedPrefix 1 (EventIdFixtures.storedId 1) syncInfo
    Assert.Equal<Ev list>(remainder, pending)
    match effects with
    | [ SubmitPendingBatch (_, submitted) ] ->
        Assert.Equal<Ev list>(remainder, submitted)
    | _ ->
        failwith "Expected remainder batch"

[<Fact>]
let ``restorePending keeps EventId.zero and does not record History`` () =
    let state0 = ModelBuilder.createState12 ()
    let root = state0.graph.nodes.[state0.graph.root]
    let node = state0.graph.nodes.[root.children.Head.id]
    let change =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Browser"
          commandName = ""
          body = EventBody.Change [ Op.SetText(node.id, node.text, "restored") ] }
    let saved = [ change ]
    let snapshot = { state0 with eventId = EventIdFixtures.storedId 1 }
    let next, restored =
        SyncPlanner.restorePending (EventIdFixtures.storedId 1) saved snapshot
    let queued = Assert.Single(restored)
    Assert.Equal(EventId.zero, queued.id)
    Assert.Equal(change.submissionId, queued.submissionId)
    Assert.Equal("restored", next.graph.nodes.[node.id].text)

[<Fact>]
let ``workspace pending item is the exact Ev used before the request`` () =
    let change = mkChange 12
    let submitted = change
    let wire = SyncBatch.toWireBatch [ submitted ]
    Assert.Equal(submitted.submissionId, wire.Head.submissionId)
    Assert.Equal(EventId.zero, wire.Head.id)
    let effect =
        ContinuePostUploadStructure(
            submitted,
            { label = "home"
              relative = "notes.md"
              kind = SyncScopeKind.File },
            None)
    match effect with
    | ContinuePostUploadStructure (item, _, _) ->
        Assert.Equal(submitted, item)
    | _ ->
        failwith "Expected ContinuePostUploadStructure to carry the singleton"
