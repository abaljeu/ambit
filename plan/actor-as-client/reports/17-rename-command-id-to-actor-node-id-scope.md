# 17 — Rename commandId to actorNodeId — scope report

Mode: implement. Ticket: [17 — Rename commandId to actorNodeId](../issues/17-rename-command-id-to-actor-node-id/17-rename-command-id-to-actor-node-id-ticket.md).

### Signatures

[ ] `ActorStart` in `src/Shared/History.fs`: `{ zoomId: NodeId; focusId: NodeId; commandId: NodeId; graphIds: NodeId list; eventId: EventId }` becomes `actorNodeId` on the third field only.
[ ] `LoadSaveCommandRequest.start` is `ActorStart`; it inherits the field rename.
[ ] `CommandRequest.actorStart` in `src/Shared/CommandRequest.fs`: `(graph: Graph) (siteMap: SiteMap) (zoomId: NodeId) (focusId: NodeId) (commandId: NodeId) (eventId: EventId) : ActorStart` — rename the fifth parameter to `actorNodeId` and the record field in the body.
[ ] `CommandRequest.commandOnOwnerPath` in `src/Shared/CommandRequest.fs`: `(graph: Graph) (focusId: NodeId) (zoomId: NodeId) : NodeId option` renames to `actorNodeOnOwnerPath` with the same parameters and return type.
[ ] `CommandRequest.tryStart`, `tryQueryStart`, `oneNodeStart` keep their public names; they call `actorNodeOnOwnerPath` / `actorStart` internally.
[ ] `EventJson.encodeStartRequest` / `decodeStartRequest` in `src/Shared/EventJson.fs`: `(start: ActorStart)` — record field becomes `actorNodeId`; wire key stays `"commandId"`.
[ ] `EventJson.startRequestFields` (private) maps `start.actorNodeId` to JSON key `"commandId"`.
[ ] `ActorInput` in `src/Server/Core/CoreActorPool.fs`: `{ graph; zoomId; focusId; commandId; sessionId; secret }` — field becomes `actorNodeId`.
[ ] `FunctionStart` in `src/Server/Core/CoreActorPool.fs`: `{ actor; getGraph; focusId; commandId }` — field becomes `actorNodeId`.
[ ] `CoreActorPool` private `LiveRow`: `{ focusId; commandId; sessionId; inbox; cancel; pending }` — field becomes `actorNodeId`.
[ ] `CoreActorPool.commandIsLive` keeps its name; parameter `commandId: NodeId` may rename to `actorNodeId` for consistency (ticket does not forbid it).
[ ] `SearchActor.actorStart`, `isQueryRequest`, `functionStart` in `src/Server/SearchActor.fs` read `request.commandId` today.
[ ] `TestActor.dispatch` in `src/Server/TestActor.fs` reads `input.commandId`.
[ ] `RunAgentActor` private helpers `commandArgs`, `firstBehaviorToken`, and `completeGrok` read `input.commandId`; `GrokBotWakeArgs.CommandId` stays the wake payload property name.

### Match sites

