# Asynchronous Core task manager facts

Date: 2026-09-05

Purpose: Fact and design-support inventory for a post–initial Core Changes discussion about asynchronous behavior, small increments, and a Core task manager. This report does not choose product decisions or design code. It does not edit the initial Core Changes implementation plan. Related: [[plan/core-creation/project.md]], [[plan/core-creation/map.md]], [[plan/core-creation/initial-core-changes-implementation.md]], [[CONTEXT.md]], [[plan/core-creation/reports/kernel-fsproj.md]], [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[.agents/skills/codebase-design/SKILL.md]].

## Deep Module frame

**Core** is locked as a deep Module: small **Interface** (**Core API** — Files, Changes, Query, Command), large **Implementation** (apply, persist ports, Actor pool). A hypothetical "task manager" must earn its keep at the **external seam** or stay an **internal seam** inside Core Implementation. The deletion test: if callers must learn launch, identity, cancel, finish, shutdown, and output admission separately at many call sites, the Module is shallow; if Command plus Changes covers most behavior, the pool deepens Core.

## 1. Asynchronous and small increments — Interface vs Implementation

### At the Core Interface (what callers must know)

**Asynchronous** in existing plans means: **Command** launch returns after spawn, not after the Actor finishes; the apply queue stays available while the Actor runs; Browsers learn results through **Poll**, not a completion push ([[plan/event-sourced-ops/details/actors-and-jobs.md]], [[CONTEXT.md]]). Launch acceptance is immediate job registration plus Core-owned job identity ([[plan/core-creation/issues/09-define-core-command-launch-contract.md]]). Finish and failure are terminal job states observable later ([[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]]). **Cancel** stops further Actor output; it is not **Undo** ([[plan/event-sourced-ops/details/soft-lock.md]]).

**Small increments** at the Interface can mean two different things already present in code and Roadmap language:

1. **Incremental Change output** — an Actor admits work as multiple **Changes** through **Core Changes**, each becoming a **Revision** step visible on **Poll** (today: Graph-only chunk posts at `maxOps = 80` via [[src/Server/GraphOnlyChangePost.fs]] and [[src/Shared/GraphOnlyChangeChunks.fs]]).
2. **Incremental user operations** — **Upload** and **Load** sending modest amounts then more, abortable mid-stream without full redo ([[plan/roadmap/epics/chapters/incremental-operations.md]], [[plan/roadmap/epics/robust-outliner.md]]). That Chapter is blocked by Actors supported and is product-facing transport/residency, not the Core pool contract.

The Interface does not yet specify progress reporting beyond **Poll** tail visibility, job-state **Query**, or partial-failure rollback across chunks.

### Inside Core Implementation (hidden from callers)

Implementation may use: tasks off the apply mailbox; a job registry mapping identity to cancellation source and running task ([[plan/event-sourced-ops/details/actors-and-jobs.md]]); sequential mailbox processing for each admitted **Change**; bounded wall-clock steps (`runBounded`, 8000 ms today in [[src/Server/FileAgent.fs]]); background document snapshot after Db commit ([[src/Server/DbAgent.fs]] `startSnapshot`); chunk splitting inside Actor definitions. Fairness, queue depth limits, and pool sizing are not specified. Process crash isolation is explicitly **not required** — abort a hung Actor; Graph stays consistent ([[plan/core-creation/reports/kernel-fsproj.md]]).

Callers should not need to know whether finish uses inner apply with Change objects (recommended) versus today's JSON encode detour ([[plan/event-sourced-ops/details/actors-and-jobs.md]] — entry packaging is don't-care; seam recommendation is inner apply).

## 2. Where a Core task manager belongs — seam options

| Option | Role | Fit with locks |
| --- | --- | --- |
| **A. Fifth Core API call ("Task" or "Jobs")** | New Interface surface for lifecycle | **Poor.** Core API is locked at four calls ([[CONTEXT.md]], [[plan/core-creation/reports/kernel-fsproj.md]]). Adds Interface breadth without a named variation point. |
| **B. Command launch + pool Implementation** | **Command** selects Actor definition and returns job identity; pool runs tasks; **Changes** admits output | **Strong.** Matches CONTEXT, kernel report, issue 02, and issue 09. Pool is **in** Core; definitions are **out**. |
| **C. Changes-only with implicit jobs** | Every Actor post is anonymous; no Command | **Weak.** Contradicts Command as launch, job identity, cancel, and multi-job shell/Agent direction ([[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/llm-connector/map.md]]). |
| **D. Internal registry; HTTP Adapter exposes jobs** | Core pool internal; routes expose cancel/status | **Partial.** ESO issue 09 owns Browser job access and soft-lock chrome; Core owns machinery ([[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]]). HTTP is Adapter, not Core API. |
| **E. Query exposes job snapshot** | **Query** answers job state alongside Graph facts | **Possible complement to B.** Issue 08 is open and does not yet say whether job observability belongs in **Query** or only via **Poll** plus identity held by the Adapter. |

