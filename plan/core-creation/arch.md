# Core creation architecture

Spec: [[issues/Implementation Planning and Record.md]] (Phase 2 Spec); hello stories from [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], and [Prove TestActor hello](issues/29-prove-testactor-hello.md). No Project `spec.md` yet.
Updated: 2026-09-18
Sequence: tracer-cut
Event stories Sequence: expand-migrate-contract

Feature under design: the hello slice of the one-mailbox Actor program locked by [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], plus the Event destination locked in [[reports/event-abstraction.md]] and created by [[plan/single-event-source/arch.md]]. Prefer existing seams. Do not open Wayfinder map tickets for items under Unsettled. Story hops name modules and doors; State / Interface / Uses live only under Module map (thin hops / fat map). Field shapes for Ev, EventLog, ClientHistory, and `postEvent` live in [[reports/event-abstraction.md]]. ClientHistory is the Emacs Action view; EventLog is the sequence. Ev types (`EventId`, `Authority`, `ActorResult`, `ActorStart`, `EventBody`, `Ev`) live in `Gambol.Shared` with `Op` in [[src/Shared/History.fs]]. Change is `EventBody.Change` of an Op list, not a leftover `{ id; submissionId; ops }` record. There is no `type Change` or `module Change`. The Ev module (`apply` / `inverseOps` via Op-list apply) is in the same file after `module Op`. `ChangeValidation` stays in that file; `ChangeAmendment` is its own file. `EventLog`, `EventJson` stay in their files. There is no `Gambol.Shared.Events` namespace. One serial is EventId (`eventId`, `getEventId`).

Implementation status for this cut: Point 0 loop code is shared ([[issues/30-reshape-coreactorpool-synchronized-table.md]], [[issues/31-one-coremsg-loop-parameterized-persist.md]], [[issues/32-move-persist-agents-under-coremailbox.md]]). Register-then-start one host. Mailbox-owned live table (no lock). Story path **Outside Core lifecycle proof** is implemented ([34b — Outside Core lifecycle proof](issues/34b-outside-core-lifecycle-proof.md)). Story path **Browser Run hello** is not. [Prove TestActor hello](issues/29-prove-testactor-hello.md) remaining Browser sections stay on [35b — Browser Run hello](issues/35b-browser-run-hello.md). Stories **Event, EventLog, and ClientHistory** and **Caller, persist, and Poll** sequence the Event destination as expand-migrate-contract.

## 1. Story paths

1. **Browser Run hello**
   1. [ ] Browser Run (`?` → one-Node Command)
   2. [ ] HTTP Adapter encodes Command + credentials
   3. [ ] CoreMailbox `startActor`
   4. [ ] CoreMsg / CoreMailboxBackend validates the caller, calls startActor synchronously, and replies with that result
   5. [ ] CoreActorPool.startActor (expand, select `test`, create live row); mailbox records ActorStarted in-loop; pool.schedule fires the body only
   6. [ ] TestActor interprets command Node → `hello`
   7. [ ] CoreMsg admit-before-`PostEvent`
   8. [ ] CoreMsg `ActorStop` of `ActorSucceeded`
   9. [ ] EventLog appends ActorStop Ev
   10. [ ] universal response when that path is exercised
   11. [ ] Browser shows one Owned child text `hello`

   [[issues\Implementation Planning and Record.md]] should be referenced when considering tickets for this story path.

2. **Outside Core lifecycle proof**
   1. [x] Test harness (no Agent transport; not HTTP-only); harness registers TestActor (injected ActorFn, not Core-owned)
   2. [x] Enter at CoreActorPool.startActor or at TestActor body input
   3. [x] When at Pool: expand, select `test`, create, pass body input
   4. [x] When at Actor: TestActor interprets → `hello`
   5. [x] Full lifecycle still uses CoreMailbox / CoreMsg for admit-before-`PostEvent` and `ActorStop`
   6. [x] CoreActorPool live table; mailbox History
   7. [x] Outer asserts Graph, ActorStarted, one ActorFinished, dropped live row, revoked secret (when full lifecycle)
   8. [x] TestActor does not assert
   9. [x] Does not require HTTP Adapter universal-response encoding
