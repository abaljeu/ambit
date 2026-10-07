# Parse thread

Stage: build
Summary: A continuous Server parse thread turns file-shaped disk into Graph, takes priority from Browser wants, and emits Changes. A continuous persist thread turns Graph into disk. Both add InMsg through the mailbox private function. The core loop sets the axes.
Updated: 2026-10-07
Started: 2026-09-28
Actual: 1h 40m

**Part of:** [[plan/roadmap/epics/chapters/automatic-parse.md]]

## Notes

- File-shaped file→Graph stays on the Server. The Browser and the App do not Parse from Workspace Download or Workspace Upload.
- Browser wants come from [[plan/browser-residency/project.md]]. File transit stays on [[plan/transport-layer/project.md]].
- 2026-09-28 — Alan lock (chat), now on [[plan/core-refinement/project.md]]: one long-lived parse thread. Nobody starts an Actor after pull. Core pushes a reconcile target onto the stack; others may push too. Parse = disk → graph. Workspace/directory Parse reconciles immediate members only, then Unparsed on children that need work, then Parsed. Persist is a persist thread, fed by graph edits, blocked while the node is Unparsed. See [02 — Git Load: Unparsed then Parse stack](plan/core-refinement/issues/02-git-load-unparsed-then-parse-stack.md), [03 — One Parse thread stack](plan/core-refinement/issues/03-one-parse-thread-stack.md), and [04 — Parsed/Unparsed and Persisted/Unpersisted](plan/core-refinement/issues/04-parsed-unparsed-and-persisted-unpersisted.md).
- 2026-09-30 — Core seam sole authority (axes, stacks, locks, mailbox git handoff into Core) is [[plan/core-refinement/arch.md]]. This Project stays the Parse product home only.
- 2026-10-01 — Wayfinder map: [[map.md]]. Stage stays chart. Directory body home is [01 — Directory body home](issues/01-directory-parse-body-home.md). Structure-match Directory Load is [02 — Structure-match Directory Load](issues/02-structure-match-directory-load.md).
- 2026-10-01 — Front-half spec: [spec](spec.md). Stage is spec. Open grilling on [03 — Workspace Load after incoming files](issues/03-workspace-load-after-incoming-files.md) and [04 — Browser want priority versus directory reconcile](issues/04-browser-want-priority-versus-directory-reconcile.md) stays open.
- 2026-10-01 — Architecture: [arch](arch.md). Stage is arch. Open grilling stays open.
- 2026-10-01 — Axis completion: the parse thread and the persist thread enqueue AxisCompletion on AxisCompletionQueue. They do not edit the graph axes. Module Core loop applies AxisCompletion. Home: [[plan/core-refinement/arch.md]] §10 Core loop. Stage stays arch. Superseded later the same day by InMsg on the one mailbox queue.
- 2026-10-01 — Persist finish on this spec is AxisCompletion SnapshotDone, the same package as setting Persisted. Home: [[plan/core-refinement/arch.md]] §10 Core loop. Stage stays arch. Superseded later the same day: the case is InMsg SnapshotDone.
- 2026-10-01 — Alan: the internal message is InMsg on the one mailbox queue. The parse thread and the persist thread add InMsg through the private function. Home: [[plan/core-refinement/arch.md]] §10 Core loop. Stage stays arch.
- 2026-10-01 — This Project owns Directory reconcile. Definition: [Parse thread architecture](arch.md) §2 Module map, item 1 **Directory reconcile**. [core-refinement architecture](../core-refinement/arch.md) §5 item 10 **Directory reconcile** is the deferred pointer. Stage stays arch.
- 2026-10-01 — Coding ticket is [05 — Directory reconcile](issues/05-directory-reconcile.md). Stage is slice.
- 2026-10-01 — First implement of [05 — Directory reconcile](issues/05-directory-reconcile.md). Stage is build.
- 2026-10-01 — Setting Unparsed, recursive update is [06 — Setting Unparsed, recursive update](issues/06-setting-unparsed-recursive-update.md). This ticket does not block [05 — Directory reconcile](issues/05-directory-reconcile.md), and 05 does not block it.
- 2026-10-01 — Alan: Directory reconcile inputs are the disk directory, the graph, and the directory id. The directory id plus the graph is the Directory Node. Directory reconcile reads the Directory body from that graph. The disk directory is required. Home: [Parse thread architecture](arch.md) §2 Module map, item 1 **Directory reconcile**, Interface **Inputs**, and [05 — Directory reconcile](issues/05-directory-reconcile.md) §1 Directory reconcile, **Inputs**.
- 2026-10-07 — Alan locked both Unparsed approaches. Directory Nodes during reconcile are [07 — Directory Unparsed during reconcile](issues/07-directory-unparsed-during-reconcile.md). The git changed list is [07 — Git changed list sets Unparsed](../core-refinement/issues/07-git-changed-list-unparsed.md). [06 — Setting Unparsed, recursive update](issues/06-setting-unparsed-recursive-update.md) stays disk-newer File Node Unparsed only. Stage stays build.
