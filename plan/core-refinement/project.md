# core-refinement

Stage: build
Summary: Sequel to [[plan/core-creation/project.md]]: refine Core so that after transport lands files, Core works through disk and Graph changes (Parse stack, Persist stack, state axes) until everything is updated.
Updated: 2026-10-07
Started: 2026-09-29
Actual: 10h15m

**Sequel to:** [[plan/core-creation/project.md]]
**Feeds:** [[plan/github-transport/project.md]] (thin remainder: lock workspace, receive files, inform Core)

## Notes

- 2026-09-29 — Split from [[plan/github-transport/project.md]]. Alan: once this Project is done, github-transport only locks the workspace, receives the files, and informs the revised Core of changes. Core works through the changes. Locked step 1 and the 2026-09-28 grilling that named Parse/Persist axes and stacks live here: [[arch.md]].
- 2026-09-29 — Sequence on [[arch.md]] is `expand-contract` (current Core → Target — Server Core on [[arch.md]]). Spec prerequisites for full `/to-arch` absent; Alan chose expand-contract only. Stage stays `chart` (not `arch`).
- 2026-09-29 — Alan: expand-contract may be slow to contract.  (whole sequence). Persist: new collectors, callers move to them, loop calls existing persist functions ([[arch.md]] §3 step 3 Core Persist stack). Unparsed and git-pull ops lock against persist ([[arch.md]] §3 step 4 **§6 locks catch-up** / §6).
- 2026-09-29 — Path control: every DataDir use is or becomes Core read/write file or read/write directory, or changes to the node / relative-path protocol. Living rule: [[arch.md]] §3 step 5 Path control. Inventory snapshot: [DataDir caller inventory](reports/datadir-caller-inventory.md).
- 2026-09-29 — Mailbox: 6.1 enable git requests, 6.2 call them, 6.3 stop any old. Note: [[arch.md]] §3 step 6 Mailbox owns file and git work.
- 2026-09-29 — Alan: leave tickets undrafted. Draft a ticket only when a use case needs one; each ticket targets a use case from the expand-contract sequence on [[arch.md]] (a beat or a caller shift), not a pre-built full set.
- Step 1 markers already shipped under github-transport as [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md) (`done`). Expand-contract migrate/contract and later sequence steps stay on this Project ([[arch.md]] §3).
- Parse product home stays [[plan/parse-thread/project.md]]. This Project owns how Core pushes work and how special-node axes drive Parse and Persist. Parse and Persist setup (stack/push/consumer; collectors/loop) live inside Core and stay hidden from outside Core such as RouteRegistration (Alan, 2026-09-30; [[arch.md]] §3 steps 2–3); that move is not done.
- Map: [[map.md]]. Expand-contract architecture note: [[arch.md]].
- 2026-09-30 — Alan locked the Core locking model (drift axes informational not locks, parse thread, persist thread when Unpersisted and Parsed, workspace lock as aggregate, per-member persist locks, filesystem backstop). Home: [[arch.md]] §6. Map decision 13.
- 2026-09-30 — Alan approved Candidate A: stand §6 workspace lock and per-member persist locks beside `withWorkTreeGate`, migrate Persist/parse/git onto §6, then contract the gate. Home: [[arch.md]] §3 step 4 **§6 locks catch-up**.
- 2026-09-30 — Alan rejected Candidate B (reading path / index). Sole Core seam authority is this Project ([[arch.md]]). Compact description of the same target: [[plan/architecture/server-core.md]], linked from [[arch.md]] Target — Server Core. Map decision 14.
- 2026-10-01 — [[arch.md]] stays expand-contract. Stage stays `build`. Committed claim marks for Core are on [[doc/current/core.md]] and the pages that hub links. Workspace graph and the persistence model keep their own claim homes. Map §4 (mailbox enable, call, and stop) stays on [[arch.md]] §3 step 6 and is not on [[doc/current/]].
- 2026-10-01 — Alan: axis completion queue. Message type `AxisCompletion`. Queue type `AxisCompletionQueue`. Module **Core loop** holds the queue. File: `src/Server/Core/CoreMailboxBackend.fs`. Story paths, the Module map, and Seams stay on [[arch.md]]. Decision 9 does not skip them. Stage stays `build`. Map decision 15. Superseded later the same day by InMsg on the one mailbox queue.
- 2026-10-01 — Alan: of `CoreMsg`, only `SnapshotDone` is an internal message, packaged with setting Persisted. `CoreMsg` loses `SnapshotDone`. `AxisCompletion` case `SnapshotDone` replaces `PersistFinished`. Stage stays `build`. Map decision 16. Superseded later the same day: the case is `InMsg` `SnapshotDone`.
- 2026-10-01 — [06 — Explicit parse command on a File (Load)](issues/06-explicit-parse-command-load-file.md) revisited. The coded finish posts a Change and writes the parsed axis with `Op.SetDocumentState`. The required finish is InMsg ParseFinished through the private function. Status `defined`. Stage stays `build`.
- 2026-10-01 — Alan: supersede map decisions 15 and 16. The internal message is `InMsg`. A private function on the mailbox adds `InMsg` to the one mailbox queue. A public function adds `CoreMsg` to that same queue. The queue puller hands the item to its handler. There is no second queue. Cases: `ParseFinished`, `SnapshotDone`. Stage stays `build`.
- 2026-10-01 — This slice is in code. File parse finish adds InMsg ParseFinished. The db agent adds InMsg SnapshotDone. CoreMsg has no SnapshotDone. The persist thread is not built. [06 — Explicit parse command on a File (Load)](issues/06-explicit-parse-command-load-file.md) Status `coded`. Stage stays `build`.
- 2026-10-07 — Alan accepted `InMsg` `MarkUnparsed`. A disk-newer File Node is set Unparsed through that case. Case home: [[arch.md]] §10 Core loop. Current claim: [[doc/current/mailbox.md]]. Stage stays `build`.
- 2026-10-07 — Alan locked both Unparsed approaches. After pull, the git changed list immediately marks matching File Nodes and Directory Nodes Unparsed. The workspace lock sequence stays [[arch.md]] §6. Ticket: [07 — Git changed list sets Unparsed](issues/07-git-changed-list-unparsed.md). The Directory reconcile approach stays [07 — Directory Unparsed during reconcile](../parse-thread/issues/07-directory-unparsed-during-reconcile.md). Stage stays `build`.
- 2026-10-07 — Alan: leftover core-creation work continues here via pointer tickets [08](issues/08-pointer-core-actor-pool.md)–[17](issues/17-pointer-prove-testactor-hello.md). Solid core v1 is implemented on [[plan/core-creation/project.md]]; this Project is v2. Stage stays `build`.
- 2026-10-07 — [18 — Core Persist stack](issues/18-core-persist-stack.md) stands the persist collectors and persist thread inside Core. Callers use the collectors. The write body stays [[src/Server/DocumentPersistChange.fs]]. Parse setup is unchanged. Stage stays `build`.

