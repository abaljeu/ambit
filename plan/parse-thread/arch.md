# Parse thread architecture

Spec: [spec](spec.md)
Updated: 2026-10-01
Sequence: module-build

Core stacks, axes, locks, and the mailbox stay [core-refinement architecture](../core-refinement/arch.md) and [core-refinement map](../core-refinement/map.md). This architecture is the Parse product delta only. A `[x]` hop is already true in code, or it is a Core door this Project cites and does not build. Open Core build stays on that architecture and on [Core](../../doc/current/core.md). A `[ ]` hop is Directory Parse body work.

Sequence is module-build. The open product delta is one module. Story paths that only cite Core do not add modules. Tests for this Project cross the Directory Parse body. They do not start the mailbox, the workspace lock, or the persist thread.

## 1. Story paths

1. **Unparsed becomes Graph**
   1. [x] **Parse thread** — [Parse thread](../../src/Server/ParseThread.fs) takes a File Node from the stack.
   2. [x] **File parse** — [Document persist write](../../src/Server/DocumentPersistWrite.fs) `planParseFile` turns that file's disk text into Graph ops.
   3. [x] **Current** — [Import document](../../src/Shared/dotnet/ImportDocument.fs) `planParseFile` marks an Unparsed File Node Current.

2. **Continuous Parse**
   1. [x] **One consumer** — The parse thread consumes the stack. It is not a new command.
   2. [x] **Unparsed start** — Unparsed starts that thread on the Core migrate. Claim: [Parse and persist](../../doc/current/parse-persist.md).

3. **One parse thread**
   1. [x] **Parse thread** — One long-lived consumer. Lock: [03 — One Parse thread stack](../core-refinement/issues/03-one-parse-thread-stack.md).
   2. [x] **Parse stack** — One stack. File: [Parse stack](../../src/Server/ParseStack.fs).

4. **Reconcile target after files land**
   1. [x] **Core push** — After files land, Core pushes a reconcile target. This Project does not add a second push.
   2. [x] **Git-pull handoff** — The handoff is [02 — Git Load: Unparsed then Parse stack](../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md).

5. **Git-pull reaches the thread**
   1. [x] **Workspace lock** — Drain, land, mark arrived files Unparsed, and release stay [core-refinement architecture](../core-refinement/arch.md) §6.
   2. [x] **Workspace Node push** — After release, Core pushes the Workspace Node. Same handoff as story path 4 **Reconcile target after files land**.

6. **No Actor after pull**
   1. [x] **Not an Actor** — The parse thread is a thread. [Parse and persist](../../doc/current/parse-persist.md) records that.
   2. [x] **No new Actor** — Nobody starts an Actor after pull. Lock: [03 — One Parse thread stack](../core-refinement/issues/03-one-parse-thread-stack.md).

7. **Download leaves Parse on the Server**
   1. [x] **Server parse** — File-shaped Parse stays on the Server.
   2. [x] **Browser** — The Browser has no `planParseFile` door. File transit stays [transport-layer](../transport-layer/project.md).

8. **Upload leaves Parse on the Server**
   1. [x] **Server parse** — File-shaped Parse stays on the Server.
   2. [x] **App** — The App has no `planParseFile` door. File transit stays [transport-layer](../transport-layer/project.md).

9. **Load a Directory Node**
   1. [x] **Mark Unparsed** — Client Load on a Directory Node marks that Directory Node Unparsed. Rule: [core-refinement architecture](../core-refinement/arch.md) §5 item 8.
   2. [x] **File Load door** — Mailbox Load of a File Node stays [Core mailbox backend](../../src/Server/Core/CoreMailboxBackend.fs) `dispatchLoad`. This Project does not widen that door.

10. **Directory that is not a Workspace**
    1. [x] **Same axis write** — Item 8 includes a Directory Node that is not a Workspace Node.
    2. [ ] **Same body** — That Directory Node uses the Directory Parse body.

11. **Spot missing disk members**
    1. [ ] **Structure-match** — The Directory Parse body spots disk members the Graph lacks.
    2. [ ] **No extra info** — That spot uses the Directory File tie and the Graph. It does not take a member hint list.

12. **Directory File walk**
    1. [ ] **Directory File** — The body walks every node tied to that Directory File (`.amb`).

