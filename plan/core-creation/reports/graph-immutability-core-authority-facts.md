# Graph immutability and Core authority facts

Date: 2026-09-05

Purpose: Fact-check the claim that the Graph model is immutable, so sharing Graph values cannot enable bypassing Core. Repository facts only. Related: [[CONTEXT.md]], [[plan/core-creation/project.md]], [[plan/core-creation/reports/initial-core-changes-runtime-facts.md]], [[plan/core-creation/reports/core-wayfinder-fact-inventory.md]].

## Claim under test

"The Graph model is immutable, so everyone having Graph model objects will not enable bypassing Core."

## 1. Are Graph and reachable Node values structurally immutable?

**Yes, in current F# code.**

| Type | Location | Shape | Mutation style |
|------|----------|-------|----------------|
| `Graph` | [[src/Shared/Model.fs]] | Record: `root`, `nodes` (`Map`), `parentByChild`, `ownerParentByChild` | No `[<Mutable>]` fields |
| `Node` | [[src/Shared/Model.fs]] | Record: `id`, `text`, `name`, `children`, `childrenStatus`, `cssClasses`, `owner`, `kind`, `documentState`, `updateTime` | All immutable F# / .NET values |
| `State` | [[src/Shared/History.fs]] | Record: `graph`, `history`, `revision` | Immutable record |
| `ChildNode` | [[src/Shared/Model.fs]] | Record | Immutable |
| `Filename`, `CssClasses` | [[src/Shared/Filename.fs]], [[src/Shared/CssClass.fs]] | Discriminated unions | Immutable |

Graph edits go through pure functions that return new values: [[src/Shared/GraphMutate.fs]] (`setText`, `replace`, …), [[src/Shared/GraphBuild.fs]] (`fromNodes`, `addDetachedNode`, `appendChildren`), [[src/Shared/History.fs]] (`Op.apply`, `applyChange` → `{ state with graph = graph }`). [[src/Shared/GraphOps.fs]] exposes the same pattern on `Graph`.

No `[<Mutable>]` appears on Graph, Node, State, or their nested model types in `src/Shared/`.

## 2. Does sharing a Graph let a caller mutate that value or the authoritative Server Graph in place?

**No for in-place mutation. Yes for stale reads if the holder keeps an old reference.**

Authoritative Server Graph lives in private `ref` cells inside agent mailboxes:

- [[src/Server/FileAgent.fs]] — `let state = ref loadedState`; publish via `state.Value <- finalState` after successful Change processing.
- [[src/Server/DbAgent.fs]] — `let state = ref initialState` and `let persistedGraph = ref initialState.graph`; publish via `state.Value <- stateToStore` (and related `persistedGraph` updates).

Reads expose the current Graph value, not a writer handle:

- `getState` / `tryGetState` return `StateResponse { graph = state.Value.graph; … }` ([[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]]).
- [[src/Shared/ApiResponses.fs]] defines `StateResponse.graph: Graph`.

Because Graph and Node are immutable records, a caller with a shared Graph reference cannot mutate fields in place. Calling `Graph.setText`, `GraphMutate.replace`, or `History.applyChange` on a held Graph produces a **new** Graph (or State) value. That local value does not update `state.Value` unless the caller also publishes through the agent mailbox (or another writer path below).

**Aliasing note:** `getState` returns the same Graph instance reference currently stored in the agent ref until the next publish. Immutability prevents corruption of that instance; it does not prevent a holder from acting on a stale snapshot after a later Change.

## 3. What capabilities actually bypass Core authority?

Graph immutability is **not** the authority boundary. Bypass depends on **who can accept Changes, publish Graph state, or persist**.

### Runtime Change writers (authoritative Graph publish)

All current runtime Graph mutations on the Server go through `FileAgent` or `DbAgent` mailbox messages `PostChange` / `PostGraphOnlyChange`, which call `ChangeAmendment.applyChange` then assign `state.Value` ([[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]]). Entry points:

| Capability | Module | Bypasses planned Core? |
|------------|--------|------------------------|
| `AgentHandle.postChange` | [[src/Server/Api.fs]] → FileAgent / DbAgent | Yes today (direct mailbox) |
| `AgentHandle.postGraphOnlyChange` | Same | Yes today |
| HTTP POST `/ambit/changes` | [[src/Server/Api.fs]] `postChange` | Yes today |
| Parse | [[src/Server/Api.fs]] `postParseFile` → `postGraphOnlyChange` | Yes today |
| Lazy-load / git reconcile | [[src/Server/LazyLoadReconciliationServer.fs]], [[src/Server/GraphOnlyChangePost.fs]] | Yes today |
| File + DB mirror | [[src/Server/Api.fs]] `ofFileWithDbMirror` (two writers on one body) | Yes today |

