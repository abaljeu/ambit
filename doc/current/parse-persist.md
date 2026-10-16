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
[x] Persist thread: runs when a node is Unpersisted and Parsed. A node is blocked when its parseState is Unparsed. Persist thread: a thread. Its collectors call the persist functions in [Document persist change](../../src/Server/DocumentPersistChange.fs). Those functions stay the write body. Setup lives in [Core](../../src/Server/Core).
[x] Parse thread: adds `InMsg` `ParseFinished` through the mailbox private function when that File parse finishes. It does not edit the graph axes. The queue puller hands `InMsg` to the InMsg handler. [Mailbox](mailbox.md)
[x] Persist thread: adds `InMsg` `SnapshotDone` through the mailbox private function when that work finishes. It does not edit the graph.
## Parse

[x] The parse thread takes a File Node from the stack. [Document persist write](../../src/Server/DocumentPersistWrite.fs) `planParseFile` turns that file's disk text into graph ops.
[x] [Import document](../../src/Shared/dotnet/ImportDocument.fs) `planParseFile` marks an Unparsed File Node Current.
[x] File parse posts ops through [Parse thread](../../src/Server/ParseThread.fs) `postParseOps`. That post is the existing graph-only Change door.
[x] Mailbox Load of a Workspace Node, a Directory Node, or a File Node pushes that node onto the Parse stack. A node inside a File Node uses that File Node. The door is [Core mailbox load](../../src/Server/Core/CoreMailboxLoad.fs) `dispatchLoad`.
[x] Desktop-mapped Load stores the mapped bytes, then posts mailbox Load for that Workspace Node, Directory Node, or File Node. That Load pushes the subject onto the Parse stack.
[x] The client hop for desktop-mapped Load is retired. That Load does not read a desktop file for parse, and it does not post directory reconcile from the Browser.
[x] The git Load after-step is retired. After git Load returns, the Browser does not run a second reconcile or parse. Git Load posts the Workspace onto the Parse stack.
[x] Load on Directory goes through that Directory's members on disk. 
[x] A new directory on disk gets marked: it is added and starts Unparsed. 
[x] A deleted directory gets marked: it is removed. 
[x] A directory that already exists and is still present is untouched; this operation does nothing to it. 
[x] Load on Directory does not mark a file Unparsed unless it is a new file. 
[x] A new file on disk gets marked Unparsed. 
[x] An existing file that is still present is left alone. 
[x] A deleted file is removed. 
[x] If Git shows a file is modified, that file is marked Unparsed. 
[x] If Git shows a file is moved, deleted, or added, the containing directory is marked Unparsed. 
[x] A modified file does not mark its directory.
[x] git Load is one merge of the tracked remote branch of the whole Workspace work tree, completed by committing that merge as part of the pull.
[x] A file only the Server changed stays the Server file.
[x] A file only the remote changed becomes the remote file.
[x] Both sides edited, or both added the path: the file contents are the remote file, with no conflict markers.
[x] Edit-versus-delete is remote-wins. A Server edit and a remote delete remove the file. A Server delete and a remote edit leave the remote file. No conflict markers.
[x] The merge commit includes the remote tip, so a later git Save can still fast-forward.
[x] git Load commits the work tree before the pull.
[x] git push is fast-forward-only.
[x] A network failure, a credential failure, or a missing remote fails git Load and git Save.
[x] File-shaped Parse stays on the Server. The Browser has no `planParseFile` door. The App has no `planParseFile` door.
[o] [Route registration](../../src/Server/RouteRegistration.fs) calls [Parse thread](../../src/Server/ParseThread.fs) `bootCore`. That function builds the stack and starts the consumer. Route registration does not hold the push function.
[ ] Parse setup (stack, push, and consumer) lives only in [Core](../../src/Server/Core). Route registration does not construct Parse, does not start Parse, and does not hold Parse handles.
[ ] Every handoff uses the Parse stack.
[ ] Unparsed starts the parse thread after the workspace lock releases.
[x] Existing member: Directory reconcile does not mark an existing File Node or an existing child Directory Node Unparsed. A disk-newer member is an existing member.
[x] New member: a missing file gets a File Node. A missing child directory gets a Directory Node. Both start Unparsed. A member node that has no file is deleted. When the Directory Node or Workspace Node is Unparsed, the ops start with SetDocumentState from Unparsed to Current.
[ ] Directory reconcile walks every node tied to that Directory File, including nodes below the immediate children. It creates missing File Nodes. When Directory reconcile is done, that Directory Node is Parsed only. Structure-match on a Directory Node, including a Directory Node that is not a Workspace Node, spots disk members the Graph lacks, with no extra info. Parse thread owns Directory reconcile. Axis rules: Directory Parse done and Client Load on Directory.
## Persist

[x] Persist: turns graph information into files.
[ ] Core: owns read file, write file, read directory, and write directory. Callers pass a node, or a relative path derived from a node. Core holds the absolute `DataDir` residency.
[ ] No caller outside Core builds or holds a `DataDir` absolute path.
## Lock

[Core locking model](../../plan/core-refinement/arch.md) Working detail of this protocol.
[o] `WorkspaceGit.withWorkTreeGate`: the exclusive gate on Persist and git paths. [Workspace git](../../src/Server/WorkspaceGit.fs).
[ ] Persist, parse-thread file use, and git Load/Save use the workspace lock and per-member persist locks as their only protocol.
[ ] Locks: held only long enough to transfer data. No holder takes more than one lock. Aggregate locks exist.
[ ] Workspace lock: an aggregate lock. It announces that member files may change. Taking it waits while those member files are already changing.
[ ] Pending: new per-member persist locks cannot be taken. In-flight member file writes and parse reads drain. Then pull or Upload land proceeds, arrived files are marked Unparsed, and the lock releases.
[ ] Parse: reads the member file and does not take a persist lock. The pending workspace lock holds new reads off until release.
[ ] After release: persist of a member waits until that node is Parsed.
[ ] Unpersisted through pull: a member can be Unpersisted when the workspace lock is requested. The pull still marks it Unparsed. A later persist can write the graph over the file that just arrived.
[ ] File backstop: a write that finds the file still being written waits until the file is released, then proceeds.
## Explanation

An Actor is a different worker.
