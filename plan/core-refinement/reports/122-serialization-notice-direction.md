# 122 — Document state moves by Notice — serialization direction

Ticket: [122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md).

## Direction

`Serialization.encodeOp` and `Serialization.decodeOp` are one codec used in both directions: the wire format for an Op leaving toward the mailbox, and the wire format for an Op arriving from the mailbox.

The codec converts an `Ev` to JSON and back. The mailbox post is the later in-memory call.

## Toward the mailbox

The client encodes a Change. The server decodes that JSON. The door then posts the `Ev`.

- `src/Client/UpdateCodec.fs` `encodePendingBatchBody` (callers: `src/Client/App.fs`, `src/Client/UpdateWorkspaceSync.fs`)
- `src/Shared/ApiResponseSerialization.fs` `encodeChangeRequest`
- `src/Shared/EventJson.fs` `encode`
- `src/Shared/EventJson.fs` `encodeBody`
- `src/Shared/EventJson.fs` `encodeOps`
- `src/Shared/Serialization.fs` `encodeOp` (`Op.SetDocumentState`)

The same encode chain also starts at:

- `src/Shared/dotnet/AmbitSession.fs` `encodeBatch`, then `EventJson.encodeEventBatch`
- `src/Client/UpdateHelpers.fs` `savePendingQueue`
- `src/Shared/BootCache.fs` `encodeEvent`

Server decode, then the post:

- `src/Server/Api.fs` `postEvents`
- `src/Shared/ApiResponseSerialization.fs` `decodeChangeRequestDecoder`
- `src/Shared/EventJson.fs` `decode`
- `src/Shared/EventJson.fs` `decodeBody`
- `src/Shared/EventJson.fs` `decodeChangeBody` (Undo and Redo use `decodeUndoBody` and `decodeRedoBody`)
- `src/Shared/EventJson.fs` `decodeOps`
- `src/Shared/Serialization.fs` `decodeOp` (`Op.SetDocumentState`)
- `src/Server/Core/CoreDoor.fs` `postEvents`
- `src/Server/Core/CoreMailboxApi.fs` `postEvents`
- `src/Server/Core/CoreMailboxApi.fs` `postedMailbox`

`postedMailbox` places `Order.Change` or posts `PostEvent`. That call is the mailbox post.

## From the mailbox

The mailbox stores the `Ev`. The log and the HTTP response then encode it. Readers decode it.

Log write for a Change and for a Notice:

- `src/Server/Core/CoreEventDispatch.fs` `appendNew` calls `persist.appendEvent`
- `src/Server/Core/FileAgent.fs` `persistHandlers` field `appendEvent` calls `EventLogFile.appendEvent`
- `src/Server/Core/DbAgent.fs` `appendPersistedEvent` calls `writePersistedEvent`
- `src/Server/EventLogFile.fs` `encodeEvent`
- `src/Server/EventLogFile.fs` `appendEvent`
- `src/Shared/EventJson.fs` `encode`, then `encodeOps`, then `Serialization.encodeOp`

A Notice reaches that append from:

- `src/Server/Core/CoreMailboxApi.fs` `postNotice` (in-memory `CoreQueue.Notice`)
- `src/Server/Core/CoreMailboxLoop.fs` `dispatchItem`
- `src/Server/Core/CoreEventDispatch.fs` `logNotice`

HTTP response, events taken from the mailbox log:

- `src/Server/Api.fs` `changeSuccessResult` (used by `acceptedChangeResult` inside `postEvents`)
- `src/Server/Api.fs` `postLoad`
- `src/Shared/ApiResponseSerialization.fs` `encodeChangeSuccessResponse`
- `src/Shared/ApiResponseSerialization.fs` `encodeLoadResponse`
- `src/Shared/ApiResponseSerialization.fs` `encodeUniversalResponse` (field inside `encodeLoadSaveCommandResponse`)
- `src/Shared/EventJson.fs` `encode`

Decode of those events:

- `src/Client/UpdateCodec.fs` `decodeChangeSuccessResponse`
- `src/Shared/ApiResponseSerialization.fs` `decodeChangeSuccessResponseDecoder`
- `src/Shared/ApiResponseSerialization.fs` `decodeLoadResponseDecoder`
- `src/Shared/ApiResponseSerialization.fs` `decodeUniversalResponseDecoder`
- `src/Client/UpdateHelpers.fs` `loadPendingQueue`
- `src/Shared/BootCache.fs` `decodeEvent`
- `src/Server/Core/DbAgent.fs` `loadReconciled`
- `src/Server/Core/FileAgent.fs` `openLoaded`
- `src/Server/EventLogFile.fs` `decodeEvent`
- `src/Server/EventLogFile.fs` `readAllEvents`
- `src/Shared/EventJson.fs` `decode`, then `decodeOps`, then `Serialization.decodeOp`

## Matching Notice codec

The document-state Notices already encode and decode on this same `EventJson.encode` / `EventJson.decode` path. They travel as `EventBody.Notice`, beside the Op list.

Encode:

- `src/Shared/EventJson.fs` `encodeBody`
- `src/Shared/EventJson.fs` `encodeNotice`
- `src/Shared/EventJson.fs` `encodeSingleNotice`

Cases and tags:

- `Notice.NodeUploading` — `nodeUploading`
- `Notice.NodeReceived` — `nodeReceived`
- `Notice.MarkUnparsed` — `markUnparsed`
- `Notice.NodeParsed` — `nodeParsed`

Decode:

- `src/Shared/EventJson.fs` `decodeBody`
- `src/Shared/EventJson.fs` `decodeNoticeBody`
- `src/Shared/EventJson.fs` `decodeSingleNotice`
- `src/Shared/EventJson.fs` `noticeFrom`

Same four tags.

`src/Shared/DocumentOpImpact.fs` `noticesBeside` pairs `SetDocumentState` with those four Notices. `src/Shared/History.fs` `documentStateFromNotice` maps them to PullingFile, Unparsed, Unparsed, and Current. `src/Shared/History.fs` `applyNotice` writes that state through `src/Shared/GraphMutate.fs` `setDocumentState`. `Op.SetDocumentState` uses that same function. The old-state argument is unused. A node already in the new state returns the same graph, so apply stays idempotent. `SetDocumentState` stays in `encodeOp` and `decodeOp`.

Persist notices share this codec and leave document state unchanged (`documentStateFromNotice` returns no state):

- `Notice.NodeParsing` — `encodeSingleNotice`, `noticeFrom`, tag `nodeParsing`
- `Notice.NodePersisting` — `encodeSingleNotice`, `noticeFrom`, tag `nodePersisting`
- `Notice.PersistFailed` — `encodePersistFailed`, `decodePersistFailed`, tag `persistFailed`
- `Notice.NodePersisted` — `encodeNodePersisted`, `decodeNodePersisted`, tag `nodePersisted` (legacy tag `snapshotDone` decodes as `NodePersisted`)

`tests/Shared.Tests/EventJsonTests.fs` `Notice JSON round trip` round-trips all eight notices.

## Desktop import caller

`Serialization.encodeDesktopImportPackage` and `Serialization.decodeDesktopImportPackage` also call `encodeOp` and `decodeOp`.

- `src/Server/Api.fs` `getImportFile`
- `src/Desktop/LocalProxy.fs` `handleImportGet` (encode at the import JSON write)
- `src/Client/UpdateCodec.fs` `decodeDesktopImportPackage`

That payload is an import package. The mailbox event codec is `EventJson`, and that codec already carries the matching Notices.

## Change

Source stays as read.

## Tests

Tests stayed unrun. No source file changed. Section 6 of 122 — Document state moves by Notice requires no new test.
