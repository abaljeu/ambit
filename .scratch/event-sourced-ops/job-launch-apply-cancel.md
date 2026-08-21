# Launch / finish / cancel — intended wiring

**Does not exist:** no multi-job launcher, job id, or cancel API. Not locked.

Today: one file `MailboxProcessor<FileAgentMsg>`. HTTP `/ambit/changes` and Parse both `PostAndAsyncReply` (`PostChange` / `PostGraphOnlyChange`) and **await**. Jobs must **not** run on that mailbox ([[server-concurrency.md]]). Apply is inner `applyBatch` ([[in-process-apply.md]]). Other Browsers **Poll**. **Parse File is the first such Actor** ([[parse-file-actor.md]]): it already plans off-mailbox and Posts apply; it is still request-scoped (no job id / cancel). **Shell command** is a later Actor that likely uses this N-job launch / cancel wiring ([[shell-command-actor.md]]).

## 1. Launch

Client HTTP starts N jobs. Each job is a **pool `Task`** (like Parse's `planParseFile`, off-mailbox). The launch request **returns** after spawn — it does not sit on the file mailbox.

Client keeps **N job ids** (Server maps id → `CancellationTokenSource` + Task). Not a mailbox handle.

## 2. Finish + apply

Job builds `Change` objects, then **posts a message** into `FileAgent.mailbox` (today: `PostGraphOnlyChange` JSON; intended: objects into the same inner apply). The **file agent loop** picks up and applies, one at a time.

The finishing Task does **not** return to ASP.NET. The launch request is already done. Requesting Browser: **Poll** (`getChangesSince`) — no completion push.

## 3. Cancel

Client cancel for job K: id → `CTS.Cancel()`. Job must not `Post*` if the token is cancelled. Apply does not run.

If the apply message is **already in the mailbox**, it runs. No dequeue-cancel today. Soft-lock **meaning** accepted (advisory subtree; Merge still runs — [[soft-lock.md]]). Soft-lock as cancel *surface* stays proposed.

## WORK.md

Add this file to the Active [[project.md]] related list. Stage `charting`. No lock.