13. **Nodes below the immediate children**
    1. [ ] **Below immediate children** — The walk includes nodes below the immediate children.

14. **Create missing File Nodes**
    1. [ ] **Missing File Node** — The body creates a File Node for a disk member the Graph lacks.

15. **Disk-newer Unparsed**
    1. [ ] **Disk-newer** — When disk is newer, the body marks that File Node Unparsed.

16. **Push when the stack exists**
    1. [ ] **Push** — The body pushes that Unparsed File Node when the Parse stack exists.
    2. [x] **Stack door** — The push door is [Parse stack](../../src/Server/ParseStack.fs).

17. **Emit Changes**
    1. [x] **File Change** — File parse posts ops through [Parse thread](../../src/Server/ParseThread.fs) `postParseOps`.
    2. [x] **Graph-only post** — That post is the existing Graph-only Change door.

18. **Pull handoff stays the pull sentence**
    1. [x] **Immediate members** — The immediate-members sentence stays the git-pull handoff. Ticket: [02 — Git Load: Unparsed then Parse stack](../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md).
    2. [x] **Body home** — The Directory Parse body stays §5 item 10. Ticket: [01 — Directory Parse body home](issues/01-directory-parse-body-home.md).

19. **Two locks stand together**
    1. [x] **Handoff** — The git-pull handoff stands.
    2. [x] **Body** — The Directory Parse body stands beside it. Same ticket as story path 18 **Pull handoff stays the pull sentence**.

20. **Structure-match is ready to code later**
    1. [x] **Recorded** — [02 — Structure-match Directory Load](issues/02-structure-match-directory-load.md) records the way as clear enough for later coding.
    2. [ ] **Later coding** — That coding is the Directory Parse body. It does not reopen §5 item 8.

21. **Axes stay on Core**
    1. [x] **Item 8** — Client Load on Directory uses [core-refinement architecture](../core-refinement/arch.md) §5 item 8.
    2. [x] **No axis redesign** — This Project does not own the axes. See [04 — Parsed/Unparsed and Persisted/Unpersisted](../core-refinement/issues/04-parsed-unparsed-and-persisted-unpersisted.md).

### 22. Shared segments

1. **File into Graph**
   1. [x] **Pop** — Parse stack pop of a File Node.
   2. [x] **Parse thread** — [Parse thread](../../src/Server/ParseThread.fs).
   3. [x] **File parse** — `planParseFile`.

2. **Directory into Graph**
   1. [x] **Mark Unparsed** — Core §5 item 8.
   2. [ ] **Directory Parse body** — Walk, create, disk-newer, push.
   3. [x] **File into Graph** — A pushed File Node uses segment 1 **File into Graph**.

3. **Outside Parse**
   1. [x] **Download** — Story path 7 **Download leaves Parse on the Server** does not cross the parse thread.
   2. [x] **Upload** — Story path 8 **Upload leaves Parse on the Server** does not cross the parse thread.

### 23. Test seam

1. [ ] **Directory Parse body** — Narrowest new door the directory paths cross. Shared tests call this interface.
2. [x] **File parse** — Narrowest door the file paths already cross. This Project does not move that seam.

## 2. Module map

