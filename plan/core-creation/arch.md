# Core creation architecture

Spec: [[issues/Implementation Planning and Record.md]] (Phase 2 Spec); hello stories from [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], and [[issues/29-prove-testactor-hello.md|Prove TestActor hello]]. No Project `spec.md` yet.
Updated: 2026-09-13
Sequence: tracer-cut

Feature under design: the hello slice of the one-mailbox Actor program locked by [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Prefer existing seams. Do not open Wayfinder map tickets for items under Unsettled. Story hops name modules and doors; State / Interface / Uses live only under Module map (thin hops / fat map).

Implementation status for this cut: Point 0 ([[issues/30-reshape-coreactorpool-synchronized-table.md]], [[issues/31-one-coremsg-loop-parameterized-persist.md]], [[issues/32-move-persist-agents-under-coremailbox.md]]) is implemented. [[issues/29-prove-testactor-hello.md|Prove TestActor hello]] sections 1ff are not.

## 1. Story paths

1. **Browser Run hello**
   1. [ ] Browser Run (`?` → one-Node Command)
   2. [ ] HTTP Adapter encodes Command + credentials
   3. [ ] CoreMailbox `startActor`
   4. [ ] CoreMsg / CoreMailboxBackend validates and hands off
   5. [ ] CoreActorPool.startActor (expand, select `test`, create, ActorStarted, schedule)
   6. [ ] TestActor interprets command Node → `hello`
   7. [ ] CoreMsg admit-before-`PostChange`
   8. [ ] CoreMsg `ActorStop` of `ActorSucceeded`
   9. [ ] History appends ActorFinished
   10. [ ] universal response when that path is exercised
   11. [ ] Browser shows one Owned child text `hello`

   [[issues\Implementation Planning and Record.md]] should be referenced when considering tickets for this story path.

2. **Outside Core lifecycle proof**
   1. [ ] Test harness (no Agent transport; not HTTP-only)
   2. [ ] Enter at CoreActorPool.startActor or at TestActor body input
   3. [ ] When at Pool: expand, select `test`, create, pass body input
   4. [ ] When at Actor: TestActor interprets → `hello`
   5. [ ] Full lifecycle still uses CoreMailbox / CoreMsg for admit-before-`PostChange` and `ActorStop`
   6. [ ] CoreActorPool table and History
   7. [ ] Outer asserts Graph, ActorStarted, one ActorFinished, dropped live row, revoked secret (when full lifecycle)
   8. [ ] TestActor does not assert
   9. [ ] Does not require HTTP Adapter universal-response encoding
3. **Browser Change posts**
   1. [ ] Browser Change submit
   2. [ ] HTTP Adapter encodes Change + credentials
   3. [ ] CoreMailbox `postChange`
   4. [ ] CoreMsg validates Browser credentials before PersistHandlers

Composition: Each Story path above is a complete end-to-end hop list. Shared segments below factor hop sequences that appear in more than one path (for core modules / test seam); they are not missing hops to splice into a path.

Shared segments across 1 and 2:
1. [ ] StartActor through HTTP / Core / Pool
2. [ ] CoreActorPool start (expand, select, create, pass)
3. [ ] TestActor `hello`
4. [ ] CoreMailbox / CoreMsg when full lifecycle
5. [ ] CoreActorPool live table
6. [ ] History for Actor lifecycle
7. [ ] admit-before-`PostChange`
8. [ ] `ActorStop`

Shared segment with path 3:
1. [ ] Credentialed `PostChange` through CoreMsg

Narrowest shared test seam:
1. [ ] CoreActorPool start and/or TestActor body input — not HTTP encoding, not PersistHandlers, not TestActor private helpers

## 2. Module map

Deltas for this Project’s hello / one-mailbox Actor program. Persist fillings and the generic CoreMailbox door from Point 0 stay; this map names how Actor work and credentialed Change posts attach. There is no Actor event sequence outside the mailbox: only CoreMailbox and History. Sole home of field shapes, validation, and event policy for this cut.

1. **CoreMsg / CoreMailboxBackend**
   1. [ ] State: the one ordered mailbox loop
   - Interface:
     1. [ ] match `CoreMsg` cases
     2. [ ] for `StartActor`, carry `zoomId`, `focusId`, `commandId`, `graphIds` (same fields as HTTP transport and Core door); validate caller Authority and secret then hand off async to CoreActorPool.startActor
     3. [ ] for every `PostChange` (Browser or Actor), require and validate Authority and secret before PersistHandlers
     4. [ ] for Actor `PostChange`, admit against the live table (same credential fields, live-row check)
     5. [ ] for `ActorStop` of `ActorResult` (`ActorSucceeded` only in this slice), append ActorFinished on History, drop the live row and secret, request terminate without waiting
   - Uses:
     1. [ ] CoreActorPool
     2. [ ] CoreCredentials / CallerTable
     3. [ ] History
     4. [ ] PersistHandlers (persist cases only)
2. **CoreMailbox**
   1. [ ] State: none beyond MailboxHost
   - Interface:
     1. [ ] public door on MailboxHost — `startActor` with `zoomId`, `focusId`, `commandId`, `graphIds`; credentialed `postChange` (Browser and Actor); `actorStop`; lifecycle event read from History
     2. [x] existing getState / getRevision / getChangesSince / postGraphOnlyChange / createFile / createDb
   - Uses:
     1. [ ] MailboxHost
     2. [ ] CoreMsg
3. **CoreActorPool**
   - State:
     1. [ ] synchronized live table (public Actor identity, secret, termination handle, Focus NodeId)
     2. [ ] registered `ActorFn` defs
     3. [ ] thread-pool runner
   - Interface:
     1. [ ] `register`
     2. [ ] `startActor` receives `zoomId`, `focusId`, `commandId`, `graphIds` (same shape as HTTP / Core)
     3. [ ] expand `graphIds` to a Graph
     4. [ ] read the command Node and select Actor kind (`test` in this slice); create the matching Actor
     5. [ ] create identities and secret, write live row, append ActorStarted on History via the mailbox path, schedule body on the thread pool
     6. [ ] pass Graph plus named `zoomId`, `focusId`, `commandId` and the Actor secret into the Actor
     7. [ ] `admit`, `drop`, `isLive`
     8. [ ] keep existing launch/query/lock helpers until later tickets replace them
   - Uses:
     1. [ ] CoreCredentials
     2. [ ] History (ActorStarted / ActorFinished live on History; no separate EventLog)
     3. [ ] `ActorFn` / TestActor
4. **History**
   1. [ ] State: ordered past/future sequence extended to carry Actor events alongside Change events
   - Interface:
     1. [ ] append Change and Actor lifecycle events (ActorStarted, ActorFinished) on the one sequence
     2. [ ] Undo / Redo still target Change events only
     3. [ ] no second Actor-only event log beside CoreMailbox
   - Uses:
     1. [ ] Change / Actor event records as History members
5. **TestActor**
   1. [ ] State: none
   - Interface:
     1. [ ] `ActorFn` for Actor name `test`
     2. [ ] input is Graph plus named `zoomId`, `focusId`, `commandId` and the Actor secret (Pool expands ids; does not invent further payload fields)
     3. [ ] interpret the command Node and switch on case text
     4. [ ] this slice has only `hello` (post one Owned child text `hello` under Focus through admitted `PostChange`, then queue `ActorStop ActorSucceeded`)
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
     4. [ ] Browser-originated Change posts supply Authority and secret (Story path 3)
   - Uses:
     1. [ ] HTTP Adapter
     2. [ ] AmbleRun
     3. [ ] Loaded descendant id list
8. **HTTP Adapter**
   1. [ ] State: none (transport)
   - Interface:
     1. [ ] decode Browser Command / Change / Poll
     2. [ ] Command / StartActor transport fields are `zoomId`, `focusId`, `commandId`, `graphIds` — same shape as the Core door and CoreActorPool.startActor
     3. [ ] Change posts carry credentials; pass Authority and secret into CoreMailbox `postChange`
     4. [ ] call Core through CoreMailbox or CoreRuntime-bound members
     5. [ ] encode universal `{ nodes; events; latestId }` for Command when that path is exercised (spec lock; not critical path for the hello outside proof)
   - Uses:
     1. [ ] CoreMailbox / CoreRuntime
9. **CoreRuntime**
   1. [ ] State: composed credentials and registered Actors
   - Interface:
     1. [ ] host composition
     2. [ ] `CoreActorPool.register` for TestActor at startup
   - Uses:
     1. [ ] CoreMailbox
     2. [ ] CoreActorPool
     3. [ ] TestActor
     4. [ ] CoreCredentials
10. **PersistHandlers**
   1. [x] State: File or Db persist implementation behind the loop
   - Interface:
     1. [x] getState, getRevision, getChangesSince, postChange, postGraphOnlyChange, snapshotDone
     2. [x] Actor cases are not on this parameter
   - Uses:
     1. [x] FileAgent / DbAgent fillings only

## 3. Seams

1. [ ] **CoreMailbox door** — External seam for production and HTTP. Interface on **CoreMailbox**.
2. [ ] **CoreMsg union** — Internal one-mailbox seam. Interface on **CoreMsg / CoreMailboxBackend**. No second Actor mailbox or nested `ActorMsg` pump in this slice.
3. [ ] **CoreActorPool table and start** — Live registry plus start seam. Interface on **CoreActorPool**. Table is the registry; mailbox loop state holds no second copy.
4. [ ] **History** — Interface on **History**. One sequence; no Actor event log outside CoreMailbox.
5. [ ] **ActorFn / TestActor input** — Definition and body-input seam. Interface on **TestActor**; register via **CoreActorPool**. Core does not embed Actor bodies.
6. [x] **PersistHandlers** — Persist seam already landed by [[issues/31-one-coremsg-loop-parameterized-persist.md|One CoreMsg loop parameterized persist]] and [[issues/32-move-persist-agents-under-coremailbox.md|Move persist agents under CoreMailbox]]. Hello does not widen it. Interface on **PersistHandlers**.
7. [ ] **Credentialed Change posts** — Browser and Actor posts validate via **CoreMsg** before PersistHandlers; Actor also admits on the live table. Story path 3 is Browser Change posts only.
8. [ ] **Loaded descendant id list** — Shared Zoom-rooted Loaded id walk. Interface on **Loaded descendant id list**. Browser Command `graphIds` and Actor reuse call the same function.
9. [ ] **Test seam for this tracer** — Story path 2: harness at CoreActorPool or TestActor. Outer facts assert Graph and lifecycle; TestActor does not assert. HTTP universal-response encoding is not on the outside-proof critical path.

## 4. Alternative considered

1. **Chosen** — One CoreMsg loop owns fast messages (`StartActor`, credentialed admit-before-`PostChange`, `ActorStop`). CoreActorPool is the synchronized live table and thread-pool runner; it owns Actor-kind selection from the command Node and expands id payload to Graph + named ids + secret for the Actor. Browser Command `graphIds` come from Shared **Loaded descendant id list** (Zoom root + Loaded children only; flat ids; ownership ignored; reusable by Actors). TestActor owns command-Node interpretation (hello). Production callers use the CoreMailbox door; outside proofs may call Pool or Actor at those seams. History carries Actor events with Change events; Undo stays Change-only. Matches [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]], Point 0 (tickets 30–32), and Alan’s lock that the live registry is CoreActorPool’s data.
2. **Rejected: mailbox-held second registry** — Keep live rows only in mailbox-loop state and treat the pool as a dumb Task runner. Loses the table as the single live registry; duplicates identity/secret/Focus beside CoreActorPool; fights ticket 30’s synchronized-table reshape.
3. **Rejected: nested ActorMsg pump / FileAgent twin mailbox** — Restore a second mailbox or per-agent Actor cases (shape in the stashed [[reports/implement-issue-29-testactor-hello.md]]). Violates one-mailbox ordering from [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] and the CoreMailbox-only door from ticket 32.
4. **Rejected: Actor event sequence outside the mailbox** — A separate EventLog / Actor-only sequence beside CoreMailbox. Misfeature; use only CoreMailbox and History, with Actor events on History.
5. **Deferred past hello** — Cancel, Failed, live query, host-stop, post-twice, duplicate terminal, and Interrupted restart stay out of the hello stories. Record only; do not ticket from Unsettled.

## 5. Unsettled

None.