**Recommendation from existing docs (not a product lock):** treat "task manager" as **Core Implementation behind Command and Changes**, not a new public Interface. **Command** = launch seam; **Changes** = output admission seam; optional **Query** = job observability if grilling resolves it.

Do **not** HTTP self-post ([[plan/event-sourced-ops/details/actors-and-jobs.md]]). Do **not** add a separate public apply Interface for Actors.

## 3. Lifecycle facts callers truly need

Facts the Interface must eventually expose or guarantee, grouped by concern:

| Concern | Caller need | Current capture |
| --- | --- | --- |
| **Launch acceptance** | Know spawn succeeded; receive Core-owned job identity | Issue 09 (open grilling) |
| **Identity** | Stable handle for cancel, soft-lock, Browser chrome | Issue 09; ESO 09 for Browser access |
| **Progress** | Know work is ongoing or how far it ran | **Not in issues 09–12.** Partial proxy today: each admitted **Change** on **Poll** |
| **Cancellation** | Stop further output; already-merged **Changes** stay | Issue 10; issue 02 checklist |
| **Cancel-after-enqueue** | Behavior when **Change** is in apply mailbox but not applied | Issue 10 (open). Assessment recommends **no** cancel-after-enqueue ([[plan/event-sourced-ops/details/actors-and-jobs.md]]) — tension to resolve in grilling |
| **Completion / failure** | Terminal job state; error or success to Adapter | Issue 11 (open) |
| **Output admission** | Each output attempt goes through **Core Changes** / inner apply; Reject vs amend vs dedup | Issue 10; merge rules after arrival are accepted (ESO architecture) |
| **Fairness / backpressure** | Apply queue stays responsive; long work off queue | Issue 02 intent; not quantified (no max jobs, no priority) |
| **Restart / shutdown** | Cancel or await running jobs; treat enqueued batches; terminal info before exit | Issue 12 (open). No process crash isolation requirement |
| **Consumption** | Other Browsers: **Poll**, rewind/replay on amendment | Accepted ESO behavior; not re-litigated in Core issues 09–12 |

**Progress** is the largest gap: neither Command nor Poll alone specifies durable job percent, staged Parse, or chunk index — only sequential **Revision** bumps per admitted **Change**.

## 4. Captured by issues 02 and 09–12 vs still open

### Issue 02 — Core Actor pool (implementation, needs-info)

**Captured intent:** launch off apply queue; Core-owned job identity; cancel further output; finish through **Core Changes** and inner apply; apply queue available while Actor runs; cancellation design before implementation; definitions and soft-lock UI out of scope.

**Still open in 02:** exact pool packaging, registry shape, task lifetime API, and all detail now delegated to grilling tickets 09–12.

### Issue 09 — Command launch (grilling, open)

**Question locked:** typed Command input, job identity and initial state at launch, what Core retains, without defining Parse/shell/Agent behavior.

**Open:** input shape, retained fields, sync vs async return semantics at the typed boundary, relationship to Actor definition registration.

### Issue 10 — Cancellation and output admission (grilling, open)

**Question locked:** exact cancel cutoff; admit/refuse each output through **Changes**; cancel-after-enqueue.

**Open:** cutoff instant; whether assessment "no cancel-after-enqueue" becomes a Committed Decision; interaction with batch atomicity inside one mailbox message.

### Issue 11 — Finish and failure (grilling, open)

**Question locked:** terminal job state; caller-visible result/error; effect of accepted, deduplicated, or rejected final **Changes**.

**Open:** one **Change** vs a set at finish ([[plan/event-sourced-ops/details/actors-and-jobs.md]] packaging gap); whether Actor receives produced sequence from inner apply (recommended in assessment) vs HTTP-style bare ack.

### Issue 12 — Shutdown (grilling, open)

**Question locked:** running/queued Actors on Server/Core shutdown; enqueued **Change** batches; terminal observability; no crash isolation.

**Open:** await vs fire-and-forget; interaction with `runBounded` abandoned tasks; DbAgent startup `isReady` gate vs in-flight jobs.

### Issues 07–08 (Files, Query)

Not async-specific. Relevant only if **Query** carries job snapshots or **Files** participates in incremental Upload (later Chapter).

### Accepted outside those tickets (not re-decided in 09–12)

- Merge/amend after **Change** arrival ([[plan/event-sourced-ops/architecture.md]]).
- Long-running Actor off queue; conclusion via same apply path ([[plan/event-sourced-ops/details/actors-and-jobs.md]]).
- **Load** stays Graph transfer; **Poll** conveys **Ops** ([[CONTEXT.md]]).
- Soft-lock meaning and job-owned lifecycle direction ([[plan/event-sourced-ops/details/soft-lock.md]]); Browser surface in ESO 09.
- Four-call **Core API**; pool inside Core; definitions outside ([[plan/core-creation/reports/kernel-fsproj.md]]).

## 5. Existing code constraints (long-running and chunked work)

These bound design discussion; they are not target behavior.

