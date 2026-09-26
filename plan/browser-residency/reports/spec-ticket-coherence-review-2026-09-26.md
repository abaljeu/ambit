# Spec-ticket coherence review 2026-09-26

Review of whether [browser-residency](plan/browser-residency/project.md) tickets realize [spec.md](plan/browser-residency/spec.md). Sources: [spec.md](plan/browser-residency/spec.md), [arch.md](plan/browser-residency/arch.md), [map.md](plan/browser-residency/map.md), [project.md](plan/browser-residency/project.md), and every file under [issues/](plan/browser-residency/issues/). This report does not read other reports. This report does not edit tickets, spec, arch, or map.

## 1. Scope

1. **Question** — Do the twelve tickets cover the spec without gaps, without ownership overlap that can fork the destination, and without a permitted outcome that violates a named story?
2. **Authority** — [3. User Stories](plan/browser-residency/spec.md) is the story list. [2. Solution](plan/browser-residency/spec.md) is the locked destination. [4. Out of Scope](plan/browser-residency/spec.md) sends fog to named grilling tickets. [arch.md](plan/browser-residency/arch.md) Sequence is expand-contract and names the migrate batches.
3. **Ticket 07** — [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md) is **Status:** `done`. This review treats that ticket as recorded expand state. It does not ask to reopen 07.

## 2. Method

1. **Dependency order** — Compare each **Blocked by** list to [doc/agents/issue-tracker.md](doc/agents/issue-tracker.md) (a ticket is unblocked when every listed ticket is `done`) and to expand-contract (migrate batches block on expand; contract blocks on every migrate batch plus the death decision).
2. **Status** — Compare each **Status:** to [doc/agents/triage-labels.md](doc/agents/triage-labels.md). `blocked` is for an external or non-ticket dependency. Linked ticket waits stay `defined`.
3. **Acceptance coverage** — Map each of the 36 stories to at least one checklist item. Flag a story that no ticket owns, or that a ticket claims but another ticket can undo.
4. **Ownership overlap** — Flag two tickets that can land different production behavior for the same story.
5. **Missing tickets** — Flag a spec story or a Solution lock with no implementation home after 07.
6. **Spec-violating outcomes** — Flag a ticket whose written options or acceptance let an implementer ship a result that contradicts a named story.

## 3. Severity

1. **Blocking** — Planning must change before migrate work can meet the spec. The coding frontier is hidden, or a migrate ticket can ship a result that contradicts a locked story.
2. **High** — A still-open grilling ticket can lock a result that contradicts a locked story, or a Server-side story has no acceptance.
3. **Medium** — Overlap or a weak checklist that does not by itself permit a violating production door.
4. **Low** — Naming or template defects that do not change outcomes.

## 4. Ticket inventory

1. **[01 — Lock Sync want + edges/Nodes package shape](plan/browser-residency/issues/01-lock-sync-want-package-shape.md)** — Type `grilling`. Status `needs-info`. Blocked by None. Spec [4. Out of Scope](plan/browser-residency/spec.md) item 11. Want field shape.
2. **[02 — Lock bootstrap visible-closure set](plan/browser-residency/issues/02-lock-bootstrap-visible-closure.md)** — Type `grilling`. Status `needs-info`. Blocked by None. Spec [4. Out of Scope](plan/browser-residency/spec.md) item 12. Zoom-restore edge cases. The ticket also re-asks reserved Children and Zoom ancestors.
3. **[03 — Lock ongoing want priority and when wants are attached](plan/browser-residency/issues/03-lock-ongoing-want-priority.md)** — Type `grilling`. Status `needs-info`. Blocked by None. Spec [4. Out of Scope](plan/browser-residency/spec.md) item 13. Want cadence details. Priority in the ticket body is already locked.
4. **[04 — Retire selective hollow-click and resident-only Find assumptions](plan/browser-residency/issues/04-retire-selective-hollow-click-find.md)** — Type `task`. Status `defined`. Blocked by None. Spec [5. Further Notes](plan/browser-residency/spec.md) item 4. Selective leftovers.
5. **[05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md)** — Type `grilling`. Status `needs-info`. Blocked by 01. Spec [4. Out of Scope](plan/browser-residency/spec.md) item 10. Server-mode Find. Design only.
6. **[06 — Dual-run vs migrate explicit Load Fetch](plan/browser-residency/issues/06-dual-run-vs-migrate-explicit-load.md)** — Type `grilling`. Status `needs-info`. Blocked by 01. Spec [4. Out of Scope](plan/browser-residency/spec.md) item 14. Death of old Load Fetch.
7. **[07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md)** — Type `coding`. Status `done`. Expand batch. Recorded: Want.compose, installWantAnswer, visibleClosureGraph beside old rootBootstrapGraph, additive `want` / `nodes` / `childMap`, ApiVersion 13.
8. **[08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md)** — Type `coding`. Status `blocked`. Blocked by 07 (done). Shared codecs and SyncLogic install.
9. **[09 — Migrate Server Sync doors](plan/browser-residency/issues/09-migrate-server-sync-doors.md)** — Type `coding`. Status `blocked`. Blocked by 07 (done). getPoll, postEvents, getState, reserved bootstrap set.
10. **[10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md)** — Type `coding`. Status `blocked`. Blocked by 07 (done). Attach Want, boot first paint, Find stays residence-only, Load dual-run stays.
11. **[11 — Migrate Bullet, Included, and bootstrap wants](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md)** — Type `coding`. Status `blocked`. Blocked by 07 (done). Bullet, Included walk, production bootstrap switch.
12. **[12 — Contract old Load Fetch packages](plan/browser-residency/issues/12-contract-old-load-fetch-packages.md)** — Type `coding`. Status `blocked`. Blocked by 06, 08, 09, 10, 11. Contract batch. Decision home is 06.

