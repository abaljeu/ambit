# Core Wayfinder fact inventory

Date: 2026-09-05

Purpose: Local evidence inventory for the breadth-first Wayfinder grilling round. The settled destination is an implementation-ready initial Core increment, with later Core detail charted but not fully specified now. This report records facts and open choices. It does not resolve the choices.

## 1. Explicitly decided

### Core language and boundary

- The Module is **Core**. Its Interface is **Core API**. The four calls are Files, Changes, Query, and Command. Core API is not the web API. [[CONTEXT.md]] and [[plan/core-creation/reports/kernel-fsproj.md]] state this directly.
- Core owns persistent state: the durable Graph and History facts, file bytes, git of those files, and the Actor pool. Core is the sole Server Graph writer. [[CONTEXT.md]]
- Core does not own advanced logic. Parse algorithms, Document codecs, Graph↔document persist algorithms, reconcile, and Actor definitions stay outside Core and work through Core API. [[CONTEXT.md]], [[plan/core-creation/reports/kernel-fsproj.md]]
- The Browser and Server share the Fable apply implementation for Graph, Op, Change, History apply, and amendment. The Server produce seam is separate .NET work. [[plan/core-creation/reports/kernel-fsproj.md]], [[src/Shared/Gambol.Shared.fsproj]], [[src/Server/Gambol.Server.fsproj]]
- Core does not get a new fsproj. The allowed physical shape is a folder or function seam in the existing projects. [[plan/core-creation/reports/kernel-fsproj.md]]
- The Actor pool is inside Core. Actor definitions, including Parse and a later Agent, stay outside Core. Actors submit Change objects through Core Changes and inner apply. They do not send HTTP requests to the Server itself. [[CONTEXT.md]], [[plan/core-creation/reports/kernel-fsproj.md]], [[plan/event-sourced-ops/details/actors-and-jobs.md]]

### Change and Actor behavior after a Change reaches apply

- The Server gives Changes one global arrival order. It amends the newest Change against the common prior plus all other accepted Changes, then applies and logs it. [[plan/event-sourced-ops/architecture.md]]
- A recoverable collision is merge success, not Reject. A Browser consumes external Changes by Poll, rewind, and replay. [[plan/event-sourced-ops/architecture.md]], [[plan/event-sourced-ops/issues/03-server-amends-recoverable-field-collisions.md]], [[plan/event-sourced-ops/issues/04-client-consumes-merge-success-without-reload.md]]
- A long-running Actor must run off the apply queue. Its concluding Change returns to the same apply queue. Other Browsers learn through Poll; there is no completion push. [[plan/event-sourced-ops/architecture.md]]
- Cancel is not Undo. Cancel stops later Actor output and does not reverse Changes that already merged. The soft-lock meaning is advisory, and edits under it remain legal. [[plan/event-sourced-ops/details/soft-lock.md]]
- Load packages remain Graph transfer. They are not Change replay. [[plan/event-sourced-ops/architecture.md]]

### Project ownership and Roadmap order

- [[plan/core-creation/project.md]] owns Core extraction, the shared Server Changes path, and Core Actor pool machinery. [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] owns the Changes path. [[plan/core-creation/issues/02-core-actor-pool.md]] owns pool machinery. [[plan/core-creation/reports/create-project-reorganization.md]]
- Parse is the first Actor definition but stays in ESO. [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]] owns that definition. Advisory soft-lock policy and Browser job access stay in [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]].
- Roadmap order is Initial Core, Actors supported, ACID apply, then Incremental operations. [[plan/roadmap/epics/robust-outliner.md]], [[plan/roadmap/epics/chapters/initial-core.md]], [[plan/roadmap/epics/chapters/actors-supported.md]], [[plan/roadmap/epics/chapters/acid-apply.md]], [[plan/roadmap/epics/chapters/incremental-operations.md]]
- The accepted Core construction order inside the Project is apply and amendment first, then the Actor pool. Parse follows as the first Actor definition. ACID cleanup and incremental Upload and Load follow later. [[plan/core-creation/reports/kernel-fsproj.md]]

### Current code facts

