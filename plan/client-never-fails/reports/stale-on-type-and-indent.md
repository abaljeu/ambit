# Stale on type and indent

Date: 2026-11-18
Tree: `origin/staging` `b19181a7` (68 — Remove CoreMsg.PostEvent)
Investigation only. The throwaway probe below was run and then left out of the commit.

## Ticket

[10 — Editing returnTo vs Poll/Load under edit+indent Tab CAS](../../client-start-time/issues/10-edit-indent-old-text-mismatch.md) owns the compare-and-swap claim. Status on this tree: `ready-for-agent`. Its report is [Edit + indent — old text does not match](../../client-start-time/reports/edit-indent-old-text-mismatch.md).

Close that ticket. The Tab compare-and-swap it names is already handled. Write a new client-never-fails ticket for the Poll overlap below. Leave ticket 10's text as history.

These other tickets are the wrong owner:

- [09 — Poll with pending changes](../issues/09-poll-with-pending-changes/09-poll-with-pending-changes-ticket.md) is marked done. `SyncPlanner.tryStartPoll` still emits `PollServer` only for `Idle` and an empty pending queue.
- [11 — Type while others change](../issues/11-type-while-others-change/11-type-while-others-change-ticket.md) is unbuilt. `UpdateHelpers.commitTextEdit` still sets `mode` to `Selecting`. A one-second typing pause does not post.
- [140 — Client never fails on server data](../../core-refinement/issues/140-client-never-fails-on-server-data.md) stays the draft umbrella.
- [Notices that trip the Browser](notice-trip-findings.md) describes an older `PollDone` condition. The empty-event arm is handled. The pending arm below is the one that remains.

## What the screen calls Stale

The server has no reply named Stale.

`StatusView.renderSyncStatus` paints `amb-stale` for three `SyncState` values:

- `ServerRejected` — "Server rejected change — reload required". `Update.rejectPending` sets this from `SubmitRejected` and from `AckReconcile.Rejected`.
- `CodeOutdated` — "New version available — click to reload".
- `DataOutdated` — "Data changed on server — click to reload". `Overlays.renderSyncRiskAlert` titles that case "View is out of date".

## Server checks

A post is `CoreMailboxApi.postEvents`. `FileAgent.applyOne` and `DbAgent.applyOne` run `ChangeAmendment.applyPosted`. `ApplyResult.Invalid` becomes the error string. `ApplyResult.Unchanged` becomes "Unchanged submission is rejected."

`GraphMutate.setText` returns "old text does not match". `ChangeAmendment.isRecoverableCas` amends that into an `amb-conflict` child and sets `externalChanges`. The probe posted that shape and got `Ok external=True`.

`GraphMutate.replace` returns "old span does not match", "old span out of bounds", "index out of bounds", and "parent children not loaded". Only "old span does not match" is in `isRecoverableCas`. The probe posted a longer old list and got `Error old span out of bounds`.

`SyncBatch.toWireBatch` sends `EventId.zero`. The server assigns ids in mailbox order. The post body carries no client event id, so a Poll cannot fail a compare-and-swap by itself.

## Client paths that build the posts

Typing stays in the edit box until a commit. Enter is `UpdateHelpers.splitNode`: `Op.NewNode`, `ChildListWire.insertAt`, and `Op.SetText` whose old text is `modelText` from the graph. Tab is `UpdateMove.indentSelection` then `tryMoveNodeFromTo`: `tryTextCommitOps` then `ChildListWire.removeRange` and `ChildListWire.insertAt`. `tryTextCommitOps` is `RunEditCommit.commitTextOps`, which reads `node.text`.

`UpdateHelpers.applyAndPost` calls `SyncPlanner.enqueuePending`. `tryStartSubmit` keeps the event on the queue while `syncState` is `Polling` or `Sending`, and it does not open a second post. `App.runPollServer` calls `SyncLogic.getPollOutcome` with `model.eventId` at the moment the response arrives. `getPollOutcome` returns `Some DataOutdated` whenever `poll.eventId` is greater than that id.

`Update.applyPollDataOutdated` sets `DataOutdated` when `pending` is non-empty. It does that before it applies the Poll events, and it does not emit `SubmitPendingBatch`.

## Repro

