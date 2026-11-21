# 79 — Place git Save as Order.Save

**Status:** retired
**Trackers:** architecture

## 1. Goal

[x] Place git Save as `Order.Save` and log that order as `EventBody.Order`.

## 2. Answer

This ticket is retired. The sequence is Solution 11 — git Save sequence in [github-transport spec](../../github-transport/spec.md).

git Save is `LoadSaveOperation.Save`. `CoreMailboxApi.startLoadSaveCommand` sends `StartLoadSaveCommand`. `GithubTransportActor.saveActorFunction` is the Save actor.

## Comments

- 2026-10-21: Split from 66 — Move Actor orders onto Order and Notice.
- 2026-10-25: Out of the type chain. The ticket adds `Order.Save` and removes no extra type.
- 2026-11-08: This ticket is retired. The sequence is Solution 11 — git Save sequence.