3. **Browser Change posts**
   1. [x] Browser Change submit
   2. [x] HTTP Adapter encodes Change + credentials
   3. [x] CoreMailbox `postEvents`
   4. [x] CoreMsg validates Browser credentials before PersistHandlers
4. **Event, EventLog, and ClientHistory**
   Sequence: expand-migrate-contract. Locked interface: [[reports/event-abstraction.md]]. Shared slice only.
   1. **Expand**
      1. [x] Additive Shared `EventId`, `ActorStart`, `EventBody`, `Ev` in `Gambol.Shared`
      2. [x] Ev module: `id`, `authority`, `ops`, `isAction`, `target`, `apply`, `inverseOps`, `fromJson`, `toJson`. No leftover Change record. No `asChange` / `ofChange`
      3. [x] EventLog module: `empty`, `append`, `nextId`, `since`, `tryFind`, `restore`
      4. [x] ClientHistory: Emacs Actions only — `record`, `undo` / `redo`, `tryPeekUndoName` / `tryPeekRedoName`
      5. [x] `Authority`, `ActorStart`, and `ActorResult` live in `Gambol.Shared` with Ev
      6. [x] Shared.Tests: append/since/tryFind; restore dedupe; `ClientHistory.record` fold; undo produces `Undo(target, inverseOps)` and redo names the Undo Ev; Actor bodies do not apply; `Ev.apply` of Undo/Redo uses carried Ops; every Ev carries `Authority`; `ActorStart` body equals the start request; `ActorStop` carries `ActorResult`
      7. [x] `ClientHistory` remains; `HistoryEvent` deleted
   2. **Migrate**
      1. [x] No production callers in this story
   3. **Contract**
      1. [x] No delete of `HistoryEvent`, `ActorLifecycleEvent`, or `ClientHistory` in this story
5. **Caller, persist, and Poll**
   Sequence: expand-migrate-contract. Follows **Event, EventLog, and ClientHistory**.
   1. **Expand**
      1. [x] `postEvent` / `postEvents` doors on CoreMailbox; payload is Ev
      2. [x] mailbox store is `EventLog ref`
      3. [x] persist EventLog as Event JSON via EventLogFile (`gambol.events` file) and `events` table (DB)
   2. **Migrate**
      1. [x] HTTP Adapter ([[src/Server/Api.fs]]) decode/encode: Change posts call `postEvents`; Poll/Load tail is an Ev list
      2. [x] Changes callers use `postEvents` (or `postEvent` for one Ev); name-only Undo/Redo may carry only `target`; dispatch fills inverse Ops (same `submissionId`)
      3. [x] command builders mint Ev (`EventId.zero`, `commandName`, `EventBody.Change` of Ops). Run is ActorStart or a Change Event with that Run command in `commandName`. No leftover Change record and no `Ev.ofChange` / `Ev.asChange`
      4. [x] `GetEventHistory` returns the log or `since`, not a two-stack
      5. [x] Poll returns an Ev tail (server return and client consume)
      6. [x] Browser Poll consume; EventId basis (`State.eventId`, `ClientSyncState.eventId`); `getEventId`
      7. [x] EventBatch wraps Ev list; SyncInfo pending is an Ev list. No leftover PendingChange record
      8. [x] Browser callers keep using ClientHistory (Ev-shaped); client holds EventLog of the same type. Do not migrate onto a module named History. `ClientHistory.undo` locally then name-only submit; ack/reconcile stays the pending path
      9. [x] CoreMsg / CoreActorPool: mailbox appends ActorStart / ActorStop Ev records; callers do not `postEvent` those bodies
      10. [x] persist ActorStart / ActorStop
      11. [x] PersistHandlers load/restore: File/Db call `EventLog.restore`; `getEventsSince` returns an Ev list
      12. [x] every start request is `ActorStart`
      13. [x] stamp `authority` on every stored Ev from the admitted Caller
   3. **Contract**
      1. [x] Delete `HistoryEvent`, `ActorLifecycleEvent`, mailbox `type History` / History name (replaced by EventLog), and `PendingKind` once no caller remains. Do not delete ClientHistory
      2. [x] Delete the name `StartActorRequest` once every caller says `ActorStart`
      3. [x] Drop the ChangeLog name; persist is EventLog