Honest end-of-line Enter, then Tab, on a `FileAgent` mailbox. Each post alone, and the three posts in one batch. A `eventsSince` read sat between the posts. Every honest post returned `Ok external=False`. After Tab, line 2's owner parent was line 1 and its text was `two`.

The create ack's `eventId` was 1. The next `graphAtEvent` tip was 3. The log tail was the Change plus `Notice.NodePersisting` and `Notice.NodePersisted` on the root id. Those notices landed after the ack snapshot.

The client fact then did the overlap those notices create:

1. `syncState` is `Polling`. `pending` holds one local Change. `eventId` is still `EventId.zero`.
2. `SyncPlanner.enqueuePending` leaves `syncState` at `Polling`, grows `pending` to 2, and emits no `SubmitPendingBatch` (`submit=False`).
3. The Poll body is one `Notice.NodeUnpersisted` whose id is ahead of the client.
4. `SyncLogic.getPollOutcome` returns `Some DataOutdated`.
5. `Update.update` of `PollDone` sets `syncState` to `DataOutdated` and leaves the pending Change unsent.

That is the banner. The server never saw a bad Enter or Tab.

`RunEditCommit.commitTextOps` on a node whose graph text is `beta` and whose live text is `beta live` emits `SetText(id, "beta", "beta live")`. `UpdateHelpers.adjustModeAfterServerApply` left `Editing` in place when focus sat on node B and node A's text had changed. It switched to `Selecting` when focus sat on the node whose text had changed.

Probe log:

```
1 create line: Ok eventId=1 external=False events=1
poll after create: count=3 tip=3 kinds=Notice NodePersisted (...) | Notice NodePersisting (...) | Change
2 enter new line: Ok eventId=4 external=False events=1
poll after enter: count=2
3 tab indent: Ok eventId=7 external=False events=1
after tab parentIsLine1=True line2Text=two
batch create+enter+tab: Ok eventId=3 external=False events=3
setText old text mismatch: Ok eventId=10 external=True events=1
replace old span mismatch: Error old span out of bounds
landed line2 parentIsLine1=True
enqueue while Polling: state=Polling pending=2 submit=False
getPollOutcome while pending: Some(DataOutdated)
PollDone syncState=DataOutdated
PollDone pendingLeft=1
commitTextOps=[SetText (..., "beta", "beta live")]
adjustMode focus B while A text changed: Editing ("beta draft", EndOfText)
adjustMode focus A whose text changed: Selecting
```

## Root cause

A commit during an in-flight Poll stays on `pending` and is not posted. The Poll's event id is ahead because persist notices were logged after the previous ack. `getPollOutcome` treats that id as `DataOutdated`. `applyPollDataOutdated` keeps that state whenever `pending` is non-empty, drops the notice apply, and never posts the queued Enter or Tab.

The compare-and-swap for Tab is honest on this tree. `commitTextOps` sends the graph text as the old text.

## Proposed fix

Keep every server check. The client sends the same ops the probe posted.

On `PollDone`, when the tail is notices and `pending` is non-empty:

- Apply the notices with `SyncLogic.applySyncResponse`.
- Leave `pending` in place.
- Set `syncState` so `SyncPlanner.tryStartSubmit` emits `SubmitPendingBatch` for that queue.
- Leave `DataOutdated` for a tail that fails to apply.

`getPollOutcome` can stay a pure id compare. `applyPollDataOutdated` is the branch that must stop treating a notice tail plus a local queue as a stale view.

## Files a fix would touch

- `src/Client/Update.fs` — `applyPollDataOutdated`
- `src/Client/App.fs` — `runPollServer`
- `src/Shared/SyncLogic.fs` — `getPollOutcome`, `applySyncResponse`
- `src/Shared/SyncPlanner.fs` — `tryStartSubmit`, `enqueuePending`
- `tests/Server.Tests/BrowserResidencyClientTests.fs` — extend the existing `PollDone leaves Sync Idle when the edit box holds new text` fact with the pending-notice case

## Ticket 10 and its report

The August report's line numbers are stale. The symbols below are the ones on this tree.

### Ticket checkboxes

[x] Handled for the post. `RunEditCommit.commitTextOps` sets the old text from `node.text`. `UpdateHelpers.commitTextEdit` takes `_originalText` and does not read it. `UpdateMove.tryMoveNodeFromTo` calls `tryTextCommitOps`.

