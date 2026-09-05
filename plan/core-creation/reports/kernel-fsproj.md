# Kernel fsproj

Date: 2026-09-05

Links: [[plan/core-creation/project.md]], [[plan/roadmap/epics/robust-outliner.md]], [[solid-core-module-fit.md]], [[plan/event-sourced-ops/overview.md]], [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[CONTEXT.md]]

This is analysis. Product word **Core** is in [[CONTEXT.md]]. See [[doc/agents/scope-vs-commitment.md]].

## Locks (2026-09-05)

1. **No fsproj.** Folder or function seam only. Work belongs to [[plan/core-creation/project.md]] (01 and 02 for Core); [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]] owns the Parse definition, and [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]] owns advisory soft-lock behavior.
2. **Name is Core.** Not kernel, not apply Module. **inner apply** stays the function seam. **Solid core** stays the Epic bar — different from **Core** the Module. Collision is mild; say **Core** for the Module and **Solid core** for the bar.
3. **Core owns persistent state**, including file bytes and git of those files. Persist algorithms (Document codecs, Graph↔document, reconcile) stay **out** and persist **via Core**. Core may **open a file for writing**; the algorithm does not open or write the file itself — it uses Core API Files. **File mode is view-only:** Core still owns persist and **does not write bytes** (open-for-write refused or not offered). **db mode:** algorithms call Core API; Core opens/writes files, git, projection. Actor management is the pool. Npgsql is an I/O helper. Alternative Alan did not need: Core contains the database driver — no.
4. **Core API** is the four-call framework (Files, Changes, Query, Command). It is not the web API. Many web messages decode in an HTTP Adapter, then call Core API.
5. **Core does not own advanced logic.** Advanced logic must use Core API.
6. **Actors work to Core API.** The pool is Core-owned. Actor definitions (Parse, later Agent, shell) call Core API, including inner apply as Changes.

## OS analogy

**Analogous** as the only writer of Graph state behind a small Interface. **Divergent** as an OS kernel (rings, syscalls into a privileged process, crash isolation, drivers).

| OS kernel | Gambol **Core** |
| --- | --- |
| Syscalls | **Core API** (Files, Changes, Query, Command). Internal, not HTTP. |
| Only writer of memory and devices | Only writer of the Server Graph: no side door; others post Changes. |
| Process crash isolation; separate rings | **Not required.** If an Actor hangs, abort it; the Graph stays consistent. |
| Hardware drivers in the kernel | Npgsql is an I/O helper. File *bytes* and git are Core persistent state; file mode does not write. Persist *algorithms* stay out. |
| Kernel is not also in userland | Shared apply runs the same Ops in the Browser. |

The better local metaphor is a **deep Module** (small Interface, large Implementation). **Core** is that Module plus “no second Graph writer.” It is not an OS.

## Name

**Core** and **Core API** are locked ([[CONTEXT.md]]). Retired for this Module: kernel, apply Module. **inner apply** is the Changes seam. **Solid core** is the Epic bar only. Core API is not `/ambit`.

## In / out (established Module)

Same list for the initial extract and the stable end state, except the persist **port** (end state only). The Actor **pool** is **in** (launch off the apply queue, job identity, cancel, finish via inner apply). Actor **definitions** are **out** (Parse File, shell command, later Agent).

**In**

- Graph, Op, Change (Fable; the Browser applies the same Ops)
- History.applyChange, ChangeAmendment, child-list merge
- Graph build/mutate/query that apply and Query/Command need
- **Core API**: Changes, Query, Command; Files send / get / git of file bytes
- Actor pool (launch, identity, cancel; definitions call Core API / inner apply; no HTTP self-post)
- Persistent file bytes and git of those files

**Out**

- Web server / HTTP Adapter
- Graph→DB persist *algorithm* (projection planning). Npgsql connection is I/O only
- Parse algorithms; Document codecs; reconcile; Graph↔document persist algorithms
- Actor definitions: Parse File, shell command, later Agent, any concrete job
- Upload / Load *policy* (incremental send). Wiki, ViewModel, Sync, Expression

**Ambiguous**

- **ACID transaction** — initial: facts in Core; I/O helper opens the connection. End: Core owns *when* commit happens.
- **History** — apply/amend of a Change is in. Browser History consume is out.
- **Daily git save** ([[plan/daily-git-save/project.md]]) — git of DataDir files is Core-owned persistent state, not an Actor definition. Do not move that Project onto this Epic unless Required already lists it (it does not).

