# 32 — Persist agents under Core, generic CoreMailbox door

**Status:** done
**Blocked by:** [[31-one-coremsg-loop-parameterized-persist.md]] — One CoreMsg loop, parameterized persist (done; keep the edge so Point 0 stays ordered)
Estimate: 2h
Actual: 1h15m

## Context

Issue 31 put one CoreMsg loop in CoreMailbox / CoreMailboxBackend and left FileAgent and DbAgent as persist fillings. Those fillings still live outside [[src/Server/Core/]], still wrap CoreMailbox, and still leak as the public door: [[src/Server/Core/CoreRuntime.fs]] and [[src/Server/DatabaseSetup.fs]] call `FileAgent.coreChanges` / `DbAgent.coreChanges`, and many non-agent tests construct the agents directly. The wrappers at FileAgent tryGetState through dispose and DbAgent isReady through coreChanges are the same mailbox door twice. This is Point 0 preamble: invert that door before new behavior. Do not implement Prove TestActor hello.

## What to build

Move the persist fillings into Core. They do not call CoreMailbox. CoreMailbox may call them. The listed agent wrappers become generic CoreMailbox functions. Only tests that are specifically for these agents name FileAgent or DbAgent.

- [x] Move [[src/Server/FileAgent.fs]] to [[src/Server/Core/FileAgent.fs]] and [[src/Server/DbAgent.fs]] to [[src/Server/Core/DbAgent.fs]]. Same module names. Update [[src/Server/Gambol.Server.fsproj]] compile order.
- [x] FileAgent and DbAgent do not open or call the `CoreMailbox` module. They keep PersistHandlers and `CoreMailboxBackend.start` / `startWithPrelude`. Drop the thin wrappers (tryGetState, getState, getRevision, getChangesSince, coreChanges, and the File flushSnapshot / dispose / initialState and Db isReady wrappers that only forward).
- [x] Split `CoreMsg` out of [[src/Server/Core/CoreMailbox.fs]] so the CoreMailbox module can compile after the agents without a cycle. Keep `CoreMsg` with the loop (same file as CoreMailboxBackend, or a sibling compiled before Backend). Do not add a third mailbox type.
- [x] Add one generic host record (name: **MailboxHost**) compiled before the agents: mailbox, isReady, flushSnapshot, dispose. File fills dispose and the existing flush stub; isReady is `fun () -> true`. Db fills isReady; flushSnapshot is `Ok ()`; dispose is a no-op. Each agent exposes a function that builds MailboxHost. `initialState` stays on FileAgent (only FileAgentFailureTests reads it).
- [x] Put the generic door on CoreMailbox: tryGetState, getState, getRevision, getChangesSince, coreChanges, isReady, flushSnapshot, dispose. They take MailboxHost (or create File/Db and return MailboxHost). Existing CoreMailbox functions that take `MailboxProcessor<CoreMsg>` may remain as the inner implementation.
- [x] Production constructors live on CoreMailbox (`createFile`, `createDb` / `createDbWithDataDir`). [[src/Server/Core/CoreRuntime.fs]] and [[src/Server/DatabaseSetup.fs]] use only CoreMailbox. DatabaseSetup’s cache holds MailboxHost or CoreChanges, not DbAgent.
- [x] Only [[tests/Server.Tests/FileAgentFailureTests.fs]], [[tests/Server.Tests/DbAgentTests.fs]], and [[tests/Server.Tests/DbAgentFailureTests.fs]] name FileAgent or DbAgent. Those tests may keep create / createWithDependencies / createForTest* / initialState. All other Server tests and Server modules go through CoreMailbox. Do not add InternalsVisibleTo.
- [x] Existing File, Db, CoreRuntime, and CoreActorPool tests still pass. No new Actor behavior, no TestActor hello, no new CoreMsg Actor cases, no new Browser chrome.

## See also

[[31-one-coremsg-loop-parameterized-persist.md]], [[29-prove-testactor-hello.md]], [[Implementation Planning and Record.md]], [[src/Server/Core/CoreMailbox.fs]], [[src/Server/Core/CoreMailboxBackend.fs]], [[src/Server/Core/CoreRuntime.fs]], [[src/Server/DatabaseSetup.fs]], [[plan/core-creation/reports/file-db-agent-mailbox-twins.md]], [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]]

## Comments

- 2026-09-13 — Alan: Point 0 after 31. FileAgent and DbAgent move to Core/. They shall not depend on CoreMailbox. CoreMailbox may depend on them. Nothing except a test that is specifically for these agents shall use them. The File and Db wrapper functions become generic and live on CoreMailbox.
- 2026-09-13 — Locked element names:
  1. **MailboxHost** — generic record the CoreMailbox door takes: mailbox, isReady, flushSnapshot, dispose. File and Db are two fillings. Not a mailbox and not a third agent type.
  2. **CoreMsg split** — `CoreMsg` compiles before Backend and the agents. The `CoreMailbox` module compiles after the agents so it may call FileAgent and DbAgent.
  3. **Agent-test exception** — FileAgentFailureTests, DbAgentTests, DbAgentFailureTests only. They construct persist fillings. They use CoreMailbox for mailbox ops.
  - Do not create Actor CoreMsg cases, TestActor, Browser bits, or InternalsVisibleTo.

## Time

- 2026-09-13 1h15m — invert persist-agent door onto CoreMailbox (from chat)