[ ] `History.fs`: `ActorStart` record field declaration only (no active pattern on the field name in this file).
[ ] `CommandRequest.fs`: `actorStart` record construction `{ commandId = commandId }`; `tryStart` `| Some commandId ->` binds scan-stop id passed to `actorStart`; `isAmbleScanStop` calls `commandOnOwnerPath`; `tryQueryStart` calls `actorStart` with `focusId` for both focus and command positions; `oneNodeStart` passes `nodeId` three times into `actorStart`; private `noRunnableCommand` string value `"No command found"` stays; binding renames to `noRunnableActor`.
[ ] `EventJson.fs`: `startRequestFields` encode line 48; `decodeStartRequest` field bind lines 62–63; nested `encodeLoadSaveCommandRequest` / order encoders use `encodeStartRequest` / `decodeStartRequest` (lines 101, 112, 324, 329, 361, 367).
[ ] `CoreActorPool.fs`: `commandIsLive` compares `row.commandId`; `admitStart` / `admitFunctionStart` use `request.commandId`; `putActorStart` sets `ActorInput.commandId` and passes `request.commandId` to `putLive`; `runStartActor` `Map.tryFind request.commandId`; `putFunctionStart` passes `request.commandId`; `putLive` local parameter `commandId` and `LiveRow` field; `startLoadSaveCommand` passes `request.start` through (field inside `ActorStart`).
[ ] `SearchActor.fs`: `actorStart` sets `commandId = graph.root`; `isQueryRequest` `Map.tryFind request.commandId`; `functionStart` binds `lineId = request.commandId` and sets `FunctionStart.commandId`.
[ ] `TestActor.fs`: `dispatch` `Map.tryFind input.commandId`.
[ ] `RunAgentActor.fs`: `commandArgs` and `firstBehaviorToken` `Map.tryFind input.commandId`; `completeGrok` `CommandId = nodeGuid input.commandId` (wake record property name unchanged).
[ ] `UpdateWorkspaceLoad.fs`: `loadRequestFor` record `{ commandId = nodeId }`.
[ ] Callers that use positional `CommandRequest.actorStart` without labeling the field (no Output edit required unless they use record syntax): `src/Client/UpdateHelpers.fs` `loadSaveCommandRequest`; `src/Client/UpdateWorkspaceSync.fs`; `src/Client/Commands.fs` `tryStart` / `tryQueryStart`.
[ ] `SearchActor.actorStart` caller: `src/Server/Api.fs` (positional; no field name in caller).
[ ] Pool API callers pass `Gambol.Shared.ActorStart` through: `CoreEventDispatch.actorStart`, `CoreMailboxApi.startActor`, `CoreActorPool.startActor` / `startPeerActor`, `DeskLoadPush.startLoadSaveCommand`, tests and harnesses in Outputs.

### Existing tests