- There is no Core module or Core API implementation in the current project compile lists. Shared apply and amendment are compiled as Shared modules. Server behavior is compiled through FileAgent, DbAgent, Api, and RouteRegistration. [[src/Shared/Gambol.Shared.fsproj]], [[src/Server/Gambol.Server.fsproj]]
- FileAgent and DbAgent each have an applyBatch copy. Both accept JSON strings in mailbox messages and decode inside the agent. [[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]]
- RouteRegistration reads the HTTP body and passes the string through Api and AgentHandle. The HTTP Adapter does not currently decode Change objects before the mailbox. [[src/Server/RouteRegistration.fs]], [[src/Server/Api.fs]]
- FileAgent and DbAgent are separate Graph writers. The file-authority path can also mirror the original body to DbAgent after FileAgent accepts it. [[src/Server/Api.fs]]
- Parse currently reads a State snapshot, plans outside the mailbox, encodes its Ops as a graph-only Change, submits by postGraphOnlyChange, and returns a bare success object. [[src/Server/Api.fs]], [[src/Server/GraphOnlyChangePost.fs]]
- DbAgent commits amended ChangeLog rows and the resulting database projection in one PostgreSQL transaction. Before that transaction, it can write affected document files. It publishes the in-memory State after the commit. [[src/Server/DbAgent.fs]], [[plan/core-creation/reports/current-edit-core-reconciliation.md]]
- FileAgent is currently file-write authoritative. Its bounded persistence task can continue after timeout. This is current behavior to replace later, not the proposed view-only file mode. [[src/Server/FileAgent.fs]], [[plan/core-creation/reports/current-edit-core-reconciliation.md]]

## 2. Actual decisions for the initial implementation-ready increment

These choices are not resolved by the current files.

1. **Increment completion boundary.** Must the first implementable increment deliver only the shared Core Changes path, or must it also deliver the minimum Core Actor pool? [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] can stand before the pool, but [[plan/roadmap/epics/chapters/initial-core.md]] defines Initial Core as the boundary plus Actor pool.
2. **Core API coverage in the first increment.** The four call names are locked, but the files do not say whether Files, Query, and Command need callable initial contracts now, may be thin wrappers over current behavior, or remain charted for a later increment. [[CONTEXT.md]], [[plan/core-creation/reports/kernel-fsproj.md]]
3. **Core Changes contract.** The exact input batch shape, result type, failure shape, deduplication result, and separation between an internal Actor result and the HTTP Post signal are not specified. ESO accepts that HTTP Post signals and Poll conveys the Change list, while the Actor notes recommend that inner apply return the produced sequence. [[plan/event-sourced-ops/details/messaging.md]], [[plan/event-sourced-ops/details/actors-and-jobs.md]]
4. **Initial authority reach.** “Core is the sole Server Graph writer” needs an initial-increment boundary. The files do not decide whether startup repair, graph-only maintenance, FileAgent/DbAgent mode selection, and every non-Change startup writer must move behind Core now, or remain named temporary exceptions until ACID apply. [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]], [[plan/core-creation/reports/solid-core-module-fit.md]]
5. **Behavior preservation during extraction.** The files do not state whether the initial Core extraction must preserve the current file-authority, database, acknowledgement, timeout, and mirror behavior exactly, except for routing Changes through one seam. This matters because file-mode view-only and the db-authority sequence are assigned to the later ACID Chapter. [[plan/core-creation/reports/current-edit-core-reconciliation.md]], [[plan/roadmap/epics/chapters/acid-apply.md]]
6. **Physical seam inside existing projects.** “No fsproj” is decided, but the exact module names, compile order, dependency direction, ownership of the mailbox, and split between Shared apply and Server produce are not specified. [[plan/core-creation/reports/kernel-fsproj.md]], [[src/Shared/Gambol.Shared.fsproj]], [[src/Server/Gambol.Server.fsproj]]
7. **Minimum pool contract, if the pool is in the first increment.** Exact Command launch input, Core-owned job identity, job registry state, task lifetime, cancellation observation, output admission, cancel-after-enqueue behavior, finish result, and shutdown behavior remain undecided. [[plan/core-creation/issues/02-core-actor-pool.md]], [[plan/event-sourced-ops/details/actors-and-jobs.md]]
8. **Proof of completion.** The implementation issues name behavioral outcomes, but no initial-increment specification says which existing producers must use Core Changes, which temporary exceptions are allowed, or what evidence proves that no second Server Graph writer remains.

