# 09 framework failure preserves children

Date: 2026-09-19. Ticket: [09 — Agent failure preserves children](../issues/09-agent-failure-preserves-children.md). Status `coded` for the framework half only.

## 1. What landed

1. **Terminal Failed only** — TestActor non-hello (`?test unknown`) queues `ActorFailed` → ActorFinished. [CoreMailboxBackend](../../../src/Server/Core/CoreMailboxBackend.fs) `dispatchActorStop` appends the terminal and [CoreActorPool](../../../src/Server/Core/CoreActorPool.fs) `finish` drops the live row and secret. The framework path posts no Change.
2. **Safe error only** — ActorFinished is `EventBody.ActorStop(focusId, ActorFailed)` with empty `commandName`. No extra error field and no provider payload on Graph Events.
3. **Out of scope** — AI Actor erase-on-CloudAgents-Failed stays open until [08 — Run Agent Actor calls CloudAgents](../issues/08-agent-ask-from-what-i-see.md) and [12 — Replace Focus Children from reply](../issues/12-replace-focus-children-from-reply.md).

## 2. Proof

1. **Reuse** — Existing [TestActorCommandErrorTests](../../../tests/Server.Tests/TestActorCommandErrorTests.fs) already cover unregistered start fail, `?test unknown` / `?test nope` → ActorFailed with no hello child, and `?test hello` success.
2. **New fact** — `TestActor non-hello preserves Focus Children and posts no Change` seeds two Owned children under Focus, runs `?test unknown`, then asserts: children and node texts match the pre-failure set; EventLog still has one Change (the seed); chronological tail after ActorStart is one ActorStop `ActorFailed`; live Focus row is gone.
3. **Hello suite** — [TestActorHelloTests](../../../tests/Server.Tests/TestActorHelloTests.fs) still pass (8 facts), including the older CSS `unknown` → ActorFailed case.

## 3. Production code

No Server or Shared edit. [TestActor](../../../src/Server/TestActor.fs) already returns `ActorFailed` without calling hello. Framework stop already writes lifecycle only.

## 4. Verify

1. Focused: `TestActorCommandErrorTests` 8 passed; `TestActorHelloTests` 8 passed.
2. Client compile gate skipped: no Client or Shared production change.
3. Full suite: `./scripts/test.sh all` after this report lands.
