# Synchronous startActor, one mailbox

Date: 2026-09-14. No commit.

## 1. What changed

### Architecture then ticket

1. **[[plan/core-creation/arch.md]]** — Updated 2026-09-14. Point 0 loop code is shared; two processors and a synchronized table are not the locked shape. Story hops 4–5, CoreMsg StartActor/GetState, mailbox-owned live table, CoreRuntime one host File or Db, PersistHandlers File/Db not Actor mailboxes, seams and Chosen/Rejected alternatives aligned. Mirror wording removed (persist mode is File or Db only).
2. **[[plan/core-creation/issues/34-outside-core-lifecycle-proof.md]]** — Section 1 item 1: validate, call startActor synchronously, reply; do not wait for the Actor body. Section 3 item 5: table is the only registry; access is mailbox-owned; not a second copy of identity/secret/Focus.

### Ownership result

Callers → one CoreMsg mailbox → File or Db PersistHandlers. The mailbox owns fast messages and live-table access. File and Db fill persist only. They do not take the pool, do not dispatch StartActor / ActorStop, and do not start Actor-capable processors. CoreRuntime registers nothing this cut, then starts one host. Persist mode chooses File or Db. CoreCredentials stays its own mailbox.

### Code

1. **[[src/Server/Core/CoreActorPool.fs]]** — `startActor: StartActorRequest -> Result<unit, string>`. Live table is ordinary mailbox-owned mutable state. `SynchronizedTable`, `withLocks`, `lockedIds`, and `model.locked` are gone. `liveFocusIds` is derived from live rows.
2. **[[src/Server/Core/CoreMailboxBackend.fs]]** — `dispatchStartActor` admits, calls startActor, replies. No `Async.Start` of startActor. GetState stamps `lockPresent` from the live table on the mailbox thread.
3. **[[src/Server/Core/FileAgent.fs]]** / **[[src/Server/Core/DbAgent.fs]]** — Persist fillings only (handlers, flush, ready, dispose). No pool, no mailbox start. `createLoaded` helpers live above `createLoaded`. `handleSnapshotDone loaded persisted` has no `LoadedPersist` annotation.
4. **[[src/Server/Core/CoreMailbox.fs]]** — `hostFile` / `hostDb` start the one `MailboxProcessor<CoreMsg>`. `startFileWithActors` / `startDbWithActors` deleted.
5. **[[src/Server/Core/CoreRuntime.fs]]** — One host. File or Db. `ofFileWithDbMirror` deleted.
6. **[[src/Server/DatabaseSetup.fs]]** — `getOrCreateDbHost` and the Db host cache deleted. `resetAgentCacheForTest` is a no-op so existing tests still call it.
7. Tests — recording pool is sync `Result`; StartActor wait-for-startActor test replaced by body-not-waited; GetState lockPresent after startActor; File/Db tests host through CoreMailbox.

## 2. Smaller / simpler

Yes at the composition layer.

Deleted:

1. Two `MailboxProcessor<CoreMsg>` hosts (2 → 1)
2. `SynchronizedTable` and its lock
3. `withLocks` / `lockedIds` / caller-thread overlay
4. `ofFileWithDbMirror` and File+Db dual persist
5. `startFileWithActors` / `startDbWithActors` / `FileAgent.MailboxStarter`
6. `getOrCreateDbHost` cache
7. `Async.Start` of startActor

`LoadedPersist` groups the refs `createLoaded` already closed over so helpers can sit above it. That is not a second registry or a mirror adapter. DbAgent public accessors use `*Of` names to avoid a RequireQualifiedAccess field clash. No dual-path compatibility layer.

Declaration edits on DbAgent/FileAgent/Core* were stopped as asked. After that stop, only compiler-forced parameter annotations were added (`ProjectionMaintenanceResult`, `(int * Change) list`, `MailboxProcessor<CoreMsg>`, `PersistGraphOk option`, `Change list`, `Revision`). `handleSnapshotDone` was not annotated with `LoadedPersist`.

## 3. Tests

Filter `FullyQualifiedName~CoreMsgActorCasesTests|FullyQualifiedName~CoreActorPoolTests|FullyQualifiedName~CredentialedChangePostsTests|FullyQualifiedName~CoreRuntimeTests`.

**23 passed / 0 failed / 0 skipped.**

Server.Tests compiled, so File/Db agent test modules typecheck. Those suites were not executed this run.

## 4. Remaining follow-ups

1. Issue 34 sections 2–7 — CoreMailbox doors, expand/select/create, ActorStarted, TestActor `hello`, harness.
2. Later startActor that expands Graph or appends ActorStarted must use in-loop persist/History, not `host.mailbox.PostAndAsyncReply` (deadlock). Add that test when those sections land.
3. TestActor register at CoreRuntime startup (section 6) still open; register-before-start is the composition rule.

## 5. Arch drift

None of the 16 deltas were unmappable. The file still used Story paths / Module map / Seams. Interface numbers were kept (GetState added as CoreMsg item 6; CoreRuntime File/Db-not-pool added as item 3). Item 8 on CoreActorPool was rewritten in place (launch/query/lock helpers gone) rather than renumbered.

## 6. Mirror deleted

1. `CoreRuntime.ofFileWithDbMirror`
2. File+Ok second MailboxHost / `getOrCreateDbHost`
3. Any File+Db persist composition inside one loop (not relocated; removed)
4. Arch/issue wording that kept a mirror mode

File mode with a live DB no longer dual-writes. Persist mode is File or Db.

## 7. createLoaded helper lift

Helpers are private lets above `createLoaded`. `createLoaded` allocates `LoadedPersist` and calls those lets. `handleSnapshotDone` is `let private handleSnapshotDone loaded persisted =` — no explicit `LoadedPersist` (and no `Graph option`) annotation.
