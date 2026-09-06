# Actor Core object and mailbox check

Date: 2026-09-06. Tree: uncommitted [[23-close-core-object-seam.md|23 (Close Core object seam)]] / [[24-clarify-core-increment-boundary.md|24 (Clarify Core increment boundary)]] on `dev`. No product code edit.

## Lead claim

**The Actor can only use `postChange` and `postGraphOnlyChange`.**

**False** (wider surface). The type enforces a `CoreChanges` handle, not those two Posts alone. Writes are only those two Posts. Reads are also on the handle. The type does not give `command`, `CoreRuntime`, Files, Query, launch, cancel, the credential set, a raw mailbox, or FileAgent.

## What launch passes

`ActorFn` is `Graph -> Credential -> CoreChanges -> Async<unit>` in [[src/Server/Core/CoreActorPool.fs]]. `runLaunch` starts `plan.actor plan.subgraph plan.credential bound`. `bound` is `CoreAuth.bindHandle` of the launch `CoreChanges` ([[src/Server/Core/CoreCredentials.fs]]). Bind wraps the two Posts with job-credential admit. It does not drop the read fields.

| Argument | Type | What the Actor can do |
| --- | --- | --- |
| subgraph | `Graph` | Local Graph value from `GraphSpan.extract`. Not Core. |
| job credential | `Credential` | The token value only. No `add` / `remove` / `contains`. |
| bound handle | `CoreChanges` | Every field of [[src/Server/Core/CoreChanges.fs]]. |

Every field on that handle:

- `getState` — full `State` (Graph + Revision), not the launch subgraph only
- `getRevision`
- `getChangesSince`
- `isReady`
- `postChange`
- `postGraphOnlyChange`

Not on that handle: `command`, `launch`, `query`, `lockedIds`, `withLocks`, `CoreRuntime`, Files, a Query subobject, cancel, `CoreCredentials`, FileAgent, DbAgent, or any mailbox.

Issue 01 named this "full `CoreChanges` handle" ([[issue-01-actor-protocol-hypothesis.md]]). Tests already call `getRevision` and `postChange` on the third argument ([[tests/Server.Tests/CoreActorPoolTests.fs]]).

## Earlier checks (not the lead)

The Actor does not receive `CoreRuntime`. HTTP holds that container; no production caller invokes `core.command.launch`.

Submit-change is `postChange` / `postGraphOnlyChange`. Apply serializes on the FileAgent or DbAgent mailbox after `CoreAuth` admit. The Actor pool mailbox is launch and lock only.