Holding a Graph snapshot alone does **not** grant any row in this table. Holding an `AgentHandle`, `FileAgent`, or `DbAgent` mailbox **does**.

### Persistence side doors (inside or beside Change path)

| Capability | Location | Notes |
|------------|----------|-------|
| `DocumentPersistence.persistGraphOps` | [[src/Server/DocumentPersistence.fs]] | Called from agent `handlePostChange` after apply; writes Directory Files |
| `DatabaseProjection.persistWithTx` | [[src/Server/DatabaseProjection.fs]] | DbAgent `persistBatch` inside one transaction with Change log |
| `Bookkeeping.writeRevision` | [[src/Server/Bookkeeping.fs]] | FileAgent meta checkpoint after clean disk persist |
| `FileAgent.runBounded` timeout | [[src/Server/FileAgent.fs]] | Abandoned background persist may still write disk after timeout (documented fire-and-forget) |
| `DocumentPersistence.writeAllDocuments` | [[src/Server/DocumentPersistence.fs]] | Defined in Server; **no production caller in `src/`** (test/setup utility) |

### Startup and repair writers (no Change log)

From [[plan/core-creation/reports/initial-core-changes-runtime-facts.md]]:

- [[src/Server/DocumentLoader.fs]] — cold load into initial agent state.
- [[src/Server/DatabaseSetup.fs]] — `rebuildFromDocumentFiles` when DB empty or mismatched.
- [[src/Server/DbAgent.fs]] / [[src/Server/DatabaseProjection.fs]] — projection startup sweep may trim or reload Graph in memory.

These mutate authoritative Server Graph without Poll-visible Changes.

### What sharing Graph **does** enable (not bypass)

Server producers routinely **read** Graph then **post** Changes:

- Parse: `getState` → plan ops → `postGraphOnlyChange` ([[src/Server/Api.fs]]).
- Lazy-load: `getState` → plan chunks → `postGraphOnlyChange` ([[src/Server/LazyLoadReconciliationServer.fs]]).

The Graph snapshot supports planning and Query; authority still requires the mailbox post (today) or Core Changes (planned).

## 4. Should plan wording sharpen from "sole Server Graph writer" to Changes authority?

**Yes, for precision.**

[[CONTEXT.md]] defines **Core API** as Files, Changes, Query, Command — not "Graph writer" as a type-level lock. **Core** owns persistent state and the Actor pool; **Changes** is the inner apply path for Graph modifications.

Current plan text ([[plan/core-creation/project.md]], [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]]) says "Core is the sole Server Graph writer." That conflates two ideas:

1. **Authority:** only Core may accept Changes, publish authoritative Server Graph + History revision, and persist (Files + projection) on the success path.
2. **Sharing:** immutable Graph snapshots may be copied, returned in `StateResponse`, passed to Parse planners, Query, and Browser Fetch without write authority.

Sharpened wording aligned with [[CONTEXT.md]]:

- Core solely **accepts**, **amends/applies**, **publishes**, and **persists** Changes (and owns Files git/bytes per mode).
- Immutable Graph values may be **shared read-only** for Query, planning, and client residency; sharing them does not substitute for Changes.

"Graph writer" is a useful shorthand only if it means "the only module that may publish authoritative Graph state after Change apply," not "the only code that may hold a `Graph` value."

## 5. Exceptions and counterexamples

| Counterexample | Why it matters |
|----------------|----------------|
| Graph snapshot + `AgentHandle` | Immutability does not block bypass; mailbox access does. Parse and LazyLoad are intentional examples. |
| Two runtime agents | FileAgent and DbAgent are separate writers; mirror mode can write both ([[src/Server/Api.fs]] `ofFileWithDbMirror`). |
| Startup / repair paths | DocumentLoader, DatabaseSetup rebuild, DbAgent projection sweep publish Graph without Change log entries. |
| `runBounded` timeout | Disk persist may complete outside mailbox ordering; not Graph immutability, but a second persistence hazard. |
| Browser `model.graph` | [[src/Client/App.fs]] uses `mutable model`; Client applies Changes locally. This is Browser residency, not Server authority. |
| Stale shared reference | Safe from in-place mutation; unsafe for assuming current revision without Poll or fresh `getState`. |

## Verdict on the claim

| Part | Verdict |
|------|---------|
| "The Graph model is immutable" | **Supported** for Graph, Node, and State in Shared model code. |
| "…so everyone having Graph model objects will not enable bypassing Core" | **Not supported as stated.** Immutability prevents in-place mutation of a shared snapshot; it does **not** prevent bypass when the holder also has mailbox/HTTP Change entry, persistence hooks, or startup-writer access. Sharing Graph is necessary for Query and planning; it is neither sufficient nor necessary for authority. |

Authority is enforced by **gating Changes accept/publish/persist** (planned Core API), not by withholding Graph values.
