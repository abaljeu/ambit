# 31 — Force-save the focused node — scope

Slice: Save force-saves the focused node. Save marks that node Parsed and pushes it onto the existing persistence stack, including when ordinary dirty or change detection would not enqueue it. Load and Save stay separate functions that share helpers. This slice does not add a combined Load-or-Save function. Context: [Solution 11 — git Save sequence](../spec.md); [30 — GitHub save path: missing workspace upward scan and workspace upload](../issues/30-github-save-path-missing-workspace-upward-scan-and-workspace-upload.md).

### Signatures

[ ] `GithubTransportActor.persistFocus` — `string -> Graph -> NodeId -> Result<unit, string>` — private — `src/Server/GithubTransportActor.fs`
[ ] `GithubTransportActor.runSaveBody` — `string -> GithubTransportActorDependencies -> NodeId -> ActorChanges -> Async<Result<unit, string>>` — private — `src/Server/GithubTransportActor.fs`
[ ] `GithubTransportActor.saveActorFunction` — `string -> GithubTransportActorDependencies -> NodeId -> ActorFunction` — `src/Server/GithubTransportActor.fs`
[ ] `GithubTransportActor.commitPullPush` — `GithubTransportGit -> string -> Result<unit, string>` — private — `src/Server/GithubTransportActor.fs`
[ ] `DocumentPersistChange.persistNode` — `string -> Graph -> NodeId -> Result<DateTime, string>` — `src/Server/DocumentPersistChange.fs`
[ ] `DocumentPartition.documentRootForNode` — `Graph -> NodeId -> NodeId option` — `src/Shared/DocumentPartition.fs`
[ ] `GraphMutate.setParseState` — `NodeId -> ParseState -> Graph -> Result<Graph, string>` — `src/Shared/GraphMutate.fs`
[ ] `NoticeGraph.writeGraph` — `Notice -> Graph -> Result<Graph, string>` — `Notice.NodeParsed` arm — `src/Shared/NoticeGraph.fs`
[ ] `CoreMailboxFilesAxis.applyNodeParsed` — `CoreMailboxAccess -> NodeId -> Result<unit, string>` — internal — `src/Server/Core/CoreMailboxFilesAxis.fs`
[ ] `CoreMailboxFilesAxis.markUnparsedThenPush` — `CoreMailboxAccess -> (NodeId -> unit) -> NodeId -> Result<unit, string>` — internal — `src/Server/Core/CoreMailboxFilesAxis.fs`
[ ] `CoreEventDispatch.markForPersist` — `Context -> EventId -> NodeId -> Result<unit, string>` — private — `src/Server/Core/CoreEventDispatch.fs`
[ ] `CoreEventDispatch.persistedNode` — `Graph -> NodeId -> bool` — internal — `src/Server/Core/CoreEventDispatch.fs`
[ ] `CoreMailboxAccess.pushPersist` — `NodeId -> unit` — field on `CoreMailboxAccess` — `src/Server/Core/CoreMsg.fs`
[ ] `PersistRequestStack.push` — `BlockingCollection<PersistRequest> -> PersistRequest -> unit` — `src/Server/Core/PersistRequestStack.fs`
[ ] `PersistRequestStack.pushBoundNode` — `PersistRequest BlockingCollection option ref -> NodeId -> unit` — `src/Server/Core/PersistRequestStack.fs`
[ ] `PersistThread.runRequest` — `PersistThreadDeps -> PersistRequest -> unit` — internal — `src/Server/Core/PersistThread.fs`
[ ] `ActorChanges.postNotice` — `Notice -> unit` — `src/Shared/History.fs`
[ ] `CoreDoor.actorChanges` — `CoreChangesDoor -> ActorChanges` — wires `postNotice` to `CoreMailboxApi.postNotice` — `src/Server/Core/CoreDoor.fs`

### Match sites

