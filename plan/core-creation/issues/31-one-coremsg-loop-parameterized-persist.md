# 31 — One CoreMsg loop, parameterized persist

**Status:** done
**Blocked by:** [[30-reshape-coreactorpool-synchronized-table.md]] — Reshape CoreActorPool to a synchronized table (done; keep the edge so Point 0 stays ordered)

## Context

FileAgent and DbAgent each run a MailboxProcessor that matches the same CoreMsg cases. That is duplication. CoreMailbox already exposes the public push API (getState, postChange, …). One entity should run the pull loop. Persist (File vs Db) is a parameter for the cases that persist. Not every message is File or Db — Actor and other mailbox-state cases stay off the persist parameter. This is Point 0 preamble: fix Core shape before new behavior. Do not implement Prove TestActor hello.

## What to build
    10|
One loop pulls CoreMsg. It is parameterized to use File or Db persist for persist cases only.

- [x] One loop entity owns MailboxProcessor<CoreMsg> / the pull match. FileAgent and DbAgent do not each copy the loop.
- [x] Persist cases (GetState, GetRevision, GetChangesSince, PostChange, PostGraphOnlyChange, and existing SnapshotDone as today) call into an injected File or Db persist implementation.
- [x] The public CoreMailbox functions remain the push side (a,b,c,d,e). The loop calls the persist (or later Actor) handlers (a',b',…).
- [x] No new Actor behavior, no TestActor hello, no new CoreMsg Actor cases in this ticket unless required to keep compile. Do not restore discarded hello code.
- [x] Existing File and Db tests still pass. No new Browser chrome.

## See also
    20|
[[30-reshape-coreactorpool-synchronized-table.md]], [[29-prove-testactor-hello.md]], [[Implementation Planning and Record.md]], [[src/Server/Core/CoreMailbox.fs]], [[src/Server/Core/CoreMailboxBackend.fs]]

## Comments

- 2026-09-12 — Alan: Point 0 preamble. 30 is a separate commit; 31 is also Point 0. One loop, File/Db injected; not all items are persist.
- 2026-09-12 — Locked element names:
  1. **Persist parameter** — record of persist handlers for GetState, GetRevision, GetChangesSince, PostChange, PostGraphOnlyChange, SnapshotDone. File and Db are two fillings. Not a mailbox.
  2. **One loop** — owns MailboxProcessor<CoreMsg> and the match. Lives in CoreMailbox / CoreMailboxBackend. No third mailbox type.
  3. **Two persist fillings** — FileAgent and DbAgent keep those names; they lose their own Start/loop. Persist logic stays; twin queues go.
  - Do not create Actor CoreMsg cases, TestActor, Browser bits, or a second pool queue.
- 2026-09-12 — Implementation complete. Time: ~2 hours. Created PersistHandlers record in CoreMailboxBackend with shared loop. FileAgent and DbAgent refactored to inject handlers. DbAgentStartup extended with startWithBackend. Tests: FileAgent 7/7, CoreRuntimeTests 7/7, CoreActorPoolTests 10/10 passing. Merge commit e03dd15 on origin/staging.
- 2026-09-12 — CoreMailbox is the only mailbox; Db extra Start removed. Deleted DbAgentStartup entirely. Only CoreMailboxBackend.start constructs MailboxProcessor<CoreMsg>. DbAgent now uses CoreMailboxBackend.start like FileAgent. Sweep runs on Task; ready/fail state encoded in PersistHandlers. Tests: FileAgent 7/7, CoreRuntimeTests 7/7, CoreActorPoolTests 10/10 passing.
