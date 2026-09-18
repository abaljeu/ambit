# Spec review — uncommitted vs HEAD

Range: working tree vs HEAD (modified and untracked). No base SHA. Spec: [[plan/core-creation/issues/29-prove-testactor-hello.md]], dispatch [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]]. Delivery notes [[plan/core-creation/reports/implement-issue-29-testactor-hello.md]] are not a substitute.

Public Core `launch` / `poll` / `post`, `{ nodes; events; latestId }`, composition register of TestActor, Command text `?test hello`, no role/Kind/CSS/Focus-Header selection, Authority+secret on launch and Actor post, FileAgent mailbox registry, off-queue `Async.Start`, Succeeded as Core-only, one ActorFinished then drop registry+secret, observe-from-outside tests, and secret-on-ActorStart are present. Proof boxes on 29 stay unchecked; the facts exist.

## (a) Missing or partial

- 29: "After launch, the outer fact sees ActorStarted with durable public Actor identity before Actor output is admitted." The launch fact asserts `started.id > 0` and authority `"test"`. It does not assert `started.actor` (`PublicActorId`). "Before output" is only empty Focus children on the launch snapshot.
- 29: "the hello Graph (one Owned child text `hello`)." Hello facts assert child text `hello`. They do not assert `Ownership.Owner`.

## (b) Scope creep

- 29: "This increment does not include live query, cancel, host-stop, fail, post-twice, duplicate terminal, Interrupted restart, Browser chrome, or Actor definitions other than TestActor." DbAgent gains `Actor` arms that ignore the message (`| _ -> ()`) and do not reply. Hello does not run there. A Launch/Post/Poll on that mailbox would not complete.

## (c) Implemented wrong

- 29: "Core registers that live Actor against Focus, appends ActorStarted, then schedules the Actor so the Core mailbox is free." 07: "registers the Actor, durably appends ActorStarted, and schedules the task so ActorStarted and the registry exist before Actor output can be admitted." `handleLaunch` does `commitLaunch`, then `scheduleActor`, then `withLive`. Live registry (Focus, secret, CTS) is written after `Async.Start`. A Post in that window is refused by `isLiveSecret`.
- 29: "then requests terminate only if the task still runs and never waits." 07: "requests asynchronous termination only when the task is still running. Core never waits for termination." `succeed` calls `Cancel()` when the token is not already cancelled. It does not test whether the task still runs. After hello, the body has already returned. It does not wait.
