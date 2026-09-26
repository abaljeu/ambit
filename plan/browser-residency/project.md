# Browser residency

Stage: slice
Summary: The Browser starts with a small visible-closure Graph and grows by auto wants: Included Nodes that miss Children first, then those Children. An Unloaded Node shows a hollow-circle Bullet until those Children arrive. Find of a not-Resident hit is later Fetch-before-navigate work. Post-Event and Poll carry Changes plus wanted Nodes.
Updated: 2026-09-26

**Part of:** [[plan/roadmap/epics/chapters/incremental-operations.md]]

## Notes

- Successor to [[plan/selective-client-loading/project.md]] (Stage done; prior whole-Workspace slice).
- Auto wants need no click and no command. Commands that name Nodes come later.
- File transit stays on [[plan/transport-layer/project.md]]. Server Parse Actor stays on [[plan/parse-actor/project.md]].
- Map: [[map.md]].
- Spec: [[spec.md]].
- Arch: [[arch.md]].
- Implementation: [07 — Expand Want and edges/Nodes package](issues/07-expand-want-and-edges-nodes-package.md) through [12 — Contract old Load Fetch packages](issues/12-contract-old-load-fetch-packages.md).
- Frontier: [01 — Lock Sync want + edges/Nodes package shape](issues/01-lock-sync-want-package-shape.md), [02 — Lock bootstrap visible-closure set](issues/02-lock-bootstrap-visible-closure.md), and [03 — Lock ongoing want priority and when wants are attached](issues/03-lock-ongoing-want-priority.md) grilling is done. Next defined work is [04 — Retire selective hollow-click and resident-only Find assumptions](issues/04-retire-selective-hollow-click-find.md) (task) and [07 — Expand Want and edges/Nodes package](issues/07-expand-want-and-edges-nodes-package.md) (coding).
