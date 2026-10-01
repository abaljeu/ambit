# Parse thread

Stage: chart
Summary: A continuous Server Parse thread turns file-shaped disk into Graph, takes priority from Browser wants, and emits Changes.
Updated: 2026-10-01
Started: 2026-09-28
Actual: 10m

**Part of:** [[plan/roadmap/epics/chapters/automatic-parse.md]]

## Notes

- File-shaped file→Graph stays on the Server. The Browser and the App do not Parse from Workspace Download or Workspace Upload.
- Browser wants come from [[plan/browser-residency/project.md]]. File transit stays on [[plan/transport-layer/project.md]].
- 2026-09-28 — Alan lock (chat), now on [[plan/core-refinement/project.md]]: one long-lived Parse actor. Nobody starts an Actor after pull. Core pushes a reconcile target onto the stack; others may push too. Parse = disk → graph. Workspace/directory Parse reconciles immediate members only, then Unparsed on children that need work, then Parsed. Persist is a Core async stack (not this Actor), fed by graph edits, blocked while the node is Unparsed. See [02 — Git Load: Unparsed then Parse stack](plan/core-refinement/issues/02-git-load-unparsed-then-parse-stack.md), [03 — One Parse thread stack](plan/core-refinement/issues/03-one-parse-thread-stack.md), and [04 — Parsed/Unparsed and Persisted/Unpersisted](plan/core-refinement/issues/04-parsed-unparsed-and-persisted-unpersisted.md).
- 2026-09-30 — Core seam sole authority (axes, stacks, locks, mailbox git handoff into Core) is [[plan/core-refinement/arch.md]]. This Project stays the Parse product home only.
- 2026-10-01 — Wayfinder map: [[map.md]]. Stage stays chart. Directory Parse body home is [01 — Directory Parse body home](issues/01-directory-parse-body-home.md). Structure-match Directory Load is [02 — Structure-match Directory Load](issues/02-structure-match-directory-load.md).