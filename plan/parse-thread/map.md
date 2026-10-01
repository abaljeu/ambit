# Parse thread

Labels: wayfinder:map

## 1. Destination

Clear the way for the Parse product: a continuous Server Parse thread turns file-shaped disk into Graph, takes priority from Browser wants, and emits Changes. The open way is directory Load and directory reconciliation: Load issued on a Directory, including a Directory that is not a Workspace, and Core pushing reconcile targets after files land.

## 2. Notes

1. **Skills** — [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/grilling/SKILL.md]], [[.agents/skills/domain-modeling/SKILL.md]], [[.agents/skills/project-work/SKILL.md]].
2. **Chapter** — [[plan/roadmap/epics/chapters/automatic-parse.md]].
3. **Core boundary** — Sole authority for the Core seam is [core-refinement architecture](../core-refinement/arch.md) and [core-refinement map](../core-refinement/map.md). The standing facts in this section are closed. This map does not redesign them.
4. **One parse thread** — One long-lived Parse stack. The consumer is the parse thread. Core pushes reconcile targets. Persist is a separate persist thread. See [03 — One Parse thread stack](../core-refinement/issues/03-one-parse-thread-stack.md).
5. **Axes and locks** — Special nodes carry Parsed|Unparsed and Persisted|Unpersisted. Both can be true at once. Locks are [core-refinement architecture](../core-refinement/arch.md) §6. See [04 — Parsed/Unparsed and Persisted/Unpersisted](../core-refinement/issues/04-parsed-unparsed-and-persisted-unpersisted.md).
6. **Git-pull handoff** — [02 — Git Load: Unparsed then Parse stack](../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md) is mainly about files changed by a git pull. Read the immediate-members sentence as that handoff. The Directory Parse body home is Decisions so far item 1.
7. **Axis-write items 8–11** — [core-refinement architecture](../core-refinement/arch.md) §5 item 8 Client Load on Directory, item 9 Client Load on File, item 10 Directory Parse body (product home is this Project), item 11 Parse stack pop.
8. **Browser wants** — [browser-residency](../browser-residency/project.md) computes wants. This Project decides how the parse thread uses them.
9. **File transit** — [transport-layer](../transport-layer/project.md).
10. **Chart limit** — This chart records locked directory/Load decisions and files the sharp questions. It does not resolve an open grilling ticket.

## 3. Decisions so far

1. [01 — Directory Parse body home](issues/01-directory-parse-body-home.md) — This Project owns [core-refinement architecture](../core-refinement/arch.md) §5 item 10. [02 — Git Load: Unparsed then Parse stack](../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md) stays the git-pull handoff and is imprecise if over-read.
2. [02 — Structure-match Directory Load](issues/02-structure-match-directory-load.md) — Structure-match Load on a Directory, including a Directory that is not a Workspace, is clear enough for later coding.

## 4. Not yet specified

1. **Directory Parse body acceptance** — §5 item 10 names the walk of nodes tied to that Directory File, missing File Nodes, and disk-newer Unparsed. What a person must still accept before coding that body is not sharp yet.
2. **Changes from directory reconcile** — The destination says the thread emits Changes. How a Directory reconcile meets Poll is not sharp yet.
3. **Order among Directory targets** — Order among several Directory reconcile targets, apart from Browser wants, is not sharp yet.

## 5. Out of scope

1. **Core stack and lock redesign** — The Parse stack, the axes, and §6 locks stay [core-refinement architecture](../core-refinement/arch.md) and [core-refinement map](../core-refinement/map.md). This effort does not reopen them.
2. **GitHub transport pull mechanics** — Pull and push mechanics stay [github-transport](../github-transport/project.md).
3. **Architecture roadmap reconcile** — Doc reconcile of workspace-file roadmap text stays [architecture](../architecture/project.md): [06 — Parse / Upload for Current Files (Warm Reconcile)](../architecture/issues/06-parse-upload-for-current-files-warm-reconcile.md) through [12 — Workspace scale file and db management](../architecture/issues/12-workspace-scale-file-and-db-management.md).
4. **Browser residency product** — How the Browser computes wants stays [browser-residency](../browser-residency/project.md). [04 — Browser want priority versus directory reconcile](issues/04-browser-want-priority-versus-directory-reconcile.md) still decides how the parse thread uses those wants.
5. **Persist thread product** — Persist stays the Core persist thread on [core-refinement](../core-refinement/project.md). This Project does not design Persist.
