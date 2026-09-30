# core-refinement architecture

Updated: 2026-09-29
Sequence: expand-contract

Home: [[project.md]]. Target architecture: [[plan/architecture/server-core.md]] (locked 2026-09-28). This note charts an incremental expand-contract path from current Core to that Server Core description. Spec.md and User Stories for a full `/to-arch` run are absent; Alan overrode that stop for expand-contract only. This note has no Story paths, Module map, or Seams. Stage stays `chart` on [[project.md]].

Decision lock: [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md). Step 1 implement (shipped): [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md). Git use is Core: [[plan/architecture/server-core.md]]. This note does not reopen that lock. This expand-contract sequence is the ticket source: tickets stay undrafted until a use case needs one; then draft against a beat or a caller shift from the sequence, not a pre-built full set.

## 1. Special-node Parse and Persist axes

Special nodes (Workspace Node, Directory Node, File Node) each carry two independent axes Core sets:

1. **Parsed | Unparsed** — Parse converts this disk object to Graph. Parse clears Unparsed by marking the node Parsed.
2. **Persisted | Unpersisted** — Persist converts this Graph to disk. A graph edit sets the nearest owning special node Unpersisted. It does not mark ancestors.

The two axes are independent markers. They are not a lock table. Do not invent a Conflicted state. This section is not the project target; the project target is [[plan/architecture/server-core.md]].

## 2. Starting point

Current Core is the [[plan/core-creation/project.md]] baseline: mailbox, Actor pool, Graph, Events, and database backend. Today `DocumentState` is `Current` | `Unparsed` | `NoServerFile`. `ParseState` and `PersistState` already exist on special nodes beside it ([20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md)). Parsed is the other pole of Unparsed (`Current` is today’s name). Load after files land still uses today’s Parse / graph-push hop ([[plan/github-transport/arch.md]] Load / git Load). The one long-lived Parse actor and the Core async Persist stack are not stood up. File Persist today goes through [[src/Server/DocumentPersistChange.fs]]. Transport still locks, receives, and informs Core; after this Project, that thin remainder stays [[plan/github-transport/project.md]].

## 3. Expand-contract sequence

Ordered path from §2 Starting point to [[plan/architecture/server-core.md]]. The expand-contract is **slow**: gradually create the Core pieces, and gradually shift use. This pace applies to the whole sequence, not only Persist. Each step expands the new form beside the old, migrates callers while both exist, then contracts the old. Do not mix tracer-cut Story paths or module-build Module map into this sequence.

1. **State axes on special nodes**
   1. **Expand** — [x] Add `ParseState` and `PersistState` on special nodes beside `DocumentState`. Shipped: [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md). Detail: §4 Step 1 locked.
   2. **Migrate** — [ ] Dual-write both new axes and old `DocumentState` wherever state changes; migrate old uses per §5 Axis-write mechanics.
   3. **Contract** — [ ] Remove `DocumentState` once no caller remains.

2. **Parse actor and stack**
   1. **Expand** — [ ] Stand up the one long-lived Parse actor and its stack beside today’s Load → Parse / graph-push hop. Product home: [[plan/parse-actor/project.md]]. Core may push reconcile targets. Old hop still runs. See [03 — One Parse actor stack](issues/03-one-parse-actor-stack.md). The background loop that pulls the parse stack and runs the old parse function cannot work except on a separate thread, so that loop is an Actor function ([[plan/architecture/server-core.md]] §2 Actors). First use case (Alan, 2026-09-29): an explicit parse command on a file (today’s `ParseFile` / `postParseFile`) — mailbox Load of a File node, push onto the stack, loop runs the old parse body. Ticket: [06 — Explicit parse command on a File (Load)](issues/06-explicit-parse-command-load-file.md).
   2. **Migrate** — [ ] After files land (git Load pull or Upload): mark Unparsed, then push onto that stack. Client Load on Directory or File marks Unparsed; File push onto the stack waits on this expand. Selection push has no special priority. See [02 — Git Load: Unparsed then Parse stack](issues/02-git-load-unparsed-then-parse-stack.md) and [05 — Selection-scoped Parse after whole-tree git Load](issues/05-selection-scoped-parse-after-whole-tree-git-load.md). Transport only informs Core; Core owns the Unparsed → push path.
   3. **Contract** — [ ] Retire today’s Load → Parse / graph-push hop once every handoff uses the stack.

