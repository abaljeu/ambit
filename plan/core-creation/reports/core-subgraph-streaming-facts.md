# Core subgraph streaming facts

Date: 2026-09-05

Purpose: Fact-check and reconcile confirmed user direction (Actor context matches Browser subgraph shape; stop whole-Workspace transfer; Core selects a subset and expands iteratively; **Poll** carries residency plus **Change** facts) against repository design. Does not choose product decisions or edit plans, issues, or source. Related: [[plan/core-creation/reports/asynchronous-core-task-manager-facts.md]], [[plan/core-creation/issues/08-define-core-query-contract.md]], [[plan/selective-client-loading/spec.md]], [[doc/roadmap/on-demand-graph-residency.md]], [[CONTEXT.md]].

## Confirmed user direction (input only)

- Actor planning context has the same structural requirement as a Browser subgraph: **Node** ids, **Children** / Owner and Ref edges, some **Nodes** **Unloaded**.
- Another Core redesign goal: stop sending a whole **Workspace** to the **Browser**.
- **Core** computes a relevant subset, then expands content iteratively.
- **Poll** returns state/residency information plus **Change** information.

## 1. Canonical subgraph / residency shape — exists partially

### What is canonical today

The wire and Shared model already use one **Graph** shape for both full authority and partial residency:

| Fact | Source |
| --- | --- |
| **Node** carries `id`, header fields, `children` (`ChildNode` list with Owner/Ref), `childrenStatus` (`Unloaded` \| `Loaded`), plain canonical `owner` **NodeId** | [[src/Shared/Model.fs]] |
| **Unloaded** requires empty `children`; **Loaded** list is authoritative and complete for that parent | [[CONTEXT.md]], [[plan/selective-client-loading/spec.md]] |
| **Resident** = Header present (**Absent** antonym); **Loaded** / **Unloaded** applies to **Children**, not Header presence | [[CONTEXT.md]] |
| Server-side **Workspace** package projection: owner-closure within one **Workspace**, nested named **Workspace** headers **Unloaded**, external **Ref** headers without **Children** | [[src/Shared/ResidentProjection.fs]] `projectWorkspaceNodes` |
| Bootstrap scope: complete **ROOT** **Workspace** closure via `rootBootstrapGraph`; optional second complete **Workspace** for saved zoom | `bootstrapGraph`, [[src/Server/Api.fs]] `getState` |
| Package wire form: `Node list` in `LoadResponse.packages` and `SyncResponse.packages` — not a separate subgraph type | [[src/Shared/ApiResponses.fs]] |
| Client merge: `installPackages` then `Graph.fromNodes`; **Loaded** wins over **Unloaded** when merging headers | [[src/Shared/ResidentProjection.fs]] |
| Projected **Change** apply: header facts on **Resident** **Nodes**; structural **Op.Replace** only when parent list is **Loaded** | `ResidentProjection.applyChange` |

**Conclusion:** A canonical partial subgraph **shape** already exists: ordinary **Graph** **Nodes** with **Unloaded**/**Loaded** **Children** and Owner/Ref edges. There is no second "Actor context" domain type.

### What is not canonical yet for the user direction

| Gap | Fact |
| --- | --- |
| **Granularity** | Delivered **Load** installs a **complete owning Workspace** package, not an arbitrary subset smaller than **Workspace** ([[plan/selective-client-loading/spec.md]]). |
| **Actor context** | **Parse** calls `handle.getState()` and plans on the **full** authoritative **Graph**, not a scoped projection ([[src/Server/Api.fs]] `postParseFile`). |
| **Server residency** | Selective loading is **client-partial** only; the **Server** **Graph** stays fully **Resident** ([[plan/selective-client-loading/spec.md]], Stage active). |
| **Future server model** | [[doc/roadmap/on-demand-graph-residency.md]] plans document-scoped packages, `NeedsDocuments`, and bootstrap/load API replacement — not started; vocabulary there still says `Unknown \| Loaded` while code uses **Unloaded**. |
| **Owner index completeness** | Issue 17 notes plain `owner` preservation and Loaded-only `ownerParentByChild` are not fully done. |

The user direction aligns with the **Node**/**Graph** residency model and **ResidentProjection** slicing rules, but goes **beyond** delivered selective loading (finer-than-**Workspace** slices, **Poll**-driven expansion, **Server**-side subset for **Actors**).