[ ] `CommandRequestTests.fs` — ``oneNodeStart uses the current Node as Command Zoom and Focus`` asserts `request.commandId`.
[ ] `CommandRequestTests.fs` — ``commandOnOwnerPath finds ? ancestor and keeps Focus distinct`` and ``commandOnOwnerPath is none when Zoom is below the Command`` call `CommandRequest.commandOnOwnerPath` (rename to `actorNodeOnOwnerPath`).
[ ] `CommandRequestTests.fs` — ``tryStart encodes distinct Focus Command and Zoom from owner-scan`` and ``tryStart one-Node when Focus is the Command`` assert `request.commandId`.
[ ] `CommandRequestTests.fs` — ``oneNodeStart is actorStart with zoom focus and command equal`` compares `expected.commandId`.
[ ] `CommandRequestTests.fs` — ``tryStart errors when no runnable Command is on the path`` expects error `"No command found"` (string unchanged).
[ ] `CommandRequestTests.fs` — ``tryQueryStart uses the equals line as focus and command`` asserts `request.commandId`.
[ ] `CommandRequestTests.fs` — ``isCommandText`` / ``isScanStopText`` tests stay; they do not read `ActorStart`.
[ ] `EventJsonTests.fs` — ``Ev JSON round-trips ActorStart and ActorStop`` builds `ActorStart` with `commandId` guid.
[ ] `EventJsonTests.fs` — ``An old actorStart or cancel row is skipped on read`` embeds JSON key `"commandId"`.
[ ] `EventJsonTests.fs` — ``Ev JSON round-trips each EventBody.Order case`` builds `start` with `commandId` and includes `Order.StartActor` / `Order.StartPeerActor` / `LoadSave`.
[ ] `SerializationTests.fs` — ``ActorStart Command request round-trips named Core ids`` round-trips `EventJson.encodeStartRequest` / `decodeStartRequest` and asserts JSON contains `"commandId"` wire key.
[ ] `ActorLiveTests.fs` — private `actorStartAt` sets `commandId = NodeId.New()`; many Facts use `commandId` as local NodeId for graph layout (not always the record field name).
[ ] `SyncLogicTests.fs` — ``applyServerTail ActorStart adds and ActorStop removes a live Focus`` uses helper `actorStartEvent` with `commandId = NodeId.New()`.
[ ] `RunEditCommitTests.fs` — ``edit then Run queues ActorStart behind the edit in one batch`` and ``edit then Run on an equals line queues ActorStart behind the edit`` assert `request.commandId`.
[ ] `EventTests.fs` — helper `startRequest ()` sets `commandId = NodeId.New()` for ActorStart fixtures.
[ ] `MailboxLogTests.fs` — ``StartPeerActor carries Shared PeerActorName`` builds `ActorStart` with `commandId = Graph.rootId`.
[ ] `SearchActorTests.fs` — ``actorStart records the root`` and ``postSearch records ActorStart at the root`` assert `start.commandId = graph.root`.
[ ] `CoreActorPoolDeliverTests.fs` — ``second start on the same commandId is rejected`` uses `startAt` helper with `commandId` field; test name may stay (refers to concept).
[ ] `CoreActorPoolFunctionStartTests.fs` — ``function start does not invoke Graph carrier`` / ``TestActor function start posts hello from Graph carrier`` / ``ID-bag startActor still starts TestActor hello`` use `FunctionStart` / `ActorStart` with `commandId`.
[ ] `CoreMsgActorCasesTests.fs` — ``StartActor with live credentials calls startActor with ActorStart`` asserts `handed.commandId`.
[ ] `CoreMailboxDoorTests.fs` — ``CoreActorPool.startActor selects actor from command node text`` / ``CoreActorPool.startActor fails when commandId not in graphIds`` / ``CoreMailboxApi.startActor appends ActorStart to lifecycle events`` read `started.commandId` or build requests with `commandId`.
[ ] `TestActorHelloTests.fs` — all Facts use `sampleRequest` with `commandId` on `ActorStart`; ``TestActor hello reply sits under question Focus not Command`` asserts `request.commandId` vs `request.focusId`.
[ ] `TestActorCommandErrorTests.fs` — `sampleRequest` and HTTP fixtures set `commandId` on `ActorStart`.
[ ] `QueryActorTests.fs` — ``question text is not a query request`` and ``edit then ActorStart classifies the equals line after the edit`` build `ActorStart` with `commandId`.
[ ] `OrderedEventStreamTests.fs` — `launch` helper and multiple door tests assert `start.commandId`.
[ ] `ApiPostCommandTests.fs` — `sampleRequest` and postCommand tests assert `started.commandId`.
[ ] `AskCancelHarness.fs` — shared helpers set and assert `request.commandId` (no ``Fact`` names in file; consumed by AgentAskTests, CancelByFocusTests, others).
[ ] `LoadSaveCommandTests.fs` / `LoadSaveCommandClientTests.fs` — `request` / seed helpers set `start.commandId`.
[ ] `ParseThreadLoadTests.fs` — `deskLoadRequest` helper sets `commandId`; used by ``held Desk Load does not push and the follow-up Load does`` and related Desk Load Facts.
[ ] `GithubTransportActorTests.fs`, `Issue41CoreMailboxTests.fs`, `Issue42PersistHandlersTests.fs`, `PersistHandlersRestoreTests.fs`, `MailboxHistoryDurabilityTests.fs`, `ActorCoreChangesDoorTests.fs` — seed `ActorStart` with `commandId = Graph.rootId` or similar.
[ ] `AgentAskTests.fs` and `CancelByFocusTests.fs` only bind local `commandId` NodeId variables; they do not read the `ActorStart` field name (likely no edit beyond compile if harness types change).
[ ] Ticket adds no new tests; `scripts/agent-test.sh --filter` per Output file is the check.

### Copy-from

[ ] No type or helper on this tree already names the field `actorNodeId`; rename in place on the signatures above.
[ ] Wire mapping pattern to keep: `EventJson.startRequestFields` writes `"commandId"` while the F# field becomes `actorNodeId` (same pattern as ticket Process step 4).

### implement