[ ] Open for the snapshot. `CommandPalette.closeCommandPaletteOp` restores `returnTo` unchanged. `ViewModelRowState.editInputSeedText` and `retargetEditingSelection` replace a draft only when the draft is `""` and the node text is non-empty. A non-empty `Editing` string survives Poll and Load. `RowView` seeds `#edit-input` from that string, unwrapping palette and search `returnTo`.

[ ] Open for focus. `UpdateHelpers.adjustModeAfterServerApply` compares text for `focusedNodeId` of the current selection. `PollDone` calls `withSiteMap` first, and `withSiteMap` calls `refreshSelection` then `retargetEditingSelection`. The probe kept `Editing` when another node's text changed.

[x] The HITL failure string does not come from this Tab path. The browser HITL in the ticket was not run. Tab's `SetText` old text is the graph text, so a stale `returnTo` string is not the old text on the wire.

### Report mechanisms

[x] Mechanism 1, the reject. `UpdateHelpers.isAutoSyncBlocked` is false while the top-level mode is `CommandPalette`, `SearchDialog`, or `FileSearchDialog`, so Poll still applies under an overlay. After close, Tab's old text is the graph text from `commitTextOps`.

[ ] Mechanism 2, the focus. `adjustModeAfterServerApply` still follows the post-refresh focus. The following `SetText` uses that focus node's graph text, so the relocated focus does not send the previous node's snapshot as the old text.

[x] Mechanism 3, whitespace. A live field that differs from the graph becomes `SetText(graphText, liveText)`. The old text is the graph text.

[x] Mechanism 4, indent order. The probe posted `commitTextOps` then `ChildListWire.removeRange` then `ChildListWire.insertAt`. The server returned `Ok`, and the indented node was the child of the previous line.

[x] Mechanism 5, for Tab. Selection changes commit through `commitTextEdit`. Paste is a different path: `UpdatePaste` still builds `Op.SetText` from its `originalText` argument, taken from `Editing`.

## Probe

Thrown away after the run. It is not in the tree.

