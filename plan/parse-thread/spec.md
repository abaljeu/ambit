# Parse thread

Sources: [Parse thread map](map.md) Destination, Notes, and Decisions so far (2026-10-01). Part of [Automatic parse](../roadmap/epics/chapters/automatic-parse.md).

## 1. Problem Statement

1. **Unparsed File Nodes wait** — A person works with documents from any connected device. File Nodes can be Unparsed. Disk text is still outside the Graph. The product path is a continuous Server parse thread that turns file-shaped disk into Graph.
2. **Landed files need one consumer** — After files land, Core pushes a reconcile target. The person needs one long-lived parse thread to consume that stack and emit Changes.
3. **Directory members the Graph lacks** — A person issues Load on a Directory Node. That Directory Node can be a Directory that is not a Workspace Node. Structure-match must spot disk members the Graph lacks, with no extra info.
4. **The Directory File is the body** — Directory Parse must walk every node tied to that Directory File, create missing File Nodes, and mark a disk-newer File Node Unparsed. The git-pull sentence about immediate members is the pull handoff. That sentence is imprecise as the Directory Parse body.
5. **Download and Upload stay off Parse** — Workspace Download and Workspace Upload move files. File-shaped Parse stays on the Server. The Browser and the App do not Parse those files.

## 2. Solution

1. **Continuous parse thread** — One long-lived Server parse thread consumes the Parse stack. Core pushes reconcile targets onto that stack. The thread turns file-shaped disk into Graph and emits Changes. Call it the parse thread. The lock is [03 — One Parse thread stack](../core-refinement/issues/03-one-parse-thread-stack.md).
2. **Git-pull push** — On the git-pull handoff, the workspace lock drains in-flight member file use, the pull lands files, arrived files are marked Unparsed, and the lock releases. Core then pushes the Workspace Node onto the parse thread. Nobody starts an Actor after pull. Detail stays [02 — Git Load: Unparsed then Parse stack](../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md) and [core-refinement architecture](../core-refinement/arch.md) §6.
3. **Server-only file-shaped Parse** — File-shaped disk becomes Graph on the Server. The Browser does not Parse from Workspace Download. The App does not Parse from Workspace Upload. File transit stays on [transport-layer](../transport-layer/project.md).
4. **Directory Parse body** — This Project owns the Directory Parse body. That body is [core-refinement architecture](../core-refinement/arch.md) §5 item 10, locked on [01 — Directory Parse body home](issues/01-directory-parse-body-home.md). The body walks every node tied to that Directory File (`.amb`), including nodes below the immediate children. It creates missing File Nodes. When disk is newer, it marks the File Node Unparsed and pushes when the stack exists.
5. **Git-pull handoff stands beside the body** — [02 — Git Load: Unparsed then Parse stack](../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md) stays the git-pull handoff. Its immediate-members sentence is that handoff. Directory Parse body (§5 item 10) remains the body this Project owns. The two stand together.
6. **Structure-match Directory Load** — Client Load on a Directory Node marks that Directory Node Unparsed. The Load includes a Directory Node that is not a Workspace Node. Structure-match means reconciliation that can spot disk members the Graph lacks, with no extra info. The axis write is Client Load on Directory in [core-refinement architecture](../core-refinement/arch.md) §5 item 8. [02 — Structure-match Directory Load](issues/02-structure-match-directory-load.md) records that this way is clear enough for later coding.

## 3. User Stories

1. **Unparsed becomes Graph** — As a person, I want an Unparsed File Node to become Graph content through the Server parse thread, so that disk text is in the Graph while I work.
2. **Continuous Parse** — As a person, I want Unparsed File Nodes to parse without a separate Parse command, so that Parse keeps up on its own.
3. **One parse thread** — As a person, I want one continuous parse thread, so that file-shaped Parse has one consumer.
4. **Reconcile target after files land** — As a person, I want Core to push a reconcile target onto that thread after files land, so that landed files reach Parse.
5. **Git-pull reaches the thread** — As a person, I want a git pull to mark arrived files Unparsed and push the Workspace Node onto the parse thread after the workspace lock releases, so that pull uses the one thread.
6. **No Actor after pull** — As a builder, I want nobody to start an Actor after pull, so that the parse thread stays the consumer.
7. **Download leaves Parse on the Server** — As a person, I want Workspace Download to leave file-shaped Parse on the Server, so that the Browser does not turn those files into Graph.
8. **Upload leaves Parse on the Server** — As a person, I want Workspace Upload to leave file-shaped Parse on the Server, so that the App does not turn those files into Graph.
9. **Load a Directory Node** — As a person, I want to Load a Directory Node, so that the Directory Node is marked Unparsed.
10. **Directory that is not a Workspace** — As a person, I want that Load to include a Directory Node that is not a Workspace Node, so that a Directory Node that is not a Workspace Node uses the same structure-match path.
11. **Spot missing disk members** — As a person, I want structure-match to spot disk members the Graph lacks, with no extra info, so that new disk members can enter the Graph.
12. **Directory File walk** — As a person, I want Directory Parse to walk every node tied to that Directory File, so that the body covers the whole `.amb` tie.
13. **Nodes below the immediate children** — As a person, I want that walk to include nodes below the immediate children, so that nested members are in the body.
14. **Create missing File Nodes** — As a person, I want Directory Parse to create File Nodes the Graph lacks, so that a disk member has a File Node.
15. **Disk-newer Unparsed** — As a person, I want a disk-newer file to mark its File Node Unparsed, so that newer disk content is queued for Parse.
16. **Push when the stack exists** — As a person, I want that Unparsed File Node pushed when the Parse stack exists, so that the parse thread can take it.
17. **Emit Changes** — As a person, I want the parse thread to emit Changes, so that Graph updates from Parse are Events.
18. **Pull handoff stays the pull sentence** — As a builder, I want the immediate-members sentence to remain the git-pull handoff, so that Directory Parse body (§5 item 10) remains the body this Project owns.
19. **Two locks stand together** — As a builder, I want the git-pull handoff and the Directory Parse body to stand together, so that later work keeps both.
20. **Structure-match is ready to code later** — As a builder, I want structure-match Directory Load recorded as clear enough for later coding, so that a later coding ticket can build it.
21. **Axes stay on Core** — As a builder, I want Client Load on Directory to mark the Directory Node Unparsed under [core-refinement architecture](../core-refinement/arch.md) §5 item 8, so that this spec uses the locked axis write.