## Issues

- [01 — Persist/git work-tree gate](issues/01-persist-git-work-tree-gate.md) — exclusive gate revoked as lasting protocol; stand §6 then contract `withWorkTreeGate` ([[arch.md]] §3 step 4 **§6 locks catch-up**). Status `done`.
- [02 — Git Load: Unparsed then Parse stack](issues/02-git-load-unparsed-then-parse-stack.md) — workspace lock → drain → pull → mark Unparsed → release → parse thread. Status `done`.
- [03 — One Parse thread stack](issues/03-one-parse-thread-stack.md) — one long-lived Parse stack; Core pushes; consumer is the parse thread. Status `done`.
- [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md) — special-node axes and Core Persist stack; locks on [[arch.md]] §6. Status `done`.
- [05 — Selection-scoped Parse after whole-tree git Load](issues/05-selection-scoped-parse-after-whole-tree-git-load.md) — selection push-on-stack. Status `done`.
- [06 — Explicit parse command on a File (Load)](issues/06-explicit-parse-command-load-file.md) — mailbox Load of a File node. On finish the parse thread adds InMsg ParseFinished through the private function. Status `coded`.
- [07 — Git changed list sets Unparsed](issues/07-git-changed-list-unparsed.md) — after pull, the git changed list marks matching File Nodes and Directory Nodes Unparsed. The §6 sequence stays. Status `defined`.
- [08 — Pointer: Core Actor pool](issues/08-pointer-core-actor-pool.md) — pointer to [[plan/core-creation/issues/02-core-actor-pool.md|02 — Core Actor pool]]; owned/continued here. Status `defined`.
- [09 — Pointer: Core Files contract](issues/09-pointer-core-files-contract.md) — pointer to [[plan/core-creation/issues/07-define-core-files-contract.md|Define the Core Files contract]]; owned/continued here. Status `defined`.
- [10 — Pointer: Core Query contract](issues/10-pointer-core-query-contract.md) — pointer to [[plan/core-creation/issues/08-define-core-query-contract.md|Define the Core Query contract]]; owned/continued here. Status `defined`.
- [11 — Pointer: Server tracks credentials](issues/11-pointer-server-tracks-credentials.md) — pointer to [[plan/core-creation/issues/14-server-tracks-credentials.md|14 — Server tracks credentials]]; owned/continued here. Status `defined`.
- [12 — Pointer: Launch Actor (Focus registration)](issues/12-pointer-launch-actor-and-hold-span.md) — pointer to [[plan/core-creation/issues/15-launch-actor-and-hold-span.md|15 — Launch an Actor and hold the span]]; owned/continued here. Status `defined`.
- [13 — Pointer: Finish and drop](issues/13-pointer-finish-and-drop.md) — pointer to [[plan/core-creation/issues/18-finish-and-drop.md|18 — Finish and drop]]; owned/continued here. Status `defined`.
- [14 — Pointer: Database down and probe](issues/14-pointer-database-down-and-host-stop.md) — pointer to [[plan/core-creation/issues/19-database-down-and-host-stop.md|19 — Database down and probe]]; owned/continued here. Status `defined`.
- [15 — Pointer: Prove Core Actor lifecycle with TestActor](issues/15-pointer-prove-core-actor-lifecycle-testactor.md) — pointer to [[plan/core-creation/issues/27-prove-core-actor-lifecycle-with-testactor.md|27 — Prove Core Actor lifecycle with TestActor]]; owned/continued here. Status `defined`.
- [16 — Pointer: Drain Actor lifecycle on host stop](issues/16-pointer-drain-actor-lifecycle-on-host-stop.md) — pointer to [[plan/core-creation/issues/28-drain-actor-lifecycle-on-host-stop.md|28 — Drain Actor lifecycle on host stop]]; owned/continued here. Status `defined`.
- [17 — Pointer: Prove TestActor hello](issues/17-pointer-prove-testactor-hello.md) — pointer to [[plan/core-creation/issues/29-prove-testactor-hello.md|29 — Prove TestActor hello]]; owned/continued here. Status `defined`.
- [18 — Core Persist stack](issues/18-core-persist-stack.md) — collectors and the persist thread inside Core; callers use the collectors; the thread adds `InMsg` `SnapshotDone`. Status `coded`.
