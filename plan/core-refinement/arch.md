# core-refinement architecture

Updated: 2026-10-17
Sequence: expand-contract

Home: [[project.md]]. This note is the sole authority for the Core seam (what will be coded): Target — Server Core, the expand-contract sequence (§3 and §9 Story paths), axes, stacks, path control, mailbox git handoff, the locking model (§6), and InMsg on the mailbox queue (§10 **Core loop**). Document-state events: [[spec.md]]. User stories for the rest of this Project are absent. Map decision 9 keeps Sequence `expand-contract`. Story paths, Shared segments, the Module map, and Seams are on this note. Expand-contract does not drop them. 
Claim marks for committed elements live under [[doc/current/]]. This note links to those pages. 
Map §4 (mailbox enable, call, and stop) stays in §3 step 6. It is not copied into [[doc/current/]].

Commitment: [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md). Core locking model: §6. Step 1 implement (shipped): [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md). Git use is Core (Target — Server Core). This note does not reopen that Commitment. This expand-contract sequence is the ticket source. Open beats that are not fully checked are published tickets. A beat that is fully checked does not get a new ticket.

## Target — Server Core

Commitment 2026-09-28 (Alan). Compact description of that same target: [[plan/architecture/server-core.md]]. This note is the sole authority for the Core seam; that page is the short description. One meaning, two roles — if they ever drift, this note wins. The Commitment does not chart coding tickets; the path is §3. Claim home for Core: [Core](doc/current/core.md).

### Dated note

1. **2026-09-28 inbound** — Alan recorded this Server description as a Commitment.
2. **2026-09-28 correction** — Git use is Core. Git Load, git Save, pull, push, and commit as the Server uses them are Core responsibilities, not outside work.
3. **2026-09-30 sole authority** — Alan: one home for the Core seam, not a reading path or index across several plan homes. This Project owns that future. The compact description stays on [[plan/architecture/server-core.md]].
4. **2026-10-17 graph monad** — Alan: the graph is in a monad. Production code modifies the graph only through the mailbox's monadic functions. The graph can be read. A constructor from a graph may exist for tests and for startup seeding. That constructor is not a path for runtime updates. Graph change Expand, Persist stack, Stack pull, Change, and Contract point at this end state.

## 1. Special-node Parse and Persist axes

A Workspace Node, a Directory Node, and a File Node each carry parse state and persist state. A Normal Node carries neither. The two states are independent. A node can be Unparsed and Unpersisted at the same time. They are informational, not locks. Terms: [GLOSSARY](../../GLOSSARY.md). Detail: [[spec.md]].

1. **Parse state** — Parsed, Unparsed, or PullingFile. PullingFile is memory-only. The column `document_state` stores Parsed or Unparsed only. The stored word for Parsed stays `current`. The stored word for Unparsed stays `unparsed`. On server reboot PullingFile is not true. `noServerFile` [o] obsolescent and `NoServerFile` [o] obsolescent are the code names until [49 — Rename NoServerFile to PullingFile](issues/49-rename-noserverfile-to-pullingfile.md). That ticket renames the code and does not change when the state is set. A second Load request, `CoreMailbox.mbLoadNode`, can repair a failure to load. Other repairs may come later. Parse cannot repair a file that never came. The parse thread runs when a node is Unparsed. NodeParsed sets Parsed when the node is Unparsed. PullingFile stays PullingFile (§10 **Core loop**).
2. **Persist state** — Unpersisted, or Persisted at a version. The persist thread runs when a node is Unpersisted and Parsed. NodePersisting sets Unpersisted. NodePersisted sets the Persisted version when the launch version is current (§10 **Core loop**).
3. **Graph change** — A graph change sets the nearest owning special node Unpersisted. It does not mark ancestors. The write is behind the mailbox. It has no state-change Op.
4. **Document-state events** — These messages change document state. `Op.SetText` and the other content ops stay.

| Event | Kind | Parse state | Persist state |
| --- | --- | --- | --- |
| NodeUploading | InMsg | PullingFile. Sets PullingFile in memory. Does not write PullingFile to `document_state`. Does not insert the node. | Unpersisted (`persisted` false) |
| NodeReceived | InMsg | Unparsed. Clears PullingFile. | Unpersisted (`persisted` false) |
| NodeParsing | InMsg | Parse started. Does not set Unparsed. Does not push. | unchanged |
| NodeParsed | InMsg | Parsed. Sets Parsed when the node is Unparsed. PullingFile stays PullingFile. | unchanged |
| NodePersisting | InMsg | unchanged | Unpersisted. Sets Unpersisted. |
| NodePersisted | InMsg | unchanged | Persisted version, when the launch version is current. An older version leaves Unpersisted. |

