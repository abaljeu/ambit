# 34b walkthrough — staging vs `dev`, by architecture checkbox

Date: 2026-09-14. No commit. Governing ticket: [[plan/core-creation/issues/34b-outside-core-lifecycle-proof.md|34b — Outside Core lifecycle proof]]. Checkbox home: [[plan/core-creation/arch.md|Core creation architecture]].

Local `dev` is five commits behind `ready` (Point 0 / `cleanup`). The 34b delta is `ready`..`staging`. This report maps that delta onto architecture checkboxes. Plan/arch files themselves do not record place names.

## 1. What to check, what to leave

Checked from 34b: Story path **Outside Core lifecycle proof**, shared Core segments 2–8, the tracer test seam, and Core modules **CoreMsg / CoreMailboxBackend**, **CoreMailbox**, **CoreActorPool**, **History** (process-lifetime), **TestActor**, **CoreRuntime**.

Left open: Story path **Browser Run hello** ([[plan/core-creation/issues/35b-browser-run-hello.md|35b — Browser Run hello]]), shared segment 1 (StartActor through HTTP), module **Loaded descendant id list**, **Browser Run**, **HTTP Adapter**, History persist-across-restart (module 4.4), Seam **Loaded descendant id list**.

## 2. Story path 2 — Outside Core lifecycle proof

| Hop | Code |
| --- | --- |
| 1 Test harness | [[tests/Server.Tests/CoreMailboxDoorTests.fs]], [[tests/Server.Tests/TestActorHelloTests.fs]] |
| 2 Enter at Pool or Actor | [[src/Server/Core/CoreMailbox.fs]] `startActor`; [[src/Server/Core/TestActor.fs]] `actorFn` |
| 3 Pool: select `test`, create, pass | [[src/Server/Core/CoreActorPool.fs]] `runStartActor` — client `graphIds` become the Actor Graph; server does not Zoom-expand |
| 4 Actor interprets `hello` | [[src/Server/Core/TestActor.fs]] `hello` |
| 5 Mailbox admit + ActorStop | [[src/Server/Core/CoreMailboxBackend.fs]] `dispatchPostChange`, `dispatchActorStop` |
| 6 Table and History | Pool `live` map; Loop `mailboxHistory` |
| 7 Outer asserts | `34b section7 outside proof - full lifecycle via CoreMailbox` in [[tests/Server.Tests/TestActorHelloTests.fs]] |
| 8 TestActor does not assert | [[src/Server/Core/TestActor.fs]] returns behavior only |
| 9 No HTTP encoding | Tests call CoreMailbox; no Adapter |

Pool hop 3 still says “expand”. 34b lock: client sends Included `graphIds`; [[src/Server/Core/CoreActorPool.fs]] builds the subgraph from those ids.

## 3. Shared segments and seams

Shared 2–8 and Seam **Test seam for this tracer** are the same Core path as Story path 2. Shared 1 stays open: no Browser HTTP StartActor.

Seams 1–5 and 9 are the 34b doors: [[src/Server/Core/CoreMailbox.fs]], `CoreMsg` in [[src/Server/Core/CoreMailboxBackend.fs]], [[src/Server/Core/CoreActorPool.fs]], mailbox `History`, [[src/Server/Core/TestActor.fs]].

## 4. Module map — what each checkbox bought

### 4.1 CoreMsg / CoreMailboxBackend

[[src/Server/Core/CoreMailboxBackend.fs]]: `StartActor`, `ActorStop`, `GetEventHistory` on `CoreMsg`. `dispatchStartActor` admits the caller, calls `pool.startActor` on the loop, replies without waiting for the body. Actor-bound `makeCoreChanges` includes `actorStop`. `admitActorPost` admits Actor `PostChange` against `pool.isLive`. `dispatchActorStop` appends `ActorFinished` for `ActorSucceeded` and `ActorFailed`, then `pool.finish` (drop live row and secret). `GetState` overlays `lockPresent` from `pool.liveFocusIds`. PersistHandlers stay six persist operations.

