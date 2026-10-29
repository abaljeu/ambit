# Missing-server-changes reload: findings

Source: `origin/staging` at `8e706123` (fetched 2026-10-28). Between `077b8538` and this tip, only `src/Shared/ChangeAmendment.fs` and `src/Server/Core/CoreMailboxLoad.fs` changed in `src`. No code changed. No PR opened. Tickets: [140](140-browser-stops-asking-for-a-reload.md), [141](141-server-stores-whole-child-lists.md), [142](142-view-shows-a-child-that-has-no-content.md), [143](143-browser-accepts-server-data.md), [144](144-server-writes-event-and-graph-together.md), [145](145-server-logs-a-notice-after-applying-it.md), [146](146-server-logs-startup-repair-deletions.md).

## 1. Model (Alan, 2026-10-28)

The page holds two maps:

- **content**: node id → node data
- **children**: node id → child list

The server sends data in the same two shapes, plus events. The page accepts what arrives. Accepting never fails.

- **Data** (a Want or Load answer: `nodes`, `childMap`): write each entry into its map.
- **Create-node event**: add the node.
- **Event that gives children to a node the page has**: replace that node's child list.
- **Event that gives data to a node the page has**: write that data.
- **Event that gives children or data to a node the page does not have**: ignore it.

**A listed child with no content is a valid state.** For example, another tab moves node X into P. This page has P but never had X, so the page ignores any data event for X while P's child list now names X. Nothing fails. P is simply not loaded (rule below). The graph already holds this state: `childMap` may name an id that is absent from `nodes`. Only the view must handle it (§2.4).

Loaded is derived and not stored. A node is loaded when the page has its child list and has content for each child in that list. Grandchildren are not part of the rule. Accepting data never sets loaded or unloaded directly.

Outcome: the page has no failing apply path. The reload prompt has no cause.

## 2. Does staging match the model?

### 2.1 Matches

- **Two maps.** `Graph` ([[src/Shared/Model.fs]] 243–254) has `nodes: Map<NodeId, Node>` and `childMap: Map<NodeId, ChildNode list>`. `parentByChild` and `ownerParentByChild` are indexes rebuilt from them (`Graph.fromNodes`).
- **Server data shape.** Poll, post reply, and Load replies carry `nodes: Node list` and `childMap: Map<NodeId, ChildNode list>` ([[src/Shared/ApiResponses.fs]] `ChangeSuccessResponse`, `LoadResponse`, `SyncResponse`). The wire's `childrenStatus: unloaded` ([[src/Shared/Serialization.fs]] ~215–262) only encodes "no child-list key". It is not a stored flag.
- **Events.** Every graph-changing event maps to one of the four cases (§3).
- **No stored loaded flag.** `GraphChildren.isLoaded` and `GraphChildren.status` ([[src/Shared/Model.fs]] 265–269) are derived from `childMap`.

### 2.2 Does not match (what tickets 141 and 143 remove or replace)

| Where | What it does now | Under the model |
| --- | --- | --- |
| `GraphChildren.isLoaded` / `status` ([[src/Shared/Model.fs]] 265–269) | loaded = the child-list key is present | loaded = key present **and** content for each child (143) |
| `SyncLogic.foldProjectedEvents` ([[src/Shared/SyncLogic.fs]] 118–153) → `ResidentProjection.applyOpsForSyncAllowing` ([[src/Shared/ResidentProjection.fs]] 120–200) → `Op.applyAllowing` ([[src/Shared/History.fs]]) → `GraphMutate` | re-runs each op with its checks: old-value compare, span bounds, "new child not found", name conflict, reserved names, the unparsed-document guard; on a compare failure, undoes all pending and retries | each op becomes one of the four accept writes; no checks (143) |
| `ResidentProjection.applyOpAllowing` ([[src/Shared/ResidentProjection.fs]] 8–34) | ignores a header op on an absent node (as the model says); also ignores a `Replace` on a present parent whose child list is absent | a `Replace` on any node the page has is accepted (143) |
| `ResidentProjection.installWantAnswer` ([[src/Shared/ResidentProjection.fs]] 207–228) | refuses the whole answer on a "dangling edge" | write both maps; a child with no content only means "not loaded" (143) |
| `SyncLogic.applyNoticeGraph` ([[src/Shared/SyncLogic.fs]] 92–101) | returns `Error` when `NoticeGraph.writeGraph` refuses a node | ignore the notice (143) |
| `SyncLogic.applyLoadResponse` "raced Load answer" ([[src/Shared/SyncLogic.fs]] 256–273) | refuses a Load answer | delete; accept like a poll (143) |
| `SyncLogic.rewindPendingBeforeWant`, `undoPendingGraph`, `reapplyLeftoverPending`, `consumeCatchUpPoll`, `CatchUpBaseline` | rewind local edits, apply, re-apply | not needed; accept on the current graph (§4) (143) |
| `SyncLogic.reconcileAck` suffix projection (`projectSuffixes`) | re-runs the server's added stamp ops; can fail | accept the reply's events like any event (143) |
| Boot novel events ([[src/Client/Program.fs]] ~140, `applyBootNovel`) | `applySyncResponse`; a failure refetches `/state` | same accept path (143) |
| `UpdateActorLive` ([[src/Client/UpdateActorLive.fs]] ~57) `SyncLogic.applyServerTail` | same failing path | same accept path (143) |
| `Op.Replace` content | `GraphMutate.replace` applies the old span at index 0. A stored `Replace` may name only a prefix of the list. Nothing enforces a whole list (`ChildListWire` is a convention). | a stored `Replace` always carries the whole old list and the whole new list, so "replace the child list" is exact (141) |

