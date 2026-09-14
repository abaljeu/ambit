# 34 — Outside Core lifecycle proof

**Status:** ready-for-agent
**Blocked by:** None — can start immediately. Point 0 ([[30-reshape-coreactorpool-synchronized-table.md]], [[31-one-coremsg-loop-parameterized-persist.md]], [[32-move-persist-agents-under-coremailbox.md]]) is done. [[33-credentialed-browser-change-posts.md|Credentialed Browser Change posts]] landed shared credentialed `PostChange` through CoreMsg; this ticket adds Actor live-table admit and the hello lifecycle. Not blocked by [[29-prove-testactor-hello.md|Prove TestActor hello]].
Actual: 4h

## Context

Core can already apply Changes and validate Browser credentials on the one mailbox. The hello slice still needs a proof that StartActor, TestActor `hello`, admit-before-`PostChange`, and `ActorStop ActorSucceeded` run as one ordered lifecycle — without Browser HTTP and without an Agent transport. Architecture names that proof as Story path **Outside Core lifecycle proof** on [[plan/core-creation/arch.md|Core creation architecture]]. Seam **Test seam for this tracer** is the entry: harness at CoreActorPool or TestActor.

Architecture (Story paths, Module map, Seams): [[plan/core-creation/arch.md|Core creation architecture]]. This ticket holds acceptance for that outside path; do not restate Module map Interface here. Parent umbrella [[29-prove-testactor-hello.md|Prove TestActor hello]] stays as written; this ticket is the takeable cut for Story path 2.

## What to build

Prove one successful TestActor `hello` from outside Core: harness enters at CoreActorPool.startActor or at TestActor body input; when at Pool, expand / select `test` / create / pass body input; when at Actor, TestActor interprets → `hello`; full lifecycle still uses CoreMailbox / CoreMsg for admit-before-`PostChange` and `ActorStop`; CoreActorPool table and History carry the live row and Actor events; outer asserts Graph, ActorStarted, one ActorFinished, dropped live row, and revoked secret; TestActor does not assert; HTTP Adapter universal-response encoding is not required. Follow arch Story path **Outside Core lifecycle proof** and Seam **Test seam for this tracer**. Point at arch Module map for State / Interface / Uses.

Command text remains `?test hello` (Pool selects `test`; TestActor runs `hello`) per [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]].

### What this increment avoids

Browser Run / one-Node Command UI, HTTP Adapter Command encoding, universal `{ nodes; events; latestId }` response encoding, Loaded descendant id list for Browser `graphIds`, live query, cancel, host-stop, fail, post-twice, duplicate terminal, Interrupted restart, new Browser chrome, and Actor definitions other than TestActor.

### 1. CoreMsg / CoreMailboxBackend

Mailbox Actor cases for this proof. Contracts on arch **CoreMsg / CoreMailboxBackend**; Seams **CoreMsg union**, **Credentialed Change posts** (Actor live-table admit).
0. [x] `StartActorRequest` type — `{ zoomId; focusId; commandId; graphIds; revision }`; one Command / StartActor payload shared by CoreMsg `StartActor`, CoreMailbox `startActor`, and CoreActorPool.startActor. `LaunchRequest` is gone.
1. [x] `StartActor` on CoreMsg — carry `StartActorRequest`; validate caller Authority and secret; call startActor synchronously; reply with that result; do not wait for the Actor body
2. [x] Admit before Actor `PostChange` — same credential fields plus live-row check against CoreActorPool table before PersistHandlers
3. [x] `ActorStop` of `ActorResult` — `ActorSucceeded` only in this slice; append ActorFinished on History; drop live row and secret; request terminate without waiting
4. [x] No second mailbox — no nested `ActorMsg` pump; Actor cases share the one CoreMsg loop with persist cases

### 2. CoreMailbox

Public door used when the harness exercises full lifecycle. Contracts on arch **CoreMailbox**; Seam **CoreMailbox door**.

1. [ ] `startActor` door — public `startActor` with `StartActorRequest`
2. [ ] Credentialed Actor posts and stop — credentialed `postChange` (Actor path); `actorStop`; lifecycle event read from History
3. [ ] Callers use CoreMailbox door — not FileAgent / DbAgent wrappers

### 3. CoreActorPool

Live table and start. Contracts on arch **CoreActorPool**; Seam **CoreActorPool table and start**.

1. [ ] `startActor` shape — receives `StartActorRequest`; expand `graphIds` to a Graph
2. [ ] Select and create — read command Node; select Actor kind `test`; create matching Actor
3. [ ] Live row and schedule — create identities and secret; write live row; append ActorStarted on History via the mailbox path; schedule body on the thread pool
4. [ ] Pass body input — Graph plus named ids from `StartActorRequest` (`zoomId`, `focusId`, `commandId`) and the Actor secret
5. [ ] Live registry is only the pool table — no second registry in mailbox-loop state; access is mailbox-owned (no SynchronizedTable); mailbox ownership of access is not a second copy of identity/secret/Focus; `admit` / `drop` / `isLive` as needed for this slice

### 4. History

