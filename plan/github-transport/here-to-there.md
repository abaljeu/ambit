# Here→There — step 1 locked

Updated: 2026-09-28

This note records the Here→There migration lock Alan made on 2026-09-28. Step 1 is locked. Later steps stay deferred. This is Graph markers only.

Home: [[project.md]]. Decision lock: [19 — Parsed/Unparsed and Persisted/Unpersisted](issues/19-file-newer-graph-newer.md). Implement: [20 — State axes on special nodes](issues/20-state-axes-on-special-nodes.md). Git use is Core: [[plan/architecture/server-core.md]]. This note does not reopen that lock.

## 1. There

Special nodes (Workspace Node, Directory Node, File Node) each carry two independent axes Core sets:

1. **Parsed | Unparsed** — Parse converts this disk object to Graph. Parse clears Unparsed by marking the node Parsed.
2. **Persisted | Unpersisted** — Persist converts this Graph to disk. A graph edit sets the nearest owning special node Unpersisted. It does not mark ancestors.

The two axes are independent markers. They are not a lock table. Do not invent a Conflicted state.

## 2. Here

Today `DocumentState` is `Current` | `Unparsed` | `NoServerFile`. Parsed is the other pole of Unparsed (`Current` is today's name). Unpersisted is not present. Load still uses today's hops. The Parse actor and the Core Persist stack are not stood up.

## 3. Step 1 locked

Step 1 is **State axes on special nodes only**. Markers / Graph state only.

Acceptance: the two axes exist on special nodes and can be set and read.

This step does not start workers.

**Approach (Mikado)** — Locked 2026-09-28 (Alan). Create the new state axes. Set both the new axes and the old `DocumentState` wherever state changes. Migrate old uses over step by step. Finally remove the old.

Implement ticket: [20 — State axes on special nodes](issues/20-state-axes-on-special-nodes.md).

## 4. Axis-write mechanics

Locked 2026-09-28 (Alan). These rules say who writes each axis and which node they mark. [20 — State axes on special nodes](issues/20-state-axes-on-special-nodes.md) still only adds the markers. It does not start workers. Items marked deferred wait for the Parse loop.

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

## 5. Later steps deferred

These steps stay deferred. Do not ticket them on this chart. Do not start them from [20 — State axes on special nodes](issues/20-state-axes-on-special-nodes.md).

1. **Parse actor and stack** — Standing up the Parse actor and its stack stays on [[plan/parse-actor/project.md]]. See [18 — One Parse actor stack](issues/18-parse-actor-stack-and-file-lock-ownership.md).
2. **Core Persist stack** — The async Persist stack on Core is not this step. Persist is not an Actor. See [19 — Parsed/Unparsed and Persisted/Unpersisted](issues/19-file-newer-graph-newer.md).
3. **Retarget git Load** — Git Load Unparsed → pull → push onto Parse stays deferred. See [17 — Git Load: Unparsed then Parse stack](issues/17-post-pull-cascade-and-gate-handoff.md). Git use is already Core.
4. **Upload and selection Parse** — Upload is the same path as git Load after files land. Selection-scoped Parse is [06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md). Both stay deferred.
5. **Path-control migration** — Core alone knows where files reside. Everyone else has a relative path. That lock stands. Migrating callers is later.
6. **Retire old hops** — Today's Load → Parse / graph-push hops stay until a later step retires them.

## 6. Vocabulary

1. Say **event source**, not ESO.
2. Do not say CAS.
3. Do not say Peer. Say Server git Actor, git Load, or git Save.
4. Name tickets in full (for example [06 — Selection-scoped Parse after whole-tree git Load](issues/06-selection-scoped-parse-after-whole-tree-git-load.md)).
5. Git use is Core. Do not imply git is outside Core.

## 7. Related

1. **Map** — [[map.md]] (decision 23)
2. **Axes lock** — [19 — Parsed/Unparsed and Persisted/Unpersisted](issues/19-file-newer-graph-newer.md)
3. **Server Core** — [[plan/architecture/server-core.md]]