The page's **own** edits still run `GraphMutate` checks through `SyncLogic.applyLocalEvent`, `applyLocalUndo`, and `applyLocalRedo`. Those checks validate a user command before it is posted. They are not sync, and they stay.

### 2.3 Does not fit cleanly (questions)

- **Q1. Notices are rules, not values.** `NodeParsed` leaves PullingFile as it is. `NodePersisted` sets Persisted only when the written version is current ([[src/Shared/NoticeGraph.fs]] 82–100). The page can run the same `NoticeGraph.writeGraph` on a node it has, and ignore a refusal. That fits "accept data, never fail". The alternative is for the server to send the resulting document state. Default in 143: run the shared rule, and ignore a refusal.
- **Q2. Server writes with no event.** The page cannot accept what it is never sent.
  - The parse push marks a node Unparsed without logging (`CoreMailboxFilesAxis.markUnparsedThenPush`, called from `CoreMailboxLoop.pushReporting` ~311). The spec chose this ([[plan/core-refinement/spec.md]] §2 item 2: "The mailbox's direct Unparsed write needs no extra event").
  - New on this tip: `ChangeAmendment.markCreatedUnparsed` ([[src/Shared/ChangeAmendment.fs]] 164–190) marks a `NewSpecialNode` from `Parse` as Unparsed on the server only. The page creates it as Parsed.
  - Should both log `Notice.NodeUnparsed`? Not drafted.
- **Q3. Create-node data is partial.** `Op.NewNode` carries id and text. The page stamps `NodeUpdateTime.now ()` from its own clock ([[src/Shared/History.fs]] `applyAllowed`). The server's update time differs. That is minor, and not drafted.
- **Q4. A create for an id the page already has.** `GraphBuild.addDetachedNode` ([[src/Shared/GraphBuild.fs]] 287–295) overwrites the content. Default in 143: same (overwrite).

### 2.4 View rendering of a listed child with no content

The graph allows it. The view does not.

- `ViewModelSiteMap` builds one site entry per id in `GraphChildren.get` ([[src/Shared/ViewModelSiteMap.fs]] 82, 139, 243). It never checks `graph.nodes`, so a row is planned for X.
- `RowView.buildRowElement` ([[src/Client/RowView.fs]] 137) reads `model.graph.nodes.[nodeId]`. For X that throws, and the render crashes.
- Other unchecked reads that a row for X reaches:
  - `ViewModelDomPlan` ([[src/Shared/ViewModelDomPlan.fs]] 170)
  - start-edit in `ViewModelRowState` ([[src/Shared/ViewModelRowState.fs]] 168)
  - `UpdateHelpers` 335, `UpdateMove` 120, `UpdateOps` 54, 66, 519, 583
  - `Commands` 245, `ViewModelSearch` 223, `ViewModelJoinOps` 49, 120, 166
  - `ViewModelDeleteOps` 30
- Today staging never builds this state on the page, because F1 refuses the `Replace` and F6 refuses the answer. Accepting (143) creates it, so the view fix (142) must land first.
- **Fetch.** `Want.compose` ([[src/Shared/Want.fs]] 11–23) asks only for ids where `GraphChildren.isLoaded` is false. `ResidentProjection.wantAnswer` returns children and their content for a wanted parent, not the wanted id's own content. With today's `isLoaded` (key present), P counts as loaded and X's content is never asked for. With the derived rule (143), P is not loaded, P is in Want, and the answer brings X's content. That is the parent's normal load.
- **View behavior (142, proposed):** X shows as a placeholder row in its place, with an empty bullet and the text "…". The row can be selected and moved as a child of P. Editing, zooming, and commands that need its text return an error value: "not loaded yet". The row becomes a normal row when X's content arrives.
- **Q6.** Hiding the row instead would shift child positions: site entries use `childIndex`, and moves use the full child list. A placeholder keeps positions exact. Alan to confirm placeholder rather than hide.

