# core-refinement architecture

Updated: 2026-10-01
Sequence: expand-contract

Home: [[project.md]]. This note is the sole authority for the Core seam (what will be coded): Target — Server Core, the expand-contract sequence (§3 and §9 Story paths), axes, stacks, path control, mailbox git handoff, the locking model (§6), and InMsg on the mailbox queue (§10 **Core loop**). Spec.md and User Stories are absent. Map decision 9 keeps Sequence `expand-contract`. Story paths, Shared segments, the Module map, and Seams are on this note. Expand-contract does not drop them. Stage on [[project.md]] stays `build`. Claim marks for committed elements live under [[doc/current/]]. This note links to those pages. Map §4 (mailbox enable, call, and stop) stays in §3 step 6. It is not copied into [[doc/current/]]. The locking model (§6) is not copied into [[doc/current/]].

Decision lock: [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md). Core locking model: §6. Step 1 implement (shipped): [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md). Git use is Core (Target — Server Core). This note does not reopen that lock. This expand-contract sequence is the ticket source: tickets stay undrafted until a use case needs one; then draft against a beat or a caller shift from the sequence, not a pre-built full set.

## Target — Server Core

Locked 2026-09-28 (Alan). Compact description of that same target: [[plan/architecture/server-core.md]]. This note is the sole authority for the Core seam; that page is the short description. One meaning, two roles — if they ever drift, this note wins. The lock does not chart coding tickets; the path is §3. Claim home for Core: [Core](doc/current/core.md).

### Dated note

1. **2026-09-28 inbound** — Alan locked this Server description.
2. **2026-09-28 correction** — Git use is Core. Git Load, git Save, pull, push, and commit as the Server uses them are Core responsibilities, not outside work.
3. **2026-09-30 sole authority** — Alan: one home for the Core seam, not a reading path or index across several plan homes. This Project owns that future. The compact description stays on [[plan/architecture/server-core.md]].

## 1. Special-node Parse and Persist axes

Special nodes (Workspace Node, Directory Node, File Node) each carry Parse Status (Parsed|Unparsed) and PersistenceStatus (Persisted|Unpersisted). These two axes are independent drift markers Core sets. Unparsed and Unpersisted are the two directions of drift; both can be true at once. They are informational, not locks:

1. **Parsed | Unparsed** — Unparsed means information on disk has not been pulled into the graph. The parse thread runs when a node is Unparsed. On completion the core loop sets Parsed only (§10 **Core loop**).
2. **Persisted | Unpersisted** — Unpersisted means information in the graph has not been written to disk. The persist thread runs when a node is Unpersisted and Parsed. On completion the core loop sets Persisted only (§10 **Core loop**). A graph edit sets the nearest owning special node Unpersisted. It does not mark ancestors.

Only Core changes files, and only Core changes the graph. The flags record which side Core just moved. Other operations set Unparsed and Unpersisted. Workspace lock and per-member persist locks are additional protocol beside the axes: §6 Core locking model. Do not invent a Conflicted state. This section is not the project target; the project target is Target — Server Core. Claim home: [Workspace graph](doc/current/graph.md) (Document state).

## 2. Starting point

Current Core is the [[plan/core-creation/project.md]] baseline: mailbox, Actor pool, Graph, Events, and database backend. Today `DocumentState` is `Current` | `Unparsed` | `NoServerFile`. `ParseState` and `PersistState` already exist on special nodes beside it ([20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md)). Parsed is the other pole of Unparsed (`Current` is today’s name). Load after files land still uses today’s Parse / graph-push hop ([[plan/github-transport/arch.md]] Load / git Load). The Parse stack expand is coded but still started from [[src/Server/RouteRegistration.fs]] (`createPersistenceContext` builds `ParseStack` and calls `ParseThread.start`); that setup does not yet live only in [[src/Server/Core]]. The Core persist thread (async Persist stack: collectors and loop) is not stood up. File Persist today goes through [[src/Server/DocumentPersistChange.fs]]. Transport still locks, receives, and informs Core; after this Project, that thin remainder stays [[plan/github-transport/project.md]].

## 3. Expand-contract sequence

Ordered path from §2 Starting point to Target — Server Core. The expand-contract is **slow**: gradually create the Core pieces, and gradually shift use. This pace applies to the whole sequence, not only Persist. Each step expands the new form beside the old, migrates callers while both exist, then contracts the old. §9 Story paths are this same sequence. §10 Module map is the home of State, Interface, and Uses.