Composition: Story paths 1–3 are tracer-cut hop lists. Stories **Event, EventLog, and ClientHistory** and **Caller, persist, and Poll** are expand-migrate-contract. Shared segments below factor hop sequences that appear in more than one hello path (for core modules / test seam); they are not missing hops to splice into a path.

Shared segments across 1 and 2:
1. [ ] StartActor through HTTP / Core / Pool
2. [x] CoreActorPool start (expand, select, create, pass)
3. [x] TestActor `hello`
4. [x] CoreMailbox / CoreMsg when full lifecycle
5. [x] CoreActorPool live table
6. [x] History for Actor lifecycle
7. [x] admit-before-`PostEvent`
8. [x] `ActorStop`

Shared segment with path 3:
1. [x] Credentialed `PostEvent` through CoreMsg

Narrowest shared test seam:
1. [x] CoreActorPool start and/or TestActor body input — not HTTP encoding, not PersistHandlers, not TestActor private helpers

Narrowest test seam for Story **Event, EventLog, and ClientHistory**:
1. [x] Shared Ev / EventLog / ClientHistory functions

Narrowest test seam for Story **Caller, persist, and Poll**:
1. [x] CoreMailbox `postEvent` / `postEvents` and EventLog `since`

## 2. Module map

Mailbox is intake. EventLog is the store after the mailbox has taken it. ClientHistory is the Emacs Action view. Persistence is the persisted EventLog. Same Ev type throughout. Field shapes for Ev, EventLog, ClientHistory, and `postEvent` live in [[reports/event-abstraction.md]]. Hello modules keep their State / Interface / Uses here.

1. **CoreMsg / CoreMailboxBackend** — [[src/Server/Core/CoreMsg.fs]], [[src/Server/Core/CoreMailboxBackend.fs]]
   1. [x] State: the one ordered mailbox loop
   - Interface:
     1. [x] match `CoreMsg` cases
     2. [x] for `StartActor`, carry `ActorStart` (include EventId); validate caller Authority and secret; call startActor; reply with that result; do not wait for the Actor body
     3. [x] for every Changes post (Browser or Actor), require and validate Authority and secret before PersistHandlers
     4. [x] for Actor Changes posts, admit against the live table (same credential fields, live-row check)
     5. [x] for `ActorStop` of `ActorResult` (`ActorSucceeded` or `ActorFailed`), store an ActorStop Ev on EventLog, drop the live row and secret, request terminate without waiting
     6. [x] for `GetState`, apply `lockPresent` from the live table on the mailbox thread, then reply; persist getState stays on PersistHandlers
     7. [x] stamp `authority` from the admitted Caller on every stored Ev
     8. [x] name-only Undo/Redo: `tryFind` the target Ev, fill inverse Ops, store the completed Ev (same `submissionId`)
   - Uses:
     1. [ ] CoreActorPool
     2. [ ] mailbox secret set (Browser) and live-table isLive (Actor). No CoreCredentials mailbox.
     3. [ ] EventLog
     4. [ ] PersistHandlers (persist cases only)