## 3. Staging event types against the four cases

`EventBody` ([[src/Shared/History.fs]] 54–62), `Op` (5–19), `Notice` (38–51).

| Event | Case | Notes |
| --- | --- | --- |
| `Change`, `Undo`, `Redo` | contain ops | each op below |
| `Op.NewNode (id, text)` | create | adds the node with an empty child list |
| `Op.NewSpecialNode (id, kind, name)` | create | same; see Q2 for the Parse case |
| `Op.SetText`, `SetClasses`, `SetUpdateTime`, `SetDocumentState` | data | the new value; the old value is ignored |
| `Op.SetName` | data | name; a special node's text follows its name (same rule as `GraphMutate.setName`) |
| `Op.Replace (parent, old, new)` | children | `new` is the whole child list once 141 lands |
| `Notice.NodeParsed`, `NodeUploading`, `NodeReceived`, `NodeUnparsed`, `NodeUnpersisted`, `NodePersisted` | data | document-state fields by the shared rule (Q1) |
| `Notice.NodeParsing`, `NodePersisting`, `ParseFailed`, `PersistFailed` | no graph data | `PersistFailed` and `ParseFailed` show a message |
| `ActorStart`, `ActorStop`, `Cancel`, `Load` | no graph data | feed the actor live focus and launch results |

**No event type falls outside the four cases.** The only conditions are Q1 (notices are rules) and the whole-list `Replace` (141).

## 4. Local unsaved edits beside blind writes

- **Pending batch** (posted or queued, not yet confirmed): accept server data onto the current graph. Do not rewind or re-apply.
  - A node the server did not touch keeps the local edit.
  - A node it did touch shows the server value until the post's reply arrives. The reply's confirmed events carry the server's final value, which may be amended through `ChangeAmendment`. The page accepts them the same way, so it converges.
  - Nothing fails.
- **Typing in the edit field** (text only in the DOM): accept as usual. Stay in Editing, and leave the field as it is. Today `adjustModeAfterServerApply` ([[src/Client/UpdateHelpers.fs]] 286–298) switches to Selecting when the edited node changed, and the typed text is lost. On commit, `tryTextCommitOps` builds `SetText` from the live text (301–304). The person typing wins.
- **Q5.** A pending edit on a node the server then changed shows the server value until the post's reply comes back. Is that acceptable, or should the page re-apply pending on top for display? Default: accept as is (simplest; one poll interval at most).

## 5. The reload prompt (unchanged facts)

- `DataOutdated` ([[src/Shared/ViewModelSync.fs]] 14) is the only "missing server changes" state. Producer: `SyncLogic.getPollOutcome` ([[src/Shared/SyncLogic.fs]] 20–25) returns it whenever the server id is ahead. `Update.fs` sets it for:
  - an empty tail
  - a dirty edit
  - each apply `Error` (PollDone ~334–445, LoadDone ~494–513, post reply ~88 and ~106)
- Copy: [[src/Client/StatusView.fs]] 49 and [[src/Client/Overlays.fs]] 196–199. Reload click: [[src/Client/App.fs]] 613–615.
- It blocks posts and polls, so the tab stays stuck until the user reloads.
- `CodeOutdated` ("New version available") has no producer. It is dead.
- `ServerRejected` stays: the server refused the page's own post, or the reply does not match the batch.
- Server `requiresReload` is the server reloading its own graph. It is not a page concern.
- Boot `FallbackState` stays: it is silent and boot-only.
- [[doc/current/http-contract.md]] §2.2 items 7–11 (planned `baseSequence` / `409` / client rewind-and-retry) contradict the model.

## 6. Failure causes, resolved by the model