| Work | Today | Constraint for async Core |
| --- | --- | --- |
| **Parse** | HTTP request reads State, plans off mailbox, **awaits** `postGraphOnlyChange`; bare `{ok:true}` ack ([[src/Server/Api.fs]]) | Not yet launch-and-return; single Change (not chunked); revision CAS mismatch vs amend-on-success ([[plan/event-sourced-ops/details/actors-and-jobs.md]]) |
| **Lazy-load / git reconciliation** | HTTP/git callback; plans ops; `postChunks` sequential Graph-only posts ([[src/Server/LazyLoadReconciliationServer.fs]]) | Multi-**Change** increment already; **awaits** full chunk chain; partial apply on mid-chain Error ([[plan/core-creation/reports/initial-core-changes-runtime-facts.md]] gap 6) |
| **Graph-only chunks** | `maxOps = 80`; fresh `changeId` per chunk; revision +1 per success ([[src/Shared/GraphOnlyChangeChunks.fs]]) | Increment = multiple **Revisions**; not atomic across chunks |
| **Browser Changes** | POST `/ambit/changes`; mailbox serializes apply | Apply queue is single-threaded per agent mailbox |
| **File persist** | `runBounded` 8000 ms; timeout abandons Task which may still write ([[src/Server/FileAgent.fs]]) | Long Actor work can exceed bounded apply/persist unless off-queue planning only |
| **Db persist** | One tx per batch; then optional background snapshot | Snapshot async; `isReady` gates mutations during startup sweep |
| **Mirror mode** | File ack authoritative; Db post best-effort ([[src/Server/Api.fs]]) | Two mailboxes; sequencing file then db only |

No `CancellationToken`, job registry, or Command launch exists in Server code today. Parse and reconciliation are request-scoped **Async** workflows that block until posts complete.

## 6. Concrete design directions and risks

### Direction 1 — Command + Changes deep Module (aligned with locks)

**Command** returns identity immediately; Actor definition runs off queue; each output batch calls inner **Changes**; terminal state via issue 11 contract; Browsers use **Poll**; Adapter may hold identity for cancel (ESO 09).

**Risks:** shallow Interface if Command grows many launch variants; cancel-after-enqueue vs assessment; multi-chunk partial failure without rollback; timeout-abandon on persist vs long jobs.

### Direction 2 — Chunked output as the increment model

Standardize "small increments" as sequential **Changes** through **Changes**, not streaming **Ops** outside History. Matches lazy-load and future large Parse tails.

**Risks:** **Poll** storms; client rewind/replay cost; job "progress" = count of **Revisions** may be wrong semantically; packaging one vs many **Changes** at finish still open.

### Direction 3 — Query-backed job observability

Keep Command minimal; add job snapshot or status to **Query** (issue 08 grilling) so Adapters poll job state without widening HTTP beyond Core API patterns.

**Risks:** two poll loops (Actions vs jobs); **Query** consistency with authoritative Graph **Revision** unspecified; duplicates Browser chrome concerns if not careful with ESO 09 ownership.

### Direction 4 — Strict serial apply, parallel plan

Many Actors plan in parallel; all output serializes through one apply Implementation (current mailbox model). Simple **locality** for merge rules.

**Risks:** head-of-line blocking; no fairness spec; large Parse plan still contends with Browser edits at admission time.

### Cross-cutting risks

- **Initial Core Changes increment** deliberately excludes pool ([[plan/core-creation/map.md]], [[plan/core-creation/initial-core-changes-implementation.md]]). Async discussion is **post-increment**; Parse realignment (ESO 08) may still be request-scoped until pool lands.
- **Incremental Upload/Load** ([[plan/roadmap/epics/chapters/incremental-operations.md]]) is a separate Epic beat from pool machinery — conflating them widens Core Interface prematurely.
- **Soft-lock** without job identity forces advisory policy before pool exists — ESO 08 explicitly allows Parse proof without multi-job chrome.

## 7. Relation to initial Core Changes increment

The bounded increment delivers typed **Changes** through `GraphAgentHandle`, sole-writer path for runtime posts, and preserves current timeout/mirror/mailbox behavior ([[plan/core-creation/initial-core-changes-implementation.md]], [[plan/core-creation/reports/initial-core-changes-runtime-facts.md]]). It explicitly does **not** implement Command, pool, cancel, or async launch. Issue 02 is blocked by issue 01 and issue 12 in the tracker — sequencing places pool after Changes seam and shutdown contract grilling.

Discussion of "program behaves asynchronously" and "Core task manager" belongs to the **next** Core frontier: issues 09–12 grilling, then issue 02 implementation, then Parse realignment as first Actor definition using the pool.

## 8. Suggested grilling order (for interview)

1. Confirm task manager = pool **Implementation** behind **Command** + **Changes**, not a fifth API call.
2. Separate **incremental Change output** (in scope for pool) from **incremental Upload/Load** (later Chapter).
3. Resolve cancel-after-enqueue vs assessment recommendation (issue 10).
4. Decide whether **progress** is **Poll**-only, **Query** job fields, or both.
5. Lock finish packaging: one **Change** vs sequential chunks vs batch at terminal apply (issues 10–11 and actors-and-jobs packaging gap).
6. Scope shutdown vs abandoned `runBounded` tasks (issue 12 vs ACID Chapter).