## 5. Confirmed findings

### 5.1 Migrate Status still blocked after expand is done

1. **Tickets** — [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md), [09 — Migrate Server Sync doors](plan/browser-residency/issues/09-migrate-server-sync-doors.md), [10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md), [11 — Migrate Bullet, Included, and bootstrap wants](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md).
2. **Spec story** — None as a story gap. This finding is Status versus [doc/agents/triage-labels.md](doc/agents/triage-labels.md) and the implement frontier in [doc/agents/issue-tracker.md](doc/agents/issue-tracker.md).
3. **Severity** — Blocking.
4. **What is wrong** — Each of 08–11 lists only [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md) in **Blocked by**, and 07 is `done`. The Blocked-by list is clear. Status stays `blocked`. `blocked` is for an external or non-ticket wait. The coding frontier is unblocked `defined` tickets. After this Status, the only `defined` unblocked ticket is [04 — Retire selective hollow-click and resident-only Find assumptions](plan/browser-residency/issues/04-retire-selective-hollow-click-find.md) (Type `task`). An implement pass that scans Status will not start 08–11.
5. **Smallest planning correction** — Set **Status:** `defined` on 08, 09, 10, and 11. Keep **Blocked by:** 07. Do not add Status `blocked` for a ticket-to-ticket wait.

### 5.2 Open wire-shape ticket can contradict done expand

1. **Tickets** — [01 — Lock Sync want + edges/Nodes package shape](plan/browser-residency/issues/01-lock-sync-want-package-shape.md), [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md), [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md).
2. **Spec story** — [19. Wants on post-Event](plan/browser-residency/spec.md), [20. Wants on Poll](plan/browser-residency/spec.md), [21. Edges in the answer](plan/browser-residency/spec.md), [22. Nodes in the answer](plan/browser-residency/spec.md). Spec [4. Out of Scope](plan/browser-residency/spec.md) item 11. Want field shape names 01 as the lock home and does not lock field names in the spec.
3. **Severity** — Blocking.
4. **What is wrong** — 07 is `done` with a recorded grill: request field `want` as a NodeId list; always send `want`; empty is `[]`; answer fields `nodes` plus `childMap`; `ApiVersion.current` is 13. 01 is still `needs-info` and still asks those same questions, including “Do not pick the integer yet.” 08 item **2. Empty Want allowed** says encoding follows 01, not the recorded 07 lock. If 01 later picks a different field, a different emptiness rule, or a different door, migrate work that follows 01 will fight the done expand, or will retrofit 07. This review does not reopen 07. The defect is that 01 is still an open lock.
5. **Smallest planning correction** — On 01, write **Answer** from the 07 Context lock (`want`, always send, `[]` when empty, `nodes` plus `childMap`, ApiVersion 13). Set 01 to `done` after that Answer is accepted, or to `defined` only if a human must accept the Answer. Change 08 item **2. Empty Want allowed** to follow that Answer (or the 07 Context), not an open 01. Do not change 07 code or Status.

### 5.3 Included first paint has no complete owner