### 4.2 CoreMailbox

[[src/Server/Core/CoreMailbox.fs]]: `startActor`, `actorStop`, `eventHistory`, credentialed `postChange` / `coreChanges` on the one host. `getState` returns Graph facts only.

### 4.3 CoreActorPool

[[src/Server/Core/CoreActorPool.fs]]: mailbox-owned `mutable` live table (no lock). `register` then start. `startActor` takes `unit -> Graph`. Requires non-empty `graphIds`, command id in that set, Actor name from CSS `actor-*` (fallback: command text), live row, `appendActorStarted`, then `Async.Start` of the body. `finish` drops on `ActorSucceeded` and `ActorFailed`.

### 4.4 History

[[src/Shared/History.fs]] adds `HistoryEvent` (`ChangeEvent` / `ActorEvent`). The mailbox owns one in-memory sequence (`Loop.mailboxHistory`): ActorStarted in `dispatchStartActor`, ActorFinished in `dispatchActorStop`, ChangeEvents on successful posts. Undo still targets Change only because Actor Events never enter persist `State.history`. Module 4.4 (persist mailbox History across restart) stays open — [[plan/core-creation/issues/35b-browser-run-hello.md|35b — Browser Run hello]] §6.

### 4.5 TestActor

[[src/Server/Core/TestActor.fs]]: `hello` posts one Owned child `hello` under Focus. Generic `dispatch` interprets command text, always queues `ActorStop`. Success is `ActorSucceeded`; exception (including command `throw`) is `ActorFailed`. No asserts in the module.

### 4.6 CoreRuntime

[[src/Server/Core/CoreRuntime.fs]]: `pool.register (ActorName "test") TestActor.actorFn` before `CoreMailbox.host`. File/Db persist fillings do not take the pool.

### 4.7 Loaded descendant id list — not this ticket

[[src/Shared/IncludedDescendantIds.fs]] exists as a childrenStatus stand-in and is unused. 34b does not call it. Client Included `graphIds` stay on 35b.

## 5. Files in the 34b delta

New: [[src/Server/Core/TestActor.fs]], [[src/Shared/IncludedDescendantIds.fs]], [[tests/Server.Tests/CoreMailboxDoorTests.fs]], [[tests/Server.Tests/TestActorHelloTests.fs]].

Touched Core: [[src/Server/Core/CoreMailbox.fs]], [[src/Server/Core/CoreMailboxBackend.fs]], [[src/Server/Core/CoreActorPool.fs]], [[src/Server/Core/CoreChanges.fs]] (`actorStop`, `ActorResult` with `ActorSucceeded` / `ActorFailed`), [[src/Server/Core/CoreRuntime.fs]], [[src/Shared/History.fs]].

Also: [[CONTEXT.md]] State glossary; [[plan/core-creation/issues/35b-browser-run-hello.md]] Fold/Included and History durability.

## 6. Actor-bound handle (closed)

[[src/Server/Core/CoreMailboxBackend.fs]] `makeCoreChanges` now includes `actorStop` (`ActorStop` on the same mailbox, same caller). [[src/Server/Core/TestActor.fs]] `asCaller(...).actorStop` is wired.

## 7. Proof test

`34b section7 outside proof - full lifecycle via CoreMailbox` in [[tests/Server.Tests/TestActorHelloTests.fs]]: `CoreMailbox.startActor` → wait for ActorFinished → one Owned `hello` under Focus → ActorStarted before Change before one ActorFinished → live row gone. No HTTP.

`TestActor throw command fails gracefully and drops live row` in the same file: command `throw` → exception → `ActorFailed` → live row dropped. `ActorStop ActorFailed drops live row and records terminal` in [[tests/Server.Tests/CoreMsgActorCasesTests.fs]] covers the mailbox case.
