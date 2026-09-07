# 18 — Finish and drop

**Status:** ready-for-agent
**Blocked by:** [[15-launch-actor-and-hold-span.md|15 Launch an Actor and hold the span]]

## Context

When an Actor stops, Core must drop the job so later query fails and lock-present goes off. Earlier Posts from that sender must still apply.

## What to build

Any Actor stop causes Core to enqueue a Core-only delete-actor mailbox item. It is not a Change. FIFO applies that sender's Posts first. Then Core drops the public number, removes the credential, and writes lock off.

- [ ] Any Actor stop enqueues a Core-only delete-actor item that is not a Change.
- [ ] delete-actor applies after that sender's earlier Posts (FIFO).
- [ ] After delete-actor, the public number is gone, the credential is out of the set, and lock-present is off.

## See also

[[11-define-actor-finish-and-failure-behavior.md]], [[02-core-actor-pool.md]]
