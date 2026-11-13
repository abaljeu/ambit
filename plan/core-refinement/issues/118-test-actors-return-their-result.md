# 118 — Test Actors return their result
**Status**: defined
One job: The two recording-pool door tests stop a run by posting `Notice.ActorStopped`. They stop calling `CoreMailboxApi.actorStop`. This is a strict refactor. The observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results stay the same. The reason is the 2026-10-24 decision in [core-refinement](../project.md): the pool posts the stop, and a test of that notice uses the notice line.

Type: coding
Blockers: [117 — The pool posts Notice.ActorStopped when an Actor ends](117-the-pool-posts-notice-actorstopped-when-an-actor-ends.md)

## Scope

Slice: migrate the two door tests that call `CoreMailboxApi.actorStop` onto `CoreMailboxApi.postNotice`. Source stays.

[ ] The build keeps the observable log (`EventBody.ActorStop` alias, `Notice.ActorStopped`) and the results the same.

### Signatures

[x] `CoreMailboxApi.actorStop` in `src/Server/Core/CoreMailboxApi.fs` posts `CoreMsg.ActorStop`.
[x] `CoreMailboxApi.postNotice` posts `CoreQueue.Notice` and returns before the mailbox applies the notice.
[x] `poolForFixedSecret` in `tests/Server.Tests/TestBackend.fs` sets `trySecretForActorNode` to a function that returns `None`.
[x] After 117, `CoreMailboxLoop.dispatchItem` finishes a run for `Notice.ActorStopped` when `trySecretForActorNode` returns a secret.

### Match sites

[x] ``CoreMailboxApi.actorStop with valid credential drops live row`` and ``CoreMailboxApi.actorStop appends ActorStop and drops live row`` in `tests/Server.Tests/CoreMailboxDoorTests.fs` call `CoreMailboxApi.actorStop`.
[x] `postActorStop` in `tests/Server.Tests/CoreMsgActorCasesTests.fs` calls `CoreMailboxApi.actorStop`. [114 — Remove CoreMsg.ActorStop](114-remove-coremsg-actorstop.md) moves those tests.
[x] ``actorStop after cancel does not emit a root Focus ActorStop`` in `tests/Server.Tests/CancelByFocusTests.fs` calls `CoreMailboxApi.actorStop`. 114 moves that test.
[x] `tests/Server.Tests/QueryActorTests.fs`, `tests/Server.Tests/OrderedEventStreamTests.fs`, and `tests/Server.Tests/AgentAskTests.fs` contain no `actorStop` call. `QueryActorTests` starts `SearchActor.functionStart`. After 117 that function returns its result and the pool posts the notice.

### Existing tests

[x] ``CoreMailboxApi.actorStop with valid credential drops live row`` builds a recording pool from `poolForFixedSecret`, calls `CoreMailboxApi.actorStop` with `ActorSucceeded`, and asserts `finish` ran once.
[x] ``CoreMailboxApi.actorStop appends ActorStop and drops live row`` calls `CoreMailboxApi.actorStop` and asserts one `EventBody.ActorStop` of the focus.
[x] The recording pools override `finish`, `isLive`, and `getActorNodeId`. They leave `trySecretForActorNode` as `None`.

### Copy-from

[x] ``ActorStopped for an idle focus logs nothing`` in 117 posts `Notice.ActorStopped` with `CoreMailboxApi.postNotice`. These two tests use that post for a focus that has a run.

### Overlap

[x] This ticket does not edit `src/Server/SearchActor.fs`. [23 — Server search works](../../online-search/issues/23-server-search-works.md) and 117 own that file. [24 — Post Notice.SearchStopped](../../online-search/issues/24-post-searchstopped-notice.md) stays with AmbotX.

## Inputs

### Working

[x] After 117, no source file calls `CoreDoor.actorStop` or `ActorChanges.actorStop`.
[x] The two door tests still call `CoreMailboxApi.actorStop`.
[x] `poolForFixedSecret` returns `None` from `trySecretForActorNode`.
[ ] The two tests post `Notice.ActorStopped` of the focus with `CoreMailboxApi.postNotice`, then read the log.
[ ] Each recording pool's `trySecretForActorNode` returns the live secret for that actor node id.

### Reference

[x] The 2026-10-24 Actor-end decision is in the project file. The One job line links it.

## Do not load

[x] `CoreMsg.ActorStop` and `CoreMailboxApi.actorStop`. 114 removes them.
[x] `postActorStop` and the three credential-refusal tests in `tests/Server.Tests/CoreMsgActorCasesTests.fs`. 114 owns those tests. A notice has no caller to refuse.
[x] ``actorStop after cancel does not emit a root Focus ActorStop``. 114 posts that notice.
[x] `src/Server/SearchActor.fs`.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Rename ``CoreMailboxApi.actorStop with valid credential drops live row`` to ``ActorStopped Notice drops live row``. The test posts `Notice.ActorStopped` of the focus with `CoreMailboxApi.postNotice`.
3. [ ] Rename ``CoreMailboxApi.actorStop appends ActorStop and drops live row`` to ``ActorStopped Notice is logged and drops live row``. The test posts the same notice, then reads one stop of that focus.
4. [ ] Each recording pool's `trySecretForActorNode` returns the live secret for that actor node id.
5. [ ] The assertions on `finish` and the stop body stay.

## Outputs

[ ] Source files: none.
[ ] Test file: `tests/Server.Tests/CoreMailboxDoorTests.fs`.

## Stop when

[ ] Rewrite `118-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. Then ask.
[ ] Stop when the edit changes a source file.
[ ] Stop when the edit changes the stop body or the `ActorResult`.
[ ] Stop when the edit needs a file outside Inputs.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``ActorStopped Notice drops live row``
  - `tests/Server.Tests`: ``ActorStopped Notice is logged and drops live row``
[ ] All executed tests pass.
[ ] Rewrite `118-build.md` with the present line. The line is `STOP: <fact>.` or `Check: pass` or `Check: fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
