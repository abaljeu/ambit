# Actor pool rewind review

Date: 2026-09-11. Interactive review of the last three agent sets, starting at Core 18. No product patch on the current head. After the review, reset the working head to before those commits and reimplement from a tighter spec.

## How we work

Accumulate confirmed misses here. Do not file `ready-for-agent` patch tickets against the current pool mailbox. Tighten [[../issues/18-finish-and-drop.md]], [[../issues/02-core-actor-pool.md]], and [[../issues/11-define-actor-finish-and-failure-behavior.md]] only after this review names the intended shape. Then reset and rebuild.

## Rewind pin

Set 1 (Core 18) parent: `e0f92b9`. Commits to drop with that set: `f3eb420`, `351ce6c`, `0c85dff`, merge `05eb06f`.

Set 2 sits later on `ready` (CloudAgents merge `4f974f0`). Set 3 is still draft PR 4 (https://github.com/abaljeu/ambit/pull/4); it is not on `ready`. Do not merge PR 4. Include that set in the discard/reimplement list. Intervening `ready` work after 18 includes the fast-clear decision. Confirm at reset whether the pin is only set 1 or set 1 plus set 2 on `ready`.

## Intended shape (confirmed)

- Exactly one Core mailbox: the Changes apply queue. Posts, cancel, delete-actor, launch, query, and admit-and-enqueue are fast messages on that mailbox. See [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]].
- Distinguish the task runner from the registry. The runner is a TaskPool (or equivalent): it runs Actors off the apply queue and may release the thread when the Actor returns or is terminated. It is not a mailbox.
- The registry (public number, lock-present, credential, handle to terminate) is state of that one mailbox. The addressable ID stays until the mailbox processes delete-actor. The task ending does not remove the ID.
- Any Actor stop, including a failed stop, enqueues delete-actor on that mailbox. Callers do not get a job Error.
- FIFO is the mailbox order, not “the Actor awaited `postChange`.” Awaiting Post is Actor-specific. Core must not rely on it.
- There is no credential `MailboxProcessor`. Admit and enqueue are one mailbox message.
- Drop removes the Actor from the registry (public number, lock-present, credential). If the task is still running, async terminate; do not wait. If it has already stopped, terminate is a no-op. Cancel and finish share this drop.

## Current implementation (to discard)

- [[src/Server/Core/CoreActorPool.fs]] is a `MailboxProcessor`. `Register`, `TryLaunch`, `Query`, `GetLocked`, and `DeleteActor` wait on that inbox. That is a second queue.
- The Actor body is `Async.Start`. Posts go to the FileAgent or DbAgent mailbox. `DeleteActor` is posted to the pool inbox after `do! plan.actor`.
- Failed Actor stop never reaches `DeleteActor`. The wrap has no catcher. Query, credential, and lock-present stay live. [[../issues/26-failed-actor-stop-still-drops.md]] recorded this, then was cancelled as a patch.
- The FIFO test awaits Post inside the test Actor, then sleeps. It does not put Post and delete-actor on one mailbox.
- `DeleteActor` does `do! credentials.remove` on a third `MailboxProcessor` ([[src/Server/Core/CoreCredentials.fs]]). Admit is `contains` on that processor, then enqueue on FileAgent or DbAgent. There is no reason for that third inbox. Drop should remove the job from the pool and async-terminate only if the task is still running. The current wrap waits for the Actor, then hops to remove the credential, and never terminates a live task.
- Standards on this delivery: `startMailbox` is 41 lines; four finish tests use `Task.Delay(100)`; [[tests/Server.Tests/CoreActorPoolTests.fs]] grew to 550 lines.

## Set 1 closed

No further independent Core 18 misses. Redo is the pool/mailbox shape above, not a wrap patch.

## Set 2 — CloudAgents stack

PR 3, merged `4f974f0`. Commits: `1b9874e` Add standalone CloudAgents stack; `2c521f7` Lead with no-repo agents in documentation. Spec: [[plan/llm-connector/reports/first-agent-cursor-cloud-agents.md]].

