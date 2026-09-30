# core-refinement

Stage: build
Summary: Sequel to [[plan/core-creation/project.md]]: refine Core so that after transport lands files, Core works through disk and Graph changes (Parse stack, Persist stack, state axes) until everything is updated.
Updated: 2026-09-29
Started: 2026-09-29
Actual: 3h5m

**Sequel to:** [[plan/core-creation/project.md]]
**Feeds:** [[plan/github-transport/project.md]] (thin remainder: lock workspace, receive files, inform Core)

## Notes

- 2026-09-29 — Split from [[plan/github-transport/project.md]]. Alan: once this Project is done, github-transport only locks the workspace, receives the files, and informs the revised Core of changes. Core works through the changes. Locked step 1 and the 2026-09-28 grilling that named Parse/Persist axes and stacks live here: [[arch.md]].
- 2026-09-29 — Sequence on [[arch.md]] is `expand-contract` (current Core → [[plan/architecture/server-core.md]]). Spec prerequisites for full `/to-arch` absent; Alan chose expand-contract only. Stage stays `chart` (not `arch`).
- 2026-09-29 — Alan: expand-contract is slow (whole sequence). Persist: new collectors, callers move to them, loop calls existing persist functions; Unparsed and git-pull ops lock against persist. Note: [[arch.md]] §3 step 3.
- 2026-09-29 — Path control: every DataDir use is or becomes Core read/write file or read/write directory, or changes to the node / relative-path protocol. Living rule: [[arch.md]] §3 step 4. Inventory snapshot: [DataDir caller inventory](reports/datadir-caller-inventory.md).
- 2026-09-29 — Mailbox: 5.1 enable git requests, 5.2 call them, 5.3 stop any old. Note: [[arch.md]] §3 step 5.
- 2026-09-29 — Alan: leave tickets undrafted. Draft a ticket only when a use case needs one; each ticket targets a use case from the expand-contract sequence on [[arch.md]] (a beat or a caller shift), not a pre-built full set.
- Step 1 markers already shipped under github-transport as [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md) (`done`). Expand-contract migrate/contract and later sequence steps stay on this Project ([[arch.md]] §3).
- Parse Actor home stays [[plan/parse-actor/project.md]]. This Project owns how Core pushes work and how special-node axes drive Parse and Persist.
- Map: [[map.md]]. Expand-contract architecture note: [[arch.md]].


## Issues

- [01 — Persist/git work-tree gate](issues/01-persist-git-work-tree-gate.md) — exclusive gate revoked; Unparsed / Unpersisted replace it. Status `done`.
- [02 — Git Load: Unparsed then Parse stack](issues/02-git-load-unparsed-then-parse-stack.md) — after files land, Unparsed then push onto Parse. Status `done`.
- [03 — One Parse actor stack](issues/03-one-parse-actor-stack.md) — one long-lived Parse actor; Core pushes. Status `done`.
- [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md) — special-node axes and Core Persist stack. Status `done`.
- [05 — Selection-scoped Parse after whole-tree git Load](issues/05-selection-scoped-parse-after-whole-tree-git-load.md) — selection push-on-stack. Status `done`.
- [06 — Explicit parse command on a File (Load)](issues/06-explicit-parse-command-load-file.md) — first use case: mailbox Load of a File node through Parse stack. Status `coded`.
