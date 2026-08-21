# How this Server runs concurrent work

Facts from [[src/Server/FileAgent.fs]] / [[src/Server/DbAgent.fs]]. Not a design lock. No long-running Server-side Actor exists yet.

## Mailbox — yes, a thread-safe queue, one at a time

`FileAgent.mailbox` is a `MailboxProcessor<FileAgentMsg>`. Comment: "serialises all reads/writes for a single file."

The loop (`MailboxProcessor.Start`) `Receive`s one message, `dispatch`es it, then loops. Same file: no two Changes apply at once. Safe to post from many ASP.NET request Tasks. Not "run each Change on a random pool thread next to other Changes for that file."

C# picture: `Channel<T>` / concurrent queue + **one** consumer loop. `PostAndAsyncReply` is post + `TaskCompletionSource` wait for `reply.Reply`.

DbAgent uses the same `FileAgentMsg` mailbox.

## `/changes` — post and **await** the reply

HTTP request is an ASP.NET Task (`Async.StartAsTask`). `FileAgent.postChange` is `mailbox.PostAndAsyncReply(PostChange(body, reply))`. The request **waits** for apply + ACK JSON. Not fire-and-forget.

`History.applyChange` runs **on the agent loop** (inside `applyBatch` / `dispatch`). The pool is used for the request Task and, separately, `FileAgent.runBounded` → `Task.Run` only around **disk persist** (`persistGraphOps`), with a timeout so a hung write does not wedge the loop forever.

## Parse — plan off the mailbox, then a message for apply

`Api.postParseFile`: `getState` (mailbox, await) → `DocumentPersistence.planParseFile` on that snapshot (**not** in the apply loop) → `postGraphOnlyChange` (mailbox, await). Same apply path as `/changes`. No job scheduler.

That is the analogue: long work **off** the mailbox, then **forward a message** so the agent picks up apply. Do not hold the mailbox for the whole job. The job itself is not implemented. Launch / finish / cancel wiring: [[job-launch-apply-cancel.md]].

## WORK.md mutations

Add this file to the Active [[project.md]] related list. No lock. No `add` / `move` / `block` / `remove`.