| # | Cause (summary) | Status |
| --- | --- | --- |
| F1 | `Replace` names a child the page lacks: `GraphMutate.replace` "new child not found" ([[src/Shared/GraphMutate.fs]] 287) | **Dissolved.** Accept the list. The parent is simply not loaded until the child's content arrives, and Want asks for it. The view shows a placeholder row ([142](142-view-shows-a-child-that-has-no-content.md)). |
| F2 | Server op re-run on top of local pending: "old span out of bounds" (282), "name conflict" (178) | **Dissolved.** There are no checks on accept (§4). |
| F3 | Compare mismatch after undo, silently skipped (`ResidentProjection.fs` 153) | **Dissolved.** There is no compare. |
| F4 | Unparsed-document guard on replay ([[src/Shared/History.fs]] 285) | **Removed** with the client op-apply path (143). |
| F5 | Server logs a notice before it applies it, so a refused write stays on the log: `CoreMailboxLoop.dispatchItem` (`logNotice` 259, then `applyNotice` 265) and `CoreEventDispatch.markForPersist` (219) | **Server fix: [145](145-server-logs-a-notice-after-applying-it.md).** The page no longer fails on it (Q1), but the log should hold only notices that applied. |
| F6 | Want answer "dangling edge" (`ResidentProjection.fs` 224) | **Removed** with `installWantAnswer`'s refusal (143). |
| F7 | "raced Load answer" (`SyncLogic.fs` 272) | **Dissolved.** The guard is deleted (143). |
| F8 | Projection written before the event row, not in one transaction (`DbAgent.fs` 482 via `CoreEventDispatch.persistNew` 308, then `store` 311 → `writePersistedEvent` 393). After a crash, `EventLog.recover` ([[src/Shared/EventLog.fs]] 132) drops the whole log, and `loadReconciled` clears the events table (`DbAgent.fs` 73). | **Server fix: [144](144-server-writes-event-and-graph-together.md).** |
| F9 | The startup repair deletes rows with no event (`DbAgent.applyMaintenance` 115; `DatabaseProjection.fs` 587) | **Server fix: [146](146-server-logs-startup-repair-deletions.md).** |
| — | Mid-edit refusal (`UpdateHelpers.isAutoSyncBlocked` ~266) | **Dissolved.** Accept, and keep the field (§4). |

## 7. Tests that cover these paths

- [[tests/Shared.Tests/SyncLogicTests.fs]]: the `getPollOutcome` tests (about 9), the `SyncInfo withSyncState …` risk tests
- [[tests/Shared.Tests/ClientHistoryRuntimeTests.fs]] (~186, ~250) and [[tests/Shared.Tests/SplitOriginTraceTests.fs]]: "raced Load answer"
- [[tests/Shared.Tests/AckReconcileTests.fs]]: ack reconcile and suffix projection
- [[tests/Shared.Tests/BrowserNodeEventTests.fs]] (ticket 48): Notice apply on the page
- [[tests/Server.Tests/BrowserResidencyClientTests.fs]]: drives `Update.update` with `PollDone`
- [[tests/Shared.Tests/BootCachePollTests.fs]]: boot novel apply
- [[tests/Server.Tests/DbAgentTests.fs]]: `requiresReload` and startup sweep

No test drives the `Update.fs` arms that set `DataOutdated`.

## 8. Overlap with Ambot2

- Open PRs on 2026-10-28: #466 (45, draft; server Core and server tests). No client sync files.
- #453, #455, and #468 are merged.
- 48 owns `SyncLogic.applyNoticeGraph`. 143 changes that function, so 48 should be `done` before 143 starts.
- 135 and 136 (document-state notices from ops) touch the notice rules that Q1 depends on.
- 45 (SetDocumentState leaves) removes one data op from §3. That does not change the case.
- On this tip, `CoreMailboxLoad.dispatchLoad` no longer logs `NodeUnparsed` (Ambot2 change). That is one more server write with no event (Q2).

## 9. Home and numbers

[[plan/core-refinement/project.md]]. Its Spec says "The client applies every log message" ([[plan/core-refinement/spec.md]] §2 item 2). On `8e706123`, the last ticket is 139, and no branch holds 140 or later.

Order:

- 140: no blockers
- 141 and 142 → 143
- 144, 145, 146: no blockers

The earlier draft of the failure analysis is at `/tmp/findings-v2-backup.md`, outside the drafts folder. It is superseded.

## 9. Alan's clarifications (call, 2026-10-28)
- A node's children are an optional list. `Some []` means the node is known to have no children. `None` means an unloaded node: the client has the node's data but not its child list.
- The new case is different: a child id appears in a parent's list, but the client has no data for that node at all. That is the placeholder row (ticket 142).

## Placeholder behavior (Alan, call 2026-10-28)
- The placeholder is the bullet row for a listed child with no node data. It exists like any row.
- Expand behaves the same as on an unloaded node.
- Select works the same as on any row. It responds to every command except edit.
- When the data arrives, the next render draws it as a normal row. Nothing special is needed; the renderer handles it.