```fsharp
module Gambol.Server.Tests.StaleTypeIndentProbe

open System
open System.IO
open Xunit
open Gambol.Client
open Gambol.Server
open Gambol.Shared
open Gambol.Shared.ViewModel
open Gambol.Server.Tests.TestBackend

let private logPath = "/tmp/stale-probe.txt"

let private note (line: string) =
    File.AppendAllText(logPath, line + "\n")

let private show (result: Result<CoreChangesAccepted, string>) =
    match result with
    | Ok accepted ->
        $"Ok eventId={accepted.eventId.Value} external={accepted.externalChanges} events={accepted.events.Length}"
    | Error err -> $"Error {err}"

let private post host events =
    CoreMailboxApi.postEvents host testCaller events |> Async.StartAsTask

let private stateOf host = task {
    let! state = CoreMailboxApi.graphAtEvent host |> Async.StartAsTask
    return
        match state with
        | Ok value -> value
        | Error err -> failwith err
}

/// Enter at end of focus: new empty sibling after focus, plus SetText when live text differs.
let private enterOps (graph: Graph) (parentId: NodeId) (focusId: NodeId) (live: string) =
    let modelText = graph.nodes.[focusId].text
    let fresh = ChildNode.New()
    let kids = Graph.children graph parentId
    let index = kids |> List.findIndex (fun child -> child.id = focusId)
    let ops =
        [ Op.NewNode(fresh.id, "")
          ChildListWire.insertAt parentId kids (index + 1) [ fresh ]
          if live <> modelText then
              Op.SetText(focusId, modelText, live) ]
    fresh.id, ops

/// Tab: commit live text from the graph text, then move focus under the previous sibling.
let private indentOps (graph: Graph) (parentId: NodeId) (focusId: NodeId) (live: string) =
    let kids = Graph.children graph parentId
    let index = kids |> List.findIndex (fun child -> child.id = focusId)
    let prevId = kids.[index - 1].id
    let selected = [ kids.[index] ]
    let prevKids = Graph.children graph prevId
    RunEditCommit.commitTextOps focusId live graph
    @ [ ChildListWire.removeRange parentId kids index 1
        ChildListWire.insertAt prevId prevKids prevKids.Length selected ]

let private change ops =
    changeEvent "probe" EventId.zero (Guid.NewGuid()) ops

[<Fact>]
let ``probe server type enter indent and a poll between posts`` () = task {
    if File.Exists logPath then File.Delete logPath
    let dir = newTempDir ()
    let host, _ = createAdmittedFile dir
    try
        let line1, first = addRootChildEvent "one"
        let! postedFirst = post host [ first ]
        note ("1 create line: " + show postedFirst)

        let! afterFirst = stateOf host
        let! between =
            CoreMailboxApi.eventsSince host EventId.zero |> Async.StartAsTask
        let kinds =
            between.events
            |> List.map (fun event ->
                match event.body with
                | EventBody.Notice notice -> "Notice " + string notice
                | EventBody.Order (Order.Change _) -> "Change"
                | EventBody.Order _ -> "Order"
                )
            |> String.concat " | "
        note (
            "poll after create: count="
            + string between.events.Length
            + " tip="
            + string afterFirst.eventId.Value
            + " kinds="
            + kinds)

        let line2, splitOps =
            enterOps afterFirst.graph Graph.rootId line1 "one"
        let! postedSplit = post host [ change splitOps ]
        note ("2 enter new line: " + show postedSplit)

        let! afterSplit = stateOf host
        let! betweenAgain =
            CoreMailboxApi.eventsSince host afterFirst.eventId |> Async.StartAsTask
        note ("poll after enter: count=" + string betweenAgain.events.Length)

        let tabOps = indentOps afterSplit.graph Graph.rootId line2 "two"
        let! postedTab = post host [ change tabOps ]
        note ("3 tab indent: " + show postedTab)

        let! afterTab = stateOf host
        let parentOfLine2 =
            Map.tryFind line2 afterTab.graph.ownerParentByChild
        note (
            "after tab parentIsLine1="
            + string (parentOfLine2 = Some line1)
            + " line2Text="
            + afterTab.graph.nodes.[line2].text)
    finally
        CoreMailboxApi.dispose host

    let dir2 = newTempDir ()
    let host2, _ = createAdmittedFile dir2
    try
        let line1, first = addRootChildEvent "one"
        let! baseState = stateOf host2
        let appliedFirst =
            ChangeValidation.applyOps (eventOps first) baseState
        let g1 =
            match appliedFirst with
            | ApplyResult.Changed s -> s.graph
            | ApplyResult.Unchanged s -> s.graph
            | ApplyResult.Invalid (_, msg) -> failwith ("local first " + msg)
        let line2, splitOps = enterOps g1 Graph.rootId line1 "one"
        let g2 =
            match ChangeValidation.applyOps splitOps { baseState with graph = g1 } with
            | ApplyResult.Changed s -> s.graph
            | ApplyResult.Unchanged s -> s.graph
            | ApplyResult.Invalid (_, msg) -> failwith ("local split " + msg)
        let tabOps = indentOps g2 Graph.rootId line2 "two"
        let! batched =
            post host2 [ first; change splitOps; change tabOps ]
        note ("batch create+enter+tab: " + show batched)
        let! landed = stateOf host2
        let wrongOld =
            [ Op.SetText(line2, "not-the-graph-text", "later") ]
        let! wrong = post host2 [ change wrongOld ]
        note ("setText old text mismatch: " + show wrong)
        let kidsNow = Graph.children landed.graph Graph.rootId
        let bogus = ChildNode.owner (NodeId.New()) :: kidsNow
        let! staleSpan =
            post host2 [ change [ ChildListWire.replace Graph.rootId bogus kidsNow ] ]
        note ("replace old span mismatch: " + show staleSpan)
        note (
            "landed line2 parentIsLine1="
            + string (
                Map.tryFind line2 landed.graph.ownerParentByChild = Some line1))
    finally
        CoreMailboxApi.dispose host2

    let text = File.ReadAllText logPath
    Assert.Contains("1 create line: Ok", text)
    Assert.Contains("2 enter new line: Ok", text)
    Assert.Contains("3 tab indent: Ok", text)
    Assert.Contains("batch create+enter+tab: Ok", text)
    Assert.Contains("after tab parentIsLine1=True", text)
}

[<Fact>]
let ``probe client poll while a commit is pending`` () =
    if File.Exists logPath then () else File.WriteAllText(logPath, "")
    let graph = Graph.create ()
    let nodeId = NodeId.New()
    let node = Node.Create(nodeId, text = "one")
    let graph =
        graph
        |> Graph.addDetachedNode node
        |> Graph.replace Graph.rootId 0 [] [ ChildNode.owner nodeId ]
        |> function
            | Ok g -> g
            | Error err -> failwith err
    let pending =
        ClientHistory.mintChange
            "Split"
            [ Op.SetText(nodeId, "one", "one!") ]
    let model =
        { VmTestHelpers.emptyModel graph with
            syncInfo =
                { SyncInfo.initial with
                    syncState = Polling
                    pending = [ pending ] }
            eventId = EventId.zero }
    let queued, effects =
        SyncPlanner.enqueuePending
            (ClientHistory.mintChange "Indent" [])
            model.eventId
            model.syncInfo
    note (
        "enqueue while Polling: state="
        + string queued.syncState
        + " pending="
        + string queued.pending.Length
        + " submit="
        + string (
            effects
            |> List.exists (function
                | SubmitPendingBatch _ -> true
                | _ -> false)))
    let notice =
        { id = EventIdFixtures.storedId 2
          submissionId = Guid.NewGuid()
          authority = Authority ""
          commandName = ""
          body = EventBody.Notice(Notice.NodeUnpersisted nodeId) }
    let poll =
        { eventId = notice.id
          buildEpochSec = 1
          pageBuildEpochSec = 1
          apiVersion = ApiVersion.current
          isReady = true
          externalChanges = false
          events = [ notice ]
          message = None
          bootstrapHash = None
          nodes = []
          childMap = Map.empty }
    let outcome = SyncLogic.getPollOutcome poll model.eventId
    note ("getPollOutcome while pending: " + string outcome)
    let response = SyncLogic.changeSuccessToSync poll
    let next, _ =
        Update.update
            (SysMsg(
                PollDone(outcome, response, Some true, Some poll.eventId)))
            model
    note ("PollDone syncState=" + string next.syncInfo.syncState)
    note (
        "PollDone pendingLeft="
        + string next.syncInfo.pending.Length)
    Assert.Equal(Some DataOutdated, outcome)
    Assert.Equal(DataOutdated, next.syncInfo.syncState)

[<Fact>]
let ``probe tab uses graph text and adjustMode uses focus`` () =
    if File.Exists logPath then () else File.WriteAllText(logPath, "")
    let a = NodeId.New()
    let b = NodeId.New()
    let graph =
        Graph.create ()
        |> Graph.addDetachedNode (Node.Create(a, text = "alpha"))
        |> Graph.addDetachedNode (Node.Create(b, text = "beta"))
        |> Graph.replace
            Graph.rootId
            0
            []
            [ ChildNode.owner a; ChildNode.owner b ]
        |> function
            | Ok g -> g
            | Error err -> failwith err
    let ops = RunEditCommit.commitTextOps b "beta live" graph
    note ("commitTextOps=" + string ops)
    match ops with
    | [ Op.SetText(id, oldText, newText) ] ->
        Assert.Equal(b, id)
        Assert.Equal("beta", oldText)
        Assert.Equal("beta live", newText)
    | other -> Assert.Fail($"expected one SetText, got {other}")
    let siteMap, _ =
        ViewModel.buildSiteMapFrom graph Graph.rootId (Sid 0)
    let root = siteMap.entries.[siteMap.rootId]
    let focusB =
        { range = { parent = root; start = 1; endd = 2 }
          focus = 1 }
    let changedA =
        match GraphMutate.setText None a "alpha" "ALPHA" graph with
        | Ok g -> g
        | Error err -> failwith err
    let editingB =
        { VmTestHelpers.emptyModel changedA with
            selectedNodes = Some focusB
            mode = Editing("beta draft", EditCaret.EndOfText) }
    let after =
        UpdateHelpers.adjustModeAfterServerApply graph editingB
    note ("adjustMode focus B while A text changed: " + string after.mode)
    match after.mode with
    | Editing _ -> ()
    | other -> Assert.Fail($"expected Editing to remain, got {other}")
    let focusA =
        { range = { parent = root; start = 0; endd = 1 }
          focus = 0 }
    let editingA =
        { editingB with
            selectedNodes = Some focusA
            mode = Editing("alpha", EditCaret.EndOfText) }
    let cleared =
        UpdateHelpers.adjustModeAfterServerApply graph editingA
    note ("adjustMode focus A whose text changed: " + string cleared.mode)
    Assert.Equal(Selecting, cleared.mode)
```