2. **CoreMailbox** — [[src/Server/Core/CoreMailbox.fs]]
   1. [x] State: none beyond MailboxHost
   - Interface:
     1. [x] public door on MailboxHost — `startActor` with `zoomId`, `focusId`, `commandId`, `graphIds`; `actorStop`; `login` (mailbox privately adds the Browser secret); `isAdmitted` (query). No public add-credential door.
     2. [x] `postEvents` is the Changes door. Payload is Ev list (`EventBody.Change` of Ops, Undo, Redo). `postEvent` posts one Ev. Name-only Undo/Redo may arrive with only `target`
     3. [x] `postGraphOnly` — Same Ev flow as `postEvents`, skips file persistence only (not EventLog)
     4. [x] `eventHistory` is EventLog (the log or `since`), not a two-stack
     5. [x] getState / getEventId / createFile / createDb
     6. [x] `getEventsSince` returns an Ev tail
   - Uses:
     1. [ ] MailboxHost
     2. [ ] CoreMsg
     3. [ ] EventLog
3. **CoreActorPool** — [[src/Server/Core/CoreActorPool.fs]]
   - State:
     1. [x] mailbox-owned live table (public Actor identity, secret, termination handle, Focus NodeId); no lock; the mailbox is the only thread that reads or writes the table
     2. [x] registered `ActorFn` defs
     3. [x] thread-pool runner
   - Interface:
     1. [x] `register` finishes before the mailbox starts
     2. [x] `startActor` receives `ActorStart` (`zoomId`, `focusId`, `commandId`, `graphIds`, EventId)
     3. [x] `startActor` returns `Result<Credential, string>`
     4. [x] expand `graphIds` to a Graph
     5. [x] read the command Node and select Actor kind (`test` in this slice); create the matching Actor
     6. [x] startActor prepares and creates the live row and secret only (no EventLog, no schedule). schedule is fire-and-forget of the body only
     7. [x] On the mailbox thread: live row → ActorStart Ev on EventLog → pool.schedule. ActorStart is in-loop, not PostAndAsyncReply
     8. [x] pass Graph plus named `zoomId`, `focusId`, `commandId` and the Actor secret into the Actor
     9. [x] `admit`, `drop`, `isLive`
     10. [x] launch / query are gone; `withLocks` / `lockedIds` leave the CoreRuntime wrap
   - Uses:
     1. [ ] `ActorFn` (injected; Core does not own Actor bodies). Pool does not Use EventLog or CoreCredentials. Live row is Actor liveness.
4. **Ev** — types after `Op`; `module Ev` after `module Op` in [[src/Shared/History.fs]] (`Gambol.Shared`; no `Gambol.Shared.Events` namespace)
   Field shapes: [[reports/event-abstraction.md]].
   1. [x] EventLog stores that Change as an Event with a unique event id greater than zero. Until EventLog stores it, the Event’s event id is zero. Poll with event id zero returns every stored Event. Only EventLog assigns stored event ids. Event id zero stays zero; it does not count up to one.
   - Interface:
     1. [x] `id`, `authority`, `ops` (none for Actor bodies), `isAction`, `target` (none except Undo/Redo)
     2. [x] `apply` — Graph apply via those Ops; Actor bodies do not touch the Graph
     3. [x] `inverseOps` — used when building Undo/Redo from a target Ev
     4. [x] `fromJson` / `toJson` — only serializing uses these. No leftover Change record. No `asChange` / `ofChange`
     5. [x] `ActorStart` is the start request (zoom, focus, command, graphIds, basis EventId)
     6. [x] `ActorStop` is `focusId` plus `ActorResult`
     7. [x] every Ev carries `Authority`; Core stamps it from the admitted Caller; the wire does not supply it
     8. [x] Command builders mint Ev (`EventId.zero`, `commandName`, `EventBody.Change` of Ops)
   - Uses:
     1. [ ] Op, Graph, `Authority`, `ActorResult`
