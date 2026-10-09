# 08 — Re-enable recursive update: enqueue on MarkUnparsed

**Type:** coding
**Status:** blocked

## 1. What to build

Set `requeueOnUnparsed` back to true in [Parse thread](../../../src/Server/ParseThread.fs). Restore the disk-newer requeue test so a disk-newer file is enqueued after MarkUnparsed and its text lands.

## 2. Precondition

This ticket stays blocked until both are true.

1. **Paced stacks** — Server functions, including the parse stack and the persist stack, stay inside the Azure Free plan CPU limits: under 60% over short windows, and under 5% daily average.
2. **Field test** — Alan has finished field testing.

## Time

- 2026-10-09 15m — filed the re-enable ticket (from chat)
