# Spec ticket-readiness product review

Project: [Browser residency](plan/browser-residency/project.md). Sources: [spec.md](plan/browser-residency/spec.md), [map.md](plan/browser-residency/map.md), [project.md](plan/browser-residency/project.md), [arch.md](plan/browser-residency/arch.md), decision tickets [01 — Lock Sync want + edges/Nodes package shape](plan/browser-residency/issues/01-lock-sync-want-package-shape.md) through [06 — Dual-run vs migrate explicit Load Fetch](plan/browser-residency/issues/06-dual-run-vs-migrate-explicit-load.md) as needed. Vocabulary: [CONTEXT.md](CONTEXT.md).

## 1. Verdict

1. **Not ready to define tickets** — The planning set does not close destination, first-paint visible-closure, or ongoing Want growth in a testable way. A ticket writer has to pick one of two product behaviors for those paths. Existing coding tickets [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md) through [12 — Contract old Load Fetch packages](plan/browser-residency/issues/12-contract-old-load-fetch-packages.md) do not make the spec ready.

## 2. Blocking findings

### 2.1 First-paint visible-closure is not a closed set

1. **Where** — [2. Solution](plan/browser-residency/spec.md) items **1. Visible-closure Graph** and **2. Bootstrap set**; stories [2. Included first paint](plan/browser-residency/spec.md), [3. Framing path](plan/browser-residency/spec.md), [4. ROOT Children](plan/browser-residency/spec.md) through [8. Not a complete Workspace](plan/browser-residency/spec.md); [1. Destination](plan/browser-residency/map.md); [2. Module map](plan/browser-residency/arch.md) item **3. ResidentProjection**; [02 — Lock bootstrap visible-closure set](plan/browser-residency/issues/02-lock-bootstrap-visible-closure.md); [4. Out of Scope](plan/browser-residency/spec.md) item **12. Zoom-restore edge cases**.
2. **Contradiction** — The spec and the map put Included in the first-paint Graph with reserved Children and Zoom ancestors. [arch.md](plan/browser-residency/arch.md) records `visibleClosureGraph` as reserved-plus-ancestors and **Not Included**, and says Included stays later. Story 2 requires first paint to show Included under Zoom. Story 3 requires ancestors **Resident**, not Loaded. [02 — Lock bootstrap visible-closure set](plan/browser-residency/issues/02-lock-bootstrap-visible-closure.md) still asks whether reserved Children are always in first paint after the spec already locks stories 4–7. Status on that ticket is `needs-info`.
3. **Why it blocks tickets** — First paint has three open product axes: (a) whether Included headers are in the bootstrap Graph or arrive on the first auto Want; (b) whether Zoom ancestors are only Resident headers, or Loaded parents whose sibling Children are also Resident; (c) whether folded Included Nodes are Loaded at first paint. Stories 1–8 cannot accept a ticket until those axes are one set.
4. **Decision needed** — Choose one first-paint lock. Then paste it into [2. Solution](plan/browser-residency/spec.md) items 1–2, [1. Destination](plan/browser-residency/map.md) paragraph 2, and story 3. Close [02 — Lock bootstrap visible-closure set](plan/browser-residency/issues/02-lock-bootstrap-visible-closure.md) questions 1 and 2 as already locked. Keep question 3 only for missing Zoom (finding **2.4 Zoom restore has no default**). Align [arch.md](plan/browser-residency/arch.md) `visibleClosureGraph` to that lock. Do not leave Included as “later.”
5. **Option A — SiteMap spine (matches stories 2–8 and 33 most closely)** — Replacement text for spec Solution items 1–2: `1. **Visible-closure Graph** — The Browser starts with a visible-closure Graph, not the whole Server Graph and not a complete Workspace. Visible-closure at first paint is all of: Loaded reserved Nodes ROOT, TRASH, Workspaces Node, and SYSTEM, with every direct Child Resident; each ancestor of the Zoom root Loaded, with every direct Child of those ancestors Resident (sibling headers on the framing path); every Included Node Resident; every Included Node that is not folded Loaded. Folded Included Nodes may stay Unloaded at first paint. 2. **Bootstrap set** — First paint uses the same edges-plus-Nodes package as later Wants. Included is in this bootstrap Graph. Included is not delayed to the first auto Want.` Replacement text for story 3 name line: `3. **Framing path** — As a person, I want each ancestor of the Zoom root Loaded at first paint, so that the framing path and the sibling headers on that path are Resident.`
6. **Option B — Headers only** — Replacement text for spec Solution items 1–2: `1. **Visible-closure Graph** — The Browser starts with a visible-closure Graph, not the whole Server Graph and not a complete Workspace. Visible-closure at first paint is all of: Loaded reserved Nodes ROOT, TRASH, Workspaces Node, and SYSTEM, with every direct Child Resident; ancestors of the Zoom root Resident as headers (not Loaded unless they are also reserved or Included); every Included Node Resident. This spec does not Load an Included Node at first paint when that Node misses Children. 2. **Bootstrap set** — First paint uses the same edges-plus-Nodes package as later Wants for reserved Nodes only. Included headers and Zoom-ancestor headers ride in that Graph. Auto Want Loads Included Nodes that miss Children after first paint.` Do not keep story 2 if Option B cannot show SiteMap Children whose parents have no `childMap` key. If SiteMap needs those lists, Option B is invalid; take Option A.