1. **Tickets** — [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md) (recorded, no Included item), [10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md), [11 — Migrate Bullet, Included, and bootstrap wants](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md), [02 — Lock bootstrap visible-closure set](plan/browser-residency/issues/02-lock-bootstrap-visible-closure.md).
2. **Spec story** — [2. Included first paint](plan/browser-residency/spec.md): “As a person, I want first paint to show Included context under Zoom, honoring Fold, so that the SiteMap I look at is Resident.” Also [2. Solution](plan/browser-residency/spec.md) item 2. Bootstrap set: first paint includes reserved Children, plus ancestors of the Zoom root, plus Included.
3. **Severity** — Blocking.
4. **What is wrong** — [arch.md](plan/browser-residency/arch.md) Module **3. ResidentProjection** records `visibleClosureGraph` as reserved-plus-ancestors and “Not Included”, and points at 02 and 11. 07 is done and does not claim story 2. 11 story-path list includes 2, but the 11 checklist has no item that adds Included Node headers to the first-paint Graph. 10 item **2. Included and framing path** claims stories 2.1 and 3.1. 10 item **3. Boot Poll may Want** lets boot attach a Want after that Graph exists. An implementer can treat Included as a first Want, not as first-paint residency. Included under Zoom is descendant context, not the ancestor path. A reserved-plus-ancestors Graph does not make Included Resident at first paint. That outcome violates story 2.
5. **Smallest planning correction** — Add one named acceptance item on 11 (the arch pointer): first-paint Graph includes Included Node headers under Zoom, honoring Fold, in the same visible-closure package as reserved Children and Zoom ancestors. On 10, keep item 2 as a boot consume check, and add that boot Poll Want must not be the path that first makes Included Resident. Do not reopen 07 to add Included.

### 5.4 Server getState can keep complete-Workspace bootstrap

1. **Tickets** — [09 — Migrate Server Sync doors](plan/browser-residency/issues/09-migrate-server-sync-doors.md), [11 — Migrate Bullet, Included, and bootstrap wants](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md).
2. **Spec story** — [8. Not a complete Workspace](plan/browser-residency/spec.md): “As a person, I want first paint to stop at that visible-closure set, so that I do not download a complete Workspace I am not looking at.” Also [1. Open a large Server Graph](plan/browser-residency/spec.md).
3. **Severity** — Blocking.
4. **What is wrong** — 09 and 11 are parallel. Both wait only on done 07. 09 item **3. State is visible-closure** says `getState` uses `bootstrapGraph`, not the whole Server Graph. [arch.md](plan/browser-residency/arch.md) Module **3. ResidentProjection** says production `bootstrapGraph` still uses the old ROOT path, and `visibleClosureGraph` sits beside it. 11 item **1. Production bootstrap** and item **2. Not a complete Workspace** are the switch off `rootBootstrapGraph`. If 09 lands first and calls production `bootstrapGraph`, `/state` can still send a complete Workspace. That meets a narrow reading of story 1 (not the whole Server Graph) and violates story 8. 09 section **2. Reserved bootstrap set** cannot be true on that door until 11 switches the function. Two tickets own first-paint scope on the same Server door.
5. **Smallest planning correction** — On 09 item **3. State is visible-closure**, name `visibleClosureGraph` (the 07 expand door), not production `bootstrapGraph`. Leave 11 as the switch of leftover `bootstrapGraph` / `rootBootstrapGraph` production callers. Do not make 09 wait on 11.

### 5.5 Bootstrap grilling can unlock a locked reserved set

1. **Tickets** — [02 — Lock bootstrap visible-closure set](plan/browser-residency/issues/02-lock-bootstrap-visible-closure.md), [09 — Migrate Server Sync doors](plan/browser-residency/issues/09-migrate-server-sync-doors.md).
2. **Spec story** — [4. ROOT Children](plan/browser-residency/spec.md), [5. TRASH Children](plan/browser-residency/spec.md), [6. Workspaces Node Children](plan/browser-residency/spec.md), [7. SYSTEM Children](plan/browser-residency/spec.md), [3. Framing path](plan/browser-residency/spec.md). Spec [4. Out of Scope](plan/browser-residency/spec.md) item 12. Zoom-restore edge cases is the only bootstrap fog the spec still sends to 02.
3. **Severity** — High.
4. **What is wrong** — 02 question 1 asks whether first paint always includes every direct Child of ROOT, TRASH, Workspaces Node, and SYSTEM, even outside Included. Stories 4–7 already lock that set. 02 question 2 asks which Zoom ancestors must be Resident and Loaded. Story 3 already locks ancestors Resident at first paint. 09 is not blocked by 02 and will implement that reserved set. If 02 later answers “no” to question 1, the lock contradicts stories 4–7 and contradicts 09.
5. **Smallest planning correction** — Narrow 02 to Zoom restore only (missing, stale, or outside-set Zoom). Mark questions 1 and 2 as already locked by stories 3–7. Do not add 02 to 09 **Blocked by**.

### 5.6 Cadence grilling can drop Want from Poll or post-Event

