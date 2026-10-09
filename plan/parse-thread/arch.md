# Parse thread architecture

Spec: [spec](spec.md)
Updated: 2026-10-09
Sequence: module-build

Core stacks, axes, locks, and the mailbox stay [core-refinement architecture](../core-refinement/arch.md) and [core-refinement map](../core-refinement/map.md). InMsg on the one mailbox queue stays [core-refinement architecture](../core-refinement/arch.md) §10 Core loop. This architecture is the Parse product delta only. A `[x]` hop is already true in code, or it is a Core door this Project cites and does not build. Open Core build stays on that architecture and on [Core](../../doc/current/core.md). A `[ ]` hop is Directory reconcile work.

Sequence is module-build. Directory reconcile is in code. Story paths that only cite Core do not add modules. Tests for this Project cross Directory reconcile. They do not start the mailbox, the workspace lock, or the persist thread.

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
   1. [x] **No new Actor** — Nobody starts an Actor after pull. Lock: [03 — One Parse thread stack](../core-refinement/issues/03-one-parse-thread-stack.md).

7. **Download leaves Parse on the Server**
   1. [x] **Server parse** — File-shaped Parse stays on the Server.
   2. [x] **Browser** — The Browser has no `planParseFile` door. File transit stays [transport-layer](../transport-layer/project.md).

8. **Upload leaves Parse on the Server**
   1. [x] **Server parse** — File-shaped Parse stays on the Server.
   2. [x] **App** — The App has no `planParseFile` door. File transit stays [transport-layer](../transport-layer/project.md).

9. **Load a Directory Node**
   1. [x] **Mark Unparsed** — Client Load on a Directory Node marks that Directory Node Unparsed. Rule: [core-refinement architecture](../core-refinement/arch.md) §5 item 8.
   2. [x] **File Load door** — Mailbox Load of a File Node stays [Core mailbox backend](../../src/Server/Core/CoreMailboxBackend.fs) `dispatchLoad`. This Project does not widen that door.

10. **Same as a Workspace**
    1. [x] **Same axis write** — Item 8 includes a Directory Node that is not a Workspace Node.
    2. [x] **Same reconcile** — That Directory Node uses Directory reconcile.

11. **Spot missing disk members**
    1. [x] **Structure-match** — Directory reconcile spots disk members the Graph lacks.
    2. [x] **No extra info** — That spot uses the Directory File tie and the Graph. It does not take a member hint list.

12. **Directory File walk**
    1. [x] **Directory File** — Directory reconcile walks the Directory body, every node tied to that Directory File (`.amb`).

13. **Nodes below the immediate children**
    1. [x] **Below immediate children** — The walk includes nodes below the immediate children.

14. **Create missing File Nodes**
    1. [x] **Missing File Node** — Directory reconcile creates a File Node for a disk member the Graph lacks.

15. **Disk-newer Unparsed**
    1. [x] **Disk-newer** — When disk is newer, the parse thread adds InMsg MarkUnparsed for that File Node. The core loop sets Unparsed. Case home: [core-refinement architecture](../core-refinement/arch.md) §10 Core loop.

16. **Push when the stack exists**
    1. [x] **Push** — Directory reconcile pushes that Unparsed File Node when the Parse stack exists.
    2. [x] **Stack door** — The push door is [Parse stack](../../src/Server/ParseStack.fs).

    > Temporarily disabled (2026-10-08): MarkUnparsed does not enqueue; see requeueOnUnparsed in src/Server/ParseThread.fs. Re-enabled by [08 — Re-enable recursive update: enqueue on MarkUnparsed](issues/08-re-enable-recursive-update-enqueue-on-markunparsed.md).

17. **Emit Changes**
    1. [x] **File Change** — File parse posts ops through [Parse thread](../../src/Server/ParseThread.fs) `postParseOps`.
    2. [x] **Graph-only post** — That post is the existing Graph-only Change door.

18. **Pull handoff stays the pull sentence**
    1. [x] **Immediate members** — The immediate-members sentence stays the git-pull handoff. Ticket: [02 — Git Load: Unparsed then Parse stack](../core-refinement/issues/02-git-load-unparsed-then-parse-stack.md).
    2. [x] **Reconcile home** — This architecture defines Directory reconcile. [core-refinement architecture](../core-refinement/arch.md) §5 item 10 **Directory reconcile** points here. Ticket: [01 — Directory body home](issues/01-directory-parse-body-home.md).

19. **Two locks stand together**
    1. [x] **Handoff** — The git-pull handoff stands.
    2. [x] **Reconcile** — Directory reconcile stands beside the git-pull handoff. Same ticket as story path 18 **Pull handoff stays the pull sentence**.

20. **Structure-match is ready to code later**
    1. [x] **Recorded** — [02 — Structure-match Directory Load](issues/02-structure-match-directory-load.md) records the way as clear enough for later coding.
    2. [x] **Later coding** — That coding is Directory reconcile. It does not reopen §5 item 8 **Client Load on Directory**.