### 2.2 Find Fetch-before-navigate is inside and outside the destination

1. **Where** — [1. Destination](plan/browser-residency/map.md) sentence “When Find picks a hit that is not Resident, later work may Fetch those Nodes before navigate”; [project.md](plan/browser-residency/project.md) Summary; [2. Solution](plan/browser-residency/spec.md) item **6. Find stays in residence**; stories [28. Find in residence](plan/browser-residency/spec.md) and [29. Find commit stays Zoom](plan/browser-residency/spec.md); [4. Out of Scope](plan/browser-residency/spec.md) item **10. Server-mode Find**; [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md); Chapter [Incremental operations](plan/roadmap/epics/chapters/incremental-operations.md) Goal, Required for done, and Notes (search hydration on this Project).
2. **Contradiction** — The spec keeps default Find on Resident Nodes only and puts Server-mode Find plus Fetch-before-navigate out of this spec. The map Destination still states Fetch-before-navigate. The project Summary still states it. The Chapter Required-for-done line for this Project still names search hydration. [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) is design only and does not implement Fetch.
3. **Why it blocks tickets** — Tickets defined from the spec omit Fetch-before-navigate. Tickets defined from the map Destination or the Chapter include it. The Project cannot be done against both.
4. **Decision needed** — Choose one destination. Do not keep both sentences.
5. **Option A — Residence-only Find in this Project** — Delete the Fetch-before-navigate sentence from [1. Destination](plan/browser-residency/map.md). Replacement for that paragraph’s Find clause: `Find defaults to Resident Nodes only. A Server-mode Find that asks the Server is later work and is not this destination.` Replacement for [project.md](plan/browser-residency/project.md) Summary Find clause: `Default Find searches Resident Nodes only.` Replacement for Chapter [Incremental operations](plan/roadmap/epics/chapters/incremental-operations.md) Required-for-done Browser-residency line: drop “search hydration”. Move search hydration to a later Project or a later Chapter note. Keep spec stories 28–29 and Out of Scope item 10.
6. **Option B — This Project includes Fetch-before-navigate** — Replacement for spec Solution item 6: `6. **Find then Fetch** — Default Find searches Resident Nodes only. When a person commits a hit that is not Resident, the Browser Fetches those Nodes before navigate.` Add stories that name that Fetch. Remove Out of Scope item 10. Change [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md) from design-only to a product lock that coding tickets consume. Keep the Chapter search-hydration line.

### 2.3 Ongoing Want tier 2 fights Fold