## 2. How **Poll** works today vs iterative expansion

### Current HTTP / types

| Endpoint | Response type | **Changes** / **Actions** | Residency / **Graph** facts |
| --- | --- | --- | --- |
| GET `/ambit/poll?rev=` | `ChangeSuccessResponse` | Ordered **Change** tail since client **Revision** | **None** — no `packages`, no scoped **Graph** |
| POST `/ambit/load` | `LoadResponse` | Same tail pattern (base→R) | `packages`: **Node list** at R |
| GET `/ambit/state` | `StateResponse` | **None** | Scoped `graph` at one **Revision** (bootstrap only) |

Implementation: [[src/Server/Api.fs]] `getPoll` reads **Revision**, `getChangesSince`, returns **changes** only. `postLoad` atomically captures **Revision**, **Changes**, and `packagesForTargets`.

### Accepted protocol rules (selective loading)

- **Poll** remains the ordinary **Change** list path; **Load** is the only residency-producing action besides `/state` ([[plan/selective-client-loading/spec.md]]).
- **Load** response: ordered **Changes** (base→R), then zero or more complete **Workspace** subgraphs at the **same** R ([[plan/selective-client-loading/spec.md]], [[plan/selective-client-loading/issues/09-define-sync-revision-correctness.md]]).
- Client install order: apply **Change** tail through `ResidentProjection.applyChange`, then `installPackages` ([[src/Shared/SyncLogic.fs]] `applySyncResponse`).
- **Poll** cannot supply initial **Graph** or **packages** on boot ([[plan/client-start-time/reports/cache-first-boot-via-poll.md]]).
- ESO locked: **Poll** = **Ops** path; **Load** = mixed **Ops** + **Graph** transfer for **Nodes** the **Browser** lacks ([[plan/event-sourced-ops/details/as-implemented-facts.md]], [[plan/event-sourced-ops/architecture.md]]).

### Tension with user direction

User direction (**Poll** + residency + iterative expansion) **conflicts** with the delivered selective-loading contract (**Poll** changes-only). It **aligns** with:

- Grilling answer on [[plan/selective-client-loading/issues/09-define-sync-revision-correctness.md]] (Poll tail may include supplemental headers/tombstones at R) — **not implemented** in `getPoll`.
- [[plan/selective-client-loading/reports/two-phase-state-loading-exploration.md]] (Phase 2 projection at one **rev**).
- [[doc/roadmap/on-demand-graph-residency.md]] (bootstrap/load replaces full **Graph** transfer; per-document packages and patches).

### What would change for iterative expansion (design facts, not a decision)

1. **Wire:** Extend **Poll** (or unify envelopes under `SyncResponse`) to carry optional `packages` or residency deltas, not only `changes`.
2. **Server capture:** Atomic read at R: **Changes** since B **plus** newly materialized **Node** packages — same pattern as `postLoad` / `captureLoadResponse`, but triggered by **Poll** or **Core** push policy.
3. **Client:** Reuse `applySyncResponse` / `applyServerTail` path; today `applyServerTail` passes `packages = []`.
4. **Granularity:** `packagesForTargets` and `projectWorkspaceNodes` must gain finer selectors (Expression, selection, job scope) — not only whole **Workspace**.
5. **Bootstrap:** Iterative expansion still needs an initial scoped `/state` or first **Load** unless **Poll** also serves cold subgraph admission.
6. **Single-flight:** **Load**, **Poll**, submit, and `/state` share one synchronization planner ([[plan/selective-client-loading/spec.md]]); residency-on-**Poll** must fit that queue.

## 3. Can **Browser** and **Actor** share one **Core Query** result?

### Today

- **Core Query** does not exist ([[plan/core-creation/issues/08-define-core-query-contract.md]] — open grilling: revision/snapshot consistency unspecified).
- **Browser** subset: HTTP Adapter → `ResidentProjection.bootstrapGraph` / `packagesForTargets` on fully resident server **Graph**.
- **Actor** subset: `handle.getState()` → full **Graph** (no projection).

### Shared logic already in Shared (candidate **Core Query** Implementation)

