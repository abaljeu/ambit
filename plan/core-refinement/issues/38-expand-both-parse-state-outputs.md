# 38 — Expand both parse-state outputs

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

[Graph change expand](plan/core-refinement/arch.md#graph-change-expand) turns one parse-state output into two. [Draft PR #257](https://github.com/abaljeu/ambit/pull/257) implemented an earlier second-path version and is superseded.

## What to build

Every parse-state change keeps today's write and also produces the new output. Existing tests pass.

### Expand

1. [ ] **Today's write** — The current parse-state write runs.
2. [ ] **New output** — The same change runs the mailbox writer on Core loop. The writer applies `InMsg.ParseFinished`, `InMsg.MarkUnparsed`, and `InMsg.SetDocumentState`, including NoServerFile. The writer logs `Op.SetDocumentState` as an Event.
3. [ ] **Both outputs** — Existing tests assert today's write and the new output. Each test proves both outputs.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change expand](plan/core-refinement/arch.md#graph-change-expand)
- [core-refinement](plan/core-refinement/project.md)