21. **Axes stay on Core**
    1. [x] **Item 8** — Client Load on Directory uses [core-refinement architecture](../core-refinement/arch.md) §5 item 8.
    2. [x] **No axis redesign** — This Project does not own the axes. See [04 — Parsed/Unparsed and Persisted/Unpersisted](../core-refinement/issues/04-parsed-unparsed-and-persisted-unpersisted.md).

22. **Add InMsg**
    1. [x] **Parse thread** — On finish the parse thread adds InMsg ParseFinished through the private function. It does not edit the graph axes.
    2. [x] **Persist thread** — On finish the persist thread adds InMsg SnapshotDone through the private function. It does not edit the graph axes. This Project does not design the persist write body.
    3. [x] **Core loop** — The mailbox has one queue. A private function adds InMsg. The queue puller in [Core mailbox backend](../../src/Server/Core/CoreMailboxBackend.fs) hands InMsg to the InMsg handler. ParseFinished sets Parsed only through `GraphMutate.setParseState`. PersistState stays unchanged. SnapshotDone sets Persisted only through `GraphMutate.setPersistState`.
    4. [x] **Not an Op** — InMsg is not an Op. `Op.SetPersistState` is not a writer. `Op.SetDocumentState` is not the writer of the parsed axis. `CoreMailbox.postEvents` and `CoreMailbox.postGraphOnly` do not carry InMsg.
    5. [x] **Type home** — [core-refinement architecture](../core-refinement/arch.md) §10 Core loop.
    6. [x] **MarkUnparsed** — For a disk-newer File Node the parse thread adds InMsg MarkUnparsed through the private function. The core loop sets Unparsed only through `GraphMutate.setParseState`. PersistState stays unchanged. `Op.SetDocumentState` is not the writer.

### 23. Shared segments

1. **File into Graph**
   1. [x] **Pop** — Parse stack pop of a File Node.
   2. [x] **Parse thread** — [Parse thread](../../src/Server/ParseThread.fs).
   3. [x] **File parse** — `planParseFile`.
   4. [x] **ParseFinished** — On finish the parse thread adds InMsg ParseFinished through the private function. It does not edit the graph axes.

2. **Directory into Graph**
   1. [x] **Mark Unparsed** — Core §5 item 8.
   2. [x] **Directory reconcile** — Walk, create, disk-newer, push.
   3. [x] **File into Graph** — A pushed File Node uses segment 1 **File into Graph**.

   > Temporarily disabled (2026-10-08): MarkUnparsed does not enqueue; see requeueOnUnparsed in src/Server/ParseThread.fs. Re-enabled by [08 — Re-enable recursive update: enqueue on MarkUnparsed](issues/08-re-enable-recursive-update-enqueue-on-markunparsed.md).

3. **Outside Parse**
   1. [x] **Download** — Story path 7 **Download leaves Parse on the Server** does not cross the parse thread.
   2. [x] **Upload** — Story path 8 **Upload leaves Parse on the Server** does not cross the parse thread.

### 24. Test seam

1. [x] **Directory reconcile** — Narrowest new door the directory paths cross. Shared tests call this interface.
2. [x] **File parse** — Narrowest door the file paths already cross. This Project does not move that seam.

## 2. Module map