1. **Tickets** — [03 — Lock ongoing want priority and when wants are attached](plan/browser-residency/issues/03-lock-ongoing-want-priority.md), [10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md).
2. **Spec story** — [19. Wants on post-Event](plan/browser-residency/spec.md): “As the Browser, I want post-Event to carry Changes plus the current Want.” [20. Wants on Poll](plan/browser-residency/spec.md): “As the Browser, I want Poll to carry Changes plus the current Want.” [27. Growth while I work](plan/browser-residency/spec.md): auto wants continue across later Poll and post-Event. Spec [4. Out of Scope](plan/browser-residency/spec.md) item 13. Want cadence details names 03.
3. **Severity** — High.
4. **What is wrong** — 03 question 1 still allows “only some of those doors” to carry Want. That answer would violate stories 19, 20, and 27. 10 is not blocked by 03 and already writes “Each Poll and each post-Event carries the current Want.” Empty-Want encoding is already recorded on done 07 as always send `[]`. [map.md](plan/browser-residency/map.md) Decisions so far item 2. No throttle already rejects batching and backpressure. 03 still reads as an open lock that can override the spec and 10.
5. **Smallest planning correction** — Narrow 03 to whatever cadence fog remains after stories 19–20 and 27 (for example, whether a Poll repeats a Want the Server already answered). Record that every Poll and every post-Event carries the current Want, including `[]`. Do not add 03 to 10 **Blocked by**.

### 5.7 Server Want answer does not accept no dangling edges

1. **Tickets** — [09 — Migrate Server Sync doors](plan/browser-residency/issues/09-migrate-server-sync-doors.md), [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md) (Client refuse only).
2. **Spec story** — [23. No dangling edges](plan/browser-residency/spec.md): “As the Browser, I want no child edge whose target Node is absent, so that the resident projection never invents a header-less Child.” [2. Solution](plan/browser-residency/spec.md) item 4. Edges plus Nodes: “The Server answers a Want with child edges (`Graph.childMap`) and, separately, the Nodes those edges point at. No dangling edges.”
3. **Severity** — High.
4. **What is wrong** — 07 installWantAnswer refuses a dangling edge. That is a Browser install guard. Story 23 and Solution item 4 bind the Server answer. 09 items **1. Poll answers Want** and **2. post-Event answers Want** say Changes plus edges plus Nodes. They do not say the Server must not emit an edge whose target is missing from the Node collection. A 09 door that sends an edge without its Node meets the 09 checklist and violates story 23. Browser refuse then leaves that parent Unloaded.
5. **Smallest planning correction** — Add one named acceptance item on 09: each Want answer includes every target Node for every emitted edge; the Server does not emit a dangling edge.

### 5.8 Ticket 10 claims install and first-paint work that other migrate tickets own

1. **Tickets** — [10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md), [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md), [09 — Migrate Server Sync doors](plan/browser-residency/issues/09-migrate-server-sync-doors.md).
2. **Spec story** — [9. Same package after bootstrap](plan/browser-residency/spec.md), [1. Open a large Server Graph](plan/browser-residency/spec.md), [19. Wants on post-Event](plan/browser-residency/spec.md), [20. Wants on Poll](plan/browser-residency/spec.md).
3. **Severity** — Medium.
4. **What is wrong** — Expand-contract allows 08, 09, and 10 in parallel after 07. 10 **What to build** says the Browser installs the Want answer after Changes, and first paint uses the visible-closure Graph. Install-after-Events is 08 SyncLogic. Visible-closure `/state` is 09. 10 can attach `want` on the request without those doors. The 10 product claims are not verifiable from 10 alone. Two implementers can both write install or both skip it.
5. **Smallest planning correction** — Shrink 10 to: send current Want on Poll and post-Event; recompute from Included; no click; no Load command; Find stays residence-only; Load `/load` `packages` stay. Leave install-after-Events on 08. Leave getState visible-closure on 09. Keep 10 Boot as consume of that Graph, not as the Server projection.

### 5.9 Shared encode checklist on 08 duplicates recorded 07 wire

1. **Tickets** — [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md), [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md).
2. **Spec story** — [9. Same package after bootstrap](plan/browser-residency/spec.md) (one install door after Events). Wire field names are [4. Out of Scope](plan/browser-residency/spec.md) item 11, recorded on 07.
3. **Severity** — Medium.
4. **What is wrong** — 07 section **3. Sync wire** is checked: additive `nodes` and `childMap` on `ChangeSuccessResponse`; request `want` on `PollRequest` / `ChangeRequest`. [arch.md](plan/browser-residency/arch.md) Module **5. Sync wire** Interface item 1 is already `[x]` encode and decode Want without dropping Changes. 08 section **1. Sync wire** still asks to encode Want on Poll and post-Event. The remaining 08 work that 07 does not claim is SyncLogic install after the Event tail. Duplicate encode work can fork the recorded field lock.
5. **Smallest planning correction** — Drop or mark complete 08 section **1. Sync wire**. Keep section **2. SyncLogic** (install after Events, dual-run `packages`, outcome stamps). Do not reopen 07.

