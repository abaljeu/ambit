# core-refinement

Stage: build
Summary: Sequel to [[plan/core-creation/project.md]]: refine Core so that after transport lands files, Core works through disk and Graph changes (Parse stack, Persist stack, state axes) until everything is updated.
Updated: 2026-10-01
Started: 2026-09-29
Actual: 3h15m

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
- 2026-10-01 — [[arch.md]] stays expand-contract. Map decision 9: no Story paths and no Module map. Stage stays `build`. Committed claim marks are on [[doc/current/server.md]], [[doc/current/workspace-graph.md]], and [[doc/current/persistence-model.md]]. Map §4 (mailbox enable, call, and stop) stays on [[arch.md]] §3 step 6 and is not on [[doc/current/]].

## Issues

- [01 — Persist/git work-tree gate](issues/01-persist-git-work-tree-gate.md) — exclusive gate revoked as lasting protocol; stand §6 then contract `withWorkTreeGate` ([[arch.md]] §3 step 4 **§6 locks catch-up**). Status `done`.
- [02 — Git Load: Unparsed then Parse stack](issues/02-git-load-unparsed-then-parse-stack.md) — workspace lock → drain → pull → mark Unparsed → release → parse thread. Status `done`.
- [03 — One Parse thread stack](issues/03-one-parse-thread-stack.md) — one long-lived Parse stack; Core pushes; consumer is the parse thread. Status `done`.
- [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md) — special-node axes and Core Persist stack; locks on [[arch.md]] §6. Status `done`.
- [05 — Selection-scoped Parse after whole-tree git Load](issues/05-selection-scoped-parse-after-whole-tree-git-load.md) — selection push-on-stack. Status `done`.
- [06 — Explicit parse command on a File (Load)](issues/06-explicit-parse-command-load-file.md) — first use case: mailbox Load of a File node through Parse stack. Status `coded`.