[ ] `GithubTransportActor.runSaveBody` calls `persistFocus`, then `commitPullPush` — `src/Server/GithubTransportActor.fs` — `runSaveBody`
[ ] `GithubTransportActor.saveActorFunction` passes `runSaveBody` into `actorFunction` — `src/Server/GithubTransportActor.fs` — `saveActorFunction`
[ ] `GithubTransportActor.start` matches `GithubTransportOperation.Save` to `saveActorFunction` — `src/Server/GithubTransportActor.fs` — `start`
[ ] `persistFocus` matches `DocumentPartition.documentRootForNode graph focusId` and sets `targetId` to the document root or `focusId` — `src/Server/GithubTransportActor.fs` — `persistFocus`
[ ] `persistFocus` calls `DocumentPersistChange.persistNode dataDir graph targetId` on the synchronous disk path — `src/Server/GithubTransportActor.fs` — `persistFocus`
[ ] `CoreMailboxLoop.applyNotice` matches `Notice.NodeParsed` to `CoreMailboxFilesAxis.applyNodeParsed` — `src/Server/Core/CoreMailboxLoop.fs` — `applyNotice`
[ ] `applyNodeParsed` calls `writeNotice` with `Notice.NodeParsed`, then `pushParsedUnpersisted` — `src/Server/Core/CoreMailboxFilesAxis.fs` — `applyNodeParsed`
[ ] `pushParsedUnpersisted` calls `persist.pushPersist nodeId` only when `parsedAndUnpersisted` — `src/Server/Core/CoreMailboxFilesAxis.fs` — `pushParsedUnpersisted`
[ ] `CoreEventDispatch.pushChangeSpecials` calls `markForPersist` per owning special, then `List.iter context.persist.pushPersist ids` on every `Order.Change` — `src/Server/Core/CoreEventDispatch.fs` — `pushChangeSpecials`
[ ] `PersistThread.runRequest` skips the write when `CoreEventDispatch.persistedNode` is true — `src/Server/Core/PersistThread.fs` — `runRequest`
[ ] `FileAgent` binds `pushPersist` to `PersistRequestStack.pushBoundNode loaded.stack` — `src/Server/Core/FileAgent.fs` — `handlersForLoaded`
[ ] `RouteRegistration` starts GitHub Save through `GithubTransportActor.start` with `request.focusId` — `src/Server/RouteRegistration.fs`
[ ] `CoreMailboxLoop.poolActor` builds `ActorChanges` through `CoreDoor.actorChanges` for every Peer Actor — `src/Server/Core/CoreMailboxLoop.fs` — `poolActor`

### Existing tests

[ ] ``git Save persists focus then commits pulls and pushes`` — `tests/Server.Tests/GithubTransportActorTests.fs` — Save with a File focus creates `note.txt`, runs Save, asserts `File.Exists` at commit time and the gate order `commit` → `pull` → `push`
[ ] ``git Save gates commit then releases before push`` — `tests/Server.Tests/GithubTransportActorTests.fs` — Save step order through a fake git gate
[ ] ``git Save stops when the pull fails and does not push`` — `tests/Server.Tests/GithubTransportActorTests.fs` — pull failure ends Save before push
[ ] ``plain Save with a GitHub remote pushes from a file focus`` — `tests/Server.Tests/LoadSaveCommandTests.fs` — routed plain Save from a File focus pushes the work tree
[ ] ``NodeParsed pushes a persist request`` — `tests/Server.Tests/CoreLoopNoticeTests.fs` — `applyNodeParsed` pushes Unparsed+Unpersisted and Parsed+Unpersisted nodes; it does not push Parsed+Persisted or `pullingFile` nodes
[ ] ``ParseFinished sets Parsed and leaves PersistState`` — `tests/Server.Tests/CoreLoopNoticeTests.fs` — `Notice.NodeParsed` sets Parsed without changing persist state
[ ] ``NodeParsed leaves the written node Persisted`` — `tests/Server.Tests/PersistThreadTests.fs` — `Notice.NodeParsed` then persist thread leaves the File Parsed and Persisted on disk
[ ] ``persist thread writes from the live graph`` — `tests/Server.Tests/PersistThreadTests.fs` — stack push runs `DocumentPersistChange.persistNode` asynchronously
[ ] ``repeated pushes write once`` — `tests/Server.Tests/PersistThreadTests.fs` — multiple stack pushes for one Unpersisted node collapse to one write while Persisted
[ ] ``An Edit pushes its owning special node`` — `tests/Server.Tests/CoreMailboxDoorTests.fs` — `pushChangeSpecials` records `pushPersist` for the File owner on Edit
[ ] ``An Edit marks its owning special node at the Edit's id`` — `tests/Server.Tests/CoreMailboxDoorTests.fs` — Edit marks Unpersisted and sets `changedVersion`

### Copy-from

[ ] `CoreMailboxFilesAxis.applyNodeParsed` — `src/Server/Core/CoreMailboxFilesAxis.fs` — posts `Notice.NodeParsed`, then enqueues through `pushPersist` when the node is Parsed and Unpersisted
[ ] `CoreEventDispatch.markForPersist` — `src/Server/Core/CoreEventDispatch.fs` — posts `Notice.NodeUnpersisted` when the node is Persisted or `changedVersion` is behind the Edit event id
[ ] `CoreMailboxFilesAxis.markUnparsedThenPush` — `src/Server/Core/CoreMailboxFilesAxis.fs` — direct axis write plus `pushPersist`; pattern for Load parse stack, not Save
[ ] `PersistThreadTests` local `applyPersisted` — `tests/Server.Tests/PersistThreadTests.fs` — wires `CoreMailboxLoop.applyNotice` to `PersistRequestStack.push` for integration-style persist tests
[ ] `DocumentPersistChange.persistNode` — `src/Server/DocumentPersistChange.fs` — remains the disk write `PersistThread` calls; Save should route through the stack to this function, not call it synchronously from the actor

### write-ticket