1. **State axes on special nodes**
   1. **Expand** — [x] Add `ParseState` and `PersistState` on special nodes beside `DocumentState`. Shipped: [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md). Detail: §4 Step 1 locked.
   2. **Migrate** — [ ] Dual-write both new axes and old `DocumentState` wherever state changes; migrate old uses per §5 Axis-write mechanics.
   3. **Contract** — [ ] Remove `DocumentState` once no caller remains.

2. **Parse stack**
   Charted approach (Alan, 2026-09-30). Same posture as Persist (§3 step 3 Core Persist stack): private stack, public push inside Core, consumer thread waits until push, then calls the existing parse body. Parse setup (stack, push, consumer thread) lives inside [[src/Server/Core]]. Outside Core, including [[src/Server/RouteRegistration.fs]], does not construct Parse, start it, or hold its handles; RouteRegistration may call a Core entry that boots Core, and does not see parse push/consumer. That move is not done; today’s start site is still RouteRegistration (`createPersistenceContext` builds `ParseStack` and calls `ParseThread.start`).
   1. **Expand** — [x] Stand up the one long-lived Parse stack beside today’s Load → Parse / graph-push hop. Product home: [[plan/parse-thread/project.md]]. Core may push reconcile targets. Old hop still runs. See [03 — One Parse thread stack](issues/03-one-parse-thread-stack.md). The background loop that pulls the parse stack and runs the old parse function runs on a separate thread. First use case (Alan, 2026-09-29): an explicit parse command on a file (today’s `ParseFile` / `postParseFile`) — mailbox Load of a File node, push onto the stack, loop runs the old parse body. Ticket: [06 — Explicit parse command on a File (Load)](issues/06-explicit-parse-command-load-file.md) (Status `coded`).
   2. **Migrate** — [ ] After §3 step 4 **§6 locks catch-up** Expand stands the workspace lock, that lock drains in-flight member file use, then pull (or Upload land) proceeds; arrived files are marked Unparsed; the lock releases; Unparsed starts the parse thread (push onto that stack). Client Load on Directory or File marks Unparsed; File push onto the stack waits on this expand. Selection push has no special priority. See [02 — Git Load: Unparsed then Parse stack](issues/02-git-load-unparsed-then-parse-stack.md), [05 — Selection-scoped Parse after whole-tree git Load](issues/05-selection-scoped-parse-after-whole-tree-git-load.md), and §6 Core locking model. Transport only informs Core; Core owns the lock → Unparsed → parse-thread path. When the parse thread finishes, it adds `InMsg` `ParseFinished` through the private function (§10). It does not edit the graph.
   3. **Contract** — [ ] Retire today’s Load → Parse / graph-push hop once every handoff uses the stack. Claim home: [Parse and persist](doc/current/parse-persist.md) (Parse stack claims).

3. **Core Persist stack**
   Charted approach (Alan, 2026-09-29). Persist is not an Actor; it is a thread. See [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md). Setup location (Alan, 2026-09-30): Persist setup (collectors and the loop that calls existing persist functions) lives inside [[src/Server/Core]]. Outside Core, including [[src/Server/RouteRegistration.fs]], does not construct Persist, start it, or hold its handles; RouteRegistration may call a Core entry that boots Core, and does not see persist collectors. That setup is not stood up yet. This step’s Expand / Migrate / Contract is collectors and call-site shape only. Standing workspace lock / per-member persist locks and removing `withWorkTreeGate` are §3 step 4 **§6 locks catch-up**, not this Contract.
   1. **Expand** — [ ] Make **new collector functions** beside today’s sync live-save. A **loop** on the persist thread will pull from the collection and **call the existing persist functions** ([[src/Server/DocumentPersistChange.fs]] / today’s Graph→disk writers). Those functions stay the write body. Do not invent a new write body in this step.
   2. **Migrate** — [ ] **Change everyone to call the collectors.** Sync call sites move to collectors; the loop is the new feeder; the existing persist functions are what the loop calls. Do not delete those functions in this step.
   3. **Contract** — [ ] Retire the old sync call-site shape once collectors and the loop feed the existing persist functions. The write body stays.
   **New behavior** (not today’s sync live-save): The persist thread runs when a node is Unpersisted and Parsed (§6). When it finishes, it adds `InMsg` `SnapshotDone` through the private function (§10). It does not edit the graph. The core loop sets Persisted. Unparsed files are not open for persist. Axis lock already says file Persist is blocked while Unparsed ([04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md)); the persist thread (collector loop) is where that block becomes real. Do not claim today’s code already does it. Lock protocol against in-flight writes during pull: §6 and §3 step 4 **§6 locks catch-up**. Claim home: [Parse and persist](doc/current/parse-persist.md) (Persist stack claims).

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
   3. **6.3 Stop any old** — [ ] Stop the old git path (contract). Map §4. These three beats stay on this note. They are not claims on [[doc/current/]]. The locked target (git use is Core; an Actor posts; Core performs the git work) has a claim home: [Core](doc/current/core.md) (Git use is Core) and [Actors](doc/current/actors.md) (the Actor posts).

