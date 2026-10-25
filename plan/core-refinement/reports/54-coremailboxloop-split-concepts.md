# 54 — CoreMailboxLoop split concepts

Branch: dev.

The mailbox loop keeps the pump. The pump receives one queue item and dispatches a behavior. A split of the rest follows three concepts. Paste is a Change.

## 1 — Pump

The pump receives, gates, and dispatches. The three concepts below are the behaviors.

- Receive and start: `pump`, `start`, `startNow`, `startWithPrelude`, `runUntil`, `isStartupRead`, `armMailbox`, `started`, `makeMailBox`, `seedEventLog`, `failedPersist`, `failedSeed`
- Gate and dispatch: `dispatch`, `dispatchMailbox`, `dispatchItem`, `runMsg`, `reportDispatchException`, `reportNotice`, `authorityHeld`, `messageCaller`, `rejectDropped`, `placeOrder`, `applyNotice`
- Query: `runQuery`, `dispatchAsk`, `queryOperation`, `replyQueryFailure`

`placeOrder` routes `Order.Load` to Load and an Edit Change to Change. `applyNotice` routes each Notice to Load with Parse or to Persist. Query stays on the pump. It answers State, LastEventId, and EventsSince.

## 2 — Load with Parse

Load with Parse is the Load-stage axis work in this module. The Load order door is `replyLoad` and `dispatchStartLoadSaveCommand`. This concept is the notices and the push those doors start.

- `writeParseState`, `markUnparsedThenPush`, `withPullingFile`, `writePullingFileUnpersisted`, `writeReceived`, `parsedAndUnpersisted`, `pushParsedUnpersisted`, `applyNodeParsed`, `reportPushError`, `pushReporting`
- Notice arms: `NodeParsed`, `NodeUploading`, `NodeReceived`, `NodeParsing`, `MarkUnparsed`

One concept: Upload and Parse are one Load stage. `NodeUploading` sets PullingFile and Unpersisted. `NodeReceived` sets Unparsed and Persisted. `MarkUnparsed` and `NodeParsed` write parse state. The queue puller logs `NodeParsing`, and that arm returns Ok. `markUnparsedThenPush` is the push Load calls. `pushParsedUnpersisted` asks Persist to run when the node is Parsed and Unpersisted.

## 3 — Persist

Persist is Save. It writes persist state for a save attempt.

- `writeUnpersisted`, `stampUpdateTime`, `withPersistedVersion`, `writePersisted`
- Notice arms: `NodePersisting`, `PersistFailed`, `NodePersisted`

One concept: `NodePersisting` writes Unpersisted. `NodePersisted` stores `persistedVersion`, stamps update time, and sets Persisted when the written version is current. An older write stays Unpersisted. `PersistFailed` calls `noteWriteFailed`. `writeUnpersisted` also serves `NodeUploading`. That call belongs to Load with Parse.

## 4 — Change

Change is Edit. Paste is the same Change.

- `editEvent`, `replyEdit`

One concept: `replyEdit` posts one Event. `editEvent` sets the order to Change with origin Edit. Paste arrives as that origin. `placeOrder` answers a Parse origin with the unsupported reply.