## 3. Later detail or dependency

- Parse realignment is after Initial Core and belongs to Actors supported. It proves the first Actor definition, not Core pool machinery. [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]], [[plan/roadmap/epics/chapters/actors-supported.md]]
- Advisory soft-lock issuance, expiry, Browser chrome, inspect, and cancel access belong to ESO issue 09. They are not needed to prove the first Changes seam. [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]], [[plan/event-sourced-ops/details/soft-lock.md]]
- The db-authority transaction boundary, view-only file mode, removal of timeout-abandon writes, startup bypass writers, and owner-edge database repair belong to ACID apply. [[plan/roadmap/epics/chapters/acid-apply.md]], [[plan/core-creation/reports/current-edit-core-reconciliation.md]]
- Durable progress or safe replay for asynchronous individual-document persistence is later ACID detail. It must not be silently pulled into the first extraction only because the proposed db-authority sequence mentions a queue. [[plan/core-creation/reports/create-project-reorganization.md]], [[plan/core-creation/reports/current-edit-core-reconciliation.md]]
- Whether view-only file mode allows Files send is unresolved later mode detail unless the first increment implements Files send. [[plan/core-creation/reports/create-project-reorganization.md]]
- Incremental Workspace Upload and Load belong after Actors supported. [[plan/roadmap/epics/chapters/incremental-operations.md]]
- Completing Ops beyond its accepted same-Change timing remains proposed and is tracked separately. [[plan/event-sourced-ops/details/completing-ops.md]], [[plan/event-sourced-ops/issues/11-completing-ops-pattern-beyond-timing.md]]
- Actor residency packaging, one Change versus a set of Changes, a shell command definition, a later Agent definition, permanent History, unrestricted Undo, and process crash isolation are not required for the initial Core increment. [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/event-sourced-ops/details/open-questions.md]], [[plan/roadmap/epics/robust-outliner.md]]

## 4. Contradictions, stale names, and tracker-shape problems

1. **A stale report filename uses retired language.** [[plan/core-creation/reports/kernel-fsproj.md]] says the name is Core and rejects the retired name, but its own filename and heading still use that retired name.
2. **Sole-writer scope conflicts with deferred work.** Core issue 01 requires Core to be the sole Server Graph writer, while [[plan/core-creation/reports/solid-core-module-fit.md]] leaves non-Change startup writers for ACID cleanup. The first increment cannot satisfy both statements without naming temporary exceptions or moving that work forward.
3. **Initial Core has two possible sizes.** [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] is independently ordered before the pool, but [[plan/roadmap/epics/chapters/initial-core.md]] treats boundary and Actor pool as one Chapter requirement. The destination needs an explicit first-increment done boundary.
4. **Issue 01 is marked ready before its decisions are ready.** Its Context says the entry seam and packaging remain proposed, but its Status is ready-for-agent. It also says to lock the seam lightly during implementation. That is implementation work mixed with an unresolved Wayfinder decision.
5. **Issue 02 is an implementation issue that contains decision work.** Its Status is needs-info, and its checklist requires cancellation and cancel-after-enqueue design before implementation. Exact API and packaging questions are in reports instead of Wayfinder decision tickets.
6. **Issue 11 is marked ready although its subject is proposed.** [[plan/event-sourced-ops/issues/11-completing-ops-pattern-beyond-timing.md]] says the full pattern is proposed but has Status ready-for-agent and asks implementation to build a “locked” pattern.
7. **The Project has no Wayfinder decision-ticket frontier.** [[plan/core-creation/project.md]] lists two implementation issues and reports. It has no map with Notes, Decisions so far, Not yet specified, and Out of scope, and no child issues with Type plus Wayfinder Status. Unresolved choices are spread through reports and implementation issue prose.
8. **Tracker guidance names the wrong root.** [[doc/agents/issue-tracker.md]] and [[doc/agents/project-status.md]] repeatedly define the tracker under plan, while [[CONTEXT.md]] and the live repository use plan. This makes the required Wayfinder file shape unclear.
9. **Project status is inconsistent in ESO.** [[plan/event-sourced-ops/project.md]] says Stage active, while [[plan/event-sourced-ops/overview.md]] says the project stage is charting.
10. **File-mode wording crosses current and later states.** [[CONTEXT.md]] and the Core boundary report state view-only file mode as the Core rule. Current code is file-write authoritative, and the Roadmap assigns the change to later ACID apply. The initial extraction must state which state it implements without weakening the final rule.
11. **The Files rule is internally incomplete.** Core owns Files send, get, and git, while view-only file mode refuses or omits writing. The files do not decide whether view-only applies to Graph document materialization only or also to uploaded bytes. [[plan/core-creation/reports/current-edit-core-reconciliation.md]]
12. **A stale contradiction note remains.** [[plan/core-creation/reports/current-edit-core-reconciliation.md]] says an earlier statement in [[plan/core-creation/reports/kernel-fsproj.md]] incorrectly claimed that the HTTP Adapter decodes before the mailbox. The current form of that report now states the correct code fact, so the accusation no longer matches the cited file.
13. **Internal and HTTP results can be confused.** ESO says HTTP Post is a signal and does not convey or apply the sequence. The Actor assessment recommends that inner apply return the produced sequence. These can coexist, but the Core Changes contract must name the two boundaries so an implementation issue does not treat them as one response.