[ ] Source file `src/Server/GithubTransportActor.fs` — replace or extend `persistFocus` so stage 1 uses `focusId`, the parse-state transition, and `pushPersist`, then waits for persist before `commitPullPush`
[ ] Source file `src/Server/Core/CoreMailboxFilesAxis.fs` — likely home for a shared helper that force-marks Parsed and enqueues the focus when `pushParsedUnpersisted` would skip (Persisted clean node, or force path)
[ ] Source file `src/Server/Core/CoreDoor.fs` — optional seam if actors need a mailbox-round helper beyond raw `postNotice` (wait-for-persisted is not on `ActorChanges` today)
[ ] Test file `tests/Server.Tests/GithubTransportActorTests.fs` — extend ``git Save persists focus then commits pulls and pushes`` for stack-driven persist; add a case where the focus is Parsed and Persisted and Save still writes before commit
[ ] Test file `tests/Server.Tests/CoreLoopNoticeTests.fs` — add or extend a test at `CoreMailboxLoop.applyNotice` if a new force-enqueue helper lands in `CoreMailboxFilesAxis`
[ ] Test file `tests/Server.Tests/PersistThreadTests.fs` — optional pattern reuse for force enqueue plus `NodePersisted` wait semantics

### Invariants

[ ] Parse state is set by `GraphMutate.setParseState` — type `ParseState` — file `src/Shared/GraphMutate.fs` — invoked from `NoticeGraph.writeGraph` on `Notice.NodeParsed` and `Notice.NodeUnparsed` — file `src/Shared/NoticeGraph.fs`
[ ] The persistence stack receives a node through `CoreMailboxAccess.pushPersist` — type `NodeId -> unit` — implemented as `PersistRequestStack.pushBoundNode` — file `src/Server/Core/FileAgent.fs` — which calls `PersistRequestStack.push` — file `src/Server/Core/PersistRequestStack.fs`
[ ] Ordinary enqueue after parse uses `CoreMailboxFilesAxis.pushParsedUnpersisted` — condition `node.parseState = ParseState.Parsed && node.persistState = PersistState.Unpersisted` — file `src/Server/Core/CoreMailboxFilesAxis.fs` — leaves a Parsed Persisted node off the stack
[ ] Ordinary enqueue after Edit uses `CoreEventDispatch.markForPersist` — condition `needsUnpersistedMark`: skip the Unpersisted mark when the node is Unpersisted with `changedVersion` already at or past the Edit id — file `src/Server/Core/CoreEventDispatch.fs` — `pushChangeSpecials` still calls `pushPersist` for owning specials, but `PersistThread.runRequest` skips the write when `CoreEventDispatch.persistedNode` is true — file `src/Server/Core/PersistThread.fs`
[ ] A previously Unparsed focus with `PersistState.Unpersisted` can become Parsed via `Notice.NodeParsed` and then enqueue through `applyNodeParsed` — same files as above — a previously Unparsed focus with `PersistState.Persisted` (`Notice.NodeReceived` shape) gets Parsed from the notice but does not enqueue until Unpersisted
[ ] Today's Save persist step is `GithubTransportActor.persistFocus` — file `src/Server/GithubTransportActor.fs` — function `persistFocus` — it writes `targetId` from `DocumentPartition.documentRootForNode`, not always `focusId`, via synchronous `DocumentPersistChange.persistNode`
[ ] Conflict when focus is a Normal node inside a document: `GraphMutate.setParseState` — file `src/Shared/GraphMutate.fs` — `artifactNode` returns Error `"normal nodes do not have parse or persist state"` — marking Parsed and enqueueing `focusId` contradicts that invariant; substituting `documentRootForNode` avoids the error but contradicts this slice
[ ] Conflict when focus has `pullingFile` set: `GraphMutate.setParseState` — file `src/Shared/GraphMutate.fs` — setting `ParseState.Parsed` is a no-op while `pullingFile` is true — force Parsed on that focus does not change parse state
[ ] Seam when focus is a Workspace, Directory, or File artifact: extend persist-focus to post `Notice.NodeUnpersisted` when `persistedNode` is true, then `Notice.NodeParsed`, then `pushPersist` on `focusId` (or a shared helper with that behavior), and wait for `Notice.NodePersisted` before git commit — actors already hold `ActorChanges.postNotice` — file `src/Shared/History.fs`
[ ] Public test seam for the Parsed transition: `CoreMailboxLoop.applyNotice` with `Notice.NodeParsed` — exercised in `tests/Server.Tests/CoreLoopNoticeTests.fs` — ``NodeParsed pushes a persist request``
[ ] Public test seam for stack enqueue and disk write: `PersistRequestStack.push` plus `PersistThread.start` — exercised in `tests/Server.Tests/PersistThreadTests.fs` — ``NodeParsed leaves the written node Persisted`` and ``persist thread writes from the live graph``
[ ] Public test seam for GitHub Save stage 1: `GithubTransportActor.start` with `GithubTransportOperation.Save` — exercised in `tests/Server.Tests/GithubTransportActorTests.fs` — ``git Save persists focus then commits pulls and pushes``
