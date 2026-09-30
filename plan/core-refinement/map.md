# core-refinement

Labels: wayfinder:map

## 1. Destination

Core works through disk and Graph changes after transport lands files: special-node Parsed|Unparsed and Persisted|Unpersisted axes, one long-lived Parse actor stack Core pushes onto, and Core’s async Persist stack. Transport (including [[plan/github-transport/project.md]]) locks the workspace, receives files, and informs Core. Core makes sure everything is updated. This Project is a sequel to [[plan/core-creation/project.md]].

## 2. Notes

Split from [[plan/github-transport/project.md]] on 2026-09-29. Grill locks and step 1 axis decisions that named the Core revision live here. Step 1 markers shipped as [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md). Expand-contract path from current Core to [[plan/architecture/server-core.md]] is charted on [[arch.md]] (Sequence `expand-contract`).

Parse Actor implementation home: [[plan/parse-actor/project.md]]. Git pull/push mechanics stay [[plan/github-transport/project.md]].

Skills: [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/grilling/SKILL.md]], [[.agents/skills/project-work/SKILL.md]].

## 3. Decisions so far

1. [01 — Persist/git work-tree gate](issues/01-persist-git-work-tree-gate.md) — **Revoked.** The exclusive Persist/git work-tree gate does not stand. Unparsed / Unpersisted replace it.
2. [02 — Git Load: Unparsed then Parse stack](issues/02-git-load-unparsed-then-parse-stack.md) — After files land (git Load pull or Upload): mark Unparsed, then push onto the one Parse actor. Nobody starts an Actor after pull.
3. [03 — One Parse actor stack](issues/03-one-parse-actor-stack.md) — One long-lived Parse actor. Core pushes reconcile targets. Persist is not an Actor.
4. [04 — Parsed/Unparsed and Persisted/Unpersisted](issues/04-parsed-unparsed-and-persisted-unpersisted.md) — Special nodes carry Parsed|Unparsed and Persisted|Unpersisted. Persist is Core’s async stack, blocked while Unparsed. git Save is permitted while Unparsed or Unpersisted. No Conflicted state.
5. [05 — Selection-scoped Parse after whole-tree git Load](issues/05-selection-scoped-parse-after-whole-tree-git-load.md) — Client Load marks selection Unparsed; File push onto the Parse stack needs the Parse loop. No special selection priority.
6. **Step 1 axes locked** — Markers only. Note: [[arch.md]] §4. Shipped: [20 — State axes on special nodes](../github-transport/issues/20-state-axes-on-special-nodes.md).
7. **Axis-write mechanics locked** — Who writes each axis and which node they mark. Note: [[arch.md]] §5.
8. **Core alone knows where files reside** — Callers pass a node or a relative path derived from a node; Core holds absolute residency via read/write file and read/write directory. Note: [[arch.md]] §3 step 4. Inventory: [DataDir caller inventory](reports/datadir-caller-inventory.md). Cross-cutting: [[plan/transport-layer/map.md]].
9. **Sequence expand-contract** — Incremental path current Core → [[plan/architecture/server-core.md]]. Note: [[arch.md]] §3. No Story paths or Module map on this Project.
10. **Slow expand-contract; Persist collectors** — Pace and Persist feeder charted on [[arch.md]] §3.
11. **Tickets as needed from sequence** — Undrafted until a use case needs one; each targets a beat or caller shift on [[arch.md]], not a pre-built full set.
12. **First use case** — Explicit parse command on a file; targets [[arch.md]] §3 step 2 Expand. Ticket: [06 — Explicit parse command on a File (Load)](issues/06-explicit-parse-command-load-file.md).

## 4. Not yet specified

1. **Mailbox enable → call → stop** — [[arch.md]] §3 step 5: 5.1 enable git requests → 5.2 call them → 5.3 stop any old. Persist collectors → loop → existing persist functions is charted (step 3). Path-control classification rule and DataDir inventory are recorded (step 4; [DataDir caller inventory](reports/datadir-caller-inventory.md)). Draft tickets as needed per Decisions §3.11.

## 5. Out of scope

1. **GitHub pull/push mechanics** — Stay [[plan/github-transport/project.md]].
2. **Parse Actor product home** — Stays [[plan/parse-actor/project.md]].
3. **Core creation baseline** — Mailbox, Actor pool, Event path stay [[plan/core-creation/project.md]].