| Function | Role |
| --- | --- |
| `ResidentProjection.projectWorkspaceNodes` | Revision-agnostic slice from authoritative **Graph** |
| `ResidentProjection.packagesForTargets` | Target → owning **Workspace** package |
| `ResidentProjection.rootBootstrapGraph` | **ROOT** bootstrap closure |
| `ResidentProjection.bootstrapGraph` | Bootstrap + optional zoom **Workspace** |
| `GraphQuery.*` | Owner chain, enclosing **Workspace**, etc. on any **Graph** |
| `DocumentPartition.*` | Document-root boundaries (server-side planning) |

**Fact:** **Browser** and **Actor** *can* share one typed **Query** result type (**Graph** or `Node list` at **Revision** R) backed by the same projection functions, while **HTTP Adapter** (routes, auth, session bootstrap) and **Actor** definitions remain separate callers. Nothing in the repo blocks that; issue 08 has not named the typed contract.

**Fact:** Sharing transport is not required and would violate Adapter separation ([[plan/core-creation/reports/kernel-fsproj.md]]). **Parse** should not depend on `/ambit/state` scoping rules unless **Query** explicitly defines Actor planning scope.

## 4. Clean division — **Query**, **Poll**, task manager

| Module / call | Locked or delivered role | User-direction stretch |
| --- | --- | --- |
| **Core Query** (issue 08) | Inspect authoritative **Graph** at a **Revision**-consistent snapshot; scope TBD | **Select** relevant subgraph for **Actor** plan-time context and for **Browser** **Fetch** targets — same slice function, different callers |
| **Poll** (CONTEXT + selective loading) | **Browser** request for **Actions** since known **Revision** | Also carry newly **Resident**/**Loaded** facts — **not** current; would overlap **Load** unless roles are re-split |
| **Load** / **Fetch** | Explicit residency growth: **Changes** + **Workspace** packages ([[CONTEXT.md]] **Fetch**) | Finer incremental packages; may shrink if **Poll** absorbs residency deltas |
| **Command** + pool (issues 02, 09–12) | Launch **Actor** off apply queue; finish via **Changes** | Schedules work; does **not** own residency packaging for **Browser** ([[plan/event-sourced-ops/details/actors-and-jobs.md]] — job emit vs **Load** still open) |
| **Changes** | Admit **Actor** output **Ops** into **History** | Distinct from residency **Graph** transfer (ESO Q6: packages are not **Ops**) |

**Deep Module read:** **Query** = point read of "what subgraph exists at R for this selector." **Poll** = delta stream of **Actions** (and, if extended, residency deltas) since B. Task manager = schedule and cancel long work; output still enters through **Changes**, not as a substitute for residency transfer.

**Risk of collapsing roles:** If **Poll** carries both **Changes** and arbitrary subgraph growth, **Load** and **Fetch** become thin wrappers or duplicate paths; issue 08 and selective-loading spec would need explicit revision.

## 5. Correctness constraints

| Constraint | Rule | Source |
| --- | --- | --- |
| **Revision consistency** | One response **Revision** R; **Changes** (B,R] and packages captured at the **same** R | [[plan/selective-client-loading/issues/09-define-sync-revision-correctness.md]], `postLoad` |
| **Install order** | Apply **Change** tail, then merge packages | [[src/Shared/SyncLogic.fs]] `applySyncResponse` |
| **Loaded never demoted** | Sync must not turn **Loaded** into **Unloaded** | Issue 09 answer |
| **Unloaded structural skip** | **Op.Replace** on **Unloaded** parent is no-op in projection | `ResidentProjection.applyOp` |
| **Header facts on Resident** | Non-structural **Ops** apply when **Node** header is **Resident** even if **Children** **Unloaded** | Spec user story 34, `applyOp` |
| **Edges to Absent / non-resident owners** | **Resident** header may name `owner` **NodeId** whose header is **Absent**; owner-parent index only from **Loaded** lists | Spec, issue 17 |
| **Package merge** | **Loaded** header beats incoming **Unloaded** header for same id | `mergePackageNodes` |
| **Dedup** | Server **changeId** idempotency on apply | [[plan/core-creation/reports/initial-core-changes-runtime-facts.md]] |
| **Poll vs pending local** | **getPollOutcome** only when no pending local **Changes** in flight | [[src/Shared/SyncLogic.fs]] |
| **Package-only race** | `applyLoadResponse` rejects package-only when **Revision** or pending local mismatch | [[src/Shared/SyncLogic.fs]] |
| **Boot oversize / hash** | Large **Revision** gap or bootstrap hash mismatch → refetch `/state` | [[src/Shared/BootCache.fs]] |
| **Snapshot retention** | Client residency monotonic per session; page refresh starts new session ([[plan/selective-client-loading/spec.md]]). Server DbAgent `startSnapshot` is async document materialization — separate from client residency | [[src/Server/DbAgent.fs]] |
| **Actions vs residency** | **Load** packages are **Graph** transfer, not **Change** replay ([[plan/event-sourced-ops/architecture.md]]) | Extending **Poll** with packages preserves this if packages stay non-**Op** |

**Actor-specific:** Planning snapshot at **Revision** R can be stale before **Changes** apply; amendment rules apply at output admission time, not at **Query** time ([[plan/event-sourced-ops/details/actors-and-jobs.md]]).

## 6. Project ownership and initial Core Changes boundary

| Work | Owner | Relation to user direction |
| --- | --- | --- |
| **Core Query** contract (revision-scoped subgraph select) | [[plan/core-creation/issues/08-define-core-query-contract.md]] — **core-creation** | Primary **Core** grilling for shared **Browser**/**Actor** slice |
| Client partial residency, **Load**/**Poll** split, `ResidentProjection` | [[plan/selective-client-loading/project.md]] — **active** | Delivered baseline; user direction extends **Poll** and slice granularity |
| Server partial residency, document packages, search | [[doc/roadmap/on-demand-graph-residency.md]] — planned | Longer-horizon; server subset + iterative API |
| Incremental **Upload**/**Load** transport | [[plan/roadmap/epics/chapters/incremental-operations.md]] | Blocked by Actors supported; not Core pool |
| **Actor** pool, **Command**, async launch | [[plan/core-creation/project.md]] issues 02, 09–12 | Schedules work; residency packaging for jobs still ESO/open |
| **Parse** / first **Actor** definition | [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]] | Today full-**Graph** plan; should move to **Query** slice |
| Initial **Core Changes** increment | [[plan/core-creation/initial-core-changes-implementation.md]] | **Out of scope** — no **Query**, no **Poll** protocol change, no residency redesign |