Actor events on the one sequence. Contracts on arch **History**; Seam **History**.

1. [ ] ActorStarted and ActorFinished — append on the same ordered sequence as Change events
2. [ ] No second Actor-only log — no EventLog beside CoreMailbox
3. [ ] Undo stays Change-only — Undo / Redo still target Change events only

### 5. TestActor

`hello` body. Contracts on arch **TestActor**; Seam **ActorFn / TestActor input**.

1. [ ] `ActorFn` for name `test` — input is Graph plus named ids from `StartActorRequest` and Actor secret
2. [ ] Interpret command Node — switch on case text; this slice has only `hello`
3. [ ] `hello` posts then stops — post one Owned child text `hello` under Focus through admitted `PostChange`, then queue `ActorStop ActorSucceeded`
4. [ ] TestActor does not assert — outer facts own Graph and lifecycle assertions

### 6. CoreRuntime

Composition register. Contracts on arch **CoreRuntime**.

1. [ ] Register TestActor — `CoreActorPool.register` for TestActor at startup / test host composition

### 7. Outside proof harness

Verifiable facts. Arch Story path **Outside Core lifecycle proof**; Seam **Test seam for this tracer**.

1. [ ] Harness entry — call CoreActorPool.startActor or TestActor at body-input / start shapes; not HTTP-only; no Agent transport; no live external service or key
2. [ ] When at Pool — expand, select `test`, create, pass body input
3. [ ] When at Actor — TestActor interprets → `hello`
4. [ ] Full lifecycle path — admit-before-`PostChange` and `ActorStop` still go through CoreMailbox / CoreMsg
5. [ ] Outer sees ActorStarted — durable public Actor identity before Actor output is admitted
6. [ ] Outer sees hello Graph — one Owned child text `hello` under Focus after hello Change and `ActorStop ActorSucceeded`
7. [ ] Outer sees exactly one ActorFinished
8. [ ] After ActorFinished — live row gone; secret no longer admits; public identity remains on Events
9. [ ] No universal-response requirement — does not require HTTP Adapter universal-response encoding

## See also

[[plan/core-creation/arch.md|Core creation architecture]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]], [[29-prove-testactor-hello.md]], [[33-credentialed-browser-change-posts.md]], [[32-move-persist-agents-under-coremailbox.md]], [[27-prove-core-actor-lifecycle-with-testactor.md]], [[18-finish-and-drop.md]], [[15-launch-actor-and-hold-span.md]], [[14-server-tracks-credentials.md]], [[02-core-actor-pool.md]], [[Implementation Planning and Record.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]

## Comments

- 2026-09-14 — Filed via `/to-tickets` for arch Story paths **Outside Core lifecycle proof** and **Browser Run hello** (tracer-cut). Sibling [[35-browser-run-hello.md|Browser Run hello]] is blocked by this ticket. Parent [[29-prove-testactor-hello.md|Prove TestActor hello]] left unchanged per no-retrofit.
- 2026-09-14 — Point 0 names `StartActorRequest` so CoreMsg, the CoreMailbox door, and CoreActorPool.startActor share one id payload instead of repeating `zoomId` / `focusId` / `commandId` / `graphIds`.
- 2026-09-14 — Section 1 CoreMsg / CoreMailboxBackend implemented. `StartActorRequest` and `ActorResult` (`ActorSucceeded` only) are shared types. CoreMsg adds `StartActor` and `ActorStop` on the one loop. Actor `PostChange` admits against the live table after credentials. `ActorStop` drops the live row and secret and cancels without waiting via `CoreActorPool.finish`. History ActorFinished records stay on section 4. CoreMailbox `startActor` / `actorStop` doors stay on section 2. Report: [[plan/core-creation/reports/implement-34-section-1-coremsg.md]].
- 2026-09-14 — Design corrections on section 1: `StartActorRequest` gains `revision` and replaces `LaunchRequest`; `Caller` groups Authority+secret; mailbox calls `CoreActorPool` directly (no `ActorMailboxHandlers`); `StartActor` replies then hands off without holding the loop. `PersistHandlers` stays — FileAgent and DbAgent have no other persist object to pass. Report: [[plan/core-creation/reports/mailbox-start-type-corrections.md]].
- 2026-09-14 — Arch correction: startActor is a synchronous mailbox call; one CoreMsg host; mailbox-owned live table (no SynchronizedTable); File and Db persist only; persist mode is File or Db (mirror deleted). Section 1 item 1 and section 3 item 5 aligned. Report: [[plan/core-creation/reports/sync-startactor-one-mailbox.md]].

## Time

- 2026-09-14 10m — Name `StartActorRequest` and point later items at it (from chat)
- 2026-09-14 50m — Section 1 CoreMsg / CoreMailboxBackend Actor cases
- 2026-09-14 1h — StartActorRequest replaces LaunchRequest; Caller; drop ActorMailboxHandlers; async StartActor handoff; dispatch size/clump (from chat)
- 2026-09-14 2h — Sync startActor, one mailbox, drop mirror, lift DbAgent createLoaded helpers (from chat)