[ ] Ticket Input claims match the tree: `ActorStart.commandId` in `History.fs`, `CommandRequest.actorStart`, `EventJson` wire key `"commandId"`, pool types in `CoreActorPool.fs`, `SearchActor` / `TestActor` / `RunAgentActor`, `UpdateWorkspaceLoad.fs` — all verified.
[ ] Ticket claim `commandOnOwnerPath` → `actorNodeOnOwnerPath` and `noRunnableCommand` → `noRunnableActor` matches `CommandRequest.fs` lines 49–50 and 90–135.
[ ] Ticket claim error string `"No command found"` matches `noRunnableCommand` binding today.
[ ] Ticket claim `Order.Cancel` → `Order.CancelActor` is already true on this tree: `History.fs` `Order.CancelActor`, `EventBody.Cancel` maps to it, `EventJson` encodes `Order.CancelActor`, `CoreMailboxLoop` handles `Order.CancelActor`; no `Order.Cancel` in `src/`.
[ ] No ticket claim differs from code except the rename work itself.
[ ] No new tests in the ticket; existing Output tests observe the rename via public seams: `CommandRequest.tryStart` / `actorStart` / `actorNodeOnOwnerPath`, `EventJson.encodeStartRequest` round-trip (wire key unchanged), `CoreActorPool.startActor` rejection and live-row behavior, server Actor integration tests reading `ActorStart` on posted events.

### Order.CancelActor (112)

[ ] `Order.CancelActor of focusId: NodeId` is defined in `src/Shared/History.fs`.
[ ] `EventJson.fs` encodes with `Order.CancelActor focusId -> encodeCancel focusId` and decodes to `Order.CancelActor`.
[ ] `CoreMailboxLoop.fs` `placeOrder` handles `Order.CancelActor focusId`.
[ ] `CancelByFocusTests.fs` member ``cancelByFocus places Order.CancelActor`` is present.

### Wire key `"commandId"` (must stay)

[ ] `src/Shared/EventJson.fs` line 48 encode and lines 62–63 decode on `ActorStart` JSON.
[ ] `src/CloudAgents/Internal/GrokBotHttp.fs` line 19 maps wake payload `args.CommandId` to JSON key `"commandId"` (Grok wake, not `ActorStart`).

### commandOnOwnerPath and noRunnableCommand

[ ] `src/Shared/CommandRequest.fs` line 49 `noRunnableCommand = "No command found"`.
[ ] `src/Shared/CommandRequest.fs` line 90 `commandOnOwnerPath` function.
[ ] `src/Shared/CommandRequest.fs` line 109 `isAmbleScanStop` calls `commandOnOwnerPath`.
[ ] `src/Shared/CommandRequest.fs` line 134–135 `tryStart` calls `commandOnOwnerPath`; `Error noRunnableCommand`.
[ ] `tests/Shared.Tests/CommandRequestTests.fs` Facts named ``commandOnOwnerPath …`` and ``tryStart errors when no runnable Command is on the path``.

### Files to change (ticket Outputs)

[ ] Source (8): `src/Shared/History.fs`, `src/Shared/CommandRequest.fs`, `src/Shared/EventJson.fs`, `src/Server/Core/CoreActorPool.fs`, `src/Server/SearchActor.fs`, `src/Server/TestActor.fs`, `src/Server/RunAgentActor.fs`, `src/Client/UpdateWorkspaceLoad.fs`.
[ ] Tests (30): all paths listed under Outputs in [17 — Rename commandId to actorNodeId](../issues/17-rename-command-id-to-actor-node-id/17-rename-command-id-to-actor-node-id-ticket.md).
[ ] No source file outside Outputs contains the actor-node `commandId` record field or `.commandId` member access; positional-only callers (`UpdateHelpers.fs`, `Commands.fs`, `Api.fs`) need no Output-listed edit.
[ ] Stop condition: no file outside Outputs requires the actor-node field rename (`tests/CloudAgents.Tests/GrokBotOneshotTests.fs` uses JSON wake key only).

### Output test files — actor-node field touch map

