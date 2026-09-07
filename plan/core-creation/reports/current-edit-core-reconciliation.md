# Current edit and Core reconciliation

Date: 2026-09-05

Scope: Browser node-text edit through Server acknowledgement in Persistence:Mode=db and Persistence:Mode=file. Proposed behavior belongs to [[plan/core-creation/project.md]] and is analyzed in [[kernel-fsproj.md]]. Roadmap sequencing: [[plan/roadmap/epics/chapters/initial-core.md]] and [[plan/roadmap/epics/chapters/acid-apply.md]].

## Executive answer

The claim “Core puts the Change into its Graph and database, then queues persistence on the associated file” is false for the current code and is not a defined proposed sequence.

- There is no Core or Core API implementation. FileAgent and DbAgent have separate copies of **applyBatch** and **handlePostChange**.
- The Server first computes a tentative State. It does not publish that State to the in-memory Graph at this point.
- In db mode, the Server writes affected document files synchronously before the PostgreSQL transaction. It then commits ChangeLog plus projection, publishes the in-memory State, replies, and starts a coalesced asynchronous snapshot task.
- In file mode, the Server writes affected document files synchronously, writes the revision checkpoint, appends and flushes the file ChangeLog, publishes the in-memory State, and replies. There is no file snapshot queue.
- The mailbox is the sequencing queue. The immediate document write runs on a Task, but the mailbox waits for it. A timeout abandons the Task; the Task can continue to write after the Server rejects the request.
- The db-mode snapshot task is after acknowledgement and is not the immediate persistence of the affected document. The immediate write already occurred before the database commit.

The planned db-authority sequence is: HTTP Adapter decodes JSON to Change objects; Core Changes sequences the request; Core applies, amends, and validates against the current State; the PostgreSQL Adapter commits the amended ChangeLog rows and resulting projection; Core publishes the committed Graph; Core enqueues asynchronous persistence of the affected individual document files; then Core acknowledges. A file write cannot be part of one PostgreSQL ACID transaction.

## Browser sequence

1. Text entry changes only the DOM. [[src/Client/RowView.fs]] **makeRow** creates contentEditable div#edit-input. There is no Change for each input event.
2. A commit command reads textContent. Escape, navigation, row selection, and a click outside interactive chrome can call **commitIfEditing** or **commitToSelectingOp**. Evidence: [[src/Client/UpdateOps.fs]] **handleEsc** and **commitToSelectingOp**; [[src/Client/App.fs]] click handling at lines 737-747.
3. [[src/Client/UpdateHelpers.fs]] **commitTextEdit** compares the DOM text with the Browser Graph. It creates one Change with one SetText Op, the current acknowledged Revision as Change.id, and a new changeId UUID.
4. [[src/Client/UpdateHelpers.fs]] **applyAndPost** calls [[src/Shared/SyncLogic.fs]] **applyLocalChange**. That function uses [[src/Shared/ResidentProjection.fs]] **applyChange**. SetText calls the shared Op logic for a Resident Node. [[src/Shared/GraphMutate.fs]] **setText** checks the old text, changes the text, and touches updateTime. This is the optimistic Browser Graph update. Browser History records it. The Browser Revision does not advance yet.
5. [[src/Shared/SyncPlanner.fs]] **enqueuePending** appends the Change to the pending list. It emits SavePendingQueue first. It emits SubmitPendingBatch only when Sync is not busy or blocked.
6. [[src/Client/App.fs]] **dispatch** publishes and renders the optimistic Browser model before it runs effects. It saves the pending queue, then starts the POST.
7. [[src/Shared/SyncBatch.fs]] **toWireBatch** rewrites Change.id values as a contiguous chain from the batch base Revision. [[src/Client/UpdateCodec.fs]] **encodePendingBatchBody** encodes the batch. [[src/Client/App.fs]] **runSubmitPendingBatch** posts it asynchronously to /ambit/changes and starts a Browser timeout.

## HTTP Adapter and decode location

[[src/Server/RouteRegistration.fs]] **registerStateRoutes** authenticates POST /ambit/changes, reads the complete body as a string, selects an AgentHandle, and calls [[src/Server/Api.fs]] **postChange**.

The HTTP Adapter does not decode the request Change JSON. Api.postChange also does not decode it. AgentHandle passes the same string to **PostAndAsyncReply**. FileAgent or DbAgent decodes **Serialization.decodeChangeBatch** inside **handlePostChange**, after the mailbox dequeues the message.

Api.postChange decodes only the successful agent acknowledgement. It adds deployment, page, API, and ready fields and encodes the HTTP response again.

Thus the previous statement in [[kernel-fsproj.md]] that “/ambit/changes decodes a body, then posts a JSON string into the agent mailbox” was wrong. The proposed “HTTP Adapter decodes, then calls Core API” is a valid target, but it is not current behavior.

## Mailbox, apply, amendment, and validation

Both agents use a MailboxProcessor of FileAgentMsg. POST uses PostAndAsyncReply. A mailbox handles one dequeued message at a time. Its immediate Task.Run work blocks mailbox progress until completion or timeout.

