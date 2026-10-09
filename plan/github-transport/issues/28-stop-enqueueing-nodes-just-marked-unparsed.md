# 28 — Stop enqueueing nodes just marked Unparsed

**Status:** `done`
**Type:** coding
**Blocked by:** None
**Trial step:** 1. Next: [23 — Directory or File Load posts to the parse stack](23-directory-file-load-parse-stack.md).

**Binding arch:** [github-transport architecture](../arch.md), [core-refinement architecture](../../core-refinement/arch.md), [parse-thread architecture](../../parse-thread/arch.md).

## Context

Trial step 1. Pull request [220 — Stop enqueueing nodes just marked Unparsed](https://github.com/abaljeu/ambit/pull/220) landed this switch. The squash commit is `ff73907e`.

`requeueOnUnparsed` is `false` in [Parse thread](../../../src/Server/ParseThread.fs). MarkUnparsed still sets Unparsed. Those ids are not pushed onto the parse stack. This is not a decision to drop the requeue. [08 — Re-enable recursive update: enqueue on MarkUnparsed](../../parse-thread/issues/08-re-enable-recursive-update-enqueue-on-markunparsed.md) turns the flag back on. That re-enable ticket sits on [parse-thread](../../parse-thread/project.md). It is not a step in this trial sequence.

## What landed

1. [x] Flag — `requeueOnUnparsed` is `false`.
2. [x] No enqueue — `enqueueIfRequeue` does not push when the flag is false.
3. [x] Unparsed stays — The caller still sets Unparsed.
