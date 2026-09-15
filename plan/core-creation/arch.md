# Core creation architecture

Spec: [[issues/Implementation Planning and Record.md]] (Phase 2 Spec); hello stories from [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], and [[issues/29-prove-testactor-hello.md|Prove TestActor hello]]. No Project `spec.md` yet.
Updated: 2026-09-14
Sequence: tracer-cut

Feature under design: the hello slice of the one-mailbox Actor program locked by [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Prefer existing seams. Do not open Wayfinder map tickets for items under Unsettled. Story hops name modules and doors; State / Interface / Uses live only under Module map (thin hops / fat map).

Implementation status for this cut: Point 0 loop code is shared ([[issues/30-reshape-coreactorpool-synchronized-table.md]], [[issues/31-one-coremsg-loop-parameterized-persist.md]], [[issues/32-move-persist-agents-under-coremailbox.md]]). Register-then-start one host. Mailbox-owned live table (no lock). Story path **Outside Core lifecycle proof** is implemented ([[issues/34b-outside-core-lifecycle-proof.md|34b — Outside Core lifecycle proof]]). Story path **Browser Run hello** is not. [[issues/29-prove-testactor-hello.md|Prove TestActor hello]] remaining Browser sections stay on [[issues/35b-browser-run-hello.md|35b — Browser Run hello]].

## 1. Story paths

1. **Browser Run hello**
   1. [ ] Browser Run (`?` → one-Node Command)
   2. [ ] HTTP Adapter encodes Command + credentials
   3. [ ] CoreMailbox `startActor`
   4. [ ] CoreMsg / CoreMailboxBackend validates the caller, calls startActor synchronously, and replies with that result
   5. [ ] CoreActorPool.startActor (expand, select `test`, create live row); mailbox records ActorStarted in-loop; pool.schedule fires the body only
   6. [ ] TestActor interprets command Node → `hello`
   7. [ ] CoreMsg admit-before-`PostChange`
   8. [ ] CoreMsg `ActorStop` of `ActorSucceeded`
   9. [ ] History appends ActorFinished
   10. [ ] universal response when that path is exercised
   11. [ ] Browser shows one Owned child text `hello`

   [[issues\Implementation Planning and Record.md]] should be referenced when considering tickets for this story path.

2. **Outside Core lifecycle proof**
   1. [x] Test harness (no Agent transport; not HTTP-only); harness registers TestActor (injected ActorFn, not Core-owned)
   2. [x] Enter at CoreActorPool.startActor or at TestActor body input
   3. [x] When at Pool: expand, select `test`, create, pass body input
   4. [x] When at Actor: TestActor interprets → `hello`
   5. [x] Full lifecycle still uses CoreMailbox / CoreMsg for admit-before-`PostChange` and `ActorStop`
   6. [x] CoreActorPool live table; mailbox History
   7. [x] Outer asserts Graph, ActorStarted, one ActorFinished, dropped live row, revoked secret (when full lifecycle)
   8. [x] TestActor does not assert
   9. [x] Does not require HTTP Adapter universal-response encoding
3. **Browser Change posts**
   1. [x] Browser Change submit
   2. [x] HTTP Adapter encodes Change + credentials
   3. [x] CoreMailbox `postChange`
   4. [x] CoreMsg validates Browser credentials before PersistHandlers

Composition: Each Story path above is a complete end-to-end hop list. Shared segments below factor hop sequences that appear in more than one path (for core modules / test seam); they are not missing hops to splice into a path.

Shared segments across 1 and 2:
1. [ ] StartActor through HTTP / Core / Pool
2. [x] CoreActorPool start (expand, select, create, pass)
3. [x] TestActor `hello`
4. [x] CoreMailbox / CoreMsg when full lifecycle
5. [x] CoreActorPool live table
6. [x] History for Actor lifecycle
7. [x] admit-before-`PostChange`
8. [x] `ActorStop`

Shared segment with path 3:
1. [x] Credentialed `PostChange` through CoreMsg

Narrowest shared test seam:
1. [x] CoreActorPool start and/or TestActor body input — not HTTP encoding, not PersistHandlers, not TestActor private helpers

## 2. Module map

Deltas for this Project’s hello / one-mailbox Actor program. Persist fillings and the generic CoreMailbox door from Point 0 stay; this map names how Actor work and credentialed Change posts attach. There is no Actor event sequence outside the mailbox: only CoreMailbox and History. Sole home of field shapes, validation, and event policy for this cut.

1. **CoreMsg / CoreMailboxBackend**
   1. [x] State: the one ordered mailbox loop
   - Interface:
     1. [x] match `CoreMsg` cases
     2. [x] for `StartActor`, carry `StartActorRequest` (include revision); validate caller Authority and secret; call startActor; reply with that result; do not wait for the Actor body
     3. [x] for every `PostChange` (Browser or Actor), require and validate Authority and secret before PersistHandlers
     4. [x] for Actor `PostChange`, admit against the live table (same credential fields, live-row check)
     5. [x] for `ActorStop` of `ActorResult` (`ActorSucceeded` or `ActorFailed`), append ActorFinished on History, drop the live row and secret, request terminate without waiting
     6. [x] for `GetState`, apply `lockPresent` from the live table on the mailbox thread, then reply; persist getState stays on PersistHandlers
   - Uses:
     1. [ ] CoreActorPool
     2. [ ] CoreCredentials / CallerTable
     3. [ ] History
     4. [ ] PersistHandlers (persist cases only)
2. **CoreMailbox**
   1. [x] State: none beyond MailboxHost
   - Interface:
     1. [x] public door on MailboxHost — `startActor` with `zoomId`, `focusId`, `commandId`, `graphIds`; credentialed `postChange` (Browser and Actor); `actorStop`; `eventHistory` for lifecycle event read from History
     2. [x] existing getState / getRevision / getChangesSince / postGraphOnlyChange / createFile / createDb
   - Uses:
     1. [ ] MailboxHost
     2. [ ] CoreMsg
3. **CoreActorPool**
   - State:
     1. [x] mailbox-owned live table (public Actor identity, secret, termination handle, Focus NodeId); no lock; the mailbox is the only thread that reads or writes the table
     2. [x] registered `ActorFn` defs
     3. [x] thread-pool runner
   - Interface:
     1. [x] `register` finishes before the mailbox starts
     2. [x] `startActor` receives `StartActorRequest` (`zoomId`, `focusId`, `commandId`, `graphIds`, `revision`)
     3. [x] expand `graphIds` to a Graph
     4. [x] read the command Node and select Actor kind (`test` in this slice); create the matching Actor
     5. [x] startActor prepares and creates the live row and secret only (no History, no schedule). On the mailbox thread: live row → ActorStarted on mailboxHistory → pool.schedule. ActorStarted is in-loop, not PostAndAsyncReply. schedule is fire-and-forget of the body only
     6. [x] pass Graph plus named `zoomId`, `focusId`, `commandId` and the Actor secret into the Actor
     7. [x] `admit`, `drop`, `isLive`
     8. [x] launch / query are gone; `withLocks` / `lockedIds` leave the CoreRuntime wrap
   - Uses:
     1. [ ] CoreCredentials
     2. [ ] `ActorFn` (injected; Core does not own Actor bodies). Pool does not Use History.
4. **History**
   1. [x] State: ordered past/future sequence extended to carry Actor events alongside Change events
   - Interface:
     1. [x] append Change and Actor lifecycle events (ActorStarted, ActorFinished) on mailboxHistory only. CoreMailboxBackend is the only writer. History module helpers that wrote Actor Events onto State.history are gone. getState stays Graph-only; do not merge mailboxHistory into State.history
     2. [x] Undo / Redo still target Change events only
     3. [x] no second Actor-only event log beside CoreMailbox
     4. [ ] persist mailbox History so the audit sequence survives restart (Graph/ChangeLog durability already separate; Undo stays Change-only)
   - Uses:
     1. [ ] Change / Actor event records as History members
5. **TestActor** (injected proof Actor; not a Core module)
   1. [x] State: none. The test host registers this ActorFn on CoreActorPool before the mailbox starts.
   - Interface:
     1. [x] `ActorFn` for Actor name `test`
     2. [x] input is Graph plus named `zoomId`, `focusId`, `commandId` and the Actor secret (Pool expands ids; does not invent further payload fields)
     3. [x] interpret the command Node and switch on case text
     4. [x] this slice has only `hello` (post one Owned child text `hello` under Focus through admitted `PostChange`, then queue `ActorStop ActorSucceeded`)
   - Uses:
     1. [ ] CoreChanges (bound through the mailbox)
     2. [ ] Graph
6. **Loaded descendant id list**
   1. [ ] State: none (Shared pure function)
   - Interface:
     1. [ ] given a Graph and a start NodeId (Zoom root), return a flat `NodeId` list
     2. [ ] include the start Node
     3. [ ] recurse only through `childrenStatus = Loaded` child lists; add every child id found there
     4. [ ] do not descend into `Unloaded` child lists
     5. [ ] do not filter or branch on ownership (Owner vs other child kinds); walk Loaded children only
     6. [ ] result is ids only — not a Graph, not edges, not ownership facts
     7. [ ] same function is reused wherever a Zoom-rooted Loaded id list is needed (Browser Command `graphIds`, Actors, and later callers)
   - Uses:
     1. [ ] Graph / Node (`children`, `childrenStatus`)
7. **Browser Run**
   1. [ ] State: Client selection and current Node text
   - Interface:
     1. [ ] existing Exec / Run command
     2. [ ] when text starts with literal `?`, send one-Node Command (current Node is Command, Zoom root, and Focus) with caller credentials as `zoomId`, `focusId`, `commandId`, and `graphIds` from **Loaded descendant id list** at that Zoom root
     3. [ ] otherwise AmbleRun (not part of Story path 3)
     4. [x] Browser-originated Change posts supply Authority and secret (Story path 3)
   - Uses:
     1. [ ] HTTP Adapter
     2. [ ] AmbleRun
     3. [ ] Loaded descendant id list
8. **HTTP Adapter**
   1. [ ] State: none (transport)
   - Interface:
     1. [ ] decode Browser Command / Change / Poll
     2. [ ] Command / StartActor transport fields are `zoomId`, `focusId`, `commandId`, `graphIds` — same shape as the Core door and CoreActorPool.startActor
     3. [x] Change posts carry credentials; pass Authority and secret into CoreMailbox `postChange`
     4. [ ] call Core through CoreMailbox or CoreRuntime-bound members
     5. [ ] encode universal `{ nodes; events; latestId }` for Command when that path is exercised (spec lock; not critical path for the hello outside proof)
   - Uses:
     1. [ ] CoreMailbox / CoreRuntime
9. **CoreRuntime**
   1. [x] State: composed credentials and registered Actors
   - Interface:
     1. [x] one `MailboxProcessor<CoreMsg>`; persist mode chooses File or Db handlers (not File-with-Db-mirror)
     2. [x] `CoreActorPool.register` for caller-supplied ActorFn entries; register finishes before the mailbox starts. CoreRuntime.create takes an actor list; it does not hardcode TestActor
     3. [x] File and Db do not take the pool
   - Uses:
     1. [ ] CoreMailbox
     2. [ ] CoreActorPool
     3. [ ] CoreCredentials
10. **PersistHandlers**
   1. [x] State: File or Db persist implementation behind the loop
   - Interface:
     1. [x] getState, getRevision, getChangesSince, postChange, postGraphOnlyChange, snapshotDone
     2. [x] Actor cases are not on this parameter
   - Uses:
     1. [ ] FileAgent / DbAgent fill persist only (handlers, flush, ready, dispose); they do not dispatch Actor cases and are not Actor mailboxes

## 3. Seams

1. [x] **CoreMailbox door** — External seam for production and HTTP. Interface on **CoreMailbox**.
2. [x] **CoreMsg union** — Internal one-mailbox seam. Interface on **CoreMsg / CoreMailboxBackend**. No second Actor mailbox or nested `ActorMsg` pump in this slice.
3. [x] **CoreActorPool table and start** — Live registry plus start/schedule seam only. Interface on **CoreActorPool**. Table is the single registry (no second copy in loop state). Access is mailbox-owned, so not a lock around the live table. Pool does not take or write History.
4. [x] **History** — Interface on mailbox **History** (`Loop.mailboxHistory`). One sequence; no Actor event log outside CoreMailbox. ActorStarted is recorded on the mailbox loop before the body runs.
5. [x] **ActorFn / TestActor input** — Definition and body-input seam. Interface on **TestActor** (outside Core). Callers pass ActorFn into **CoreActorPool.register** / **CoreRuntime.create**. Core does not embed Actor bodies.
6. [x] **PersistHandlers** — Persist seam already landed by [[issues/31-one-coremsg-loop-parameterized-persist.md|One CoreMsg loop parameterized persist]] and [[issues/32-move-persist-agents-under-coremailbox.md|Move persist agents under CoreMailbox]]. Hello does not widen it. Actor cases stay off this parameter. File and Db are not Actor mailboxes. Interface on **PersistHandlers**.
7. [x] **Credentialed Change posts** — Browser and Actor posts validate via **CoreMsg** before PersistHandlers; Actor also admits on the live table. Story path 3 is Browser Change posts only.
8. [ ] **Loaded descendant id list** — Shared Zoom-rooted Loaded id walk. Interface on **Loaded descendant id list**. Browser Command `graphIds` and Actor reuse call the same function.
9. [x] **Test seam for this tracer** — Story path 2: harness at CoreActorPool or TestActor. Outer facts assert Graph and lifecycle; TestActor does not assert. HTTP universal-response encoding is not on the outside-proof critical path.

## 4. Alternative considered

1. **Chosen** — One CoreMsg loop owns fast messages (`StartActor`, credentialed admit-before-`PostChange`, `ActorStop`) and live-table access. CoreActorPool.startActor is synchronous prepare (validate, live row, secret) and returns ActorStart; the mailbox writes ActorStarted on mailboxHistory, then pool.schedule fires the body. The mailbox does not wait for the body. CoreActorPool owns the table data, defs, selection, and runner; it does not Use History. Persist mode chooses File or Db handlers; there is no File-with-Db-mirror. Browser Command `graphIds` come from Shared **Loaded descendant id list** (Zoom root + Loaded children only; flat ids; ownership ignored; reusable by Actors). TestActor (outside Core) owns command-Node interpretation (hello) and is passed in as ActorFn. Production callers use the CoreMailbox door; outside proofs may call Pool or Actor at those seams. Mailbox History carries Actor events with Change events; Undo stays Change-only. Matches [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]], Point 0 loop code (tickets 30–32), and Alan’s lock that the live registry is CoreActorPool’s data.
2. **Rejected: mailbox-held second registry** — Keep live rows only in mailbox-loop state and treat the pool as a dumb Task runner. Loses the table as the single live registry; duplicates identity/secret/Focus beside CoreActorPool. Mailbox ownership of access is not a second copy of identity/secret/Focus beside the pool.
3. **Rejected: nested ActorMsg pump / FileAgent twin mailbox** — Restore a second mailbox or per-agent Actor cases (shape in the stashed [[reports/implement-issue-29-testactor-hello.md]]). File and Db must not start Actor-capable processors. Twin queues for Actor work stay rejected. Violates one-mailbox ordering from [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] and the CoreMailbox-only door from ticket 32.
4. **Rejected: Actor event sequence outside the mailbox** — A separate EventLog / Actor-only sequence beside CoreMailbox. Misfeature; use only CoreMailbox and History, with Actor events on History.
5. **Deferred past hello** — Cancel, live query, host-stop, post-twice, duplicate terminal, and Interrupted restart stay out of the hello stories. `ActorFailed` is in this slice: same drop as success; TestActor exceptions stop as `ActorFailed`. Record only; do not ticket from Unsettled.

## 5. Unsettled

- Cherry-pick Undo (invert a chosen ChangeEvent; skip Actor Events). Deferred past hello; not a ticket yet.
- Restore older file versions via actual git (from the commits already made on edits). Deferred past hello; not a ticket yet.
