# 17 — Post-pull cascade and gate handoff

**Type:** grilling
**Status:** done
Blocked by: [16 — Persist/git work-tree gate](16-persist-git-work-tree-gate.md)
Actual: 20m

## 1. Question

- [x] After git Load pull, when does the exclusive Workspace gate release, and how do directory fine locks cascade?
- [x] May a long parse block a later git Load pull?
- [x] What does locking protect?
- [x] What is the deadlock / acquire-order rule?
- [x] Is the file fine lock a separate table, or Graph state?

## 2. Answer

Locked 2026-09-28 (Alan, chat). Exact sequence. Locks are Graph state, not a separate lock table.

1. Lock the **Workspace Node** by setting it to **Reconciling**. That prohibits other processes from making or deleting files here. This is [16 — Persist/git work-tree gate](16-persist-git-work-tree-gate.md)’s exclusive gate, named as Graph state.
2. **Pull** files.
3. For each **modified** file, set the Graph node to **Unparsed**. That **is** the file fine lock (File Newer: [19 — File Newer / Graph Newer](19-file-newer-graph-newer.md)).
4. Set **Directory** nodes to **Reconciling**. That is a **new** directory lock (directory Graph state).
5. Then **release** the Workspace lock (clear Workspace Reconciling).

Unparsed is set while the Workspace Node is still Reconciling, before release. Anti-Persist does not need a later window: Persist already writes only Current roots (`DocumentPartition.shouldWriteDocumentRoot` in [[src/Shared/DocumentPartition.fs]]).

After release, directory cascade continues: lock children that need work → unlock parent → process children. Acquire only down the tree — parent then children, never child-then-parent. Unlock the parent before waiting on deeper work. Directory-reconcile worker holds directory Reconciling and clears it as each directory’s Graph nodes are updated. Parse holds only file Unparsed. See [18 — Parse Actor stack and file-lock ownership](18-parse-actor-stack-and-file-lock-ownership.md).

Long parse must not block further git Load pulls. Parsing an entire Workspace can take a very long time. File Unparsed and directory Reconciling must not hold Workspace Reconciling, or otherwise serialize, across the whole parse. A later git Load pull proceeds by taking Workspace Reconciling again. Persist still queues while that later pull holds Workspace Reconciling.

The correct pipeline is github → file → parse → (merge) graph → file.

One owner per lock kind: directory-reconcile holds directory Reconciling; Parse holds file Unparsed. Nobody takes a second lock while holding one that would reverse parent-then-child order.

Map gist: [[../map.md]] Decisions so far item 17.

## Notes

- This ticket refines the post-pull window after [16 — Persist/git work-tree gate](16-persist-git-work-tree-gate.md). It does not rewrite that exclusive-gate lock. Wording aligns: exclusive gate = Workspace Reconciling.
- `DocumentState` today is `Current` | `Unparsed` | `NoServerFile` ([[src/Shared/Model.fs]]). **Reconciling** is a new Graph state for Workspace Node and Directory Node. That is implement, not an open decision.
- File this on github-transport. Parse stack and file Unparsed: [18 — Parse Actor stack and file-lock ownership](18-parse-actor-stack-and-file-lock-ownership.md). File Newer / Unpersisted: [19 — File Newer / Graph Newer](19-file-newer-graph-newer.md). Parse Actor home: [[plan/parse-actor/project.md]].

## Comments

- 2026-09-28: Alan locked the exact sequence. Workspace Reconciling → pull → Unparsed on modified files → directory Reconciling → release Workspace Reconciling. File fine lock = Unparsed. Directory fine lock = Reconciling (new). Not a gap: Unparsed is set before Workspace release.

## Time

- 2026-09-28 5m — recorded lock from chat
- 2026-09-28 5m — recorded follow-up locks from chat
- 2026-09-28 5m — recorded Unparsed Persist-block clarification from chat
- 2026-09-28 5m — rewrote cascade to Workspace Reconciling sequence from chat
