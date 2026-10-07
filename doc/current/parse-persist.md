# Parse and persist

Category: Capability
See Also:
- [Core](core.md)
- [Mailbox](mailbox.md)
- [Actors](actors.md)
- [Persistence model](persistence-model.md)
- [Workspace graph](graph.md)
Parse and persist move text between disk and the graph.
## Job

[x] Server: one long-lived Parse stack and one parse thread. [Parse stack](../../src/Server/ParseStack.fs), [Parse thread](../../src/Server/ParseThread.fs).
[x] Nobody starts an Actor after pull.
[ ] Persist thread: runs when a node is Unpersisted and Parsed. Persist thread: a thread. Its collectors call the persist functions in [Document persist change](../../src/Server/DocumentPersistChange.fs). Those functions stay the write body. Setup lives in [Core](../../src/Server/Core).
[x] Parse thread: adds `InMsg` `ParseFinished` through the mailbox private function when that File parse finishes. It does not edit the graph axes. The queue puller hands `InMsg` to the InMsg handler. [Mailbox](mailbox.md)
[ ] Persist thread: adds `InMsg` through the mailbox private function when that work finishes. It does not edit the graph.
## Parse

[x] The parse thread takes a File Node from the stack. [Document persist write](../../src/Server/DocumentPersistWrite.fs) `planParseFile` turns that file's disk text into graph ops.
[x] [Import document](../../src/Shared/dotnet/ImportDocument.fs) `planParseFile` marks an Unparsed File Node Current.
[x] File parse posts ops through [Parse thread](../../src/Server/ParseThread.fs) `postParseOps`. That post is the existing graph-only Change door.
[x] Mailbox Load of a File Node pushes that node onto the Parse stack. The door is [Core mailbox backend](../../src/Server/Core/CoreMailboxBackend.fs) `dispatchLoad`. That door stays File-only.
[x] File-shaped Parse stays on the Server. The Browser has no `planParseFile` door. The App has no `planParseFile` door.
[o] [Route registration](../../src/Server/RouteRegistration.fs) `createPersistenceContext` builds `ParseStack` and calls `ParseThread.start`.
[ ] Parse setup (stack, push, and consumer) lives only in [Core](../../src/Server/Core). Route registration does not construct Parse, does not start Parse, and does not hold Parse handles.
[ ] Every handoff uses the Parse stack.
[ ] After the workspace lock drains in-flight member file use, pull or Upload land proceeds. Arrived files are marked Unparsed. The lock releases. Unparsed starts the parse thread.
[x] Disk-newer File Node: the parse thread adds `InMsg` `MarkUnparsed` through the mailbox private function. The core loop sets that node Unparsed through `GraphMutate.setParseState`. PersistState stays unchanged. `Op.SetDocumentState` is not the writer. Directory reconcile names that File Node for push. [Mailbox](mailbox.md)
[ ] Directory reconcile walks every node tied to that Directory File, including nodes below the immediate children. It creates missing File Nodes. When Directory reconcile is done, that Directory Node is Parsed only. Structure-match on a Directory Node, including a Directory Node that is not a Workspace Node, spots disk members the Graph lacks, with no extra info. Parse thread owns Directory reconcile. Axis rules: Directory Parse done and Client Load on Directory.
## Persist

[x] Persist: turns graph information into files.
[ ] Core: owns read file, write file, read directory, and write directory. Callers pass a node, or a relative path derived from a node. Core holds the absolute `DataDir` residency.
[ ] No caller outside Core builds or holds a `DataDir` absolute path.
## Lock

[o] `WorkspaceGit.withWorkTreeGate`: the exclusive gate on Persist and git paths. [Workspace git](../../src/Server/WorkspaceGit.fs).
[ ] Persist and git paths use the workspace lock as their only protocol.
[ ] A workspace lock and per-member persist locks are the protocol for member files. While the workspace lock is pending, new persist locks for those member files cannot be taken. In-flight member file writes and parse reads drain. Then pull proceeds, arrived files are marked Unparsed, and the lock releases.
[ ] Persist, parse-thread file use, and git Load/Save follow that lock protocol.
## Explanation

An Actor is a different worker.