### 5.10 Contract ticket uses Status blocked for ticket waits

1. **Tickets** — [12 — Contract old Load Fetch packages](plan/browser-residency/issues/12-contract-old-load-fetch-packages.md).
2. **Spec story** — [31. Load may dual-run Fetch](plan/browser-residency/spec.md). Spec [4. Out of Scope](plan/browser-residency/spec.md) item 14. Death of old Load Fetch.
3. **Severity** — Medium.
4. **What is wrong** — 12 correctly waits on [06 — Dual-run vs migrate explicit Load Fetch](plan/browser-residency/issues/06-dual-run-vs-migrate-explicit-load.md) and on migrate batches 08–11. Status `blocked` is still the wrong token for those ticket waits. The **Blocked by** list already holds the order. This does not hide current migrate work the way 5.1 does, because 12 must wait on 06.
5. **Smallest planning correction** — Set 12 **Status:** `defined`. Keep the same **Blocked by** list.

### 5.11 Blocked-by on 05 and 06 omits the ticket name

1. **Tickets** — [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md), [06 — Dual-run vs migrate explicit Load Fetch](plan/browser-residency/issues/06-dual-run-vs-migrate-explicit-load.md).
2. **Spec story** — None. This is [refer-by-name](.agents/rules/refer-by-name.md) on the Blocked-by line (`Blocked by: 01`).
3. **Severity** — Low.
4. **What is wrong** — The line cites only the number 01. Other tickets wrap the name in the link, as in 08 **Blocked by** [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md).
5. **Smallest planning correction** — Write **Blocked by:** [01 — Lock Sync want + edges/Nodes package shape](plan/browser-residency/issues/01-lock-sync-want-package-shape.md) on 05 and on 06.

## 6. Questions

### 6.1 Framing path Loaded versus Resident

1. **Tickets** — [02 — Lock bootstrap visible-closure set](plan/browser-residency/issues/02-lock-bootstrap-visible-closure.md) question 2, [10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md) item **2. Included and framing path**.
2. **Spec story** — [3. Framing path](plan/browser-residency/spec.md): ancestors of the Zoom root are Resident at first paint. The story does not say Loaded.
3. **Why it is a question** — A Resident ancestor with an absent `childMap` key is Unloaded. The framing path may not walk. 02 asks which ancestors must be Loaded. 10 asks only Resident. This review does not invent a Loaded rule. If the destination needs Loaded edges along the ancestor chain, add that sentence to 02’s narrowed Zoom-restore ticket or to 11 bootstrap acceptance. Until then, 10 matches the story’s Resident wording.

### 6.2 Does 06 need to wait on 01?

1. **Tickets** — [06 — Dual-run vs migrate explicit Load Fetch](plan/browser-residency/issues/06-dual-run-vs-migrate-explicit-load.md) **Blocked by:** 01.
2. **Spec story** — [31. Load may dual-run Fetch](plan/browser-residency/spec.md). Spec [4. Out of Scope](plan/browser-residency/spec.md) item 14.
3. **Why it is a question** — Dual-run versus death of `packages` does not need the Want field names. After 5.2 records 01’s Answer from 07, the wait is harmless. If 01 stays open, 06 stays `needs-info` behind it for no spec reason. Smallest optional cut: set 06 **Blocked by:** None, or blocked by 07’s recorded lock only.

### 6.3 Repeat Want on a tight Poll loop

1. **Tickets** — [03 — Lock ongoing want priority and when wants are attached](plan/browser-residency/issues/03-lock-ongoing-want-priority.md) question 3.
2. **Spec story** — [27. Growth while I work](plan/browser-residency/spec.md). [map.md](plan/browser-residency/map.md) Decisions so far item 2. No throttle.
3. **Why it is a question** — 03 still asks what stops a tight Poll loop from repeating a Want the Server already answered. Map No throttle says there is no batching or backpressure. No coding ticket owns a de-dupe rule. That is coherent with No throttle if 03 is narrowed (finding 5.6). Confirm that repeated Want is allowed, and do not add a throttle ticket.

### 6.4 Arch Sources versus map Decisions so far

1. **Tickets** — None. [arch.md](plan/browser-residency/arch.md) Sources line says map Decisions so far is empty. [map.md](plan/browser-residency/map.md) section **3. Decisions so far** lists ApiVersion bump, No throttle, and Same wants. 07 cites those map decisions.
2. **Spec story** — None as a story gap. Spec [5. Further Notes](plan/browser-residency/spec.md) item 1. Map: live decisions stay on the map.
3. **Why it is a question** — This is an arch/map stale line, not a ticket defect. It does not by itself change 08–12. A later arch pass can drop “Decisions so far empty.” This review does not edit arch.

## 7. Non-findings