**Miss (confirmed):** [[tests/CloudAgents.Tests/]] does not prove the stack. `AgentRunner.start` hits live `api.cursor.com`. Facts accept any Error or Ok. `PublicTypesTests` only construct records. CloudAgents is the Cursor connector; it needs its own tests (HTTP seam, no live key). TestActor does not replace those tests.

**Three test layers (confirmed):**

1. CloudAgents library — async start/poll/cancel; fake HTTP; no Ambit refs.
2. TestActor — Actor *system* only. No Cursor. Existing Server Core tests (`dotnet test` on FileAgent / Core).
3. CloudAgent Actor — optional later facts for that Actor’s pack/reply path. Not TestActor.

**TestActor (not implemented):** ActorName `test`. Focus Header is the case id (not `?`). TestActor switches on that line, does only what the case needs, and `postChange`s Owned children under Focus. It does not Assert and does not return a job result. Outer fact: launch, wait until the public number is gone, match Graph to a table for that case. First cases: `echo`, `fail`, `post-twice`. No `cloud-stub` in this Actor.

**Confirmed algorithm:** the Actor is not a foreground worker. Await POST (ids), then await each poll GET; between polls `do! Async.Sleep` (or later stream). Wake when that HTTP response arrives. Do not `Thread.Sleep` or `RunSynchronously` on the Actor path. Sync `waitUntilComplete` is console-only, or drop it. CloudAgents must not leak exceptions: HTTP failures are `AgentError`. Keep `cancel` in the library now (Core/Actor cancel protocol can stay later).

**Set 2 leftovers dropped:** `try`/`with` at the HTTP edge is the no-leak rule, already present. Library `cancel` is in scope.

## Set 3 — Create cloud-agent posts reply

