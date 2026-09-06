# Issue 01: cross-thread `postChange`

Date: 2026-09-05

Answer to Alan on how an Actor produce call reaches Core apply, and what fire-and-forget actually starts. No software change. Issue: [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]].

## Alan's container/push model

Correct. The Actor does not run Core apply on its own thread. `CoreChanges.postChange` is a function call on the Actor thread, but that call only **enqueues a request** into the FileAgent or DbAgent `MailboxProcessor`. The mailbox loop is the sole writer. Core here is the thread-safe container (the mailbox plus the apply that drains it), not a direct apply invoke.

Issue 01 still says the Actor holds a full `CoreChanges` handle and calls Normal `postChange`. That call is the push. It is not HTTP self-post. It is not `postGraphOnlyChange`.

## No Actor response queue

There is **no** standing Actor inbox for Core replies. The only `MailboxProcessor` on this path is the agent's (FileAgent or DbAgent). There is no second mailbox owned by the Actor.

The produce acknowledgement is a **one-shot** `AsyncReplyChannel` built inside `MailboxProcessor.PostAndAsyncReply` for that one `postChange` call. FileAgent and DbAgent carry that channel on the `PostChange` message and call `reply.Reply(...)` when apply finishes. Completing that channel completes the `Async<Result<CoreChangesAccepted, string>>` that `postChange` returned. If the Actor does not await that Async, the Result is dropped. Nothing is posted onto an Actor-owned queue.

The "mailman" is the **agent mailbox loop** (`MailboxProcessor.Start` in [[src/Server/FileAgent.fs]] and [[src/Server/DbAgent.fs]]). It receives the `PostChange` request, runs apply, then Replies on the channel that came with the request. It does not deliver into an Actor mailbox.

Issue 01 protocol stays: push a request into the agent mailbox, optionally await the reply channel. A per-Actor response queue would be new machinery (pool / finish behavior: [[plan/core-creation/issues/02-core-actor-pool.md]], [[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]]). It is not in this produce path.

**Poll is not this ack.** Other Browsers consume by Poll (`Api.getPoll` / `getChangesSince`). That is History since a Revision. The produce acknowledgement is the `CoreChangesAccepted` (or Error string) on this post's reply channel: `revision`, confirmed Changes, `externalChanges`, `message`, `isReady`.

## What `postChange` is

[[src/Server/Core/CoreChanges.fs]] types `postChange` as `Change list -> Async<Result<CoreChangesAccepted, string>>`. FileAgent and DbAgent implement it as:

`agent.mailbox.PostAndAsyncReply(fun reply -> PostChange(changes, reply))`

F# `Async` is cold. Calling `handle.postChange changes` **builds** that Async. It does not run it. Enqueue happens only after someone **starts** the Async (`let!` / `do!`, `Async.Start`, `Async.StartAsTask`, `Async.RunSynchronously`).

`MailboxProcessor.Post` / `PostAndAsyncReply` are the cross-thread seam. They are thread-safe by design. Several Actor (or HTTP) threads may Post at once. The mailbox serializes apply.

## Actor thread vs mailbox thread

On the Actor thread, once the Async **starts**:

1. Build the `PostChange` message, including the one-shot reply channel.
2. `Post` that message into the agent mailbox (enqueue returns immediately).
3. Wait on the reply channel until the mailbox loop Replies (this wait is what `let!` awaits; `Async.Start` still waits in the background and then discards the Result).

On the mailbox loop (not the Actor thread):

1. `inbox.Receive` dequeues `PostChange`.
2. `handlePostChange` runs: `ChangeAmendment.applyChange`, path validation, persist, ChangeLog append, then `state` update. That is amend, log, and Poll-visible sequence.
3. `reply.Reply(Ok accepted)` or `reply.Reply(Error ...)`.

FileAgent runs that apply synchronously inside the loop. DbAgent uses the same `PostChange` + `Reply` shape (apply is bounded via `FileAgent.runBounded`).

[[src/Server/Core/CoreRuntime.fs]] `ofFileWithDbMirror` still only Posts; it awaits file then optionally Posts to Db. The Actor still does not apply.

The existing test in [[tests/Server.Tests/CoreChangesTests.fs]] starts with `Async.StartAsTask` and awaits. It is not an Actor. It is the same enqueue-and-wait seam.

## Fire-and-forget rule

Safe (enqueue actually happens once the Async body begins):

- `let! accepted = handle.postChange changes` (or `do!` / `Async.Ignore`) inside a **started** Actor Async.
- `Async.Start (handle.postChange changes)` before the Actor function returns. Apply still runs. The Result is ignored, so Error is hidden.
- `Async.StartAsTask (handle.postChange changes)` — same start; the Task holds the Result if someone awaits it later.

Never fires:

- Call `handle.postChange changes` and return (or ignore) the **unstarted** Async. Example: `async { handle.postChange changes; return () }` without `let!` / `Async.Start`. That is "calling Async x and returning." The mailbox never sees a message. Apply never runs.

Race after a real start: `Async.Start` schedules the body on the thread pool. Post is the first work in that body, but it has not happened until the pool runs it. A process or test that exits (or disposes the agent) before that run can drop the post. A long-running Server almost always wins that race. A one-shot test that Starts and then ends the method can lose it. Once Post has run, the request is in the mailbox; abandoning the waiter does not cancel apply.

`Async.Start` is "start the enqueue (then wait for ack in the background)." Returning an unstarted Async is not a start.

If Start ran and you do not wait: apply still amends, logs, and becomes Poll-visible when the mailbox processes the message. The produce acknowledgement is dropped. Poll still sees the Change. The Actor does not see Error.