1. **Where** — [2. Solution](plan/browser-residency/spec.md) item **3. Auto wants**; stories [13. Auto want Included](plan/browser-residency/spec.md), [14. Auto want those Children](plan/browser-residency/spec.md), [33. SiteMap honors Fold](plan/browser-residency/spec.md); [2. Notes](plan/browser-residency/map.md) item **7. Want priority (ongoing)**; [03 — Lock ongoing want priority and when wants are attached](plan/browser-residency/issues/03-lock-ongoing-want-priority.md).
2. **Contradiction** — Priority (1) is Included Nodes that miss Children. Priority (2) is “those Children.” After (1) is answered, unfolded Children sit in Included and miss Children, so they already fall under (1). The only distinct effect of (2) is a Want for Children of Resident Nodes that are not Included — folded-away Children. Story 33 says a folded Node is not a deep visible tree for wants. Story 14 says the next rank under Included arrives after the parents. Those two stories do not name the same set.
3. **Why it blocks tickets** — Compose tests and Poll attach tests cannot know whether auto growth under a folded Included Node is required, forbidden, or empty. [03 — Lock ongoing want priority and when wants are attached](plan/browser-residency/issues/03-lock-ongoing-want-priority.md) says priority is already locked, so the fork is hidden.
4. **Decision needed** — Choose one Want rule. Rewrite spec Solution item 3 and story 14. Rewrite map Notes item 7.
5. **Option A — Fold bounds both tiers** — Replacement for spec Solution item 3: `3. **Auto wants** — A Want names Resident Nodes whose Children are desired. Ongoing Want is only Included Nodes that miss Children. There is no second ongoing tier. After those Children become Resident and, if unfolded, Included, the next Poll may Want them because they are now Included and miss Children. Reserved Nodes and Zoom ancestors stay bootstrap. Auto wants ride every post-Event and every Poll with Changes. They need no click and no command.` Replacement for story 14: `14. **Next rank after unfold** — As a person, I want the next rank under Included to be wanted only when that rank is Included, so that a folded Node does not start a Want for hidden Children.`
6. **Option B — Prefetch one rank under Fold** — Replacement for spec Solution item 3: `3. **Auto wants** — A Want names Resident Nodes whose Children are desired. Ongoing priority is (1) Included Nodes that miss Children, then (2) Resident Children of those Nodes even when Fold hides them from Included. Tier 2 prefetches one extra rank under folded Included Nodes. Auto wants ride every post-Event and every Poll with Changes.` Replacement for story 33: `33. **SiteMap honors Fold** — As a person, I want Included to honor Fold for SiteMap display, so that hidden descendants are not shown, even when auto Want prefetches one rank under a folded Included Node.`

### 2.4 Zoom restore has no default

1. **Where** — Stories [1. Open a large Server Graph](plan/browser-residency/spec.md) through [3. Framing path](plan/browser-residency/spec.md); [4. Out of Scope](plan/browser-residency/spec.md) item **12. Zoom-restore edge cases**; [02 — Lock bootstrap visible-closure set](plan/browser-residency/issues/02-lock-bootstrap-visible-closure.md) question 3.
2. **Gap** — First-paint stories assume a Zoom root. The spec puts missing or stale saved Zoom out of the spec. The map [4. Not yet specified](plan/browser-residency/map.md) is empty. No default Zoom is named (ROOT or other).
3. **Why it blocks tickets** — Boot and `/state` tickets cannot write an acceptance case for “open the Browser” when saved Zoom is absent or names a Node that is gone.
4. **Decision needed** — Lock one default. Put it in spec Solution, not in Out of Scope.
5. **Correction text** — Add as spec Solution item (new named item after Bootstrap set): `**Zoom default** — When saved Zoom is missing, stale, or names a Node that is not Resident in visible-closure, first paint uses Zoom = ROOT. Restore does not add Nodes beyond the visible-closure set in finding 2.1.` If Zoom default is not ROOT, replace `ROOT` with the chosen Node and keep the second sentence.

### 2.5 Spec Out of Scope holds in-project product locks