## Sequence

Accepted: (1) assemble **Core** from existing functionality — apply/amend first, then the pool (both *inside* Core). (2) first Actor *definition* (Parse File) uses that pool. ACID remainder and incremental Upload/Load follow. They are not required to extract.

Chapter split: [[plan/roadmap/epics/chapters/initial-core.md]] points to [[plan/core-creation/project.md]]. [[plan/roadmap/epics/chapters/actors-supported.md]] owns the Parse definition through [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]]. Do not merge the Chapters.

## Uploaded file

Alan: “Message: uploaded file. Our core has the information. What can it do with it; how.”

Today App Upload writes artifact bytes (WebDAV / `/ambit/direct-upload` / DataDir). After Files send, those **bytes are Core persistent state**. A separate Change may add an Unparsed File Node (`WorkspaceUploadStructure`). Parse is a later HTTP `/ambit/file/parse` today: read text, plan Ops, post a Change. Cold load leaves File Nodes Unparsed until Parse.

After “uploaded file” Core may hold:

- File bytes (Files send)
- A File Node in the Graph (often Unparsed), if a Change already created or updated it
- Not parsed document structure until Parse (out) posts Changes

**Core:** Query the File Node; Files send / get / git; apply a Change that creates or updates the Unparsed File Node. **Outside (Parse algorithm):** Files get, plan Ops, finish via inner apply (Changes).

**How Parse is invoked:** Command (name + selection) launches a pool job. The pool is in; the Parse definition is out and registered with the pool. Changes land only when that job finishes. Do not HTTP self-post.

Recommended sequence (do not rename Upload or Parse):

1. Upload Adapter: Files send (bytes to the port).
2. Changes: File Node Unparsed, if not already present (Query first).
3. Command: launch Parse on that File Node.
4. Parse (out) → Files get → Changes through inner apply. Then Load/Fetch for the Browser if needed.

## Boundary answers (initial and final)

Alan asked whether Core includes the web server, database connections, and file persist. Web server: no. Npgsql *algorithm*: no (I/O helper only). File *bytes* and git: Core owns them; file mode does not write; db mode writes. Graph↔document algorithm: no.

### 1. Web server — no

Kestrel and ASP.NET route maps live in [[src/Server/Server.fs]] and [[src/Server/RouteRegistration.fs]]. `/ambit/changes` currently passes a JSON string into the agent mailbox, which decodes it. The future HTTP **Adapter** decodes that body before it calls Changes. Core Implementation is inner apply of Change objects. Actors must not HTTP self-post ([[plan/event-sourced-ops/details/actors-and-jobs.md]]). Putting the host inside Core would not deepen the Module; it would force Actors through HTTP. Process crash isolation is not required.

### 2. Database connections — no (owns the facts; persist port + ACID transaction)

Do not conflate three things. **Connections/drivers** (Npgsql, `Database.getConnection`) live in [[src/Server/Database.fs]] and [[src/Server/DatabaseProjection.fs]]. That is the db persist **Adapter**. **Persistent information** is Graph, Revision, History/ChangeLog as facts — Core owns those. **ACID apply** is one PostgreSQL transaction for the amended ChangeLog rows and their resulting projection. Today [[src/Server/DbAgent.fs]] `persistBatch` opens a connection, begins a transaction, appends the log, writes the projection, then commits. Core must not contain the connection pool. Core **calls** a persist port. After the ACID Chapter, Core owns when that commit happens. The Adapter still opens the connection.

### 3. File bytes — Core owns; file mode does not write

Core owns file bytes and git (Core API Files). Persist algorithms stay out and persist via Core: they call Files; Core opens and writes. **File mode is view-only:** Core does not write bytes (open-for-write refused or not offered). Today FileAgent still writes via `persistGraphOps` — that is debt (algorithm opening the file). **db mode:** Core writes bytes, git, and projection. [[src/Server/FileAgent.fs]] `persistGraphOps` is a Graph↔document *algorithm* — that stays out. Timeout-abandon on that path is ACID Chapter debt.

**In / out:** see the established-Module list above.

## What already looks like Core