## 4. Out of Scope

1. **Core stack and lock redesign** — This spec does not reopen the Parse stack, the Parsed|Unparsed and Persisted|Unpersisted axes, or the locks. Those stay [core-refinement architecture](../core-refinement/arch.md) and [core-refinement map](../core-refinement/map.md).
2. **GitHub transport pull mechanics** — This spec does not design pull or push mechanics. Those stay [github-transport](../github-transport/project.md).
3. **Architecture roadmap reconcile** — This spec does not reconcile workspace-file roadmap text. That stays [architecture](../architecture/project.md), from [06 — Parse / Upload for Current Files (Warm Reconcile)](../architecture/issues/06-parse-upload-for-current-files-warm-reconcile.md) through [12 — Workspace scale file and db management](../architecture/issues/12-workspace-scale-file-and-db-management.md).
4. **Browser residency product** — This spec does not design how the Browser computes wants. That stays [browser-residency](../browser-residency/project.md).
5. **Persist thread product** — This spec does not design the persist thread. Persist stays the Core persist thread on [core-refinement](../core-refinement/project.md).

## 5. Further Notes

1. **Map** — Live decisions stay on [Parse thread map](map.md). This spec synthesizes the Destination, the Notes, and Decisions so far. Open grilling stays open.
2. **Directory Parse body acceptance** — Directory Parse body (§5 item 10) names the walk of nodes tied to that Directory File, missing File Nodes, and disk-newer Unparsed. What a person must still accept before coding that body is open. See [Parse thread map](map.md) Not yet specified, Directory Parse body acceptance.
3. **Changes from directory reconcile** — The destination says the thread emits Changes. How a Directory reconcile meets Poll is open. See [Parse thread map](map.md) Not yet specified, Changes from directory reconcile.
4. **Order among Directory targets** — Order among several Directory reconcile targets, apart from Browser wants, is open. See [Parse thread map](map.md) Not yet specified, Order among Directory targets.
5. **Workspace Load after incoming files** — [03 — Workspace Load after incoming files](issues/03-workspace-load-after-incoming-files.md) stays `defined`. The open question is what the parse thread does when the reconcile target is the Workspace Node, what that pass pushes for child Directory Nodes and File Nodes, and how that pass interacts with structure-match Directory Load.
6. **Browser want priority versus directory reconcile** — [04 — Browser want priority versus directory reconcile](issues/04-browser-want-priority-versus-directory-reconcile.md) stays `defined`. [browser-residency](../browser-residency/project.md) computes Browser wants. [05 — Selection-scoped Parse after whole-tree git Load](../core-refinement/issues/05-selection-scoped-parse-after-whole-tree-git-load.md) says a selection push has no special priority. Whether a Browser want uses that same rule when the other work is Directory reconcile stays on that ticket.
7. **Chapter goal** — [Automatic parse](../roadmap/epics/chapters/automatic-parse.md) names a continuous Server parse thread that turns file-shaped disk into Graph, takes priority from Browser wants, and emits Changes. The priority rule and the Poll meeting stay the open items above.
8. **Vocabulary** — Say parse thread, persist thread, Directory Node, File Node, Workspace Node, Directory File, Change, Load, Parse, and Poll. Core seam words stay on [core-refinement architecture](../core-refinement/arch.md) §7.
9. **Coding tickets** — This spec does not file coding tickets. Structure-match Directory Load is clear enough for later coding on [02 — Structure-match Directory Load](issues/02-structure-match-directory-load.md). Coding the Directory Parse body still waits on Directory Parse body acceptance.