For each Change in a batch, **applyBatch** first checks changeId deduplication. FileAgent searches SYSTEM/gambol.log. DbAgent queries PostgreSQL. A duplicate returns the stored Change as confirmation and does not apply it again.

For a new Change, [[src/Shared/ChangeAmendment.fs]] **applyChange** first calls [[src/Shared/History.fs]] **applyChange**. Ops apply in order. History.applyChange validates ownership after a changed shape Op batch. A plain SetText still has the SetText checks in GraphMutate.

For a stale SetText old-value comparison, ChangeAmendment does not overwrite the current text. It can replace the failed Op with Ops that add an amb-conflict child containing the posted text. It applies the amended Change again. The agent rejects Invalid and Unchanged results. A changed result gets the next Server Revision. The amended Change, not the submitted Change, becomes the fresh confirmation and log entry. externalChanges becomes true.

The resulting State is still tentative. For a normal post with DataDir file effects, the agent next runs [[src/Server/DocumentPersistence.fs]] **validatePathMoves** and **validateGraphDiskEffects**. These reject unavailable move destinations and new destinations ignored by git. Graph-only posts skip these checks.

## Affected document selection and file write

For the text edit, [[src/Shared/DocumentOpImpact.fs]] **documentRootsAffectedByOps** marks the SetText Node as touched. [[src/Shared/DocumentPartition.fs]] **documentRootsAffectedByNodeIds** finds containing document roots in both the pre-Graph and post-Graph. If the touched Node is itself a nested document root, it also includes the parent document root. It then keeps post-Graph Workspace, Directory, and File roots that are Current and whose Node name is not .amb.

[[src/Server/DocumentPersistence.fs]] **persistGraphOps** first plans and executes artifact path moves. It then calls **writeDocumentsSoft** for the selected roots. **writeDocument** reads prior text, runs DocumentWarm.writeArtifact, initializes a Workspace git repository when needed, writes a .tmp file, and replaces the target file. It stamps document-root updateTime from file mtime. The agent appends resulting SetUpdateTime Ops to the last fresh Change.

A document compute or write failure is soft. The Server can still accept the Change and return a “file couldn't save” message. A path-move failure is hard. Thus “accepted” does not always mean that the document file contains the Change.

## Persistence:Mode=file

When PostgreSQL is absent or the file and database states do not match, the primary path is FileAgent only.

The order for a new text Change is:

1. Dequeue and decode JSON.
2. Deduplicate, apply or amend, increment the tentative Revision, and validate.
3. Run affected-document persistence synchronously with an eight-second bound.
4. On a clean file result, atomically replace SYSTEM/gambol.meta with the new Revision. On a soft file failure, keep the checkpoint behind.
5. Append amended and stamped fresh Changes to SYSTEM/gambol.log and flush it.
6. Update the in-memory State reference.
7. Encode and reply with confirmations, Revision, externalChanges, and an optional file message.

There is no snapshot work after this reply. [[src/Server/FileAgent.fs]] **flushSnapshot** is a no-op.

If PostgreSQL is available and matches at startup, [[src/Server/RouteRegistration.fs]] selects [[src/Server/Api.fs]] **ofFileWithDbMirror**. It first completes all FileAgent steps. It then sends the original request JSON, not the FileAgent amended confirmation, to DbAgent and waits for that result. A database failure is only logged; the HTTP response still uses the FileAgent acknowledgement. This mirror is synchronous but is not atomic with file persistence.

Therefore current file mode is write-authoritative. This directly conflicts with the proposed “file mode is view-only” rule.

## Persistence:Mode=db

With a healthy database, RouteRegistration selects DbAgent created with DataDir live-save enabled.

The order for a new text Change is:

1. Dequeue and decode JSON.
2. Query PostgreSQL for changeId deduplication.
3. Apply or amend to a tentative State and validate DataDir effects.
4. Run affected-document persistence synchronously with the same eight-second bound.
5. Add file-mtime stamp Ops to the fresh amended Changes and stamped tentative State.
6. Open a PostgreSQL connection and transaction.
7. Insert each fresh Change in changes with client base Revision, UUID, Server Revision, and encoded amended payload. Evidence: [[src/Server/Database.fs]] **appendChangeWithTx**.
8. [[src/Server/DatabaseProjection.fs]] **plan** selects Node upserts from all touched Op Node IDs and child-list replacements from Replace parent IDs. **persistWithTx** applies that patch, or initializes the full projection.
9. Commit the transaction.
10. Publish state.Value.
11. Reply with the acknowledgement.
12. Set persistedGraph to the accepted Graph. Start, or coalesce, an asynchronous **startSnapshot** task.

The PostgreSQL ChangeLog and normalized projection are in one transaction. The apply or amendment computation, document file write, and in-memory publication are not in that transaction. A PostgreSQL failure after the file write leaves a file effect from a rejected Change.

