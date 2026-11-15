Check: pass

# 68 — Remove CoreMsg.PostEvent

Ticket: [68 — Remove CoreMsg.PostEvent](68-remove-coremsg-postevent.md)

Review: [68 — Remove CoreMsg.PostEvent](68-review.md)

`origin/staging` at `da2f036e` is the merge base. Client log rows and client replies stay the same.

## 1. Shared reply

- `replyStoredOr` in `src/Server/Core/CoreMailboxActors.fs` replies the stored event when `storedBySubmission` finds the submission.
- `replyClientRequest` and `replyPlacedCancel` call `replyStoredOr`, then run their fresh path.
- `postBuiltOrder` places `Order.StartActor` and replies that place. Its comment names that place. The repeated-submission sentence sits on `replyStoredOr`.
- `postBuiltOrder` replies the async result of `MailboxHost.postPlacedOrder`. That reply stays in `postBuiltOrder`.

## 2. PostedReply

- `PostedReply` in `src/Server/Core/CoreMailboxEvents.fs` stays. It is the `EventId` reply channel.
- `CoreMailboxActors` keeps `type PostedReply = CoreMailboxEvents.PostedReply` beside `MailboxContext`.
- `postBuiltOrder`, `replyClientRequest`, and `replyPlacedCancel` use that alias.

## 3. Fresh path

- `replyClientRequest` places `?test`, `?ai`, and a query as `Order.StartActor` through `postBuiltOrder`. Any other name replies `Error "unknown actor"`.
- `replyPlacedCancel` replies the stored stop from `runCancel`, or `EventId.zero`.

## 4. Suite

- An earlier `scripts/agent-test.sh --full` printed `3253` and `/tmp/ambit-full-suite-20261115-164213.log`. That process was stopped before it printed a result. The counts below are the finished run.
- `scripts/agent-test.sh --full` printed `11221` and `/tmp/ambit-full-suite-20261115-170103.log`. `bash scripts/check-test.sh 11221` printed `DONE`. The log ends `full suite finished`.
- `tests/Server.Tests` passed 775, skipped 1, failed 0, total 776, duration 3 m 3 s. The skip is ``Change.upload structure persistence preserves existing file mtime``.
- `tests/Shared.Tests` passed 1946, skipped 3, failed 0, total 1949, duration 26 s.
- Shared skip: ``reuses existing owned path without duplicating``
- Shared skip: ``planServerFilePresentOps transitions only matching absent files``
- Shared skip: ``listForPush without git still returns walk``
