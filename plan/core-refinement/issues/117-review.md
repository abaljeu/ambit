# 117 — The pool posts Notice.ActorStopped when an Actor ends

Ticket: [[117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

Behavior commit `a5247a36`. `origin/staging` at `4273ef22` is that commit's parent. This file is the following commit on `cursor/pool-posts-actor-stopped-9868`.

## 1. Standards

- `python scripts/standards-scan.py --diff origin/staging` printed `postStopUnlessCancelled` 12 lines, `runReturned` 10 lines, `logActorStopped` 24 lines, and `logPostedNotice` 15 lines. Each size is inside the 40-line function limit in `.agents/rules/fsharp-source.md`.
- No findings.

## 2. Spec

- The Signatures box for `CoreMailboxLoop.dispatchItem` now says `Notice.ActorStopped` goes to `CoreMailboxLoop.logActorStopped`, and every other `CoreQueue.Notice` goes to `CoreMailboxLoop.logPostedNotice`. The Inputs box and Process step 2 now say `CoreActorPool.runReturned` binds the result and `CoreActorPool.postStopUnlessCancelled` posts it. `117-build.md` names those helpers. The checked boxes match the tree.
- `CoreActorPool.runReturned` posts in the success continuation of the actor async, on the thread that finished that async. Awaited posts from `TestActor` (`postEvents`, `postChanges`), `RunAgentActor.postReplace`, and `GithubTransportActor` work are queued before that post. Staging called `actorStop` in that same continuation. The stop row stays after those events. Production bodies return after their own posts. A child actor's stop is queued when that child returns, the same moment staging called that child's `actorStop`.
- `CoreMailboxActors.runCancel` is unchanged. `Order.CancelActor`, `CoreMailboxApi.cancelByFocus`, and a posted Cancel log one `EventBody.ActorStop` of the focus and `ActorCancelled`, then `CoreActorPool.finish` cancels the row token. `postStopUnlessCancelled` sees that token and leaves the queue alone. The log holds that one cancel row.
- `CoreMailboxApi.actorStop` after that cancel finds no live secret in `CoreMailboxActors.dispatchActorStop` and appends nothing. ``actorStop after cancel does not emit a root Focus ActorStop`` still expects one stop on the focus and none on the root.
- Shutdown calls `CoreActorPool.cancelLiveActors` before the `Shutdown` item. A queued `Notice.ActorStopped` finds no secret. A body that is still running sees the cancelled token. Staging's queued `CoreMsg.ActorStop` found no secret after the same clear. Shutdown adds no stop row.
- `RunAgentActor.actorFn` returns `ActorCancelled` for `CompleteCancelled` while the row token is clear. The pool posts that result. Staging `RunAgentActor.stop` posted that same result.
- Staging called stop once on each return path of `GithubTransportActor.finishActor`, `RunAgentActor.actorFn`, `SearchActor.queryActorBody`, `SearchActor.searchActorAt`, `TestActor.dispatch`, and `TestActor.runBehavior`. The pool posts that one stop. `GithubTransportActor.runSave` calls `runSaveBody` and stays outside the pool, as it did on staging.
- `SearchActor.searchActorAt` is placed by `CoreMailboxApi.recordSearchStart`. The pool posts one stop for that run. `CoreMailboxApi.recordSearchStop` still appends its own stop and leaves the live row for the pool finish. A search that runs both still has those two rows.
- An exception in the work ends `runReturned` before the post. Staging called `actorStop` after the work binding returned, so an exception produced no stop row then and produces none now.
- The `src/Server/SearchActor.fs` diff removes `SearchActor.actorCaller` and edits `queryActorBody`, `functionStart`, `functionActorAt`, and `searchActorAt`. `SearchActor.reply`, `SearchActor.actorStart`, and `SearchActor.oneReply` stay.
- The scope report records the pre-change tree at `4273ef22`. Ticket text and `117-build.md` name files and symbols.
- The same-focus limit in the ticket stays: `logActorStopped` looks up the live secret by actor node id.

## 3. Arch

- No findings.

## 4. Tests

- `bash scripts/check-test.sh 3506` printed `DONE` and `/tmp/ambit-full-suite-20261113-203754.log`. The log printed `full suite finished`.
- Server.Tests passed 776, failed 0, skipped 1 (`DocumentPersistenceTests.Change.upload structure persistence preserves existing file mtime`), total 777, duration 2 m 53 s.
- Shared.Tests passed 1944, failed 0, skipped 3 (`WorkspaceUploadStructureTests.reuses existing owned path without duplicating`, `WorkspaceUploadStructureTests.planServerFilePresentOps transitions only matching absent files`, `WorkspaceLocalInventoryTests.listForPush without git still returns walk`), total 1947, duration 30 s.
- That Server run includes ``an Actor that returns logs Notice.ActorStopped``, ``ActorStopped for an idle focus logs nothing``, ``actorStop after cancel does not emit a root Focus ActorStop``, ``mailbox appends StartActor and ActorStop in lifecycle order``, and ``ActorStop reads the carrier graph once``.

## 5. Miss

- No findings.

## 6. Found elsewhere

The following issues were found beyond the ticket scope.  The fixes are suggested.

- No findings. The PR body has no Found elsewhere list.