`InMsg` and `CoreMsg` are the same kind of thing. Both end up on the same event log. `InMsg.NodeUploading`, `InMsg.NodeReceived`, `InMsg.NodeParsing`, `InMsg.NodeParsed`, `InMsg.NodePersisting`, and `InMsg.NodePersisted` are `InMsg` cases posted through `CoreMailboxApi.addInMsg`. They are not Ops on a Change. The parse thread posts `InMsg.NodeParsing` and `InMsg.NodeParsed`. The persist thread posts `InMsg.NodePersisting` and `InMsg.NodePersisted`. `Api.postEvents` posts `InMsg.NodeUploading` when it runs the upload Change, and `InMsg.NodeReceived` when it runs the incoming-file Change. `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `InMsg.NodeReceived` for git Load. `WorkspaceUploadStructure.planStubOps`, `WorkspaceUploadStructure.markNoServerFile` [o] obsolescent, and `WorkspaceUploadStructure.planServerFilePresentOps` stay shared Op-list planners and do not post. `CoreMailboxLoad.applyInMsg` applies each message and logs it. The client receives every log message. There is no filter. The client learns of parsing from `InMsg.NodeParsing`. The mailbox's direct Unparsed write needs no extra event. The browser updates only its resident Graph. Display reads from that graph. The events name a Workspace Node, a Directory Node, or a File Node. `CoreMailboxLoad` `LoadNode` sets Unparsed and pushes and does not post a Node message. Who posts each `InMsg`, the persist stack, the stack pull, the persist version, undo, and non-File create are on Graph change, one rule per step. When [[spec.md]] is met, `Op.SetDocumentState`, `Op.SetPersistState`, and `Op.SetParseState` are not ops.

Only Core changes files, and only Core changes the graph. The states record which side Core just moved. Other operations set Unparsed and Unpersisted. Workspace lock and per-member persist locks are additional protocol beside the states: §6 Core locking model. Do not invent a Conflicted state. This section is not the project target; the project target is Target — Server Core. Claim home: [Workspace graph](doc/current/graph.md) (Document state).

## 2. Starting point

Current Core is the [[plan/core-creation/project.md]] baseline: mailbox, Actor pool, Graph, Events, and database backend. Today the Node field `documentState` stores the `DocumentState` record `{ parseState: ParseState; persistState: PersistState; noServerFile: bool }`. Parse-state changes go out as `Op.SetDocumentState`. `Op.SetPersistState` is not an Op. There is no standalone Node `parseState` or `persistState` field, and the record has no nested `documentState` field. `DocumentStateEnum` remains `Current` [o] obsolescent | `Unparsed` | `NoServerFile` for `Op.SetDocumentState` and for JSON/SQL encoding. `Current` [o] obsolescent is the old name for Parsed. `NoServerFile` [o] obsolescent is the old code name for PullingFile. Load after files land still uses today’s Parse / graph-push hop ([[plan/github-transport/arch.md]] Load / git Load). The Parse stack expand is coded but still started from [[src/Server/RouteRegistration.fs]] (`createPersistenceContext` builds `ParseStack` and calls `ParseThread.start`); that setup does not yet live only in [[src/Server/Core]]. The Core persist thread (async Persist stack: collectors and loop) is not stood up. File Persist today goes through [[src/Server/DocumentPersistChange.fs]]. Transport still locks, receives, and informs Core; after this Project, that thin remainder stays [[plan/github-transport/project.md]].

## 3. Expand-contract sequence

Ordered path from §2 Starting point to Target — Server Core. The expand-contract is **slow**: gradually create the Core pieces, and gradually shift use. This pace applies to the whole sequence, not only Persist. Each step expands the new form beside the old, migrates callers while both exist, then contracts the old. §9 Story paths are this same sequence. §10 Module map is the home of State, Interface, and Uses.

1. **State axes on special nodes**
   1. **Expand** — [x] Add `ParseState` and `PersistState` on special nodes beside `DocumentState`. Shipped: [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md). Detail: §4 Step 1 Commitment.
   2. **Migrate** — [x] Change the Node field `documentState` from `DocumentStateEnum` to the temporary four-field `DocumentState` record `{ parseState: ParseState; persistState: PersistState; noServerFile: bool; documentState: DocumentStateEnum }`. Dual-write the record and the standalone axes wherever state changes, and migrate old uses per §5 Axis-write mechanics. The four-field record is a valid temporary expand-contract state. Until this migrate, the Node field stays `DocumentStateEnum`.
   3. **Contract** — [x] Remove the standalone `parseState` and `persistState` Node fields and the obsolete nested `documentState` field from the record. The final `DocumentState` is exactly `{ parseState: ParseState; persistState: PersistState; noServerFile: bool }`. Keep `noServerFile` [o] obsolescent; do not remove it. Remove `DocumentStateEnum` after its callers have migrated. The Node field `documentState` remains and stores the `DocumentState` record.

2. **Parse stack**
   Charted approach (Alan, 2026-09-30). Same posture as Persist (§3 step 3 Core Persist stack): private stack, public push inside Core, consumer thread waits until push, then calls the existing parse body. Parse setup (stack, push, consumer thread) lives inside [[src/Server/Core]]. Outside Core, including [[src/Server/RouteRegistration.fs]], does not construct Parse, start it, or hold its handles; RouteRegistration may call a Core entry that boots Core, and does not see parse push/consumer. That move is not done. [[src/Server/RouteRegistration.fs]] calls [[src/Server/ParseThread.fs]] `bootCore`, which builds the stack and starts the consumer. Route registration does not hold the push function.
   1. **Expand** — [x] Stand up the one long-lived Parse stack beside today’s Load → Parse / graph-push hop. Product home: [[plan/parse-thread/project.md]]. Core may push reconcile targets. Web desk Load sends a core message whose subject is a Workspace Node, Directory Node, or File Node. The parse push marks that subject Unparsed, then pushes it onto this stack. The mailbox posts Events. Desktop-mapped Load and the git Load after-step use this stack. The client hop for those Loads is retired. Ticket: [21 — Contract Load onto the Parse stack](issues/21-contract-load-onto-the-parse-stack.md). See [03 — One Parse thread stack](issues/03-one-parse-thread-stack.md). The background loop that pulls the parse stack and runs the old parse function runs on a separate thread. First use case (Alan, 2026-09-29): an explicit parse command on a file (today’s `ParseFile` / `postParseFile`) — mailbox Load of a File node, push onto the stack, loop runs the old parse body. Ticket: [06 — Explicit parse command on a File (Load)](issues/06-explicit-parse-command-load-file.md) (Status `coded`).
   2. **Migrate**
      1. [ ] **Workspace lock handoff** — After §3 step 4 **§6 locks catch-up** Expand stands the workspace lock, that lock drains in-flight member file use, then pull (or Upload land) proceeds; arrived files are marked Unparsed; the lock releases; Unparsed starts the parse thread (push onto that stack). See [02 — Git Load: Unparsed then Parse stack](issues/02-git-load-unparsed-then-parse-stack.md) and §6 Core locking model. Transport only informs Core; Core owns the lock → Unparsed → parse-thread path.
      2. [x] **Parse push marks Unparsed** — The parse push marks a Workspace Node, Directory Node, or File Node Unparsed before that id is pushed. The mark writes the axis and does not enqueue. `requeueOnUnparsed` stays false. The Load command sends a core message and does not mark Unparsed itself. A node inside a File Node uses that File Node.
      3. [x] **File push** — Mailbox Load of a File Node pushes that node onto the Parse stack. Selection push has no special priority. Ticket: [06 — Explicit parse command on a File (Load)](issues/06-explicit-parse-command-load-file.md) (Status `coded`). See [05 — Selection-scoped Parse after whole-tree git Load](issues/05-selection-scoped-parse-after-whole-tree-git-load.md).
      4. [x] **Directory reconcile** — The parse thread runs Directory reconcile for a Directory Node and for a Workspace Node. The walk covers the Directory body. It adds a missing member node and deletes a member node that has no file. It does not mark an existing member Unparsed. Definition: [Parse thread architecture](../parse-thread/arch.md) §2 Module map, item 1 **Directory reconcile**. Ticket: [14 — Directory reconcile matches disk members](../parse-thread/issues/14-directory-reconcile-matches-disk-members.md) (Status `coded`).

      > Temporarily disabled (2026-10-08): MarkUnparsed does not enqueue; see requeueOnUnparsed in src/Server/ParseThread.fs. Disabling step: [28 — Stop enqueueing nodes just marked Unparsed](../github-transport/issues/28-stop-enqueueing-nodes-just-marked-unparsed.md). Re-enabled by [08 — Re-enable recursive update: enqueue on MarkUnparsed](../parse-thread/issues/08-re-enable-recursive-update-enqueue-on-markunparsed.md).

      5. [x] **Child Directory Unparsed** — A new child Directory Node starts Unparsed. An existing child Directory Node is not marked Unparsed. Ticket: [14 — Directory reconcile matches disk members](../parse-thread/issues/14-directory-reconcile-matches-disk-members.md) (Status `coded`).
      6. [x] **NodeParsed** — When the parse thread finishes a File Node or a Directory Node, it adds `InMsg` `NodeParsed` through the private function (§10). It does not edit the graph. `NodeParsed` is the renamed `ParseFinished`.
   3. **Contract** — [x] Web desk Load of a Workspace Node, Directory Node, or File Node posts through mailbox Load. Desktop-mapped Load and the git Load after-step use the Parse stack. The client hop for those Loads is retired. Claim home: [Parse and persist](doc/current/parse-persist.md) (Parse stack claims). Ticket: [21 — Contract Load onto the Parse stack](issues/21-contract-load-onto-the-parse-stack.md).

3. **Core Persist stack**
   Charted approach (Alan, 2026-09-29). Persist is not an Actor; it is a thread. See [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md). Setup location (Alan, 2026-09-30): Persist setup (collectors and the loop that calls existing persist functions) lives inside [[src/Server/Core]]. Outside Core, including [[src/Server/RouteRegistration.fs]], does not construct Persist, start it, or hold its handles; RouteRegistration may call a Core entry that boots Core, and does not see persist collectors. That setup stands in Core. This step’s Expand / Migrate / Contract is collectors and call-site shape only. Standing workspace lock / per-member persist locks and removing `withWorkTreeGate` are §3 step 4 **§6 locks catch-up**, not this Contract.
   1. **Expand** — [x] Make **new collector functions** beside today’s sync live-save. A **loop** on the persist thread will pull from the collection and **call the existing persist functions** ([[src/Server/DocumentPersistChange.fs]] / today’s Graph→disk writers). Those functions stay the write body. Do not invent a new write body in this step.
   2. **Migrate** — [x] **Change everyone to call the collectors.** Sync call sites move to collectors; the loop is the new feeder; the existing persist functions are what the loop calls. Do not delete those functions in this step.
   3. **Contract** — [x] Retire the old sync call-site shape once collectors and the loop feed the existing persist functions. The write body stays.
   **New behavior** (not today’s sync live-save): The persist thread runs when a node is Unpersisted and Parsed (§6). When it finishes, it adds `InMsg` `SnapshotDone` through the private function (§10). It does not edit the graph. Today the core loop sets Persisted from `SnapshotDone`. [Graph change persist version](#graph-change-persist-version) moves that set to `InMsg.NodePersisted`. There is no whole-graph snapshot. [Remove dead snapshot code](#remove-dead-snapshot-code). Unparsed files are not open for persist. The axis Commitment already says file Persist is blocked while Unparsed ([04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md)); the persist thread (collector loop) is where that block becomes real. Do not claim today’s code already does it. Lock protocol against in-flight writes during pull: §6 and §3 step 4 **§6 locks catch-up**. Claim home: [Parse and persist](doc/current/parse-persist.md) (Persist stack claims).

4. **§6 locks catch-up**
   Code still runs today’s `WorkspaceGit.withWorkTreeGate` on Persist and git paths. That exclusive gate is not the workspace lock ([01 — Persist/git work-tree gate](issues/01-persist-git-work-tree-gate.md) revoked). Axes stay drift markers; locks are §6. Do not drop the workspace lock. Do not invent a new write body or a new git process home: DocumentPersistChange stays the write body; mailbox/parse-thread placement stays as already charted. [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md) git Save is permitted while Unparsed or Unpersisted. Catch-up when the code meets §6 and the old gate leaves:
   1. **Expand** — [ ] Stand the workspace lock and per-member persist locks up beside `withWorkTreeGate`, matching §6 (pending, drain in-flight member file writes and parse reads, then pull, mark arrived files Unparsed, release). While the workspace lock is pending, new persist locks for member files cannot be taken.
   2. **Migrate** — [ ] Persist, parse-thread file use, and git Load/Save follow §6. `withWorkTreeGate` remains only while dual-running is required.
   3. **Contract** — [ ] Remove `withWorkTreeGate` from Persist and git paths once §6 is the only protocol. Claim home: [Parse and persist](doc/current/parse-persist.md) (workspace lock claims).

5. **Path control**
   1. **Expand** — [ ] Core owns four operations that hold absolute residency: read file, write file, read directory, write directory. Callers pass a node, or a relative path derived from a node, to say where ([[plan/transport-layer/map.md]]).
   2. **Migrate** — [ ] Classify each inventoried DataDir use as already one of those Core functions, or change it to that protocol (caller passes a node or a relative path derived from a node; Core holds the absolute residency). Inventory snapshot: [DataDir caller inventory](reports/datadir-caller-inventory.md). Do not invent migrate batches beyond that classification.
   3. **Contract** — [ ] No caller outside Core builds or holds a DataDir absolute path. Callers pass a node or a relative path derived from a node. Claim home: [Parse and persist](doc/current/parse-persist.md) (path control claims).

6. **Mailbox owns file and git work**
   Charted approach (Alan, 2026-09-29). Three beats for git requests. File work stays where earlier steps already require it; this step does not invent a file-migrate inventory.
   1. **6.1 Enable git requests** — [ ] Core/mailbox accepts git requests (expand). Actors remain defined outside Core and post to the mailbox; Core performs that work in the background with an end Event when needed (Target — Server Core · Core deals with). Keep today’s doors that already reach Core for Load / Save.
   2. **6.2 Call them** — [ ] Callers use those git requests (migrate).
   3. **6.3 Stop any old** — [ ] Stop the old git path (contract). Map §4. These three beats stay on this note. They are not claims on [[doc/current/]]. The Commitment (git use is Core; an Actor posts; Core performs the git work) has a claim home: [Core](doc/current/core.md) (Git use is Core) and [Actors](doc/current/actors.md) (the Actor posts).

## 4. Step 1 Commitment

Step 1 is **State axes on special nodes only**. Markers / Graph state only. It is sequence step 1 Expand above.

Acceptance: the two axes exist on special nodes and can be set and read.

This step does not start workers.

**Approach (Mikado)** — Commitment 2026-09-28 (Alan), corrected 2026-10-16. Create the new state axes. Set both the new axes and the old `DocumentStateEnum` wherever state changes. Migrate old uses over step by step. The existing four-field `DocumentState` record `{ parseState: ParseState; persistState: PersistState; noServerFile: bool; documentState: DocumentStateEnum }` is the valid temporary migrate shape. Contract the standalone axes and the record's obsolete nested `documentState` field. The final `DocumentState` is exactly `{ parseState: ParseState; persistState: PersistState; noServerFile: bool }`. Keep `noServerFile` [o] obsolescent. The Node field `documentState` remains and stores that record. This contract stores the three-field record on the Node. `DocumentStateEnum` remains for `Op.SetDocumentState` and for JSON/SQL encoding until those callers migrate. Graph change Contract removes `Op.SetDocumentState`. That removal is not this step.

Implement ticket: [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md) (Status `done` on github-transport). Claim home: [Workspace graph](doc/current/graph.md) (Document state).

## 5. Axis-write mechanics

Commitment 2026-09-28 (Alan). These rules say who writes each axis and which node they mark. [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md) still only adds the markers. It does not start workers. Claim home: [Workspace graph](doc/current/graph.md) (Document state). Axis-write rules stay in this section. The current page does not copy them. `InMsg` on the one mailbox queue is a Commitment 2026-10-01 (Alan). A private function adds `InMsg`. A public function adds `CoreMsg`. The queue puller hands the item to its handler. State, Interface, and Uses are §10 **Core loop**. Claim home: [Mailbox](doc/current/mailbox.md). The thread post is [Parse and persist](doc/current/parse-persist.md). Those pages do not copy this section.

1. **Writer target** — The core loop writes the axes on the live graph. The parse thread and the persist thread enqueue an axis completion. They do not edit the graph. Until the loop owns every axis write, set the axes at today’s file-edit sites and graph-edit sites.
2. **Graph edit** — Mark the nearest owning special node (File Node, Directory Node, or Workspace Node) **Unpersisted** only. Do not mark ancestors. The write is behind the mailbox. It has no state-change Op.
3. **Discovery** — Whoever finds a disk change writes the axis. Parse is not the discovery tool. A new member or a deleted member marks the Directory Node **Unparsed**. A modified file marks the File Node **Unparsed**.
4. **git pull finish** — After the workspace lock drains and files land (§6), `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` for a changed File Node already on the server. An incoming file is not that path: `WorkspaceUploadStructure.planServerFilePresentOps` stays a shared Op-list planner and does not post. `Api.postEvents` posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` when it runs that Change, and reconcile does not move PullingFile to Parsed. An added or removed member's Directory Node or Workspace Node is set Unparsed and pushed by the mailbox handler. It does not post NodeParsing (Graph change expand). The pull marks each file it received Unparsed. Each file is an entry in the git changed list, at any depth. An added or removed file marks its containing Directory Node Unparsed. A renamed file marks both Directory Nodes. When the containing Directory Node is absent, the pull marks the nearest ancestor Directory Node that exists. A file at the top of the workspace uses the Workspace Node. A modified file leaves its containing Directory Node Parsed. The changed list does not drive discovery otherwise. There is no separate axis path. The lock sequence stays §6. Commitment: [09 — Grill reconcile discovery depth](../parse-thread/issues/09-grill-reconcile-discovery-depth.md). Tickets: [12 — Git pull marks each received file Unparsed](../parse-thread/issues/12-git-pull-marks-each-received-file-unparsed.md), [13 — Git pull marks containing Directories Unparsed](../parse-thread/issues/13-git-pull-marks-containing-directories-unparsed.md).
5. **Persist done** — The persist thread sends `InMsg` `NodePersisted` and does not edit the graph. **Core loop** applies it (§10). NodePersisted records the graph version persist launched with. An older version leaves the node Unpersisted (Graph change persist version). There is no whole-graph snapshot. [Remove dead snapshot code](#remove-dead-snapshot-code). `PersistFinished` was the persist thread's file-write finish.
6. **Directory Parse done** — Mark that Directory Node **Parsed** only. `ParseThread` `finishParse` posts `InMsg.NodeParsed`. That case is the renamed `InMsg.ParseFinished` (Graph change expand). The same handler sets Parsed (§10). That completion does not change persist state. NodeParsed sets Parsed when the parse state is Unparsed. PullingFile is a parse value, so NodeParsed does not apply to it.
7. **Create special** — `Op.NewSpecialNode` stays an Op on the event log and inserts the node. NodeUploading then sets PullingFile in memory and Unpersisted. It does not insert the node and does not write PullingFile to `document_state`. An acceptable alternative, not chosen: the NodeUploading handler puts `Op.NewSpecialNode` on the log. `WorkspaceUploadStructure.markNoServerFile` [o] obsolescent stays a shared Op-list planner and does not post. `Api.postEvents` posts `InMsg.NodeUploading` through `CoreMailboxApi.addInMsg` when it runs that Change for an existing node. When the file arrives, `WorkspaceUploadStructure.planServerFilePresentOps` stays a shared Op-list planner and does not post. `Api.postEvents` posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg`. Reconcile does not post that incoming-file `InMsg.NodeReceived`, and reconcile does not move PullingFile to Parsed. A new Directory Node or Workspace Node is created by `postGraphOnly` or `postChange`. There is no new message for that create (Graph change apply). NodeReceived sets a File Node Unparsed and Unpersisted and clears PullingFile. NodeParsing means parse started. It does not set Unparsed and does not push. (Alan, 2026-10-15; event names 2026-10-17.)
8. **Load on Directory** — Load on Directory goes through that Directory's members on disk. A new directory on disk gets marked: it is added and starts Unparsed. A deleted directory gets marked: it is removed. A directory that already exists and is still present is untouched; this operation does nothing to it. Load on Directory does not mark a file Unparsed unless it is a new file. A new file on disk gets marked Unparsed. An existing file that is still present is left alone. A deleted file is removed. Workspace Load: Load on a Workspace has an additional existing step: it pulls files from the Git server. If Git shows a file is modified, that file is marked Unparsed. If Git shows a file is moved, deleted, or added, the containing directory is marked Unparsed. A modified file does not mark its directory. The Load command sends a core message whose subject is that Directory Node. Ticket: [14 — Directory reconcile matches disk members](../parse-thread/issues/14-directory-reconcile-matches-disk-members.md).
9. **Load on File** — The parse push marks the File Node **Unparsed** before it is pushed. The Load command sends a core message whose subject is that File Node. That push is coded. Ticket: [06 — Explicit parse command on a File (Load)](issues/06-explicit-parse-command-load-file.md) (Status `coded`). The Unparsed mark is [CoreMailboxLoad.fs](../../src/Server/Core/CoreMailboxLoad.fs) `markUnparsedThenPush`, called from `LoadNode`. The door is `CoreMailbox.mbLoadNode`. That handler sets Unparsed and pushes. It does not post a Node message. The client learns of parsing from `InMsg.NodeParsing` when the parse thread's stack puller posts it (Graph change stack pull). NodeParsing does not set Unparsed and does not push. A second `CoreMailbox.mbLoadNode` can repair a PullingFile failure to load. Other repairs may come later. Parse cannot repair a file that never came.
10. **Directory reconcile** — [Parse thread](../parse-thread/project.md) owns this. Definition: [Parse thread architecture](../parse-thread/arch.md) §2 Module map, item 1 **Directory reconcile**. It adds a missing member node, deletes a member node that has no file, and does not mark an existing member Unparsed. A new File Node and a new child Directory Node start Unparsed. Ticket: [14 — Directory reconcile matches disk members](../parse-thread/issues/14-directory-reconcile-matches-disk-members.md) (Status `coded`). A Workspace Node uses the same reconcile.

    > Temporarily disabled (2026-10-08): MarkUnparsed does not enqueue; see requeueOnUnparsed in src/Server/ParseThread.fs. Disabling step: [28 — Stop enqueueing nodes just marked Unparsed](../github-transport/issues/28-stop-enqueueing-nodes-just-marked-unparsed.md). Re-enabled by [08 — Re-enable recursive update: enqueue on MarkUnparsed](../parse-thread/issues/08-re-enable-recursive-update-enqueue-on-markunparsed.md).

11. **Parse stack pop** — If the node is already **Parsed**, the parse thread skips it. Real work arrives **Unparsed**. That skip is coded.
12. **InMsg** — `InMsg` and `CoreMsg` are the same kind of thing. Both are appended to the same event log. `InMsg` is not an Op. Today's cases: `NodeParsed`, `SnapshotDone`, `MarkUnparsed`, `NodeUploading`, and `NodeReceived`. `MarkUnparsed` leaves in Graph change contract. Graph change expand renamed `ParseFinished` to `InMsg.NodeParsed`. It is the same case. The same handler sets Parsed. `InMsg.NodeUploading` and `InMsg.NodeReceived` are logged and do not set state. [44 — Apply document state from Node events](issues/44-apply-document-state-from-file-events.md) owns those handlers. `InMsg.NodeParsing`, `InMsg.NodePersisting`, and `InMsg.NodePersisted` are later `InMsg` cases. Producers post Node messages through the private function. `CoreMailboxLoad.applyInMsg` applies each message and logs it in Graph change apply. NodeParsing means parse started and does not set Unparsed. Document-state messages are the Node messages in §1. They are not Ops on a Change.
13. **One mailbox queue** — The mailbox has one queue. A private function adds `InMsg`. A public function adds `CoreMsg`. The queue puller hands a `CoreMsg` to the `CoreMsg` handler and an `InMsg` to the `InMsg` handler. There is no second queue. The puller file is `src/Server/Core/CoreMailboxLoad.fs`. The private function is on the mailbox, `src/Server/Core/CoreMailbox.fs`.
14. **Private axes** — Persist state and parse state stay internal. The writer interface is **Core loop** (§10). A graph change sets Unpersisted behind the mailbox and has no state-change Op.

## 6. Core locking model

Commitment 2026-09-30 (Alan). Axes remain informational drift markers (§1), not locks. The workspace lock and per-member persist locks are additional protocol beside the axes. This section is current truth for Core-revision workers. Claim home: [Parse and persist](doc/current/parse-persist.md) (workspace lock claims). Planned architecture is claimed in [[doc/current/]], and intermediate steps stay on this note.

1. **Unparsed and Unpersisted** — The two directions of drift. Both can be true at once.
   1. **Unparsed** — Information on disk has not been pulled into the graph.
   2. **Unpersisted** — Information in the graph has not been written to disk.
2. **Core alone moves both sides** — Only Core changes files, and only Core changes the graph. The flags record which side Core just moved.
3. **Parse thread** — Runs when a node is Unparsed. On completion it adds `InMsg` `NodeParsed` through the private function (§10 **Core loop**). The core loop sets Parsed from `NodeParsed`. Graph change expand renamed `ParseFinished` to `InMsg.NodeParsed`. `ParseThread` `finishParse` posts `InMsg.NodeParsed`. The same handler sets Parsed.
4. **Persist thread** — Runs when a node is Unpersisted and Parsed. On completion it adds `InMsg` `SnapshotDone` through the private function (§10 **Core loop**). Today the core loop sets Persisted from `SnapshotDone`. Graph change persist version moves that set to `InMsg.NodePersisted`. There is no whole-graph snapshot. [Remove dead snapshot code](#remove-dead-snapshot-code).
5. **Other operations** — Set Unparsed and Unpersisted.
6. **Locks** — Prevent race conditions between the parse and persist threads and other operations that access files. Locks are held briefly; only long enough to transfer data. No one holds more than one lock, but aggregate locks exist.
7. **Workspace lock** — An aggregate lock. Announces that member files may change.
   1. **Take** — Waits while member files are already changing.
   2. **Pending** — While the lock is pending, new persist locks for those member files cannot be taken, so a persist cannot start between the announcement and the last in-flight write finishing.
   3. **Drain then pull** — When in-flight member file writes and parse reads have drained, the pull proceeds, the arrived files are marked Unparsed, and the lock releases.
   4. **After release** — Persist of a member then waits until that node is Parsed again.
8. **Per-member persist locks** — Additional protocol beside the axes. While the workspace lock is pending, new per-member persist locks for those member files cannot be taken.
9. **Parse and the workspace lock** — Parse does not take a persist lock. It reads the file. The core loop changes the graph. An in-flight parse of a member is still a use of that file, so the same pending lock drains those reads and holds new ones off until the lock releases. After release, Unparsed is what starts the parse thread.
10. **Unpersisted through pull** — A member can already be Unpersisted when the lock is requested, with no persist lock held yet. The pull still marks it Unparsed. The parse thread then runs, and the core loop brings the new file into the graph while Unpersisted is still set. A later persist can write that graph back over the file that just arrived. This is part of the model.
11. **File read/write backstop** — File read/write is strict buffer to/from. A write that finds the file still being written waits until the file is released, then proceeds. That wait is the filesystem backstop; the locks above are the protocol.

## 7. Vocabulary

1. Say **event source**, not ESO.
2. Do not say CAS.
3. Do not say Peer. Say Server git Actor, git Load, or git Save.
4. Name tickets in full (for example [05 — Selection-scoped Parse after whole-tree git Load](issues/05-selection-scoped-parse-after-whole-tree-git-load.md)).
5. Git use is Core. Do not imply git is outside Core.
7. Say **persist thread**, not persist actor. Persist is a thread; do not invent an Actor mailbox for it.
8. Say **InMsg** for the internal message. `InMsg` and `CoreMsg` are the same kind of thing. Both end up on the same event log. `InMsg.NodeUploading`, `InMsg.NodeReceived`, `InMsg.NodeParsing`, `InMsg.NodeParsed`, `InMsg.NodePersisting`, and `InMsg.NodePersisted` are `InMsg` cases posted through `CoreMailboxApi.addInMsg`. They are not Ops on a Change. They name a Workspace Node, a Directory Node, or a File Node. The client receives every log message. There is no filter. `CoreMailboxLoad.applyInMsg` applies each message and logs it. NodeParsing means parse started and does not set Unparsed. NodePersisting sets the node Unpersisted. The parse thread posts `InMsg.NodeParsing` and `InMsg.NodeParsed`. The persist thread posts `InMsg.NodePersisting` and `InMsg.NodePersisted`. `Api.postEvents` posts `InMsg.NodeUploading` when it runs the upload Change, and `InMsg.NodeReceived` when it runs the incoming-file Change. `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `InMsg.NodeReceived` for git Load. `WorkspaceUploadStructure.planStubOps`, `WorkspaceUploadStructure.markNoServerFile` [o] obsolescent, and `WorkspaceUploadStructure.planServerFilePresentOps` stay shared Op-list planners and do not post. NodeUploading sets PullingFile in memory and does not write PullingFile to `document_state`. `InMsg.MarkUnparsed` leaves in Graph change contract. Graph change expand renames `InMsg.ParseFinished` to `InMsg.NodeParsed`. The persist thread posts `InMsg.NodePersisted` and does not edit the graph.

## 8. Related

1. **Map** — [[map.md]] (decision 6–7, 10, 13, 15, 16)
2. **Axes Commitment** — [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md)
3. **Core locking model** — §6
4. **Server Core target** — Target — Server Core (this note); compact description [[plan/architecture/server-core.md]]
5. **Parse product home** — [[plan/parse-thread/project.md]]
6. **Transport remainder** — [[plan/github-transport/project.md]]
7. **InMsg** — §10 **Core loop**
8. **Message shape** — [[cqrs-messages.md]]

## 9. Story paths

Sequence stays `expand-contract`. These paths are that implementation sequence. Each hop names a module or a door. State, Interface, and Uses stay in §10 Module map.

1. **State axes on special nodes**
   1. [x] **Expand** — `ParseState` and `PersistState` exist on special nodes. [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md).
   2. [x] **Migrate** — A state change writes the standalone axes and the temporary four-field `DocumentState` record `{ parseState: ParseState; persistState: PersistState; noServerFile: bool; documentState: DocumentStateEnum }`. This migrate changes the Node field `documentState` from `DocumentStateEnum` to that record. Until this migrate, the Node field stays `DocumentStateEnum`.
   3. [x] **Contract** — Remove the standalone axes and the record's obsolete nested `documentState` field. The Node field `documentState` remains and stores the final record `{ parseState: ParseState; persistState: PersistState; noServerFile: bool }`. Keep `noServerFile` [o] obsolescent; remove `DocumentStateEnum` after its callers have migrated.

2. **Parse stack**
   1. [x] **Expand** — One parse thread consumes one Parse stack. [03 — One Parse thread stack](issues/03-one-parse-thread-stack.md).
   2. **Migrate**
      1. [ ] **Workspace lock** — Unparsed starts the parse thread after the workspace lock releases.
      2. [ ] **Client Load Unparsed** — Client Load on a Directory Node or a File Node marks that node Unparsed.
      3. [x] **File push** — Mailbox Load of a File Node pushes onto the Parse stack.
      4. [x] **Directory reconcile** — Directory reconcile walks the Directory body, adds a missing member node, deletes a member node that has no file, and does not mark an existing member Unparsed. A Workspace Node uses the same reconcile. Ticket: [14 — Directory reconcile matches disk members](../parse-thread/issues/14-directory-reconcile-matches-disk-members.md).

      > Temporarily disabled (2026-10-08): MarkUnparsed does not enqueue; see requeueOnUnparsed in src/Server/ParseThread.fs. Disabling step: [28 — Stop enqueueing nodes just marked Unparsed](../github-transport/issues/28-stop-enqueueing-nodes-just-marked-unparsed.md). Re-enabled by [08 — Re-enable recursive update: enqueue on MarkUnparsed](../parse-thread/issues/08-re-enable-recursive-update-enqueue-on-markunparsed.md).

      5. [x] **Child Directory Unparsed** — A new child Directory Node starts Unparsed. An existing child Directory Node is not marked Unparsed. Ticket: [14 — Directory reconcile matches disk members](../parse-thread/issues/14-directory-reconcile-matches-disk-members.md) (Status `coded`).
      6. [x] **NodeParsed** — File parse and Directory reconcile add `InMsg` `NodeParsed` through the private function. `NodeParsed` is the renamed `ParseFinished`.
   3. [x] **Contract** — Load uses that stack. Ticket: [21 — Contract Load onto the Parse stack](issues/21-contract-load-onto-the-parse-stack.md).

3. **Persist stack**
   1. [x] **Expand** — The persist thread calls the existing persist functions.
   2. [x] **Migrate** — Callers use the collectors. The write body stays.
   3. [x] **Contract** — The old sync call-site shape is gone.

4. **Workspace lock catch-up**
   1. [ ] **Expand** — The workspace lock and per-member persist locks stand beside `withWorkTreeGate`.
   2. [ ] **Migrate** — Persist, parse-thread file use, and git Load/Save follow §6.
   3. [ ] **Contract** — `withWorkTreeGate` is gone from Persist and git paths.

5. **Path control**
   1. [ ] **Expand** — Core owns read file, write file, read directory, and write directory.
   2. [ ] **Migrate** — A caller passes a node or a relative path derived from a node.
   3. [ ] **Contract** — No caller outside Core holds a DataDir absolute path.

6. **Mailbox git handoff**
   1. [ ] **Enable** — The mailbox accepts git requests.
   2. [ ] **Call** — Callers use those requests.
   3. [ ] **Stop** — The old git path is stopped.

7. **InMsg**
   1. [x] **Expand** — The mailbox queue accepts `InMsg` through a private function, beside the public function that adds `CoreMsg`.
   2. **Migrate**
      1. [x] **Parse thread** — The parse thread adds `InMsg` `NodeParsed` and `InMsg` `MarkUnparsed` through the private function. **Core loop** applies that `InMsg`.
      2. [ ] **Persist thread** — The persist thread adds `InMsg` `SnapshotDone` through the private function.
   3. [x] **Contract** — `CoreMsg` has no `SnapshotDone`. `Op.SetPersistState` is not a writer. `Op.SetDocumentState` is not the writer of the parsed axis.

8. <a id="remove-dead-snapshot-code"></a>**Remove dead snapshot code**
   1. [x] **Dead flush** — `flushSnapshot` leaves `DbAgent`, `FileAgent`, `PersistFilling`, `MailboxHost`, and `CoreMailboxApi`. `SavePrep.syncDataDir` does not call it. When `DbStatus` is not `Ok`, save still reads the event id. `prepareGitSave` and `flushForGit` stop passing a flush.
   2. [x] **No Graph** — The persist-finished signal carries a node id and no `Graph`. `applyInMsg` still sets `PersistState.Persisted` from that id. `DocumentPersistPath.stampNode` still stamps the graph the writer returns. The `graphEquals` copy leaves. `noteDiffDone` calls `countDiffMark`.
   3. [x] **File no-op** — `FileAgent` does not supply snapshot bookkeeping. The database filling keeps the diff-count hook, with no `Graph`.
   4. [x] **Rename the batch** — The follow-up file diff keeps its behavior and loses the snapshot names. `requestSnapshot` becomes `requestFileDiff`. `startSnapshot` becomes `startFileDiff`. `enqueueSnapshot` becomes `enqueueFileDiff`. `snapshotWorkspaces` becomes `diffWorkspaces`. `snapshotIds` becomes `diffIds`. `snapshotMarks` becomes `diffMarksLeft`. `snapshotNeeded` becomes `diffNeeded`. `snapshotInProgress` becomes `diffInProgress`. `persistedGraph` becomes `baselineGraph`. `snapshotPost` becomes `persistFinished`. `bindSnapshot` becomes `bindPersistFinished`. `PersistHandlers.snapshotDone` becomes `noteDiffDone` of `NodeId`. `opsNotifiedIds` keeps its name. The version check stays [46 — Record the persist version](issues/46-record-the-persist-version.md). Upload, import, parse, Load, undo, and persist stay working. Ticket: [58 — Remove dead snapshot code](issues/58-remove-dead-snapshot-code.md).

### Shared segments

1. **InMsg**
   1. [x] **Private add** — The parse thread or the persist thread adds `InMsg` through the private function.
   2. [x] **Core loop** — The queue puller hands `InMsg` to the `InMsg` handler. **Core loop** applies `InMsg`.

2. **Graph change**
   1. [x] <a id="graph-change-expand"></a>**Expand** — `Api.postEvents` posts `InMsg.NodeUploading` through `CoreMailboxApi.addInMsg` when it runs the upload Change. `Api.postEvents` posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` when it runs the incoming-file Change. `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` for a git-changed File Node already on the server. `ImportText.markDocumentCurrentBeforeParse`, `ImportDocument.planParseFile`, and `DirectoryReconcile.planDirectoryReconcile` stay unchanged and do not post. `planParseFile` and `planDirectoryReconcile` run inside the parse thread, so `ParseThread` `finishParse` posts `InMsg.NodeParsed`. `Api.postEvents` posts `InMsg.NodeParsed` through `CoreMailboxApi.addInMsg` when it runs the import Change. `Api.postParseFile` posts `InMsg.NodeParsed` through `CoreMailboxApi.addInMsg` beside `Op.SetDocumentState`. Each post is beside `Op.SetDocumentState`. `InMsg.NodeUploading` and `InMsg.NodeReceived` do not set state. Today's write still sets `NoServerFile` [o] obsolescent, which is PullingFile. `WorkspaceUploadStructure.planStubOps`, `WorkspaceUploadStructure.markNoServerFile` [o] obsolescent, and `WorkspaceUploadStructure.planServerFilePresentOps` stay shared Op-list planners and do not post. This step logs the posts. `CoreMailboxLoad.applyInMsg` does not apply `InMsg.NodeUploading` or `InMsg.NodeReceived`. [44 — Apply document state from Node events](issues/44-apply-document-state-from-file-events.md) owns those handlers. `InMsg.ParseFinished` is renamed to `InMsg.NodeParsed`. It is the same case. The same handler sets Parsed. `ParseThread` `finishParse` posts `InMsg.NodeParsed`. This step points at the graph monad under Target — Server Core. Upload, import, undo, stored replay, parse, persist, and client display stay working. Ticket: [43 — Expand Node events beside the Set output](issues/43-expand-file-events-beside-the-set-output.md).
   2. [ ] <a id="graph-change-persist-stack"></a>**Persist stack** — Stand a persist stack of nodes inside Core, and a persist thread that pulls a node from it. Current Core has no such stack. File persist goes through `src/Server/DocumentPersistChange.fs`. [18 — Core Persist stack](issues/18-core-persist-stack.md) coded a collector loop that pulls `PersistWork`. This step adds the node stack. The thread calls `DocumentPersistChange` and does not edit the graph. It is not an Actor. Route registration does not construct the stack. After a successful write the thread posts `InMsg.NodePersisted`. A failed write does not. There is no whole-graph snapshot. [Remove dead snapshot code](#remove-dead-snapshot-code). This step points at the graph monad under Target — Server Core. Ticket: [51 — Stand the persist stack](issues/51-stand-the-persist-stack.md).
   3. [ ] <a id="graph-change-stack-pull"></a>**Stack pull** — The parse thread's stack puller posts `InMsg.NodeParsing` when it pulls a node. NodeParsing means parse started. It does not set Unparsed and does not push. The persist thread's stack puller posts `InMsg.NodePersisting` when it pulls a node. That pull waits on Persist stack. NodePersisting sets Unpersisted in the Core-loop handler. Neither thread edits the graph. This step points at the graph monad under Target — Server Core. Ticket: [52 — Log stack pulls](issues/52-log-stack-pulls.md).
   4. <a id="graph-change-change"></a><a id="graph-change-migrate-upload"></a><a id="graph-change-migrate-parse"></a><a id="graph-change-migrate-push"></a>**Change**
      1. [ ] <a id="graph-change-apply"></a>**Apply** — New document-state handlers in `CoreMailboxLoad.applyInMsg` receive `InMsg.NodeUploading`, `InMsg.NodeReceived`, `InMsg.NodeParsing`, `InMsg.NodePersisting`, and `InMsg.NodePersisted`, apply the state effect in §1, and log the message. `InMsg.NodeParsed` is the renamed `InMsg.ParseFinished` from Graph change expand. The same handler sets Parsed. Those messages are `InMsg` cases. They are not Ops on a Change. Tests call the handlers. Producers stay on their current writes. The NodePersisting handler calls `GraphMutate.setPersistState` with Unpersisted and leaves parse state unchanged. The NodeParsing handler does not set Unparsed and does not push. Each handler writes the resulting `document_state` and `persist_state` through `DatabaseProjection`. The stored word for Parsed in `document_state` stays `current`. The column stores Parsed or Unparsed only. The `InMsg.NodeUploading` handler sets PullingFile in memory and does not write PullingFile to `document_state`. The `InMsg.NodeReceived` handler sets Unparsed and clears PullingFile. [44 — Apply document state from Node events](issues/44-apply-document-state-from-file-events.md) is blocked by [36 — Add the persist column](issues/36-add-the-persist-column.md). Ticket: [44 — Apply document state from Node events](issues/44-apply-document-state-from-file-events.md).
      2. [ ] <a id="graph-change-upload"></a><a id="graph-change-producers"></a>**Upload** — Until the upload Actor exists, `Api.postEvents` posts `InMsg.NodeUploading` through `CoreMailboxApi.addInMsg` when it runs the upload Change. The upload Actor posts `InMsg.NodeUploading` once it exists. `CoreMailboxLoad.applyInMsg` applies `InMsg.NodeUploading` and logs it. This step follows Apply. Ticket: [50 — Upload emits NodeUploading](issues/50-upload-emits-nodeuploading.md).
      3. [ ] <a id="graph-change-arrival"></a>**Arrival** — `LazyLoadReconciliationServer.reconcileReceivedFiles` posts `InMsg.NodeReceived` directly through `CoreMailboxApi.addInMsg` for a changed File Node already on the server (pull request 263). Until the upload Actor exists, `Api.postEvents` posts `InMsg.NodeReceived` through `CoreMailboxApi.addInMsg` for an incoming file. The upload Actor posts `InMsg.NodeReceived` once it exists. `CoreMailboxLoad.applyInMsg` applies `InMsg.NodeReceived` and logs it. A second Load repairs a file that never arrived. This step follows Apply. Ticket: [53 — Arrival and reconcile emit NodeReceived](issues/53-arrival-and-reconcile-emit-nodereceived.md).

      > **Note.** `Api.postEvents` is the general client change endpoint, so posting `InMsg.NodeUploading` and `InMsg.NodeReceived` there means it recognizes upload and arrival from the Ops in the Change. This is a known design weakness: there should be two sets of commands (public commands and `InMsg`), not an inferred third path. The planned resolution is the upload Actor. That Actor has an `InMsg` handle and posts `InMsg.NodeUploading`, `InMsg.NodeReceived`, and `InMsg.SetDocumentState`. Until that Actor exists, `Api.postEvents` posts `InMsg.NodeUploading` and `InMsg.NodeReceived`. Step: [Graph change upload Actor](#graph-change-upload-actor). Ticket: [57 — Upload Actor posts document state](issues/57-upload-actor-posts-document-state.md).

      4. [ ] <a id="graph-change-parsed"></a>**Parsed producers** — `ImportText.markDocumentCurrentBeforeParse`, `ImportDocument.planParseFile`, and `DirectoryReconcile.planDirectoryReconcile` stop emitting `Op.SetDocumentState` for the Parsed mark. `ParseThread.finishParse` posts `InMsg.NodeParsed` through `CoreMailboxApi.addInMsg` when the parse thread finishes that node. The handler sets Parsed. `ParseThread.parseDirectory` runs directory reconcile. `ParseThread.parseFile` runs file parse through `DocumentPersistWrite.planParseFile`. Import does not run on the parse thread. Route `ImportText.buildImportChange`, `ImportText.buildDirectoryMergeChange`, and `Api.postParseFile` through the parse thread so `ParseThread.finishParse` posts `InMsg.NodeParsed`. This step follows Apply. Ticket: [54 — Parse and import stop the Parsed Set](issues/54-parse-and-import-stop-the-parsed-set.md).
      5. [ ] <a id="graph-change-remove-markunparsed"></a>**Remove markUnparsed** — Remove `ParseThread.markUnparsed`. It does not post `InMsg.MarkUnparsed` and it does not emit NodeParsing. Unparsed is set by the mailbox before a node is on the parse thread. Directory reconcile does not mark existing members Unparsed. This step follows Apply. Ticket: [56 — Remove ParseThread markUnparsed](issues/56-remove-parsethread-markunparsed.md).
      6. [ ] <a id="graph-change-persist-version"></a>**Persist version** — The persist collectors put the launch version on the persist job. After a successful write, the persist thread receives that version from the job and emits `InMsg.NodePersisted(nodeId, version)`. The Core change applier records a newer version when it marks the nearest owning special node Unpersisted. The Core-loop NodePersisted handler sets the Persisted version only when the launch version is current. An older launch version leaves Unpersisted, including after Undo or Redo. This step follows Persist stack. Ticket: [46 — Record the persist version](issues/46-record-the-persist-version.md).
      7. [ ] <a id="graph-change-undo"></a><a id="graph-change-undo-replay"></a>**Undo** — The server and Browser Undo and Redo appliers receive Undo or Redo and mark the nearest owning special node Unpersisted after they apply its inverse Ops. Redo gets the same Unpersisted mark as Undo. Neither writes a version. They do not apply NodeUploading, NodeReceived, NodeParsing, NodeParsed, NodePersisting, or NodePersisted. This step follows Apply. Ticket: [47 — Undo and redo set Unpersisted](issues/47-undo-and-redo-set-unpersisted.md).
      8. [ ] <a id="graph-change-replay"></a>**Stored replay** — The server and Browser stored-replay appliers receive stored Events, mark the nearest owning special node Unpersisted for Graph-changing Events, and skip NodeUploading, NodeReceived, NodeParsing, NodeParsed, NodePersisting, and NodePersisted. This step follows Apply. Ticket: [55 — Stored replay sets Unpersisted](issues/55-stored-replay-sets-unpersisted.md).
      9. [ ] <a id="graph-change-browser"></a>**Browser apply** — `SyncLogic.foldProjectedEvents` receives `InMsg.NodeUploading`, `InMsg.NodeReceived`, `InMsg.NodeParsing`, `InMsg.NodeParsed`, `InMsg.NodePersisting`, and `InMsg.NodePersisted` that `CoreMailboxLoad.applyInMsg` logged and applies the §1 effect of each message to the resident Browser Graph. The client receives every log message. There is no filter. The browser updates only its resident Graph. Display reads from that graph. This step follows Expand. The server writer is the current writer or the Core-loop handlers. Ticket: [48 — Browser applies Node events](issues/48-browser-applies-node-events.md).
      10. [ ] <a id="graph-change-upload-actor"></a>**Upload Actor** — Upload runs as an Actor. The Actor has an `InMsg` handle. It posts `InMsg.NodeUploading`, `InMsg.NodeReceived`, and `InMsg.SetDocumentState`. `Api.postEvents` does not infer those three from the Ops of a client Change. This step follows [44 — Apply document state from Node events](issues/44-apply-document-state-from-file-events.md) and [45 — SetDocumentState leaves CoreMsg](issues/45-remove-document-state-set-ops.md). This step is the planned resolution of the `Api.postEvents` design weakness. Ticket: [57 — Upload Actor posts document state](issues/57-upload-actor-posts-document-state.md).
   5. [ ] <a id="graph-change-contract"></a>**Contract** — `SetDocumentState` leaves `CoreMsg`. Planners and handlers do not write it as a document-state Op. `InMsg.SetDocumentState` is an `InMsg` case. `EventBody.SetDocumentState` is a valid event-log item. `Op.SetPersistState` and `Op.SetParseState` are not ops. `InMsg.MarkUnparsed` leaves `InMsg` and `CoreMailboxLoad.applyInMsg`. Production code modifies the graph only through the mailbox's monadic functions. Ticket: [45 — SetDocumentState leaves CoreMsg](issues/45-remove-document-state-set-ops.md). Tickets [38 — Mailbox parse-state writer logs the Event](issues/38-mailbox-parse-state-writer-logs-the-event.md) through [42 — Public post refuses SetDocumentState](issues/42-public-post-refuses-set-document-state.md) are cancelled history.

### Test seam

1. [x] **Core loop apply** — Narrowest point the completion paths cross. Tests apply `InMsg` on **Core loop**.

Implementation: add `InMsg` through the private function on the one mailbox queue, then move the threads onto that function. Testing: tests cross **Core loop** apply.

## 10. Module map

Deltas for this Project. Claim homes: [Mailbox](doc/current/mailbox.md) and [Parse and persist](doc/current/parse-persist.md).

1. **Core loop**
   The `CoreMailboxLoad` module holds the queue puller. The private add function is on the mailbox. Puller file: `src/Server/Core/CoreMailboxLoad.fs`. Mailbox file: `src/Server/Core/CoreMailbox.fs`.
   1. **State**
      1. [x] **InMsg** — Internal message type. Not an Op. It carries the node.
         1. [x] **NodeParsed** — That `NodeId`. Parse finished. Graph change expand renamed this case from `InMsg.ParseFinished`.
         2. [x] **SnapshotDone** — That `NodeId`. No `Graph`. Persist finished. Today this case sets Persisted. [Graph change persist version](#graph-change-persist-version) moves that set to `NodePersisted`. There is no whole-graph snapshot. [Remove dead snapshot code](#remove-dead-snapshot-code). This case replaces `PersistFinished`.
         3. [x] **MarkUnparsed** — That `NodeId`. Directory reconcile does not name an existing member for this case.
         4. [ ] **Node messages** — `InMsg.NodeUploading`, `InMsg.NodeReceived`, `InMsg.NodeParsing`, `InMsg.NodePersisting`, `InMsg.NodeParsed`, and `InMsg.NodePersisted` are `InMsg` cases. Producers post them through the private function. `CoreMailboxLoad.applyInMsg` applies each message and logs it. None of these messages are Ops on a Change. They name a Workspace Node, a Directory Node, or a File Node. The client receives every log message. There is no filter. Effects are §1. Steps: [Graph change expand](#graph-change-expand), [Graph change change](#graph-change-change), and [Graph change contract](#graph-change-contract).
      2. [x] **One queue** — The mailbox queue. Its element is a private sum of `CoreMsg` and `InMsg`. That sum has no public name. There is no second queue.
   2. **Interface**
      1. [x] **Private add** — A private function on the mailbox takes `InMsg` and adds it to the queue.
      2. [x] **Public add** — A public function takes `CoreMsg` and adds it to the same queue.
      3. [x] **Pull** — The queue puller receives the next item and hands a `CoreMsg` to the `CoreMsg` handler and an `InMsg` to the `InMsg` handler.
      4. [x] **Parse finished** — `NodeParsed` sets that node Parsed only. The field write is `GraphMutate.setParseState`. PersistState stays unchanged. Graph change expand renamed this case from `InMsg.ParseFinished`. The same handler sets Parsed.
         1. [x] **Parsed when clear** — `NodeParsed` sets a File Node Parsed only when `noServerFile` [o] obsolescent is clear.
      5. [x] **Snapshot done** — `SnapshotDone` sets that node Persisted through `GraphMutate.setPersistState`. The message carries the node id and no `Graph`. `noteDiffDone` counts the file diff, clears the in-progress flag, and starts another file diff when one is needed.
      6. [x] **Public post** — `CoreMailbox.postEvents` and `CoreMailbox.postGraphOnly` do not carry `InMsg`.
      7. [ ] **Not an Actor mailbox** — The mailbox queue is not an Actor mailbox. The persist thread does not get a `MailboxProcessor`.
      8. [x] **CoreMsg** — `CoreMsg` has no `SnapshotDone`. The other `CoreMsg` cases are the public mailbox doors. The live-document completion is `InMsg` `SnapshotDone`, added by the private function.
      9. [ ] **Private axes** — Parse state and persist state are internal. Outsiders may see them. The core loop writes them. A graph change sets Unpersisted behind the mailbox and has no state-change Op. `Op.SetText` stays. `Op.SetPersistState` and `Op.SetParseState` are not ops. `Op.SetDocumentState` leaves in [Graph change contract](#graph-change-contract). Steps: [Graph change expand](#graph-change-expand) through [Graph change contract](#graph-change-contract).
      10. [x] **Mark unparsed** — `MarkUnparsed` sets that node Unparsed only. The field write is `GraphMutate.setParseState`. PersistState stays unchanged. `Op.SetDocumentState` is not the writer.
      11. [ ] **Node parsed** — `NodeParsed` sets that node Parsed when it is Unparsed. PullingFile stays PullingFile. The field write is `GraphMutate.setParseState`. The handler writes the resulting `document_state` and `persist_state` through `DatabaseProjection`. Persist state stays unchanged. Step: [Graph change apply](#graph-change-apply).
      12. [ ] **Node persisted** — `NodePersisted` records the graph version persist launched with. A current version sets Persisted. An older version leaves Unpersisted. Parse state stays unchanged. The handler writes the resulting `document_state` and `persist_state` through `DatabaseProjection`. There is no whole-graph snapshot. [Remove dead snapshot code](#remove-dead-snapshot-code). Step: [Graph change persist version](#graph-change-persist-version).
      13. [ ] **Node parsing** — `InMsg.NodeParsing` means parse started. The handler does not set Unparsed and does not push. Parse state and persist state stay unchanged. Step: [Graph change apply](#graph-change-apply).
      14. [ ] **MarkUnparsed leaves** — `InMsg.MarkUnparsed` leaves `InMsg` and `CoreMailboxLoad.applyInMsg`. Step: [Graph change contract](#graph-change-contract).
      15. [ ] **Node persisting** — `InMsg.NodePersisting` sets that node Unpersisted. Parse state stays unchanged. The handler writes the resulting `document_state` and `persist_state` through `DatabaseProjection`. Step: [Graph change apply](#graph-change-apply).
      16. [x] **Node parsed rename** — Graph change expand renames `InMsg.ParseFinished` to `InMsg.NodeParsed`. It is the same case. The same handler sets Parsed. Step: [Graph change expand](#graph-change-expand).
   3. **Uses**
      1. [x] **Parse thread** — Adds `InMsg` `NodeParsed` through the private function.
      2. [x] **Persist thread** — Adds `InMsg` `SnapshotDone` through `PersistThread.finishWhenBound` when a file write finishes.
      3. [x] **Db agent** — Binds that poster. It does not post `InMsg` `SnapshotDone` or `CoreMsg`.
      4. [x] **Field write** — `GraphMutate.setParseState` and `GraphMutate.setPersistState` are the existing field writes.
      5. [x] **Disk-newer** — Directory reconcile does not add `InMsg` `MarkUnparsed` for an existing member. It does not edit the graph.
      6. [x] **Node parsed** — `ParseThread` `finishParse` posts `InMsg.NodeParsed`. That case is the renamed `InMsg.ParseFinished`. The same handler sets Parsed. Step: [Graph change expand](#graph-change-expand).
      7. [ ] **Node persisted** — The persist thread posts `InMsg.NodePersisted` through the private function and does not edit the graph. Step: [Graph change persist stack](#graph-change-persist-stack). The version rule is [Graph change persist version](#graph-change-persist-version).
      8. [ ] **Node parsing** — The parse thread posts `InMsg.NodeParsing` when it pulls the node from the parse stack. It does not post NodeParsing for a Directory or Workspace Unparsed mark. It does not edit the graph. Step: [Graph change stack pull](#graph-change-stack-pull).
      9. [ ] **Node persisting** — The persist thread posts `InMsg.NodePersisting` when it pulls the node from the persist stack. The thread does not edit the graph. Step: [Graph change stack pull](#graph-change-stack-pull). The Core-loop handler applies the set in [Graph change apply](#graph-change-apply).
      10. [ ] **Unparsed producers** — `CoreMailboxLoad.markUnparsedThenPush` sets Unparsed and pushes and does not emit NodeParsing. `ParseThread` `markUnparsed` is removed. Unparsed is set by the mailbox before a node is on the parse thread. Directory reconcile does not mark existing members Unparsed. Step: [Graph change remove markUnparsed](#graph-change-remove-markunparsed).

2. **Parse thread**
   File: `src/Server/ParseThread.fs`.
   1. **State**
      1. [x] **No queue** — The parse thread does not hold the mailbox queue.
   2. **Interface**
      1. [x] **Add** — On finish it adds `InMsg` `NodeParsed` for that node through the private function. It does not edit the graph.
      2. [x] **Mark unparsed** — Directory reconcile does not add `InMsg` `MarkUnparsed` for an existing member. It does not edit the graph.
      3. [ ] **Node parsing** — When it pulls a node from the parse stack it posts `InMsg.NodeParsing`. It does not post NodeParsing when a Directory Node or Workspace Node is marked Unparsed. It does not edit the graph. Step: [Graph change stack pull](#graph-change-stack-pull).
      4. [x] **Node parsed** — On finish `ParseThread` `finishParse` posts `InMsg.NodeParsed`. That case is the renamed `InMsg.ParseFinished`. The same handler sets Parsed. Step: [Graph change expand](#graph-change-expand).
      5. [ ] **No Unparsed producer** — `ParseThread` `markUnparsed` is removed. Unparsed is set by the mailbox before a node is on the parse thread. Directory reconcile does not mark existing members Unparsed. Step: [Graph change remove markUnparsed](#graph-change-remove-markunparsed).
   3. **Uses**
      1. [x] **Private add** — The private function on the mailbox.

3. **Persist thread**
   File: `src/Server/Core/PersistThread.fs`.
   1. **State**
      1. [x] **No queue** — The persist thread does not hold the mailbox queue.
   2. **Interface**
      1. [x] **Add** — On finish it adds `InMsg` `SnapshotDone` for that node through the private function. It does not edit the graph.
      2. [ ] **Node persisting** — When it pulls a node from the persist stack it posts `InMsg.NodePersisting`. It does not edit the graph. Step: [Graph change stack pull](#graph-change-stack-pull). The Core-loop handler applies the set in [Graph change apply](#graph-change-apply).
      3. [ ] **Node persisted** — On finish it posts `InMsg.NodePersisted` for that node through the private function. It does not edit the graph. The client sees NodePersisted because it is in the event log. Step: [Graph change persist stack](#graph-change-persist-stack). The version rule is [Graph change persist version](#graph-change-persist-version).
   3. **Uses**
      1. [x] **Private add** — The private function on the mailbox.
      2. [x] **Persist functions** — The thread calls the existing functions in `src/Server/DocumentPersistChange.fs`.

## 11. Seams

1. [x] **Private add** — Interface on **Core loop**.
2. [x] **Parse add** — Interface on **Parse thread**.
3. [x] **Persist add** — Interface on **Persist thread**.

## 12. Alternative considered

1. **Public post** — Put parse finished and persist finished on `CoreMailbox.postEvents` or `CoreMailbox.postGraphOnly`. That loses. Actors use those doors, and the completion is not an Op.
2. **Second processor** — Give the persist thread its own `MailboxProcessor`. That loses. §7 forbids an Actor mailbox for the persist thread.
3. **Second queue** — Hold `InMsg` on a queue beside the mailbox queue. That loses. One queue keeps one order between a public post and an internal finish.
4. **InMsg on the mailbox queue** — One mailbox queue. A private function adds `InMsg`. A public function adds `CoreMsg`. The queue puller hands the item to its handler. This wins.
5. **Two persist messages** — Keep `PersistFinished` for the persist thread and leave `SnapshotDone` on `CoreMsg`. That loses. One `SnapshotDone` case of `InMsg` is packaged with setting Persisted.

## 13. Unsettled

None.