### 7.1 Done expand matches the expand-contract first batch

1. **Tickets** — [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md).
2. **Spec stories covered as recorded expand** — [13. Auto want Included](plan/browser-residency/spec.md) compose, [14. Auto want those Children](plan/browser-residency/spec.md) compose, [15. No third ongoing tier](plan/browser-residency/spec.md), [33. SiteMap honors Fold](plan/browser-residency/spec.md) on compose, [21. Edges in the answer](plan/browser-residency/spec.md) install, [22. Nodes in the answer](plan/browser-residency/spec.md) install, [23. No dangling edges](plan/browser-residency/spec.md) install refuse, [24. Absent key stays Unloaded](plan/browser-residency/spec.md) install, [25. Present key is Loaded](plan/browser-residency/spec.md) install, [8. Not a complete Workspace](plan/browser-residency/spec.md) add-beside-old, [9. Same package after bootstrap](plan/browser-residency/spec.md) package shape. Map Decisions so far: No throttle, Same wants, ApiVersion bump.
3. **Why it is not a finding** — 07 Status `done` matches that expand. Production doors stay on later tickets 08–11, as the 07 Comments say. This review does not reopen 07.

### 7.2 Expand then four migrate batches then contract matches Sequence

1. **Tickets** — 07 expand; 08 Shared wire; 09 Server doors; 10 Browser Poll, post-Event, and Boot; 11 Bullet, Included, and bootstrap; 12 contract.
2. **Spec story** — Destination as a whole. [arch.md](plan/browser-residency/arch.md) Sequence: expand-contract.
3. **Why it is not a finding** — The named set matches expand / migrate / contract. Parallel **Blocked by:** 07 on 08–11 is the expand-contract migrate shape. 12 waits on 06 plus every migrate batch. Do not insert a linear 08→09→10→11 chain for Sequence reasons (finding 5.8 shrinks 10 instead).

### 7.3 Server-mode Find stays later design

1. **Tickets** — [05 — Chart server-mode Find](plan/browser-residency/issues/05-chart-server-mode-find.md), [10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md) section **4. Find**.
2. **Spec stories** — [28. Find in residence](plan/browser-residency/spec.md), [29. Find commit stays Zoom](plan/browser-residency/spec.md). Spec [4. Out of Scope](plan/browser-residency/spec.md) item 10. Server-mode Find.
3. **Why it is not a finding** — 05 is Type `grilling`, Status `needs-info`, and says it does not implement Fetch-before-navigate. 10 forbids Want or Fetch from Find and keeps commit on Zoom. Default Find stays resident-only. 05 as a later-design ticket is what the spec asked for. It is not a missing implementation ticket for this destination.

### 7.4 Explicit Load dual-run and contract are sequenced on 06

1. **Tickets** — [06 — Dual-run vs migrate explicit Load Fetch](plan/browser-residency/issues/06-dual-run-vs-migrate-explicit-load.md), [10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md) item **2. Load dual-run stays**, [12 — Contract old Load Fetch packages](plan/browser-residency/issues/12-contract-old-load-fetch-packages.md).
2. **Spec stories** — [30. Load command still there](plan/browser-residency/spec.md), [31. Load may dual-run Fetch](plan/browser-residency/spec.md). Spec [4. Out of Scope](plan/browser-residency/spec.md) item 14.
3. **Why it is not a finding** — 10 keeps `/load` `packages` during migrate. 12 removes `packages` only after 06 locks the cut. If 06 keeps dual-run, 12 stays unstarted or becomes `cancelled`. Charting 12 now is expand-contract, not a spec violation. 12 does not permit death of the Load command (item **2. Load command remains**).

### 7.5 Hollow-click Load may remain is consistent

1. **Tickets** — [04 — Retire selective hollow-click and resident-only Find assumptions](plan/browser-residency/issues/04-retire-selective-hollow-click-find.md), [10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md) item **3. Hollow-click Load may remain**, [12 — Contract old Load Fetch packages](plan/browser-residency/issues/12-contract-old-load-fetch-packages.md) item **3. Hollow-click Load may remain**.
2. **Spec stories** — [16. No click](plan/browser-residency/spec.md), [17. No command](plan/browser-residency/spec.md), [18. Hollow-click Load may remain](plan/browser-residency/spec.md).
3. **Why it is not a finding** — 04 cancels the old ticket that required hollow-click Load, and says do not un-wire it if present. 10 and 12 repeat that. Auto wants do not use that wiring. That matches Solution item 5. Hollow-circle Bullet.

### 7.6 No-click and no-command auto wants have an owner