1. **Where** — [4. Out of Scope](plan/browser-residency/spec.md) items **11. Want field shape**, **12. Zoom-restore edge cases**, **13. Want cadence details**, **14. Death of old Load Fetch**; [5. Unsettled](plan/browser-residency/arch.md); tickets 01, 02, 03, 06 still `needs-info`.
2. **Contradiction** — [4. Out of Scope](plan/browser-residency/map.md) is the effort boundary (Parse Actor, file transit, partitions, leases, LRU, IndexedDB, Server eviction, named-Node commands, whole-Workspace bootstrap). Spec items 11–14 are open questions inside this Project. Item 12 is first-paint product (finding 2.1 and 2.4). Item 13 includes whether every Poll carries Want, which stories [19. Wants on post-Event](plan/browser-residency/spec.md), [20. Wants on Poll](plan/browser-residency/spec.md), and [27. Growth while I work](plan/browser-residency/spec.md) already state as the growth doors. Item 14 is expand-contract timing, not a product exclusion. Item 11 is wire layout and belongs in architecture, not in spec Out of Scope.
3. **Why it blocks tickets** — `/to-tickets` consumes a closed spec plus arch Sequence. Questions parked in spec Out of Scope still change first paint and silent growth. Coding tickets already exist while those questions stay `needs-info`.
4. **Correction text** — Delete spec Out of Scope items 11, 12, and 13. Replace item 14 with: `11. **Death of old Load Fetch** — This spec keeps the old Fetch path on the user-facing Load command. [06 — Dual-run vs migrate explicit Load Fetch](plan/browser-residency/issues/06-dual-run-vs-migrate-explicit-load.md) decides when that path dies. Auto wants and bootstrap do not use that path.` Move Want field names and empty-Want JSON encoding to [arch.md](plan/browser-residency/arch.md) or to [3. Decisions so far](plan/browser-residency/map.md) once locked. Put Zoom default and Want attachment into spec Solution (findings 2.4 and 3.1 Option A/B text already carry attachment: every post-Event and every Poll).

## 3. Non-blocking findings

### 3.1 Want is not in CONTEXT.md

1. **Where** — [2. Solution](plan/browser-residency/spec.md) item **3. Auto wants**; [2. Notes](plan/browser-residency/map.md) items **4. Vocabulary** and **6. Want**; [CONTEXT.md](CONTEXT.md) Additional approved terms.
2. **Gap** — Want is the named unit of silent residency. [CONTEXT.md](CONTEXT.md) does not list Want. The map vocabulary line lists Resident, Unloaded, Included, Fetch, Load, Poll, Change, and `childMap`, and omits Want. [CONTEXT.md](CONTEXT.md) says to raise a new term rather than invent a synonym.
3. **Correction text** — Raise Want for [CONTEXT.md](CONTEXT.md): `**Want**: A list of Resident Node ids whose Children are desired. The Server answers with child edges (`Graph.childMap`) and the Nodes those edges point at. Auto wants ride Poll and post-Event. Want is not the user-facing Load command and is not Fetch.` Until that lands, keep the same sentence in spec Further Notes **2. Vocabulary**.

### 3.2 Map Destination says visible Nodes

1. **Where** — [1. Destination](plan/browser-residency/map.md) “first visible Nodes that miss Children”; [project.md](plan/browser-residency/project.md) Summary; [CONTEXT.md](CONTEXT.md) Included context (_Avoid_: visible).
2. **Correction text** — On the map and the project Summary, replace `visible Nodes that miss Children` with `Included Nodes that miss Children`.

### 3.3 Map carries wanted Nodes, spec carries a Want

1. **Where** — [1. Destination](plan/browser-residency/map.md) “Post-Event and Poll carry Changes plus the Nodes the Browser wants”; [project.md](plan/browser-residency/project.md) Summary; spec stories 19–20.
2. **Correction text** — Replace that clause with: `Post-Event and Poll carry Changes plus the current Want.`

### 3.4 Solution 1 says Zoom-scoped then adds reserved Children outside Zoom

1. **Where** — [2. Solution](plan/browser-residency/spec.md) items 1–2; stories 4–7.
2. **Correction text** — In Solution item 1, replace `That set is Zoom-scoped.` with: `Included and the framing path are Zoom-scoped. Direct Children of reserved Nodes ROOT, TRASH, Workspaces Node, and SYSTEM are also first-paint even when those Children sit outside Included.` Finding 2.1 Option A already includes this. Use that Option A paragraph and drop the old “Zoom-scoped” sentence.

### 3.5 Stories that are constraints, not stories

1. **Where** — Stories [12. No new loading Status](plan/browser-residency/spec.md), [15. No third ongoing tier](plan/browser-residency/spec.md), [18. Hollow-click Load may remain](plan/browser-residency/spec.md), [31. Load may dual-run Fetch](plan/browser-residency/spec.md), [32. Commands that name Nodes later](plan/browser-residency/spec.md), [36. Reserved spelling](plan/browser-residency/spec.md).
2. **Gap** — “May remain” and “may dual-run” are not pass/fail. Spelling and “no new Status” are constraints.
3. **Correction text** — Move 12, 15, 32, and 36 into Solution as named constraints. Replace story 18 with Out of Scope: `**Hollow-click Load** — This spec does not remove a hollow-circle → Load wiring if that wiring is present. Auto wants do not use that wiring.` Replace story 31 with Solution: `**Explicit Load Fetch** — The user-facing Load command keeps the old Fetch path in this effort. Auto wants and bootstrap use edges plus Nodes.`