PR 4 DRAFT (https://github.com/abaljeu/ambit/pull/4). Branch `cursor/cloud-agent-actor-posts-reply-0b7d`. Commits vs `ready`: `eb5c4bf` Add cloud-agent actor with POST /ambit/actors; `68d14a2` Add issue 05 and update project stage; `d79c706` Update llm-connector project stage; merge `8ba1e97`. Spec: [[plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md]], grill [[plan/llm-connector/reports/grill-run-agent-actor-2026-09-08.md]], first-agent [[plan/llm-connector/reports/first-agent-cursor-cloud-agents.md]]. Launch map must not revise [[../issues/09-define-core-command-launch-contract.md]].

### Spec

**Miss (tests do not prove Create → launch → reply):** Issue 05 asks a Server test that proves POST Create with cookie, job start, and Owned children under Focus when the Actor finishes. [[tests/Server.Tests/CloudAgentActorTests.fs]] does not POST `/ambit/actors`, does not wait until the public number is gone, and does not assert reply children. The registration fact accepts any Error except `unknown actor`. The extract fact copies the `?` trim locally; it does not call the Actor. Each fact’s `task { }` is the `try` value, so `finally` disposes FileAgent before xUnit awaits. TestActor does not replace CloudAgents HTTP-seam tests or CloudAgent Actor tests.

**Miss (Actor HTTP is sync wait):** Grill v1 is poll full result. Set 2 already locked the Actor algorithm: await POST ids, await each poll GET, `do! Async.Sleep` between polls. [[src/Server/CloudAgentActor.fs]] `runAgent` calls `AgentRunner.start` and `AgentRunner.waitUntilComplete`. That wait uses `Thread.Sleep`. Cursor HTTP uses `Async.RunSynchronously`. Sync `waitUntilComplete` stays console-only or drop. Do not wrap-patch this Actor on the current library.

**Miss (launch map and lock):** Create body is `{ actor, nodelist, focusnode, rootnode, revision }`. Issue 03 extract is the nodelist subgraph under `rootnode`. Grill lock is Focus only; pack may be larger. `Api.payloadToLaunchRequest` ignores `rootnode`, requires nodelist items as children of Focus, and sets `span.pnode` to Focus. Min/max index fills holes, so a non-contiguous nodelist becomes a contiguous span that includes nodes not in the list. Core then locks `spanIds` (those children), not Focus. `createActorFn` treats `subgraph.root` as Focus, which is true only for that inverted map.

**Miss (write-back and pack):** Grill write-back is result text → Md→graph → `postChange` Owned children of Focus. `markdownToGraph` takes only the document-root’s direct children. `addChildrenToFocus` posts `Op.NewNode` header text (and optional CSS) for those nodes. Nested Owned children from the Md read never get ops. `packToMarkdown` turns `MdDocument.writeArtifact` Error into `""`, so a pack miss still calls CloudAgents.

**Miss (stop and secrets path are incomplete on failure):** Core never sees `CURSOR_API_KEY` at register: composition in [[src/Server/RouteRegistration.fs]] bundles the key into `ActorFn`. No repo is passed (`None`). On CloudAgents Error the Actor `eprintfn`s and returns `()`. That matches “no job Error” only on the happy `async` return. There is no `try`/`with` around start/wait, so an exception on the Actor path still skips delete-actor (set 1). Redo: keep no job Error to the Create caller; Actor HTTP failures stay `AgentError` and still enqueue delete-actor.

**Miss (issue marked done; plan file broken):** Merge `8ba1e97` marks issue 05 Status `done` and checks every box. `ready` still has Status `ready-for-agent` and open boxes. [[plan/llm-connector/project.md]] on this branch contains unresolved `<<<<<<< HEAD` conflict markers.

### Standards

Measure ([[.agents/skills/code-review-fsharp/scripts/measure-fs-size.py]] with `python3`, `--diff origin/ready` on the PR tip): `addChildrenToFocus` lines 93–138 (46 lines, over 40). Long added lines: [[src/Server/Api.fs]] 387 (104); [[src/Server/CloudAgentActor.fs]] 35 (113), 79 (102); [[tests/Server.Tests/CloudAgentActorTests.fs]] 36 (108). `markdownToGraph` and `runAgent` were not measured (comma in the `let` type). Both are under 40.

[[.agents/rules/fsharp-source.md]]: no exceptions; Error types. The Actor maps `AgentError` into `string` and then discards it. `runAgent` duplicates the same five-case map for start and wait (Duplicated Code, judgement). `focusId` on `markdownToGraph` is unused. New F# in this environment is LF, same as neighbouring Server files; do not treat LF as a unique miss here.

### Redo spec (not a wrap patch)

Do not land PR 4. After rewind: Create POST `/ambit/actors` maps nodelist + Focus + root to Core launch at the edge without editing issue 09. Span parent is the extract parent (`rootnode` / nodelist parent), not Focus-as-parent unless the nodelist truly is Focus children. Lock Focus only. Actor awaits CloudAgents HTTP; no `waitUntilComplete` / `Thread.Sleep` / `RunSynchronously` on that path. Md pack Error is Error, not empty text. Md→graph posts the reply tree as Owned children of Focus. Proof has three layers (set 2): CloudAgents fake HTTP, TestActor for the Actor system, CloudAgent Actor facts with fake HTTP for pack/reply. A Server Create test with cookie asserts public number, wait until the number is gone, then Graph children under Focus. Do not mark issue 05 done until that proof exists.

### Rewind pin

PR 4 is not on `ready`. The `ready` rewind pin does not list `eb5c4bf` / `68d14a2` / `d79c706` / `8ba1e97` unless someone merges PR 4 first. Do not merge it. Treat set 3 as part of the three-set redo.

## Still open

Rewind, tighten the specs named in Intended shape plus this Create map/lock/HTTP/test proof, then reimplement. Extra `HttpClient` per call remains an optional library note from set 2.

## Time

- 2026-09-11 — Core 18 interactive review; rewind/redo plan (from chat)
- 2026-09-11 — Set 3 Create cloud-agent review (from chat)