1. **Directory reconcile**
   Directory reconcile scans the directory and updates the Directory body. The Directory body is the nodes tied to that `.amb`. Scanning the `.amb` file would be Directory parse. That scan is not this operation. Directory reconcile walks all nodes tied to that `.amb`, not only immediate children. It creates missing File Nodes. When disk is newer, the parse thread adds InMsg MarkUnparsed for that File Node and names that File Node for push. The core loop sets Unparsed. A Directory Node under that node that needs reparse is marked Unparsed the same way. Ticket: [07 — Directory Unparsed during reconcile](issues/07-directory-unparsed-during-reconcile.md).
   File: `src/Shared/dotnet/DirectoryReconcile.fs`
   Claim home: [Parse and persist](../../doc/current/parse-persist.md)

   1. **State**
      1. [x] **Whole tie** — The walk covers the Directory body, every node tied to that Directory File, including nodes below the immediate children.
      2. [x] **Missing File Node** — A disk member the Graph lacks has a File Node after Directory reconcile.
      3. [x] **Disk-newer** — A disk-newer file has its File Node Unparsed after Directory reconcile.
      4. [x] **Directory needs reparse** — A child directory that is disk-newer, or a disk directory the Graph lacks, has its Directory Node Unparsed after Directory reconcile. Ticket: [07 — Directory Unparsed during reconcile](issues/07-directory-unparsed-during-reconcile.md).
   2. **Interface**
      1. [x] **Inputs** — Directory reconcile takes the disk directory, the graph, and the directory id. The directory id plus the graph is the Directory Node. Directory reconcile reads the Directory body from that graph. The disk directory is required.
      2. [x] **Create** — Directory reconcile returns ops that create each missing File Node. A new node appends alphabetically under the Directory Node.
      3. [x] **Unparsed** — Directory reconcile names a disk-newer File Node. The parse thread adds InMsg MarkUnparsed through the private function. The core loop sets that node Unparsed only through `GraphMutate.setParseState`. PersistState stays unchanged. Case home: [core-refinement architecture](../core-refinement/arch.md) §10 Core loop.
      4. [x] **Push** — Directory reconcile names that File Node for push.
      5. [x] **No extra info** — Structure-match uses the Directory body and the Graph only.
      6. [x] **Workspace** — Workspace nodes use the same reconcile process. (They also do other things.)
      7. [x] **ParseFinished** — When Directory reconcile is done, the parse thread adds InMsg ParseFinished for that Directory Node through the private function. It does not edit the graph axes. The core loop sets Parsed only through `GraphMutate.setParseState`. PersistState stays unchanged.
      8. [x] **Directory Unparsed** — Directory reconcile names a Directory Node that needs reparse. A disk directory the Graph lacks gets a Directory Node. The parse thread sets Unparsed by the same InMsg as a File Node. The parse thread does not edit the graph axes. Reconcile ops do not carry the Unparsed write. Directory reconcile names that Directory Node for push when the Parse stack exists. Ticket: [07 — Directory Unparsed during reconcile](issues/07-directory-unparsed-during-reconcile.md).
   3. **Uses**
      1. [x] **Parse thread** — [Parse thread](../../src/Server/ParseThread.fs) calls Directory reconcile when the popped node is a Directory Node.
      2. [x] **Parse stack** — Push uses [Parse stack](../../src/Server/ParseStack.fs).
      3. [x] **File parse** — File text stays [Document persist write](../../src/Server/DocumentPersistWrite.fs) `planParseFile`.
      4. [x] **Create File Node** — Missing File Nodes use [File node ops](../../src/Shared/FileNodeOps.fs) `planCreateOwnedFile`.
      5. [x] **Pop skip** — A Parsed node skips work. Rule: [core-refinement architecture](../core-refinement/arch.md) §5 item 11.
      6. [x] **ParseFinished** — Directory Parse done adds InMsg ParseFinished through the private function. The core loop sets that Directory Node Parsed only through `GraphMutate.setParseState`. PersistState stays unchanged. Rule: [core-refinement architecture](../core-refinement/arch.md) §5 item 6 and §10 Core loop.
      7. [x] **Load axis** — Client Load on Directory stays §5 item 8.
      8. [x] **Reconcile rule** — Directory reconcile is defined in this item. [core-refinement architecture](../core-refinement/arch.md) §5 item 10 **Directory reconcile** points here. Lock: [01 — Directory body home](issues/01-directory-parse-body-home.md).
      9. [x] **Locks** — Workspace lock and member locks stay §6. Directory reconcile does not take them.
      10. [x] **Claim** — [Parse and persist](../../doc/current/parse-persist.md) holds the Should Become claim.

## 3. Seams

1. **Directory reconcile**
   1. [x] Interface on **Directory reconcile**. Tests cross this seam.
2. **Parse thread**
   1. [x] Interface on the existing parse thread. A File Node pop stays `planParseFile`. A Directory Node pop calls **Directory reconcile**. On finish the parse thread adds InMsg ParseFinished through the private function.
3. **File parse**
   1. [x] Interface on `planParseFile`. Unchanged.
4. **Client Load on Directory**
   1. [x] Interface on [core-refinement architecture](../core-refinement/arch.md) §5 item 8. The mailbox Load door stays File-only.

## 4. Alternative considered

1. **Widen planParseFile** — One function accepts a File Node or a Directory Node. The File door rejects a non-File today. A Directory walk creates File Nodes and pushes them. That is a second responsibility. The File door stays narrow.
2. **Fold into Lazy Load reconciliation** — [Lazy load reconciliation](../../src/Shared/dotnet/LazyLoadReconciliation.fs) takes discovered changed paths. [core-refinement architecture](../core-refinement/arch.md) §5 item 3 **Discovery** says Parse is not the discovery tool. Directory reconcile walks the Directory body. The inputs differ.
3. **Winner** — **Directory reconcile** is its own module. The parse thread calls it. The File seam stays. Discovery stays outside Parse.

## 5. Unsettled

1. **Directory reconcile acceptance** — §2 Module map, item 1 **Directory reconcile** names the walk, missing File Nodes, and disk-newer Unparsed. The coding ticket is [05 — Directory reconcile](issues/05-directory-reconcile.md), Status `coded`. Child Directory Unparsed stays [07 — Directory Unparsed during reconcile](issues/07-directory-unparsed-during-reconcile.md), Status `defined`.
2. **Changes from directory reconcile** — The destination says the thread emits Changes. How a Directory reconcile meets Poll is open. See [Parse thread map](map.md) Not yet specified, Changes from directory reconcile. Story path 17 **Emit Changes** records only the existing File Change door.
3. **Browser want priority versus directory reconcile** — [04 — Browser want priority versus directory reconcile](issues/04-browser-want-priority-versus-directory-reconcile.md) stays `defined`. [browser-residency](../browser-residency/project.md) computes Browser wants. [05 — Selection-scoped Parse after whole-tree git Load](../core-refinement/issues/05-selection-scoped-parse-after-whole-tree-git-load.md) says a selection push has no special priority. Whether a Browser want uses that rule when the other work is Directory reconcile stays on that ticket.