## 4. Step 1 locked

Step 1 is **State axes on special nodes only**. Markers / Graph state only. It is sequence step 1 Expand above.

Acceptance: the two axes exist on special nodes and can be set and read.

This step does not start workers.

**Approach (Mikado)** — Locked 2026-09-28 (Alan). Create the new state axes. Set both the new axes and the old `DocumentState` wherever state changes. Migrate old uses over step by step. Finally remove the old.

Implement ticket: [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md) (Status `done` on github-transport). Claim home: [Workspace graph](doc/current/graph.md) (Document state).

## 5. Axis-write mechanics

Locked 2026-09-28 (Alan). These rules say who writes each axis and which node they mark. [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md) still only adds the markers. It does not start workers. Items marked deferred wait for the Parse loop. Claim home: [Workspace graph](doc/current/graph.md) (Document state). Axis-write rules stay in this section. The current page does not copy them. `InMsg` on the one mailbox queue is locked 2026-10-01 (Alan). A private function adds `InMsg`. A public function adds `CoreMsg`. The queue puller hands the item to its handler. State, Interface, and Uses are §10 **Core loop**. Claim home: [Mailbox](doc/current/mailbox.md). The thread post is [Parse and persist](doc/current/parse-persist.md). Those pages do not copy this section.

1. **Writer target** — The core loop writes the axes on the live graph. The parse thread and the persist thread enqueue an axis completion. They do not edit the graph. Until the loop owns every axis write, set the axes at today’s file-edit sites and graph-edit sites.
2. **Graph edit** — Mark the nearest owning special node (File Node, Directory Node, or Workspace Node) **Unpersisted** only. Do not mark ancestors.
3. **Discovery** — Whoever finds a disk change writes the axis. Parse is not the discovery tool. A new member or a deleted member marks the Directory Node **Unparsed**. A modified file marks the File Node **Unparsed**.
4. **git pull finish** — After the workspace lock drains and files land (§6), notes the same discoveries (arrived files marked Unparsed). There is no separate axis path.
5. **Persist done** — Mark that node **Persisted** only. The persist thread adds `InMsg` `SnapshotDone` through the private function. **Core loop** applies it (§10). `PersistFinished` was the persist thread's file-write finish, and today's `SnapshotDone` is the background live-document snapshot on `CoreMsg`; the target is one `SnapshotDone` case of `InMsg`, packaged with setting Persisted.
6. **Directory Parse done** — Mark that Directory Node **Parsed** only. The parse thread adds `InMsg` `ParseFinished` through the private function. **Core loop** applies it (§10). That completion does not change PersistState.
7. **Create special** — A new special node starts **Unparsed** and **Persisted**.
8. **Client Load on Directory** — Mark the Directory Node **Unparsed** (re-process). Reconciliation with no extra info can spot disk members the Graph lacks.
9. **Client Load on File** — Mark the File Node **Unparsed**. Push onto the Parse stack is deferred. That push needs the Parse loop.
10. **Directory reconcile** (deferred) — [Parse thread](../parse-thread/project.md) owns this. Definition: [Parse thread architecture](../parse-thread/arch.md) §2 Module map, item 1 **Directory reconcile**. Walks all nodes tied to that `.amb`, not only immediate children. Create missing File Nodes. Disk-newer marks the File Node **Unparsed** and pushes when the stack exists.
11. **Parse stack pop** (deferred) — If the node is already **Parsed**, skip. Real work arrives **Unparsed**.
12. **InMsg** — The internal message type is `InMsg` on **Core loop** (§10). It is not an Op. Cases: `ParseFinished`, `SnapshotDone`.
13. **One mailbox queue** — The mailbox has one queue. A private function adds `InMsg`. A public function adds `CoreMsg`. The queue puller hands a `CoreMsg` to the `CoreMsg` handler and an `InMsg` to the `InMsg` handler. There is no second queue. The puller file is `src/Server/Core/CoreMailboxBackend.fs`. The private function is on the mailbox, `src/Server/Core/CoreMailbox.fs`.
14. **Private axes** — PersistState and the parsed axis stay internal. The writer interface is **Core loop** (§10).

