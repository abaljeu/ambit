# Browser residency

Labels: wayfinder:map

## 1. Destination

A person opens the Browser on a Graph that is already large on the Server. The Browser starts with a small visible-closure Graph (Included context and the path that frames it), not the whole Server Graph. It grows by auto wants: first visible Nodes that miss Children, then those Children. An Unloaded Node shows a hollow-circle Bullet until those Children arrive. Auto wants need no click and no command. When Find picks a hit that is not Resident, later work may Fetch those Nodes before navigate (see tickets). Post-Event and Poll carry Changes plus the Nodes the Browser wants. Commands that name Nodes come later and are not required for this destination.

Bootstrap is that same Zoom-scoped visible-closure, not a complete Workspace: Children of reserved Nodes ROOT, TRASH, Workspaces Node, and SYSTEM, plus ancestors of the Zoom root, plus Included. Auto wants ride post-Event and Poll with Changes. The Server answers with child edges (`Graph.childMap`) and the Nodes those edges point at. Hollow-circle presentation stays Unloaded (absent `childMap` key) or Unparsed as today. Find defaults to residence only; a Server-mode Find is later work on this map.

## 2. Notes

1. **Skills** — [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/grilling/SKILL.md]], [[.agents/skills/domain-modeling/SKILL.md]], [[.agents/skills/research/SKILL.md]]. Use [[.agents/skills/implement-fsharp-feature/SKILL.md]] only after the way is clear.
2. **Part of** — [[plan/roadmap/epics/chapters/incremental-operations.md]].
3. **Successor** — This Project succeeds [[plan/selective-client-loading/project.md]] (Stage done; prior whole-Workspace slice).
4. **Vocabulary** — Prefer CONTEXT terms Resident, Unloaded, Included, Fetch, Load, Poll, Change, and `childMap`.
5. **Silent residency growth** — Auto wants ride post-Event and Poll with Changes. The Server answers with edges plus Nodes. Auto growth is not the user-facing Load command. Do not un-wire hollow-circle → Load if present; hollow-click Load may remain, but the auto path does not require a click or a command.
6. **Want** — A Want names Nodes whose Children are desired. The answer returns child edges (`Graph.childMap`) and the Nodes those edges point at — no dangling edges. An absent `childMap` key is Unloaded. This matches the childMap redesign.
7. **Want priority (ongoing)** — (1) Included Nodes that miss Children, (2) those Children. Reserved Nodes and Zoom ancestors are bootstrap, not a third ongoing tier.
8. **Hollow circle** — Unloaded (absent `childMap`) or Unparsed as today. No new per-Node loading Status.
9. **Find** — Default searches residence only. A later ticket on this map redesigns a Server mode that asks the Server and the Browser receives found Nodes.
10. **Explicit Load** — May dual-run the old Fetch path. New auto and bootstrap use the edges-plus-Nodes package. Migrate or dual-run is a later ticket on this map.
11. **Parse Actor** — How Parse consumes Browser wants is out of scope. Pointer: [[plan/parse-actor/project.md]].
12. **File transit** — Out of scope. Pointer: [[plan/transport-layer/project.md]].
13. **Selective leftovers** — [28 — Make hollow-circle clicks invoke Load](plan/selective-client-loading/issues/28-make-hollow-circle-clicks-invoke-load.md) and [29 — Validate two-phase state loading exploration](plan/selective-client-loading/issues/29-validate-two-phase-state-loading.md) are cancelled against this destination. [24 — Keep navigation and Find resident-only](plan/selective-client-loading/issues/24-keep-navigation-and-find-resident-only.md), [27 — Document delivered selective-loading baseline](plan/selective-client-loading/issues/27-document-delivered-selective-loading-baseline.md), [30 — Ledger reuse on already-synced Load](plan/selective-client-loading/issues/30-ledger-reuse-on-already-synced-load.md), [31 — Skip workspace-inventory when Unloaded](plan/selective-client-loading/issues/31-skip-workspace-inventory-when-unloaded.md), and [32 — Defer or narrow path-sync ledger waterfall after push](plan/selective-client-loading/issues/32-defer-path-sync-ledger-waterfall.md) are cancelled or retargeted from that done Project. Pointers live on [04 — Retire selective hollow-click and resident-only Find assumptions](issues/04-retire-selective-hollow-click-find.md).

## 3. Decisions so far

<!-- the index — one numbered line per resolved ticket: enough to judge relevance, then zoom the link for the detail the ticket holds -->

## 4. Not yet specified

1. **Batching and backpressure** — How many wanted Nodes ride one post-Event or Poll, and what happens when the Browser wants more than the Server should answer in one package.
2. **ApiVersion number** — Which integer marks the edges-plus-Nodes package. [01 — Lock Sync want + edges/Nodes package shape](issues/01-lock-sync-want-package-shape.md) asks the field shape and ApiVersion implications; the number itself is not pinned yet.
3. **Desktop vs Browser** — Whether the App and the Browser attach or answer wants differently, if they differ at all.

## 5. Out of scope

1. **Parse Actor design** — How Parse consumes Browser wants. Pointer: [[plan/parse-actor/project.md]].
2. **File transit Actor** — Workspace Upload, Download, and ledger or inventory Load-path work. Pointer: [[plan/transport-layer/project.md]].
3. **Document partition** — Document-scoped Server residency and partition membership.
4. **Cache leases** — Server or Browser interest leases.
5. **LRU** — Eviction by recency.
6. **IndexedDB** — Browser persistence of a resident snapshot.
7. **Server eviction** — Removing Nodes from the Server Graph cache.
8. **Commands that name Nodes** — User-facing commands that request named Nodes. They come later and are not required for this destination.
9. **Whole-Workspace bootstrap** — Complete Workspace residency at first paint. Superseded by Zoom-scoped visible-closure.