**Remain outside initial Core Changes:** subgraph streaming, **Poll** envelope extension, finer-than-**Workspace** packages, and **Actor** planning context belong to **core-creation** issue 08 plus **selective-client-loading** / on-demand residency follow-on — not the bounded typed **Changes** extract.

## 7. Reconciliation summary

| User claim | Repository verdict |
| --- | --- |
| Actor context = Browser subgraph structure | **Supported** at the **Graph**/**Node** model level; **not** enforced for **Actors** today (**Parse** uses full **Graph**) |
| Stop whole **Workspace** to **Browser** | **Partially delivered** — bootstrap scopes **ROOT** (+ one zoom **Workspace**); **Load** still sends **complete owning Workspace** when `includeWorkspace` |
| **Core** computes subset, expands iteratively | **Projection functions exist**; iterative **Poll**-driven expansion **not** implemented; conflicts with selective-loading **Poll**=changes-only rule |
| **Poll** = residency + **Changes** | **Aspirational** relative to current code and spec; **SyncResponse** shape and `applySyncResponse` are ready; **getPoll** is not |

## 8. Design-support questions (for interview)

1. Is residency growth still explicit **Load**/**Fetch**, or does **Poll** gain optional `packages` (superseding part of selective-loading)?
2. Is slice granularity **Workspace**, Expression-selected subgraph, or document-scoped ([[doc/roadmap/on-demand-graph-residency.md]])?
3. Does **Core Query** return the same type for **Browser** **Fetch** and **Actor** plan-time snapshot, parameterized by selector + **Revision**?
4. Do residency deltas on **Poll** require supplemental headers for **Changes** that touch **Absent** **Nodes** (issue 09 grilling answer)?
5. Does iterative expansion stay client-only (server fully **Resident**) or join on-demand server residency?