## 6. Core locking model

Locked 2026-09-30 (Alan). Axes remain informational drift markers (§1), not locks. The workspace lock and per-member persist locks are additional protocol beside the axes. This section is current truth for Core-revision workers. Claim home: [Parse and persist](doc/current/parse-persist.md) (workspace lock claims). Do not copy this section into [[doc/current/]].

1. **Unparsed and Unpersisted** — The two directions of drift. Both can be true at once.
   1. **Unparsed** — Information on disk has not been pulled into the graph.
   2. **Unpersisted** — Information in the graph has not been written to disk.
2. **Core alone moves both sides** — Only Core changes files, and only Core changes the graph. The flags record which side Core just moved.
3. **Parse thread** — Runs when a node is Unparsed. On completion it adds `InMsg` `ParseFinished` through the private function (§10 **Core loop**). The core loop sets Parsed.
4. **Persist thread** — Runs when a node is Unpersisted and Parsed. On completion it adds `InMsg` `SnapshotDone` through the private function (§10 **Core loop**). The core loop sets Persisted.
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
8. Say **InMsg** for the internal message. The cases are `ParseFinished` and `SnapshotDone`.

## 8. Related

1. **Map** — [[map.md]] (decision 6–7, 10, 13, 15, 16)
2. **Axes lock** — [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md)
3. **Core locking model** — §6
4. **Server Core target** — Target — Server Core (this note); compact description [[plan/architecture/server-core.md]]
5. **Parse product home** — [[plan/parse-thread/project.md]]
6. **Transport remainder** — [[plan/github-transport/project.md]]
7. **InMsg** — §10 **Core loop**

## 9. Story paths

Sequence stays `expand-contract`. These paths are that implementation sequence. Each hop names a module or a door. State, Interface, and Uses stay in §10 Module map.

1. **State axes on special nodes**
   1. [x] **Expand** — `ParseState` and `PersistState` exist on special nodes. [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md).
   2. [ ] **Migrate** — A state change writes both new axes and `DocumentState`.
   3. [ ] **Contract** — `DocumentState` is gone once no caller remains.

2. **Parse stack**
   1. [x] **Expand** — One parse thread consumes one Parse stack. [03 — One Parse thread stack](issues/03-one-parse-thread-stack.md).
   2. [ ] **Migrate** — Unparsed starts the parse thread after the workspace lock releases.
   3. [ ] **Contract** — Load uses that stack.

3. **Persist stack**
   1. [ ] **Expand** — The persist thread calls the existing persist functions.
   2. [ ] **Migrate** — Callers use the collectors. The write body stays.
   3. [ ] **Contract** — The old sync call-site shape is gone.

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
   2. [ ] **Migrate** — The parse thread and the persist thread add `InMsg` through the private function. The queue puller hands each item to its handler. **Core loop** applies `InMsg`.
   3. [x] **Contract** — `CoreMsg` has no `SnapshotDone`. `Op.SetPersistState` is not a writer. `Op.SetDocumentState` is not the writer of the parsed axis.

### Shared segments

1. **InMsg**
   1. [x] **Private add** — The parse thread or the persist thread adds `InMsg` through the private function.
   2. [x] **Core loop** — The queue puller hands `InMsg` to the `InMsg` handler. **Core loop** applies `InMsg`.

2. **Graph change**
   1. [ ] **Core loop** — A path that changes the graph crosses **Core loop**.

### Test seam

1. [x] **Core loop apply** — Narrowest point the completion paths cross. Tests apply `InMsg` on **Core loop**.

Implementation: add `InMsg` through the private function on the one mailbox queue, then move the threads onto that function. Testing: tests cross **Core loop** apply.

## 10. Module map

Deltas for this Project. Claim homes: [Mailbox](doc/current/mailbox.md) and [Parse and persist](doc/current/parse-persist.md).