[ ] `SyncLogicTests.fs` — helper `actorStartEvent`; Fact ``applyServerTail ActorStart adds and ActorStop removes a live Focus``.
[ ] `MailboxLogTests.fs` — ``StartPeerActor carries Shared PeerActorName``.
[ ] `RunEditCommitTests.fs` — ``edit then Run queues ActorStart behind the edit in one batch``; ``edit then Run on an equals line queues ActorStart behind the edit``.
[ ] `SerializationTests.fs` — ``ActorStart Command request round-trips named Core ids``.
[ ] `ActorLiveTests.fs` — `actorStartAt` helper; Facts that embed `ActorStart` via that helper (e.g. ``ActorStart adds and ActorStop removes a live Focus``).
[ ] `CommandRequestTests.fs` — all Facts listed in Existing tests that assert `commandId` or call `commandOnOwnerPath`.
[ ] `EventJsonTests.fs` — ``Ev JSON round-trips ActorStart and ActorStop``; ``An old actorStart or cancel row is skipped on read``; ``Ev JSON round-trips each EventBody.Order case``.
[ ] `EventTests.fs` — `startRequest ()` helper used across log/history Facts.
[ ] `GithubTransportActorTests.fs` — seed types and ops with `commandId: NodeId` on `ActorStart`.
[ ] `Issue41CoreMailboxTests.fs`, `Issue42PersistHandlersTests.fs`, `PersistHandlersRestoreTests.fs` — ActorStart fixtures with `commandId = Graph.rootId`.
[ ] `LoadSaveCommandClientTests.fs` — private `request` helper (all Load/Save Facts use it).
[ ] `LoadSaveCommandTests.fs` — git load seeds with `commandId` on `ActorStart`.
[ ] `MailboxHistoryDurabilityTests.fs` — ActorStart fixture with `commandId`.
[ ] `OrderedEventStreamTests.fs` — `launch` / stream Facts asserting `start.commandId`.
[ ] `ParseThreadLoadTests.fs` — `deskLoadRequest` helper for Desk Load Facts.
[ ] `QueryActorTests.fs` — ``question text is not a query request``; ``edit then ActorStart classifies the equals line after the edit``; equals-line `ActorStart` records.
[ ] `SearchActorTests.fs` — ``actorStart records the root``; ``postSearch records ActorStart at the root``.
[ ] `TestActorCommandErrorTests.fs` — `sampleRequest` and production door Fact with `commandId`.
[ ] `TestActorHelloTests.fs` — every Fact via `sampleRequest` / `commandId` on `ActorStart`.
[ ] `AgentAskTests.fs` — no record field identifier; uses harness only (optional zero-line change).
[ ] `ApiPostCommandTests.fs` — `sampleRequest`; ``postCommand through CoreMailbox encodes ActorStart in latestId events``; ``postCommand startActor can produce hello child``; ``production boot Run of ?test hello posts Owned hello child``.
[ ] `AskCancelHarness.fs` — helpers `seedBrowserCommand`, `startOnCommand`, assertions on `request.commandId`.
[ ] `CancelByFocusTests.fs` — local `commandId` only (harness).
[ ] `CoreActorPoolDeliverTests.fs` — `startAt`; ``second start on the same commandId is rejected``; focus exclusivity helpers.
[ ] `CoreActorPoolFunctionStartTests.fs` — `FunctionStart` / `ActorStart` builders with `commandId`.
[ ] `CoreMailboxDoorTests.fs` — multiple startActor Facts (see Existing tests).
[ ] `CoreMsgActorCasesTests.fs` — ``StartActor with live credentials calls startActor with ActorStart``.
[ ] `ActorCoreChangesDoorTests.fs` — ActorStart fixture `commandId = Graph.rootId`.

### Output files without actor-node record field

[ ] None of the 30 Output test files lack the `commandId` record-field literal or `.commandId` member; every Output test file has at least one `commandId` token in ripgrep.
[ ] `AgentAskTests.fs` and `CancelByFocusTests.fs` do not spell the record field; they still compile through `AskCancelHarness` after harness types update.

### Files NOT in Outputs with actor-node field

[ ] None under `src/` or `tests/` except Output-listed files (stop condition clear).

### Token classification (`src/` and `tests/`)

#### commandId