5. **EventLog** — planned [[src/Shared/EventLog.fs]]
   Field shapes: [[reports/event-abstraction.md]].
   1. [x] State: append-only newest-head Ev sequence; mailbox store after intake. Persistence is this same EventLog on file/DB via EventLogFile (`gambol.events` file) and `events` table (DB). Not a second log.
   - Interface:
     1. [x] `empty`, `append`, `nextId` — empty `nextId` is the first assignable stored Int, not next of Zero; append stamps a unique positive Int and advances `nextId`; EventLog is the only assigner of stored event ids
     2. [x] `since eventId` — Poll/Load tail (self-contained Ev records). `since` of Zero is every stored Int
     3. [x] `tryFind` — Core name-only Undo/Redo
     4. [x] `restore` — merge persisted Ev records; dedupe by `submissionId`; `nextId` stays past every merged stored Int
     5. [x] CoreMailboxBackend is the only writer of the mailbox store. State has no `history` field. getState stays Graph-only
     6. [x] no second Actor-only event log beside CoreMailbox
     7. [x] encode and read Event JSON [[src/Shared/EventJson.fs]] (`EventJson` in `Gambol.Shared`)
     8. [x] persist ActorStart / ActorStop
   - Uses:
     1. [ ] Ev
6. **ClientHistory** — [[src/Shared/ClientHistory.fs]]
   Field shapes: [[reports/event-abstraction.md]].
   1. [x] State: newest-head `past`/`future` of Actions (Change/Undo/Redo). `commandName` lives on Ev; `record` still takes it for peek. Not persisted. Not sent on Poll
   - Interface:
     1. [x] `record commandName event` — fold `future` into `past`
     2. [x] `undo` / `redo` — move the local stack and produce the Undo/Redo Ev (target + inverse Ops)
     3. [x] `tryPeekUndoName` / `tryPeekRedoName`
   - Uses:
     1. [ ] Ev (Change / Undo / Redo bodies)
7. **TestActor** (injected proof Actor; not a Core module) — [[src/Server/TestActor.fs]]
   1. [x] State: none. The test host registers this ActorFn on CoreActorPool before the mailbox starts.
   - Interface:
     1. [x] `ActorFn` for Actor name `test`
     2. [x] input is Graph plus named `zoomId`, `focusId`, `commandId` and the Actor secret (Pool expands ids; does not invent further payload fields)
     3. [x] interpret the command Node and switch on case text
     4. [x] this slice has only `hello` (post one Owned child text `hello` under Focus through admitted `postEvents`, then queue `ActorStop ActorSucceeded`). Any other command text is `ActorFailed` with no Owned child. No exception path.
   - Uses:
     1. [ ] CoreChanges (bound through the mailbox)
     2. [ ] Graph
8. **Included descendant id list** — [[src/Shared/IncludedDescendantIds.fs]]
   1. [ ] State: none (Shared pure function)
   - Interface:
     1. [ ] given a Graph and a start NodeId (Zoom root), return a flat `NodeId` list
     2. [ ] include the start Node
     3. [ ] recurse only through unfolded (expanded) child lists; add every child id found there
     4. [ ] do not descend into folded children
     5. [ ] do not filter or branch on ownership (Owner vs other child kinds); walk unfolded children only
     6. [ ] result is ids only — not a Graph, not edges, not ownership facts
     7. [ ] same function is reused wherever a Zoom-rooted Included id list (unfolded context) is needed (Browser Command `graphIds`, Actors, and later callers)
   - Uses:
     1. [ ] Graph / Node (`children`; walks SiteMap fold state when built, childrenStatus residency as stand-in)
9. **Browser Run** — [[src/Client/Commands.fs]]
   1. [ ] State: Client selection and current Node text
   - Interface:
     1. [ ] existing Exec / Run command
     2. [ ] when text starts with literal `?`, send one-Node Command (current Node is Command, Zoom root, and Focus) with caller credentials as `zoomId`, `focusId`, `commandId`, and `graphIds` from **Included descendant id list** at that Zoom root
     3. [ ] otherwise AmbleRun (not part of Story path 3)
     4. [x] Browser-originated Change posts supply Authority and secret (Story path 3)
   - Uses:
     1. [ ] HTTP Adapter
     2. [ ] AmbleRun
     3. [ ] Included descendant id list