Shared apply/amend: [[src/Shared/History.fs]] (~690), [[src/Shared/ChangeAmendment.fs]] (~182), [[src/Shared/ChildListMerge.fs]] (~87), [[src/Shared/ChildListWire.fs]] (~61). Graph substrate that apply needs: [[src/Shared/Model.fs]] (~271), [[src/Shared/GraphMutate.fs]] (~365), [[src/Shared/GraphBuild.fs]] (~366), [[src/Shared/GraphOps.fs]] (~56). About 2k lines of apply path. Shared as a whole is ~15k lines of `*.fs`.

Server produce: [[src/Server/FileAgent.fs]] and [[src/Server/DbAgent.fs]] each fold `ChangeAmendment.applyChange` in a copied `applyBatch`. Mailbox `PostChange` takes a JSON string. The HTTP Adapter passes that string to the mailbox, which decodes it. Parse File plans ops, then `postGraphOnlyChange` (revision CAS, bare ack) — not inner apply. DbAgent `persistBatch` already opens one PostgreSQL transaction for log + projection. FileAgent `runBounded` can abandon a persist that still writes later.

HTTP today is wider than Core API: `/ambit/state`, `/ambit/poll`, `/ambit/load`, `/ambit/changes`, `/ambit/file`, `/ambit/file/parse`, `/ambit/save`, plus auth, git, WebDAV. Core API is not a 1:1 route list.

## Module shape

**Core** is a **deep Module**: small Interface (**Core API**), large Implementation. Depth is leverage at that Interface, not a line-count ratio.

Two layers must not be collapsed:

- **Apply Implementation (Fable).** Graph, Op, Change, History.applyChange, ChangeAmendment. The Browser applies the same Ops. This stays in Shared. A Server-only assembly would break the Browser.
- **Server produce (dotnet).** inner apply that takes `Change list`, the Actor pool (launch, identity, cancel), and (after ACID) a db persist port so one transaction covers amended ChangeLog rows and the resulting projection. HTTP is an **Adapter**: decode body, then call Core API. Actor *definitions* stay outside and work to Core API. Do not HTTP self-post.

Extract is a folder or function seam on the existing `applyBatch`. No new fsproj. Two persist **Adapters** already exist (FileAgent, DbAgent). Do not move ViewModel, Sync, Expression, or Document into Core.

**In / out:** see the established-Module list above (pool in; definitions out; persist port at end state only).

Pool extract: same inner apply the request path uses ([[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]]); job identity and cancellation ([[plan/core-creation/issues/02-core-actor-pool.md]]). First definition: Parse ([[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]]). Advisory soft-lock behavior stays in [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]].

## Solid core fit

Modular pieces, four-call, and the Actor pool: [[plan/roadmap/epics/chapters/initial-core.md]]. First Actor definition (Parse): [[plan/roadmap/epics/chapters/actors-supported.md]]. ACID (timeout-abandon, bypass writers, file view-only): [[plan/roadmap/epics/chapters/acid-apply.md]], not required to extract. Not process crash isolation.

## Which Project owns the work

[[plan/core-creation/project.md]] owns Core extraction, inner apply ([[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]]), and the Actor pool ([[plan/core-creation/issues/02-core-actor-pool.md]]). [[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]] owns the Parse definition. [[plan/event-sourced-ops/issues/09-job-identity-with-advisory-soft-lock.md]] owns advisory soft-lock behavior and Browser-facing job access. [[plan/architecture/map.md]] documents after; it does not own the extract. Folder or function seam; no fsproj.

## Today vs new

**True today:** apply/amend in Shared; two agents copy applyBatch; mailbox is JSON; Parse is request-scoped CAS; no pool.

**New if extracted:** named **Core** (folder / function seam), **Core API** as the only Actor surface, inner apply without HTTP, pool in Core. File-mode view-only and ACID cleanup are later Chapter scope.

## Open questions

- owner-edge repair writes with no History Change — that fights the ACID Chapter, not the extract.
- The Core pool packaging and exact job API are not specified enough for implementation; see [[plan/core-creation/issues/02-core-actor-pool.md]].

## Chapters charted

Current: [[plan/roadmap/epics/chapters/initial-core.md]]. Then [[plan/roadmap/epics/chapters/actors-supported.md]]. Later: [[plan/roadmap/epics/chapters/acid-apply.md]], [[plan/roadmap/epics/chapters/incremental-operations.md]], [[plan/roadmap/epics/chapters/rowview-layout-vs-behavior.md]]. Wiki and ESO remainder stay on the Epic Required list.