### 3.6 App and Browser same Want is on the map only

1. **Where** — [3. Decisions so far](plan/browser-residency/map.md) item **3. Same wants**; spec has no App story; [06 — Dual-run vs migrate explicit Load Fetch](plan/browser-residency/issues/06-dual-run-vs-migrate-explicit-load.md) question 3 still asks App versus Browser.
2. **Correction text** — Add spec Solution item: `**Same Want** — App and Browser use one Want compose and one edges-plus-Nodes package. This spec does not add a second App Want.` Add the same line under spec Further Notes. Leave Load-Fetch death for the App on [06 — Dual-run vs migrate explicit Load Fetch](plan/browser-residency/issues/06-dual-run-vs-migrate-explicit-load.md).

### 3.7 No throttle is on the map only

1. **Where** — [3. Decisions so far](plan/browser-residency/map.md) item **2. No throttle**; spec silent.
2. **Correction text** — Add spec Out of Scope item: `**Want throttle** — This spec does not batch, cap, or apply backpressure to Wants.` Or add Solution: `**No throttle** — The Browser sends the current Want list with no batching and no backpressure design.`

### 3.8 New Session start is only implied

1. **Where** — [4. Out of Scope](plan/browser-residency/spec.md) item **6. IndexedDB**; [CONTEXT.md](CONTEXT.md) Session.
2. **Correction text** — Add story: `**New Session starts small** — As a person, I want a new Session to start from visible-closure, so that refresh does not restore a prior resident snapshot.` Optional. Item 6 already forbids Browser persistence of a resident snapshot.

### 3.9 Map Decisions so far have no ticket links

1. **Where** — [3. Decisions so far](plan/browser-residency/map.md) items 1–3 versus Wayfinder index rule (gist plus link to the ticket that holds the answer).
2. **Correction text** — When those locks have a home ticket, index them as `1. [NN — title](issues/NN-….md) — gist`. Until then, they stay map-only and the spec must copy Same wants and No throttle (findings 3.6 and 3.7) so ticket writers do not miss them.

## 4. Product that is already locked

1. **Problem** — A large Server Graph must not delay first paint. A complete Workspace is also too much. Residency grows without a click and without Load. Answers are complete edges (no dangling targets). Absent `childMap` key is Unloaded. Present key, including `[]`, is Loaded.
2. **Hollow-circle Bullet** — Unloaded or Unparsed as today. No new per-Node loading Status. Auto growth does not require a click.
3. **Doors** — Auto wants ride post-Event and Poll with Changes. They are not the user-facing Load command.
4. **Find default** — Residence-only search is locked for this spec’s happy path, even while destination-level Fetch-before-navigate stays conflicted (finding 2.2).
5. **Load remains** — Upload, Parse, and explicit Fetch still have the Load command.
6. **No named-Node commands** — Not required for this destination.
7. **Reserved spelling** — SYSTEM is SYSTEM. Bootstrap reserved set names ROOT, TRASH, Workspaces Node, and SYSTEM.
8. **True Out of Scope** — Parse Actor design, file transit Actor, document partition, cache leases, LRU, IndexedDB, Server eviction, named-Node commands, whole-Workspace bootstrap. These match [5. Out of scope](plan/browser-residency/map.md) and do not block tickets.
9. **Server stays large** — Visible-closure is a Browser projection. Later Wants read the same Server Graph.

## 5. Ticket-readiness bar after corrections

1. **Close findings 2.1–2.5** — One first-paint set, one Find destination, one ongoing Want rule, one Zoom default, Out of Scope limited to effort exclusions.
2. **Copy map locks into spec** — Same Want (App and Browser), No throttle, every Poll and post-Event carries Want.
3. **Then define tickets** — From [arch.md](plan/browser-residency/arch.md) Sequence after that spec is stable. Do not treat current coding files 07–12 as proof that the spec was ready.