10. **HTTP Adapter** — [[src/Server/Api.fs]]
   1. [ ] State: none (transport)
   - Interface:
     1. [ ] decode Browser Command / Change / Poll
     2. [ ] Command / StartActor transport fields are `zoomId`, `focusId`, `commandId`, `graphIds` — same shape as the Core door and CoreActorPool.startActor
     3. [x] Change posts carry credentials
     4. [x] Changes posts call CoreMailbox `postEvents`
     5. [x] Poll returns an Ev tail
     6. [ ] call Core through CoreMailbox or CoreRuntime-bound members
     7. [ ] encode universal `{ nodes; events; latestId }` for Command when that path is exercised (spec lock; not critical path for the hello outside proof)
   - Uses:
     1. [ ] CoreMailbox / CoreRuntime
11. **CoreRuntime** — [[src/Server/Core/CoreRuntime.fs]]
   1. [x] State: composed host and registered Actors. Does not export a credentials field.
   - Interface:
     1. [x] one `MailboxProcessor<CoreMsg>`; persist mode chooses File or Db handlers (not File-with-Db-mirror)
     2. [x] `CoreActorPool.register` for caller-supplied ActorFn entries; register finishes before the mailbox starts. CoreRuntime.create takes an actor list; it does not hardcode TestActor
     3. [x] File and Db do not take the pool. Login and cookie admit go through CoreMailbox doors. No public add-credential.
   - Uses:
     1. [ ] CoreMailbox
     2. [ ] CoreActorPool
12. **PersistHandlers** — [[src/Server/Core/FileAgent.fs]], [[src/Server/Core/DbAgent.fs]]
   1. [x] State: File or Db persist implementation behind the loop
   - Interface:
     1. [x] getState, getEventId, applyEvent, snapshotDone
     2. [x] getEventsSince / persist EventLog
     3. [x] Actor cases are not on this parameter
   - Uses:
     1. [ ] FileAgent / DbAgent fill persist only (handlers, flush, ready, dispose); they do not dispatch Actor cases and are not Actor mailboxes
     2. [ ] EventLog

## 3. Seams

1. [x] **CoreMailbox door** — External seam for production and HTTP. Interface on **CoreMailbox**.
2. [x] **CoreMsg union** — Internal one-mailbox seam. Interface on **CoreMsg / CoreMailboxBackend**. No second Actor mailbox or nested `ActorMsg` pump in this slice.
3. [x] **CoreActorPool table and start** — Live registry plus start/schedule seam only. Interface on **CoreActorPool**. Table is the single registry (no second copy in loop state). Access is mailbox-owned, so not a lock around the live table. Pool does not take or write EventLog.
4. [x] **Ev** — Shared Ev type and functions in `Gambol.Shared`. Interface on **Ev**. Field shapes: [[reports/event-abstraction.md]].
5. [x] **EventLog** — Mailbox store after intake; persist is this same EventLog on file/DB. Interface on **EventLog**. `since` is the Poll/Load tail.
6. [x] **ClientHistory** — Emacs Action view. Interface on **ClientHistory**. Not persisted. Not sent on Poll.
7. [x] **`postEvents` door** — Changes door. Interface on **CoreMailbox**. Payload is Ev. `postEvent` posts one Ev. `postGraphOnly` is graph-only Ev.
8. [x] **ActorFn / TestActor input** — Definition and body-input seam. Interface on **TestActor** (outside Core). Callers pass ActorFn into **CoreActorPool.register** / **CoreRuntime.create**. Core does not embed Actor bodies.
9. [x] **PersistHandlers** — Persist seam already landed by [One CoreMsg loop parameterized persist](issues/31-one-coremsg-loop-parameterized-persist.md) and [Move persist agents under CoreMailbox](issues/32-move-persist-agents-under-coremailbox.md). Hello does not widen it. Actor cases stay off this parameter. File and Db are not Actor mailboxes. Interface on **PersistHandlers**.
10. [x] **Credentialed Change posts** — Browser and Actor posts validate via **CoreMsg** before PersistHandlers; Actor also admits on the live table. Story path 3 is Browser Change posts only.
11. [ ] **Included descendant id list — Shared Zoom-rooted Included id walk (unfolded context). Interface on **Included descendant id list**. Browser Command `graphIds` and Actor reuse call the same function.
12. [x] **Test seam for this tracer** — Story path 2: harness at CoreActorPool or TestActor. Outer facts assert Graph and lifecycle; TestActor does not assert. HTTP universal-response encoding is not on the outside-proof critical path.

