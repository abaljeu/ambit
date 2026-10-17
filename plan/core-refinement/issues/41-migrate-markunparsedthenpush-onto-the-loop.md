# 41 — Migrate markUnparsedThenPush onto the loop

**Status:** `defined`
**Type:** coding
**Blocked by:** [38 — Expand both parse-state outputs](38-expand-both-parse-state-outputs.md)
**Trackers:** architecture

## Context

[Graph change migrate — markUnparsedThenPush](plan/core-refinement/arch.md#graph-change-migrate-push). `markUnparsedThenPush` runs on the mailbox loop and relies on the new output. Today's write stays.

## What to build

`markUnparsedThenPush` runs on the mailbox loop. A caller outside that loop enqueues `InMsg.MarkUnparsed`.

### Migrate

1. [ ] **On the loop** — `markUnparsedThenPush` runs on the mailbox loop.
2. [ ] **Callers** — A caller outside that loop enqueues `InMsg.MarkUnparsed` and relies on the new output.

### Obligations

1. [ ] **`markUnparsedThenPush on the loop relies on the mailbox writer`** — [Core loop InMsg tests](tests/Server.Tests/CoreLoopInMsgTests.fs). The test runs on the mailbox loop. The node ends Unparsed from the new output.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change migrate — markUnparsedThenPush](plan/core-refinement/arch.md#graph-change-migrate-push)
- [core-refinement](plan/core-refinement/project.md)