## 5. Recommended breadth-first human decision questions

Do not answer these from the reports. Ask them in this dependency order.

### Round A — destination boundary

- **Q1. What exact observable outcome ends the initial implementation increment: the shared Core Changes seam, or that seam plus a minimal Core Actor pool?** Prerequisites: none.
- **Q2. Must the initial extraction preserve current persistence-mode and HTTP behavior, with db-authority and view-only file mode left to ACID apply? If not, which later behavior moves into this increment?** Prerequisites: none.
- **Q3. What does “Core is the sole Server Graph writer” cover in this increment, and which named startup or maintenance writers may remain temporary exceptions?** Prerequisites: none.
- **Q4. Must all four Core API calls exist in the initial increment, or may the increment implement Changes while it only charts Files, Query, and Command?** Prerequisites: none.

### Round B — contracts and placement

- **Q5. What is the internal Core Changes contract for Browser-originated and Server-Actor-originated Change batches, including success, Reject, deduplication, and produced Changes?** Prerequisites: answers Q1, Q2, and Q3.
- **Q6. Where does JSON decode end and the Change-object contract begin, and how does the internal result become the separate HTTP Post signal and Poll sequence?** Prerequisite: answer Q5.
- **Q7. What folder and module seam owns the Server mailbox and mode-specific persistence calls, while Shared keeps the Browser-compatible apply implementation?** Prerequisites: answers Q2, Q3, Q4, and Q5.
- **Q8. If Files, Query, and Command are in the increment, what is the minimum callable contract for each, and which current Server operations map to each one?** Prerequisites: answers Q2 and Q4.

### Round C — pool only if selected in Round A

- **Q9. What does Command launch, what Core job identity does it return, and what state does Core retain for that job?** Prerequisites: answers Q1, Q4, Q7, and Q8.
- **Q10. At which exact point does cancel stop output, and what happens when a Change is already waiting in Core Changes?** Prerequisites: answers Q5 and Q9.
- **Q11. How does a finishing Actor submit one Change or a batch, and what result does the Actor receive after Core Changes accepts, amends, deduplicates, or rejects it?** Prerequisites: answers Q5, Q9, and Q10.
- **Q12. What pool shutdown and failed-task behavior belongs to the initial increment, without adding soft-lock policy, Browser chrome, Parse realignment, or process isolation?** Prerequisites: answers Q9 through Q11.

### Round D — implementation-ready proof

- **Q13. Which current write paths must be routed through Core for this increment, and which named paths are expressly deferred?** Prerequisites: answers Q2, Q3, Q5, and Q7.
- **Q14. What focused acceptance evidence proves the increment: one shared amend/log/Poll-visible Changes path, no unintended second Graph writer within the chosen boundary, and an available apply queue during Actor work if the pool is included?** Prerequisites: answers Q1 through Q13 that apply.
- **Q15. Which unresolved later choices need decision tickets now, and what blocks each ticket, without turning them into implementation issues?** Prerequisite: all earlier answers, because their boundaries define the later frontier.