## 4. Alternative considered

1. **Chosen** — One CoreMsg loop owns fast messages (`StartActor`, credentialed admit-before-`PostEvent`, `ActorStop`) and live-table access. CoreActorPool.startActor is synchronous prepare (validate, live row, secret) and returns `Result<Credential, string>`; the mailbox writes the ActorStart Ev on EventLog, then pool.schedule fires the body. The mailbox does not wait for the body. CoreActorPool owns the table data, defs, selection, and runner; it does not Use EventLog. Persist mode chooses File or Db handlers; there is no File-with-Db-mirror. Browser Command `graphIds` come from Shared **Included descendant id list** (unfolded context; flat ids; ownership ignored; reusable by Actors). TestActor (outside Core) owns command-Node interpretation (hello) and is passed in as ActorFn. Production callers use the CoreMailbox door; outside proofs may call Pool or Actor at those seams. Matches [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]], Point 0 loop code (tickets 30–32), and Alan’s lock that the live registry is CoreActorPool’s data.
2. **Rejected: mailbox-held second registry** — Keep live rows only in mailbox-loop state and treat the pool as a dumb Task runner. Loses the table as the single live registry; duplicates identity/secret/Focus beside CoreActorPool. Mailbox ownership of access is not a second copy of identity/secret/Focus beside the pool.
3. **Rejected: nested ActorMsg pump / FileAgent twin mailbox** — Restore a second mailbox or per-agent Actor cases (shape in the stashed [[reports/implement-issue-29-testactor-hello.md]]). File and Db must not start Actor-capable processors. Twin queues for Actor work stay rejected. Violates one-mailbox ordering from [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] and the CoreMailbox-only door from ticket 32.
4. **Rejected: Actor event sequence outside the mailbox** — A second Actor-only sequence beside CoreMailbox. EventLog is the mailbox store after intake, not a second sequence.
5. **Deferred past hello** — Cancel, live query, host-stop, post-twice, duplicate terminal, and Interrupted restart stay out of the hello stories. `ActorFailed` is in this slice: same drop as success; unregistered Actor name fails start; TestActor non-hello command stops as `ActorFailed` (no exception path). Record only; do not ticket from Unsettled.
6. **Chosen Event destination** — One Ev type in `Gambol.Shared`. Mailbox is intake; EventLog is the store; ClientHistory is the Emacs Action view; persistence is the persisted EventLog. `postEvents` is the Changes door (Ev list); `postGraphOnly` is graph-only Ev (skips file persist, not EventLog); `postEvent` posts one Ev. `ActorStart` is the start request; pool `startActor` returns `Result<Credential, string>`. `authority` is on every Ev, stamped from the admitted Caller. Poll returns an Ev tail. There is no destination module named History; [[src/Shared/History.fs]] holds Op, Ev types, EventBody, ChangeValidation, and the Ev module; `ChangeAmendment` is its own file; the former mailbox History type is now EventLog. Command builders mint Ev (`EventId.zero`, `commandName`, `EventBody.Change` of Ops). There is no leftover Change record and no `Ev.ofChange` / `Ev.asChange`. One serial is EventId (`eventId`, `getEventId`): Zero or a positive stored Int; EventLog assigns stored serials; next of Zero is Zero.

## 5. Unsettled

1. Cherry-pick Undo (invert a chosen Change Event; skip Actor Events). Deferred past hello; not a ticket yet.
2. Restore older file versions via actual git (from the commits already made on edits). Deferred past hello; not a ticket yet.