[ ] IN SCOPE (actor node id field or parameter): `History.fs`; `CommandRequest.fs` (param, record field, `tryStart` bind); `EventJson.fs` (F# field only); `CoreActorPool.fs`; `SearchActor.fs`; `TestActor.fs`; `RunAgentActor.fs` (`input.commandId`); `UpdateWorkspaceLoad.fs`; every Output test file hit listed above.
[ ] OUT OF SCOPE (editor `CommandId` parameter name): `src/Client/UpdateOps.fs` line 599 `(commandId: CommandId)`.
[ ] OUT OF SCOPE (dock helper function name): `src/Shared/CommandDockLayout.fs` line 75 `commandIds`.
[ ] OUT OF SCOPE (Grok wake JSON key string): `src/CloudAgents/Internal/GrokBotHttp.fs` line 19; `tests/CloudAgents.Tests/GrokBotOneshotTests.fs` JSON assert.
[ ] OUT OF SCOPE (documentation): `src/CloudAgents/README.md` protocol field list.
[ ] OUT OF SCOPE (local test variable name only, same NodeId): many tests use `let commandId = NodeId.New()` for graph nodes; rename optional; field access `request.commandId` / `start.commandId` is IN SCOPE.
[ ] OUT OF SCOPE (calls `commandIds` helper): `tests/Shared.Tests/CommandDockLayoutTests.fs`.

#### CommandId

[ ] OUT OF SCOPE (editor command union): `src/Shared/CommandEntry.fs`; `src/Client/Commands.fs`; `tests/Shared.Tests/CommandEntryTests.fs`.
[ ] OUT OF SCOPE (Grok wake record): `src/CloudAgents/PublicTypes.fs` `GrokBotWakeArgs.CommandId`; `RunAgentActor.fs` wake object initializer property name `CommandId`; `tests/CloudAgents.Tests/GrokBotOneshotTests.fs`.

#### commandIds / dockCommandIds

[ ] OUT OF SCOPE: `CommandDockLayout.commandIds`; `CommandIconLookup.dockCommandIds`; `CommandDockLayoutTests.fs`; `CommandIconsTests.fs`.

#### isCommandText / isScanStopText

[ ] OUT OF SCOPE (names stay): `CommandRequest.isCommandText`, `CommandRequest.isScanStopText`; `ActorLive.fs` calls; `CommandRequestTests.fs` Facts; `SearchActor.isQueryRequest` uses `isQueryText` only.

#### commandIsLive

[ ] IN SCOPE (body uses `row.commandId` / `request.commandId`): `CoreActorPool.fs` private `commandIsLive` and admit paths; function name not listed for rename in ticket.

#### commandName

[ ] OUT OF SCOPE (event / Ev metadata): widespread `Ev.commandName`, `commandName` on Change events, `MailboxLog`, `SyncLogic`, client `Controller`, tests — not actor-node id.

#### CommandEntry.CommandId

[ ] OUT OF SCOPE: `CommandEntry.fs` type and `CommandEntry.id: CommandId`.

#### LoadSaveCommand

[ ] OUT OF SCOPE (load-save feature names): `LoadSaveCommand.fs`, `LoadSaveCommandRequest`, `StartLoadSaveCommand`, `LoadSaveCommandClient`, route `/ambit/load-save-command`, tests exercising load-save — except where `LoadSaveCommandRequest.start` embeds `ActorStart` (IN SCOPE field inside `start` only).

#### GrokBotWakeArgs.CommandId

[ ] OUT OF SCOPE: property name and HTTP mapping in `GrokBotHttp.fs`; `RunAgentActor` assigns `CommandId = nodeGuid input.commandId` (right-hand `input` field becomes `actorNodeId`; left property stays `CommandId`).

### File counts

[ ] In-scope files to edit: 38 (8 source + 30 test from ticket Outputs).
[ ] Out-of-scope files under `src/` and `tests/` with a `commandId` token hit that must not receive the actor-node field rename: 7 (`UpdateOps.fs`, `CommandDockLayout.fs`, `CommandIconLookup.fs`, `GrokBotHttp.fs`, `CloudAgents/README.md`, `GrokBotOneshotTests.fs`, `CommandDockLayoutTests.fs`).