1. **Directory Parse body**
   File: `src/Shared/dotnet/DirectoryParse.fs`
   Claim home: [Parse and persist](../../doc/current/parse-persist.md)

   1. **State**
      1. [ ] **Whole tie** — The walk covers every node tied to that Directory File, including nodes below the immediate children.
      2. [ ] **Missing File Node** — A disk member the Graph lacks has a File Node after the body.
      3. [ ] **Disk-newer** — A disk-newer file has its File Node Unparsed after the body.
   2. **Interface**
      1. [ ] **Inputs** — The body takes a Directory Node and the nodes tied to that Directory File.
      2. [ ] **Create** — The body returns ops that create each missing File Node.
      3. [ ] **Unparsed** — The body returns a disk-newer File Node as Unparsed.
      4. [ ] **Push** — The body names that File Node for push when the Parse stack exists.
      5. [ ] **No extra info** — Structure-match uses the tie and the Graph only.
      6. [ ] **Not a Workspace** — A Directory Node that is not a Workspace Node uses this same body.
      7. [ ] **Parsed only** — When the body is done, that Directory Node is Parsed only.
   3. **Uses**
      1. [ ] **Parse thread** — [Parse thread](../../src/Server/ParseThread.fs) calls the body when the popped node is a Directory Node.
      2. [x] **Parse stack** — Push uses [Parse stack](../../src/Server/ParseStack.fs).
      3. [x] **File parse** — File text stays [Document persist write](../../src/Server/DocumentPersistWrite.fs) `planParseFile`.
      4. [x] **Create File Node** — Missing File Nodes use [File node ops](../../src/Shared/FileNodeOps.fs) `planCreateOwnedFile`.
      5. [x] **Pop skip** — A Parsed node skips work. Rule: [core-refinement architecture](../core-refinement/arch.md) §5 item 11.
      6. [x] **Parsed only** — Directory Parse done marks that Directory Node Parsed only. Rule: §5 item 6.
      7. [x] **Load axis** — Client Load on Directory stays §5 item 8.
      8. [x] **Body rule** — The body is §5 item 10. Product home: [01 — Directory Parse body home](issues/01-directory-parse-body-home.md).
      9. [x] **Locks** — Workspace lock and member locks stay §6. This body does not take them.
      10. [x] **Claim** — [Parse and persist](../../doc/current/parse-persist.md) holds the Should Become claim.

## 3. Seams

1. **Directory Parse body**
   1. [ ] Interface on **Directory Parse body**. Tests cross this seam.
2. **Parse thread**
   1. [x] Interface on the existing parse thread. A File Node pop stays `planParseFile`. A Directory Node pop calls **Directory Parse body**.
3. **File parse**
   1. [x] Interface on `planParseFile`. Unchanged.
4. **Client Load on Directory**
   1. [x] Interface on [core-refinement architecture](../core-refinement/arch.md) §5 item 8. The mailbox Load door stays File-only.

## 4. Alternative considered

1. **Widen planParseFile** — One function accepts a File Node or a Directory Node. The File door rejects a non-File today. A Directory walk creates File Nodes and pushes them. That is a second responsibility. The File door stays narrow.
2. **Fold into Lazy Load reconciliation** — [Lazy load reconciliation](../../src/Shared/dotnet/LazyLoadReconciliation.fs) takes discovered changed paths. [core-refinement architecture](../core-refinement/arch.md) §5 item 3 says Parse is not the discovery tool. The body walks the Directory File tie. The inputs differ.
3. **Winner** — **Directory Parse body** is its own module. The parse thread calls it. The File seam stays. Discovery stays outside Parse.

## 5. Unsettled

1. **Directory Parse body acceptance** — §5 item 10 names the walk, missing File Nodes, and disk-newer Unparsed. What a person must still accept before coding that body is open. See [Parse thread map](map.md) Not yet specified, Directory Parse body acceptance. The locked sentences stay in the Module map. This architecture adds no acceptance leaves. Coding the body waits on that acceptance. See [spec](spec.md) Further Notes.
2. **Changes from directory reconcile** — The destination says the thread emits Changes. How a Directory reconcile meets Poll is open. See [Parse thread map](map.md) Not yet specified, Changes from directory reconcile. Story path 17 **Emit Changes** records only the existing File Change door.
3. **Order among Directory targets** — Order among several Directory reconcile targets, apart from Browser wants, is open. See [Parse thread map](map.md) Not yet specified, Order among Directory targets.
4. **Workspace Load after incoming files** — [03 — Workspace Load after incoming files](issues/03-workspace-load-after-incoming-files.md) stays `defined`. Open: what the parse thread does when the reconcile target is the Workspace Node, what that pass pushes for child Directory Nodes and File Nodes, and how that pass meets structure-match Directory Load.
5. **Browser want priority versus directory reconcile** — [04 — Browser want priority versus directory reconcile](issues/04-browser-want-priority-versus-directory-reconcile.md) stays `defined`. [browser-residency](../browser-residency/project.md) computes Browser wants. [05 — Selection-scoped Parse after whole-tree git Load](../core-refinement/issues/05-selection-scoped-parse-after-whole-tree-git-load.md) says a selection push has no special priority. Whether a Browser want uses that rule when the other work is Directory reconcile stays on that ticket.
