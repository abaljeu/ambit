# Is issue 01 produce path done?

Date: 2026-09-05

Question: Alan says we now have a Core Changes path defined by issues 03–06. Is [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] done?

Answer: **No.** Issues 03–06 delivered the typed Core Changes kernel, HTTP Adapter, and Browser path. That work **enables** issue 01. It does **not** deliver a first-class Server-side Actor produce path. The issue is **partial** only in the sense that the locked Normal seam and a test-only caller already exist.

Recommended Status: **ready-for-agent**. Do not set `done`. Leave the issue file unchanged until the coordinator decides.

## What 03–06 delivered

Resolved grilling tickets: [[03-define-typed-core-changes-contract.md]], [[04-separate-http-adapter-from-core-changes.md]], [[05-place-core-changes-in-existing-projects.md]], [[06-ready-the-initial-core-changes-increment.md]]. All four are Status `resolved`.

Implemented per [[initial-core-changes-implementation.md]] and [[implement-initial-core-changes.md]]:

- Typed `CoreChanges` with Normal `postChange` and Parse-reserved `postGraphOnlyChange`.
- HTTP Adapter `Api.postChange` decodes JSON, calls Normal, encodes the acknowledgement.
- Parse, `GraphOnlyChangePost`, and lazy-load reconciliation call Graph-only with typed Change lists and no internal JSON or HTTP self-post.
- Production selection lives in [[src/Server/Core/CoreRuntime.fs]]. Routes take `CoreChanges`, not raw agents.
- Agent `postChange` functions are private. Tests and Core build a handle through `FileAgent.coreChanges` / `DbAgent.coreChanges`. See [[contain-core-change-authority.md]].
- A **test-only** caller in [[tests/Server.Tests/CoreChangesTests.fs]] posts Normal without HTTP and proves Poll returns the accepted Change and Revision.

The implementation plan states the non-goal in one line: do not implement issue 01; do not add a production Server Actor or another production Server-producer path. The typed Normal test **enables** issue 01; it does **not** deliver it. Issue 06’s acceptance evidence is that same harness.

## What issue 01 still requires

Issue 01 asks for one Core Changes path that **Server-side Actors** hand Changes into without HTTP self-post. A non-Browser producer must use the **same** amend, log, and Poll-visible sequence as a Browser Change. Core must be the sole Server Graph writer. Auth and malformed stay Reject. The issue does not invent multi-job identity or soft-lock UI.

That is not the 03–06 kernel. Normal is defined for Browser **or** a Server Actor. No production Server Actor uses Normal today.

Parse is **not** that producer. Issue 03 reserves Graph-only for Parse because those Changes started from files. Graph-only skips document validation and document persistence. Issue 01 wants the Browser-equivalent Normal sequence.

Issue 02 (Actor pool) still waits on issue 01. Finish-through-Changes belongs to the pool after the produce path exists. Do not fold pool, Command, cancel, or job identity into issue 01.

Packaging (one Change versus a set) remains proposed in [[plan/event-sourced-ops/details/actors-and-jobs.md]]. Issue 01 already says lock the entry seam lightly, then implement. The entry seam is now the typed Normal `CoreChanges.postChange`. Packaging can stay light during implementation.

## Code evidence

Can a Server-side producer submit a Change through Core Changes without HTTP self-post **today**?

- **Capability, test-only:** yes. `typed Normal caller publishes accepted Change to Poll` constructs `FileAgent.coreChanges`, calls `handle.postChange` with a typed `Change list`, then `Api.getPoll`. No HTTP body.
- **Production Server Actor:** no. The only production Normal caller is `Api.postChange` on `/ambit/changes` (Browser HTTP).
- **Production in-process, different path:** Parse and reconciliation call `postGraphOnlyChange`. That is Core Changes without HTTP self-post, but it is Graph-only, not the Browser Normal sequence.

Is that Change amended, logged, and Poll-visible like a Browser Change?

- Normal and Graph-only share `applyBatch` → `ChangeAmendment.applyChange`, then log persist, then mailbox publication. Poll reads `getChangesSince`.
- Browser Normal also runs document validation and document persist. Graph-only skips those. Issue 01’s “like a Browser-posted Change” means Normal, not Graph-only.
- Amendment for recoverable field collisions is already in Shared apply (event-sourced-ops issues 03 and 04 are Status `done`). Arrival at Normal apply is enough for that merge. Issue 01 does not re-implement amend.

Is Core the sole Graph writer?

- Runtime posts in production go through `CoreRuntime.create` → `CoreChanges`. Direct `FileAgent.postChange` / `DbAgent.postChange` are private.
- Named exceptions remain: initialization, repair, and reconciliation protocols until ACID apply; `DbAgentStartup` still holds a mailbox that can accept `PostChange`; tests may construct `coreChanges` on a raw agent.
- Core is the intended sole **runtime** Change authority. It is not yet the deep Module that owns apply inside Core rather than inside the agent mailboxes.

Auth and malformed Reject: unchanged on the HTTP Adapter. Empty and no-effect lists remain Rejects on Core. Issue 01 does not need new Reject cases.

## Blockers

Issue 01 lists:

- [[plan/event-sourced-ops/issues/03-server-amends-recoverable-field-collisions.md]] — Status `done`.
- [[plan/event-sourced-ops/issues/04-client-consumes-merge-success-without-reload.md]] — Status `done`.
- [[06-ready-the-initial-core-changes-increment.md]] — Status `resolved` and implemented.

The wait that set Status `needs-info` (open 03–06 decisions) is over. `needs-info` is stale.

## Recommended Status

Keep the issue open. Set **ready-for-agent** when the coordinator updates Status.

Do not set `done`. Checkboxes on issue 01 are still unchecked and still true as remaining work.

`needs-info` was correct while 03–06 were open. It is no longer the right triage role unless Alan wants the first production producer named before an agent starts (Parse stays Graph-only; pool stays issue 02). That naming is a one-line scope pin, not missing merge-or-consume design.

## Smallest remaining work (do not implement here)

1. Treat `CoreChanges.postChange` (Normal) as the Server Actor entry. Do not HTTP self-post. Do not send Actor output through Graph-only.
2. Add one **production** in-process Normal submit — a real Server caller, not [[tests/Server.Tests/CoreChangesTests.fs]] and not Parse. The first caller can be a thin production helper that a later Actor (issue 02 / Parse realignment) will use.
3. Prove that caller’s Change is amended, logged, and Poll-visible the same way as Browser Normal.
4. Leave auth and malformed as Reject. Do not add job identity, cancel, pool, or soft-lock UI.
5. Lock packaging only as far as that first caller needs (one Change is enough).

Out of this issue: Actor pool ([[02-core-actor-pool.md]]), Parse-as-Actor ([[plan/event-sourced-ops/issues/08-parse-file-realignment-tracer.md]]), mirror deletion ([[13-delete-runtime-mirror-and-remove-production-persistence-mode.md]]), Core Files/Query/Command.

## Sources

- [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]]
- [[plan/core-creation/initial-core-changes-implementation.md]]
- [[plan/core-creation/reports/implement-initial-core-changes.md]]
- [[plan/core-creation/reports/contain-core-change-authority.md]]
- [[src/Server/Core/CoreChanges.fs]], [[src/Server/Core/CoreRuntime.fs]], [[src/Server/Api.fs]], [[src/Server/FileAgent.fs]], [[src/Server/DbAgent.fs]]
- [[tests/Server.Tests/CoreChangesTests.fs]]
- [[plan/event-sourced-ops/details/actors-and-jobs.md]], [[plan/event-sourced-ops/architecture.md]]