The legacy function named **startSnapshot** uses [[src/Server/DocumentPersistence.fs]] **persistGraphChange**, which computes affected roots from a full Graph difference and writes individual files. It does not write an abandoned Gambol snapshot design. snapshotNeeded is one coalescing Boolean, not the planned per-file persistence queue. For the normal immediate-live-save path, DbAgent sets persistedGraph to the accepted Graph before it starts this task, so the first pre/post difference is normally empty. Treat **DbAgent.startSnapshot** as leftover naming and code, not as the future persistence design.

If db mode cannot get a healthy database, RouteRegistration exposes a read-only FileAgent handle. It rejects postChange. It does not silently accept the text Change in file mode.

## Acknowledgement and Browser reconciliation

The Server acknowledgement is after the primary persistence steps above. [[src/Client/App.fs]] **onPostOk** decodes it and dispatches SubmitResponse.

For an unchanged confirmation prefix plus allowed stamp suffix Ops, [[src/Shared/SyncLogic.fs]] **reconcileAck** applies only the stamp suffix to the already optimistic Graph, sets the acknowledged Revision, removes the submitted queue prefix, saves the queue, and submits any remaining Changes.

For an amended confirmation, externalChanges is true. **reconcileExternalAck** records a baseline that removes optimistic pending Changes, retires the submitted prefix, and starts Poll after the queue drains. Poll then supplies the authoritative amended Change. The Browser rewinds to the baseline and replays it. The POST acknowledgement does not directly replace the optimistic text with the conflict result.

A network timeout can cause retry while Server work continues. changeId deduplication makes the retry idempotent when the first attempt completed.

## Timeout and atomicity defects

[[src/Server/FileAgent.fs]] **runBounded** starts synchronous work with Task.Run and waits for eight seconds. On timeout it returns Error and abandons the Task. The Task can continue.

- A timed-out document task can write files after the mailbox rejects the Change and processes later Changes.
- A timed-out DbAgent persistBatch task can later acquire a lock and commit PostgreSQL after the mailbox rejected the Change. The in-memory State is not then published from that task.
- File mode has separate document, revision-checkpoint, and file-log writes. They are not one commit.
- db mode has document writes before its PostgreSQL commit. Files and PostgreSQL are not one commit.

These are the exact defects that [[plan/roadmap/epics/chapters/acid-apply.md]] must remove.

## Reconciliation with the proposed Core

The proposed boundary is useful: HTTP outside Core; Change objects enter one inner apply; Shared apply/amend remains usable by the Browser; one Core-owned mailbox is the only Server Graph writer; persist algorithms work through a port. [[plan/core-creation/project.md]] owns this design and implementation.

The documents have these contradictions or missing decisions:

1. **Decode boundary:** [[kernel-fsproj.md]] describes the corrected target: the future Adapter decodes before Core. The extract must change the message type from JSON string to Change objects to meet that requirement.
2. **Initial versus later file mode:** [[plan/roadmap/epics/chapters/acid-apply.md]] says file mode becomes view-only in the later ACID Chapter. The Core Project must keep that dependency explicit.
3. **Upload versus view-only Files:** [[kernel-fsproj.md]] says Files send stores uploaded bytes, but also says file mode refuses or does not offer open-for-write. The Project must state whether “view-only” applies only to Graph document materialization or to every Files send.
4. **Meaning of one commit:** Amendment is a computation. The atomic database statement is: one transaction stores the amended ChangeLog rows and their resulting projection.
5. **File acceptance boundary:** The planned answer is a derived post-commit queue that persists affected individual files asynchronously. Core enqueues this work before acknowledgement, but file completion does not gate Change acceptance. Retry or replay is not specified.
6. **Current mirror is omitted:** file mode can synchronously mirror the original JSON body to DbAgent after FileAgent acceptance. It is best effort and can produce a different amendment if the two States differ. It is not the proposed single-writer Core.
7. **Snapshot name can mislead:** DbAgent has an asynchronous function named startSnapshot, but current immediate affected-document writes occur before database commit and acknowledgement. It is leftover naming and code, not the planned queue.

## Minimal corrected conceptual sequence

For the planned db authority:

1. Browser applies a committed text Change optimistically and queues it.
2. HTTP Adapter authenticates and decodes JSON into Change objects.
3. Core Changes enqueues those objects in the sole apply mailbox.
4. Core deduplicates, applies or amends, validates, and computes a tentative State plus durable facts.
5. The PostgreSQL Adapter commits amended ChangeLog rows and the resulting projection in one transaction.
6. Core publishes the committed Graph.
7. Core enqueues affected individual document files for asynchronous persistence. The queue needs durable progress or safe replay.
8. Core returns the produced Change sequence and Revision. The HTTP Adapter encodes the acknowledgement.

For proposed view-only file mode, step 5 has no write authority and Core must reject Changes. It must not run today’s FileAgent write path.

This sequence changes the claim to: “Core tentatively applies the Change, commits the amended ChangeLog and Graph projection, publishes the committed Graph, enqueues affected individual document files for asynchronous persistence, and then acknowledges.” This is not current behavior. Retry or replay for queued file writes remains unspecified.
