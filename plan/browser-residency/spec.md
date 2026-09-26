# Browser residency

Sources: [map.md](map.md) Destination and Notes (2026-09-26 grill locks). Part of [[plan/roadmap/epics/chapters/incremental-operations.md]]. Successor to [[plan/selective-client-loading/project.md]].

## 1. Problem Statement

1. **Whole Server Graph is too much** — A person opens the Browser on a Graph that is already large on the Server. First paint and Sync must not wait on the whole Server Graph.
2. **Complete Workspace is still too much** — The prior [selective client loading](plan/selective-client-loading/project.md) slice starts with a complete Workspace and grows only through the user-facing Load command. That slice is done. A person looking at Included context still pays for Workspace Children they do not see.
3. **Residency must grow without a command** — After first paint, visible Unloaded Nodes need their Children. The person must not click a hollow-circle Bullet or run Load for that growth.
4. **Answers must be complete edges** — When the Browser wants Children, the Server must not return dangling edges. The Browser must receive child edges and the Nodes those edges point at, so an absent `childMap` key stays Unloaded and a present key is Loaded.

## 2. Solution

1. **Visible-closure Graph** — The Browser starts with a small visible-closure Graph: Included context and the path that frames it. That set is Zoom-scoped. It is not the whole Server Graph and not a complete Workspace.
2. **Bootstrap set** — First paint includes Children of reserved Nodes ROOT, TRASH, Workspaces Node, and SYSTEM, plus ancestors of the Zoom root, plus Included. Bootstrap uses the same edges-plus-Nodes package as later wants.
3. **Auto wants** — A Want names Nodes whose Children are desired. Ongoing priority is (1) Included Nodes that miss Children, then (2) those Children. Reserved Nodes and Zoom ancestors are bootstrap, not a third ongoing tier. Auto wants ride post-Event and Poll with Changes. They need no click and no command.
4. **Edges plus Nodes** — The Server answers a Want with child edges (`Graph.childMap`) and, separately, the Nodes those edges point at. No dangling edges. An absent `childMap` key is Unloaded. A present key, including an empty list, is Loaded.
5. **Hollow-circle Bullet** — An Unloaded Node (absent `childMap`) or an Unparsed Node shows a hollow-circle Bullet as today. There is no new per-Node loading Status. Auto growth does not require a click. If hollow-circle → Load is already wired, it may remain; auto wants do not use that command.
6. **Find stays in residence** — Default Find searches Resident Nodes only. A Server-mode Find that asks the Server and receives found Nodes is later work on [05 — Chart server-mode Find](issues/05-chart-server-mode-find.md).
7. **Explicit Load remains** — The user-facing Load command may dual-run the old Fetch path. New auto and bootstrap use the edges-plus-Nodes package. When the old Fetch path dies is [06 — Dual-run vs migrate explicit Load Fetch](issues/06-dual-run-vs-migrate-explicit-load.md).

## 3. User Stories

1. **Open a large Server Graph** — As a person, I want the Browser to open a Graph that is already large on the Server without receiving that whole Graph, so that first paint depends on what I see, not on Server size.
2. **Included first paint** — As a person, I want first paint to show Included context under Zoom, honoring Fold, so that the SiteMap I look at is Resident.
3. **Framing path** — As a person, I want the ancestors of the Zoom root Resident at first paint, so that the path that frames Included context is present.
4. **ROOT Children** — As a person, I want direct Children of ROOT Resident at first paint, so that reserved structure under ROOT is present even when a Child sits outside Included.
5. **TRASH Children** — As a person, I want direct Children of TRASH Resident at first paint, so that the recycle bin's first rank is present.
6. **Workspaces Node Children** — As a person, I want direct Children of the Workspaces Node Resident at first paint, so that named Workspace headers are present.
7. **SYSTEM Children** — As a person, I want direct Children of SYSTEM Resident at first paint, so that the SYSTEM first rank is present.
8. **Not a complete Workspace** — As a person, I want first paint to stop at that visible-closure set, so that I do not download a complete Workspace I am not looking at.
9. **Same package after bootstrap** — As the Browser, I want bootstrap and later wants to use the same edges-plus-Nodes package, so that first paint and ongoing growth install residency the same way.
10. **Hollow Unloaded Bullet** — As a person, I want an Unloaded Node (absent `childMap` key) to show a hollow-circle Bullet, so that I can see that Children are not present.
11. **Hollow Unparsed Bullet** — As a person, I want an Unparsed Node to show the same hollow-circle Bullet as today, so that Unloaded and Unparsed stay distinct facts on one glyph.
12. **No new loading Status** — As a person, I want no new per-Node loading Status, so that Unloaded, Loaded, and Unparsed remain the residency and source facts I already know.
13. **Auto want Included** — As a person, I want Included Nodes that miss Children wanted first, so that what I am looking at gains Children without my asking.
14. **Auto want those Children** — As a person, I want those Children wanted next, so that the next rank under Included can arrive after their parents.
15. **No third ongoing tier** — As the Browser, I want reserved Nodes and Zoom ancestors kept as bootstrap, so that ongoing wants do not grow a third priority tier.
16. **No click** — As a person, I want auto wants to run without a click, so that a hollow-circle Bullet is a signal, not a required control.
17. **No command** — As a person, I want auto wants to run without Load or any other command, so that residency growth is silent.
18. **Hollow-click Load may remain** — As a person, I want an existing hollow-circle → Load wiring left in place if it is present, so that a click still runs Load while auto wants do not need it.
19. **Wants on post-Event** — As the Browser, I want post-Event to carry Changes plus the current Want, so that an Action I just sent also asks for desired Children.
20. **Wants on Poll** — As the Browser, I want Poll to carry Changes plus the current Want, so that quiet Sync still grows residency.
21. **Edges in the answer** — As the Browser, I want the Server answer to include child edges for each wanted Node, so that `Graph.childMap` gains an authoritative list.
22. **Nodes in the answer** — As the Browser, I want the Server answer to include the Nodes those edges point at, separately from the edges, so that every new edge has a Resident target.
23. **No dangling edges** — As the Browser, I want no child edge whose target Node is absent, so that the resident projection never invents a header-less Child.
24. **Absent key stays Unloaded** — As the Browser, I want a parent with no `childMap` key to stay Unloaded, so that a missing list is not an empty leaf.
25. **Present key is Loaded** — As the Browser, I want a present `childMap` key, including `[]`, to mean Loaded, so that a true leaf and an Unloaded parent stay distinct.
26. **Children arrive on the Bullet** — As a person, I want the hollow-circle Bullet to give way once those Children are Loaded and the Node is not Unparsed, so that the glyph matches the new residency.
27. **Growth while I work** — As a person, I want auto wants to continue while I edit, so that Included Unloaded Nodes keep receiving Children across later Poll and post-Event.
28. **Find in residence** — As a person, I want default Find to search Resident Nodes only, so that Find stays synchronous and does not ask the Server.
29. **Find commit stays Zoom** — As a person, I want committing a residence Find hit to navigate with ordinary Zoom, so that Find does not Fetch in this spec.
30. **Load command still there** — As a person, I want the user-facing Load command to remain, so that Upload, Parse, and explicit Fetch still have a command when I invoke them.
31. **Load may dual-run Fetch** — As the Browser, I want explicit Load able to dual-run the old Fetch path while auto and bootstrap use edges plus Nodes, so that the command does not have to migrate on the same day as silent growth.
32. **Commands that name Nodes later** — As a person, I want no new command that names Nodes for this destination, so that silent wants are enough to reach visible-closure growth.
33. **SiteMap honors Fold** — As a person, I want Included to honor Fold, so that a folded Node is not treated as a deep visible tree for wants.
34. **Unloaded is not empty** — As a person, I want an Unloaded hollow-circle Bullet to mean Children are not here yet, so that I do not mistake it for a Loaded leaf.
35. **Server stays large** — As a person, I want the Server Graph to stay large and authoritative, so that the Browser can keep asking for the next wanted Children.
36. **Reserved spelling** — As a builder, I want reserved Node SYSTEM spelled SYSTEM, so that the bootstrap set matches [[CONTEXT.md]] and the Graph's SYSTEM Node.