1. **Tickets** — [10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md) items **4. No click**, **5. No command**, **1. Poll carries Want**, **2. post-Event carries Want**.
2. **Spec stories** — [16. No click](plan/browser-residency/spec.md), [17. No command](plan/browser-residency/spec.md), [19. Wants on post-Event](plan/browser-residency/spec.md), [20. Wants on Poll](plan/browser-residency/spec.md), [32. Commands that name Nodes later](plan/browser-residency/spec.md) also on 11 item **3. No new named-Node command**.
3. **Why it is not a finding** — Silent growth has a coding home on 10. 11’s do-not-add-command item does not conflict.

### 7.7 Bullet, Unloaded, Unparsed, and no new loading Status have an owner

1. **Tickets** — [11 — Migrate Bullet, Included, and bootstrap wants](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md) section **1. Bullet**.
2. **Spec stories** — [10. Hollow Unloaded Bullet](plan/browser-residency/spec.md), [11. Hollow Unparsed Bullet](plan/browser-residency/spec.md), [12. No new loading Status](plan/browser-residency/spec.md), [26. Children arrive on the Bullet](plan/browser-residency/spec.md), [34. Unloaded is not empty](plan/browser-residency/spec.md), [24. Absent key stays Unloaded](plan/browser-residency/spec.md) indicator, [25. Present key is Loaded](plan/browser-residency/spec.md) indicator.
3. **Why it is not a finding** — One migrate batch owns the glyph and the absent-key reading. 07 already owns install semantics for absent versus present keys. Split is expand versus reader migrate.

### 7.8 Fold bounds Included on compose and on expand

1. **Tickets** — [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md) item **4. Honor Fold**, [11 — Migrate Bullet, Included, and bootstrap wants](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md) section **2. Included**.
2. **Spec story** — [33. SiteMap honors Fold](plan/browser-residency/spec.md).
3. **Why it is not a finding** — 07 compose uses Included, not a deep unfold. 11 walks `childMap` and stops at folded children. Complementary, not a fork.

### 7.9 Reserved spelling and Server stays large have an owner

1. **Tickets** — [09 — Migrate Server Sync doors](plan/browser-residency/issues/09-migrate-server-sync-doors.md) items **5. Load packages remain**, **2. SYSTEM spelling**, **4. Server stays large**, section **2. Reserved bootstrap set**.
2. **Spec stories** — [4. ROOT Children](plan/browser-residency/spec.md), [5. TRASH Children](plan/browser-residency/spec.md), [6. Workspaces Node Children](plan/browser-residency/spec.md), [7. SYSTEM Children](plan/browser-residency/spec.md), [35. Server stays large](plan/browser-residency/spec.md), [36. Reserved spelling](plan/browser-residency/spec.md).
3. **Why it is not a finding** — After finding 5.4 names `visibleClosureGraph` on getState, this reserved set is the right 09 work. SYSTEM spelling is on 09. Core Graph stays complete. No extra ticket is required.

### 7.10 Spec out-of-scope product slices have no extra tickets

1. **Tickets** — None, by design.
2. **Spec** — [4. Out of Scope](plan/browser-residency/spec.md) items 1–9: Parse Actor design, File transit Actor, Document partition, Cache leases, LRU, IndexedDB, Server eviction, Commands that name Nodes, Whole-Workspace bootstrap.
3. **Why it is not a finding** — No ticket asks to build those slices. 04 points file-transit leftovers at [transport-layer](plan/transport-layer/project.md). That matches the spec pointers.

### 7.11 Selective leftover retirement is ticketed

1. **Tickets** — [04 — Retire selective hollow-click and resident-only Find assumptions](plan/browser-residency/issues/04-retire-selective-hollow-click-find.md).
2. **Spec** — [5. Further Notes](plan/browser-residency/spec.md) item 4. Selective leftovers.
3. **Why it is not a finding** — 04 is `defined`, unblocked, Type `task`. It lists the required cancel and retarget pointers. It does not implement residency. It can run beside migrate work.

### 7.12 Same wants for App and Browser is recorded

1. **Tickets** — [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md) Shared.Tests item **2. App and Browser same wants**, [10 — Migrate Browser Poll, post-Event, and Boot](plan/browser-residency/issues/10-migrate-browser-poll-post-event-and-boot.md) item **6. Same wants**.
2. **Spec story** — No separate story. [map.md](plan/browser-residency/map.md) Decisions so far item 3. Same wants.
3. **Why it is not a finding** — One compose function. 10 attaches that same Want. No second App Want door.

## 8. Story coverage

Each row is one [3. User Stories](plan/browser-residency/spec.md) item. Owner is the ticket that must make the story true in production. 07 is recorded expand only.