1. **Core loop**
   The `CoreMailboxBackend` module holds the queue puller. The private add function is on the mailbox. Puller file: `src/Server/Core/CoreMailboxBackend.fs`. Mailbox file: `src/Server/Core/CoreMailbox.fs`.
   1. **State**
      1. [x] **InMsg** — Internal message type. Not an Op. It carries the completion and the node.
         1. [x] **ParseFinished** — That `NodeId`. Parse finished.
         2. [x] **SnapshotDone** — That `NodeId` and the snapshot `Graph option`. Snapshot finished and that node is Persisted. This case replaces `PersistFinished`.
      2. [x] **One queue** — The mailbox queue. Its element is a private sum of `CoreMsg` and `InMsg`. That sum has no public name. There is no second queue.
   2. **Interface**
      1. [x] **Private add** — A private function on the mailbox takes `InMsg` and adds it to the queue.
      2. [x] **Public add** — A public function takes `CoreMsg` and adds it to the same queue.
      3. [x] **Pull** — The queue puller receives the next item and hands a `CoreMsg` to the `CoreMsg` handler and an `InMsg` to the `InMsg` handler.
      4. [x] **Parse finished** — `ParseFinished` sets that node Parsed only. The field write is `GraphMutate.setParseState`. PersistState stays unchanged.
      5. [x] **Snapshot done** — `SnapshotDone` sets that node Persisted through `GraphMutate.setPersistState`, and the same completion stores the snapshot graph as `persistedGraph` when it equals the live graph, clears the in-progress flag, and starts another snapshot when one is needed.
      6. [x] **Public post** — `CoreMailbox.postEvents` and `CoreMailbox.postGraphOnly` do not carry `InMsg`.
      7. [ ] **Not an Actor mailbox** — The mailbox queue is not an Actor mailbox. The persist thread does not get a `MailboxProcessor`.
      8. [x] **CoreMsg** — `CoreMsg` has no `SnapshotDone`. The other `CoreMsg` cases are the public mailbox doors. The live-document completion is `InMsg` `SnapshotDone`, added by the private function.
      9. [ ] **Private axes** — PersistState and the parsed axis are internal. Outsiders may see them. The core loop writes them. `Op.SetPersistState` is not a writer. `Op.SetDocumentState` is not the writer of the parsed axis.
   3. **Uses**
      1. [x] **Parse thread** — Adds `InMsg` `ParseFinished` through the private function.
      2. [ ] **Persist thread** — Adds `InMsg` `SnapshotDone` through the private function.
      3. [x] **Db agent** — Adds `InMsg` `SnapshotDone` through the private function when the live-document snapshot finishes. It does not post `CoreMsg`.
      4. [x] **Field write** — `GraphMutate.setParseState` and `GraphMutate.setPersistState` are the existing field writes.

2. **Parse thread**
   File: `src/Server/ParseThread.fs`.
   1. **State**
      1. [x] **No queue** — The parse thread does not hold the mailbox queue.
   2. **Interface**
      1. [ ] **Add** — On finish it adds `InMsg` `ParseFinished` for that node through the private function. It does not edit the graph.
   3. **Uses**
      1. [x] **Private add** — The private function on the mailbox.

3. **Persist thread**
   File: `src/Server/Core/PersistThread.fs`.
   1. **State**
      1. [ ] **No queue** — The persist thread does not hold the mailbox queue.
   2. **Interface**
      1. [ ] **Add** — On finish it adds `InMsg` `SnapshotDone` for that node through the private function. It does not edit the graph.
   3. **Uses**
      1. [ ] **Private add** — The private function on the mailbox.
      2. [ ] **Persist functions** — The thread calls the existing functions in `src/Server/DocumentPersistChange.fs`.

## 11. Seams

1. [x] **Private add** — Interface on **Core loop**.
2. [x] **Parse add** — Interface on **Parse thread**.
3. [ ] **Persist add** — Interface on **Persist thread**.

## 12. Alternative considered

1. **Public post** — Put parse finished and persist finished on `CoreMailbox.postEvents` or `CoreMailbox.postGraphOnly`. That loses. Actors use those doors, and the completion is not an Op.
2. **Second processor** — Give the persist thread its own `MailboxProcessor`. That loses. §7 forbids an Actor mailbox for the persist thread.
3. **Second queue** — Hold `InMsg` on a queue beside the mailbox queue. That loses. One queue keeps one order between a public post and an internal finish.
4. **InMsg on the mailbox queue** — One mailbox queue. A private function adds `InMsg`. A public function adds `CoreMsg`. The queue puller hands the item to its handler. This wins.
5. **Two persist messages** — Keep `PersistFinished` for the persist thread and leave `SnapshotDone` on `CoreMsg`. That loses. One `SnapshotDone` case of `InMsg` is packaged with setting Persisted.

## 13. Unsettled

1. **None for InMsg** — `InMsg` and the one mailbox queue are committed in §10. They are not unsettled.