## 4. Out of Scope

1. **Parse Actor design** — This spec does not design how Parse consumes Browser wants. Pointer: [[plan/parse-actor/project.md]].
2. **File transit Actor** — This spec does not design Workspace Upload, Download, or ledger or inventory Load-path work. Pointer: [[plan/transport-layer/project.md]].
3. **Document partition** — This spec does not add document-scoped Server residency or partition membership.
4. **Cache leases** — This spec does not add Server or Browser interest leases.
5. **LRU** — This spec does not evict Resident Nodes by recency.
6. **IndexedDB** — This spec does not persist a resident snapshot in the Browser.
7. **Server eviction** — This spec does not remove Nodes from the Server Graph cache.
8. **Commands that name Nodes** — This spec does not add user-facing commands that request named Nodes. They come later and are not required for this destination.
9. **Whole-Workspace bootstrap** — This spec does not start the Browser with a complete Workspace.
10. **Server-mode Find** — This spec does not ask the Server for Find hits or Fetch found Nodes before navigate. That design is [05 — Chart server-mode Find](issues/05-chart-server-mode-find.md).
11. **Want field shape** — Locked on [01 — Lock Sync want + edges/Nodes package shape](issues/01-lock-sync-want-package-shape.md): JSON `want` (`NodeId` list) on Poll and post-Event; always send, empty compose is `want: []`; Poll may need a body so both doors share that field; answer `nodes` and `childMap` on `ChangeSuccessResponse`; `ApiVersion.current = 13` ships with the expand.
12. **Zoom-restore edge cases** — This spec does not lock what happens when saved Zoom is missing or stale. That decision is [02 — Lock bootstrap visible-closure set](issues/02-lock-bootstrap-visible-closure.md).
13. **Want cadence details** — This spec does not lock Poll-versus-post-Event cadence. Empty-Want encoding is locked on [01 — Lock Sync want + edges/Nodes package shape](issues/01-lock-sync-want-package-shape.md). Cadence remains [03 — Lock ongoing want priority and when wants are attached](issues/03-lock-ongoing-want-priority.md).
14. **Death of old Load Fetch** — This spec does not lock when the old Fetch path dies. That decision is [06 — Dual-run vs migrate explicit Load Fetch](issues/06-dual-run-vs-migrate-explicit-load.md).

## 5. Further Notes

1. **Map** — Live decisions stay on [map.md](map.md). This spec synthesizes Destination and Notes locks. It does not resolve map tickets.
2. **Vocabulary** — Prefer Resident, Unloaded, Included, Fetch, Load, Poll, Change, `childMap`, and Bullet. An Unloaded parent is an absent `childMap` key.
3. **Find later** — Default Find searches residence only. [05 — Chart server-mode Find](issues/05-chart-server-mode-find.md) will design a Server mode that asks the Server and lets the Browser receive found Nodes, then later work may Fetch those Nodes before navigate.
4. **Selective leftovers** — [04 — Retire selective hollow-click and resident-only Find assumptions](issues/04-retire-selective-hollow-click-find.md) records cancelled [selective client loading](plan/selective-client-loading/project.md) tickets. File-transit leftovers point at [[plan/transport-layer/project.md]].
5. **Grill** — Destination locked 2026-09-26. Reserved SYSTEM spelling is SYSTEM.
