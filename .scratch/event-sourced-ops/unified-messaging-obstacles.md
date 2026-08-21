# Unified messaging — obstacles

Worker report. Later: success envelope **accepted**; slice 2 obsolete for recoverable kick-back ([[slice-2-obsoleted.md]]). Home: [[unified-messaging.md]]. This file stays the obstacle analysis (no hard block).

## Verdict

**No hard obstacle** to making ACK and Poll share one Change-list success envelope (POST sends Changes; Poll does not). The design is not false. Today's Server already has `getChangesSince` (Poll's list). A success ACK can be that same tail after POST.

What exists today is a **different kind of ACK list** plus a **different apply function**. Those are contract/migration. Two **naive** implementations would be unsafe or would falsify Undo — they are traps, not reasons the envelope cannot be real.

## Hard obstacles

**None** to the envelope itself, given accepted rewind+replay ([[merge.md#Client correction]]).

Traps — **answered** ([[post-ack-history.md]]):

- **Apply the ACK list on top of the optimistic Local Graph.** User: rewind, then apply the response Change list. Accepted. Do not apply onto unamended optimistic state.
- **Reuse Poll's History-clear on POST.** **Resolved:** neither POST nor Poll clears History. Poll = empty POST. Today's Poll clear is debt.

The global revision gate (`change.id <> server revision` → Error in [[src/Server/FileAgent.fs]] `applyBatch`) means a success ACK **today** cannot contain other Actors' Changes: intervening commits reject the POST. That does not make the envelope false. On success, `getChangesSince(posted base)` is your new Changes (plus stamps). When the gate drops / amendment lands, that same tail grows to others + amended own. Reject stays a different status.

## Contract / migration obstacles (expected)

These make today's tests and specs fail until changed. They do not make the design unsafe.

- **undo-spec items 5, 6, 9** ([[.scratch/selective-client-loading/undo-spec.md]]): ACK is one confirmation per submitted Change; submitted Ops are an exact prefix; suffix is `SetUpdateTime` only; changed-prefix / forbidden-suffix / unmatched identity → reload. An amended Change or extra other-Actor Changes **break** that contract on purpose.
- **`reconcileAck`** ([[src/Shared/SyncLogic.fs]]): `identityError` requires equal length and the same `changeId` order; `takeSuffix` allows only stamp Ops. Tests in [[tests/Shared.Tests/AckReconcileTests.fs]] lock that. Unified ACK cannot keep this as the success path.
- **Server ACK construction** ([[src/Server/FileAgent.fs]]): `ackChanges` are confirmations (`overlayFresh` + stamp Ops), not `GetChangesSince`. Poll is `getPoll` → `getChangesSince` ([[src/Server/Api.fs]]). Unify by returning the log tail after POST.
- **Envelope fields:** `PollResponse` has deploy/page stamps and `isReady`. `ChangeBatchAck` has optional persist `message`. Types both have `changes`. Sharing **kind** may still grow fields. Not locked.
- **Client submit path** ([[src/Client/Update.fs]] `applySubmitResponse`) vs Poll (`applyServerTail`). One receive/apply path means submit stops calling `reconcileAck` for success.
- **Duplicate retry** (undo-spec 15): stored echo vs a fresh `getChangesSince` tail. Unified retry should return the current tail (lost ACK can include later others). Contract change.

## Not obstacles (success envelope)

- **Reject** (auth, malformed POST, similar request failures). Not the success list. Do not require remote-changes on 400 to lock ACK≈Poll on 200.
- **Load packages.** Graph transfer. Parked. Not this envelope.
- **`SetUpdateTime` as Ops in the list.** They can ride in the Change list. History-neutral stamp practice is a fact, not a block.
- **Revision remaining one number.** Parked. Poll already uses `rev=N` + response revision as the list bounds.

## Files read

- [[unified-messaging.md]], [[rewind-replay-unified-msg.md]], [[merge.md]], [[undo.md]], [[poll-load-conveyance.md]]
- [[.scratch/selective-client-loading/undo-spec.md]] items 5–12, 15
- [[.scratch/relaxed-concurrency/map.md]] slice 2
- [[src/Shared/SyncLogic.fs]] `applySyncResponse`, `applyServerTail`, `reconcileAck`
- [[src/Shared/ApiResponses.fs]] `PollResponse`; [[src/Shared/Serialization.fs]] `ChangeBatchAck`
- [[src/Server/Api.fs]] `getPoll`; [[src/Server/FileAgent.fs]] `applyBatch` / ACK
- [[src/Client/Update.fs]] submit vs Poll
- [[tests/Shared.Tests/AckReconcileTests.fs]]

## Files changed

- [[unified-messaging.md]] — pointer to this report; still proposed
- This file

No software. No lock. Stage still `charting`. No [[WORK.md]] edit here.

## WORK.md mutations

Update the Active [[project.md]] related list: add [[unified-messaging-obstacles.md]] (no hard obstacle; stay proposed). No `add` / `move` / `block` / `remove`.

## Open questions

1. Poll History-clear **resolved** (neither clears; Poll = empty POST).
2. Does POST return `PollResponse` (plus optional persist message) or keep `ChangeBatchAck` with the same Change-list kind?