3. **Core Persist stack**
   Charted approach (Alan, 2026-09-29). Persist is not an Actor. See [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md).
   1. **Expand** — [ ] Make **new collector functions** beside today’s sync live-save. A **loop** will pull from the collection and **call the existing persist functions** ([[src/Server/DocumentPersistChange.fs]] / today’s Graph→disk writers). Those functions stay the write body. Do not invent a new write body in this step.
   2. **Migrate** — [ ] **Change everyone to call the collectors.** Sync call sites move to collectors; the loop is the new feeder; the existing persist functions are what the loop calls. Do not delete those functions in this step.
   3. **Contract** — [ ] Retire the old sync call-site shape once collectors and the loop feed the existing persist functions. The write body stays.
   **New behavior** (not today’s sync live-save): Unparsed files are not open for persist. Ops like git pull also lock against persist running. The loop must honor both. Axis lock already says file Persist is blocked while Unparsed ([04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md)); the collector loop is where that block becomes real. Do not claim today’s code already does it.
   **Gate tension (leave both):** [01 — Persist/git work-tree gate](issues/01-persist-git-work-tree-gate.md) exclusive gate stays revoked; [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md) git Save is permitted while Unparsed or Unpersisted. Beside that: git pull (and similar ops) lock against persist running. Tension: git Save may proceed while nodes are Unpersisted; persist does not run during git pull.

4. **Path control**
   1. **Expand** — [ ] Core owns four operations that hold absolute residency: read file, write file, read directory, write directory. Callers pass a node, or a relative path derived from a node, to say where ([[plan/transport-layer/map.md]]).
   2. **Migrate** — [ ] Classify each inventoried DataDir use as already one of those Core functions, or change it to that protocol (caller passes a node or a relative path derived from a node; Core holds the absolute residency). Inventory snapshot: [DataDir caller inventory](reports/datadir-caller-inventory.md). Do not invent migrate batches beyond that classification.
   3. **Contract** — [ ] No caller outside Core builds or holds a DataDir absolute path. Callers pass a node or a relative path derived from a node.

5. **Mailbox owns file and git work**
   Charted approach (Alan, 2026-09-29). Three beats for git requests. File work stays where earlier steps already require it; this step does not invent a file-migrate inventory.
   1. **5.1 Enable git requests** — [ ] Core/mailbox accepts git requests (expand). Actors remain defined outside Core and post to the mailbox; Core performs that work in the background with an end Event when needed ([[plan/architecture/server-core.md]] §2). Keep today’s doors that already reach Core for Load / Save.
   2. **5.2 Call them** — [ ] Callers use those git requests (migrate).
   3. **5.3 Stop any old** — [ ] Stop the old git path (contract).

## 4. Step 1 locked

Step 1 is **State axes on special nodes only**. Markers / Graph state only. It is sequence step 1 Expand above.

Acceptance: the two axes exist on special nodes and can be set and read.

This step does not start workers.

**Approach (Mikado)** — Locked 2026-09-28 (Alan). Create the new state axes. Set both the new axes and the old `DocumentState` wherever state changes. Migrate old uses over step by step. Finally remove the old.

Implement ticket: [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md) (Status `done` on github-transport).

## 5. Axis-write mechanics

Locked 2026-09-28 (Alan). These rules say who writes each axis and which node they mark. [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md) still only adds the markers. It does not start workers. Items marked deferred wait for the Parse loop.

1. **Writer target** — Core / mailbox only. Until that lands, set the axes at today’s file-edit sites and graph-edit sites.
2. **Graph edit** — Mark the nearest owning special node (File Node, Directory Node, or Workspace Node) **Unpersisted** only. Do not mark ancestors.
3. **Discovery** — Whoever finds a disk change writes the axis. Parse is not the discovery tool. A new member or a deleted member marks the Directory Node **Unparsed**. A modified file marks the File Node **Unparsed**.
4. **git pull finish** — Notes the same discoveries. There is no separate axis path.
5. **Persist done** — Mark that node **Persisted** only.
6. **Directory Parse done** — Mark that Directory Node **Parsed** only.
7. **Create special** — A new special node starts **Unparsed** and **Persisted**.
8. **Client Load on Directory** — Mark the Directory Node **Unparsed** (re-process). Reconciliation with no extra info can spot disk members the Graph lacks.
9. **Client Load on File** — Mark the File Node **Unparsed**. Push onto the Parse stack is deferred. That push needs the Parse loop.
10. **Directory Parse body** (deferred) — Walks all nodes tied to that `.amb`, not only immediate children. Create missing File Nodes. Disk-newer marks the File Node **Unparsed** and pushes when the stack exists.
11. **Parse stack pop** (deferred) — If the node is already **Parsed**, skip. Real work arrives **Unparsed**.

## 6. Vocabulary

1. Say **event source**, not ESO.
2. Do not say CAS.
3. Do not say Peer. Say Server git Actor, git Load, or git Save.
4. Name tickets in full (for example [05 — Selection-scoped Parse after whole-tree git Load](issues/05-selection-scoped-parse-after-whole-tree-git-load.md)).
5. Git use is Core. Do not imply git is outside Core.

## 7. Related

1. **Map** — [[map.md]] (decision 6–7, 10)
2. **Axes lock** — [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md)
3. **Server Core** — [[plan/architecture/server-core.md]]
4. **Parse Actor home** — [[plan/parse-actor/project.md]]
5. **Transport remainder** — [[plan/github-transport/project.md]]