1. **Open a large Server Graph** — 09 getState projection; 10 boot consumes it. Gap: finding 5.4 if 09 calls old `bootstrapGraph`.
2. **Included first paint** — Claimed by 10; arch points at 11; 07 excludes Included. Gap: finding 5.3.
3. **Framing path** — 10 item 2; 09 reserved-plus-ancestors on the Server Graph. Question: 6.1 Loaded versus Resident.
4. **ROOT Children** — 09 reserved bootstrap set. Covered after finding 5.4.
5. **TRASH Children** — 09. Same.
6. **Workspaces Node Children** — 09. Same.
7. **SYSTEM Children** — 09. Same.
8. **Not a complete Workspace** — 07 add-beside-old (done); 11 production switch. Gap: finding 5.4 on 09 getState.
9. **Same package after bootstrap** — 07 shape (done); 08 SyncLogic one install door.
10. **Hollow Unloaded Bullet** — 11 Bullet.
11. **Hollow Unparsed Bullet** — 11 Bullet.
12. **No new loading Status** — 11 Bullet.
13. **Auto want Included** — 07 compose (done); 10 attach.
14. **Auto want those Children** — 07 compose (done); 10 attach; 09 Server answer.
15. **No third ongoing tier** — 07 (done).
16. **No click** — 10.
17. **No command** — 10.
18. **Hollow-click Load may remain** — 04, 10, 12. Non-finding 7.5.
19. **Wants on post-Event** — 08 codecs/install, 09 Server, 10 send. Gap: findings 5.2 and 5.6 if 01 or 03 override.
20. **Wants on Poll** — Same as 19.
21. **Edges in the answer** — 07 install (done); 09 Server build.
22. **Nodes in the answer** — 07 install (done); 09 Server build.
23. **No dangling edges** — 07 Client refuse (done); 09 Server acceptance missing. Gap: finding 5.7.
24. **Absent key stays Unloaded** — 07 install (done); 11 indicator.
25. **Present key is Loaded** — 07 install (done); 11 indicator.
26. **Children arrive on the Bullet** — 11.
27. **Growth while I work** — 10 recompute Want. Cadence risk: finding 5.6.
28. **Find in residence** — 10 Find. Non-finding 7.3.
29. **Find commit stays Zoom** — 10 Find.
30. **Load command still there** — 10 and 12.
31. **Load may dual-run Fetch** — 10 keeps `packages`; 06 decides death; 12 contracts. Non-finding 7.4.
32. **Commands that name Nodes later** — 11 do-not-add; 10 adds no command.
33. **SiteMap honors Fold** — 07 compose (done); 11 Included walk.
34. **Unloaded is not empty** — 11.
35. **Server stays large** — 09.
36. **Reserved spelling** — 09.

## 9. Dependency and Status summary

1. **Correct order** — 07 expand before 08–11 migrate is the right edge. 12 after 06 and after 08–11 is the right contract edge. 05 after 01 is acceptable for later Find design. 04 unblocked is correct.
2. **Wrong Status** — 08–11 remain `blocked` after 07 is `done` (finding 5.1). 12 uses `blocked` for ticket waits (finding 5.10).
3. **Open grilling in front of recorded locks** — 01, 02, and 03 are `needs-info` with Blocked by None, while 07 already shipped wire shape and compose rules, and while 09–11 will ship bootstrap and Want attachment (findings 5.2, 5.5, 5.6).
4. **Frontier today** — Grilling frontier: 01, 02, 03. Task frontier: 04. Coding frontier after finding 5.1: 08, 09, 10, 11. 05 and 06 wait on 01. 12 waits on 06 and 08–11.

## 10. Recommended planning corrections in order

1. **Status** — Set 08–11 to `defined` (finding 5.1). Set 12 to `defined` and keep its Blocked-by list (finding 5.10).
2. **Close the shipped wire lock** — Answer 01 from 07 Context; point 08 empty-Want at that Answer (finding 5.2).
3. **Name Included on first paint** — One 11 acceptance item plus a 10 constraint that boot Poll Want is not first paint for Included (finding 5.3).
4. **Point 09 getState at visibleClosureGraph** — Do not call leftover production `bootstrapGraph` (finding 5.4).
5. **Narrow 02 and 03** — 02 to Zoom restore only (finding 5.5). 03 so it cannot drop Want from Poll or post-Event (finding 5.6).
6. **Add Server no-dangling-edges on 09** — Finding 5.7.
7. **Shrink 10 product claims; drop duplicate 08 encode** — Findings 5.8 and 5.9.
8. **Name 01 on 05 and 06 Blocked-by** — Finding 5.11.

Do not reopen [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md). Do not add a new implementation ticket until 5.3 is tried as one acceptance line on 11.

## 11. Outcome

Blocking findings exist: 5.1, 5.2, 5.3, and 5.4. High findings exist: 5.5, 5.6, and 5.7. The tickets can realize the spec after those planning corrections. They do not realize it coherently in the current text.
