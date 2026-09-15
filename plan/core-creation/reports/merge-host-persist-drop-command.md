# One host, persist filling, no command door

Date: 2026-09-14. No commit.

## 1. Merged shape

1. **`CoreMailbox.host`** — one starter. Takes `CoreCredentials`, `CoreActorPool`, and `PersistFilling`. Starts `start` or `startWithPrelude` from the filling’s `until`, then `bindMailbox`, then copies ready / flush / dispose onto the existing `MailboxHost`.
2. **`PersistFilling`** — lives next to `MailboxHost` in [[src/Server/Core/MailboxHost.fs]]. File and Db build it (`FileAgent.persist`, `DbAgent.persist`). CoreMailbox does not read agent fields.
3. **`hostFile` / `hostDb`** — deleted. `createFile` / `createDb` / `createDbWithDataDir` are thin wrappers around `host` + `persist`.
4. **`CoreRuntime.command`** — deleted. `create` still builds the pool and passes it into `host`; the mailbox closes over it. Callers have no second handle. No lock restored. CoreCredentials stays its own mailbox. No façade type.

## 2. Files changed

1. [[src/Server/Core/MailboxHost.fs]] — add `PersistFilling`.
2. [[src/Server/Core/FileAgent.fs]] — `persist` (until `None`, bind `ignore`); drop per-field accessors.
3. [[src/Server/Core/DbAgent.fs]] — `persist` (until `Some`, bind mailbox); drop `*Of` / `attachMailbox` accessors. Private `DbAgent` record unchanged.
4. [[src/Server/Core/CoreMailbox.fs]] — one `host`; delete `hostFile` / `hostDb`.
5. [[src/Server/Core/CoreRuntime.fs]] — call `host` with persist; drop `command` field and `bindRuntime` pool arg.
6. [[tests/Server.Tests/TestBackend.fs]] — `admittedHostFile` / `admittedHostDb` pass fillings.
7. [[tests/Server.Tests/CoreMsgActorCasesTests.fs]] — `host` + `FileAgent.persist`.
8. [[tests/Server.Tests/CoreRuntimeTests.fs]] — drop `runtime.command.isLive` poke; keep changes revision.

RouteRegistration and adapters had no `command` use. Production HTTP already did not use it.

## 3. Tests

Filter `FullyQualifiedName~CoreMsgActorCasesTests|FullyQualifiedName~CoreRuntimeTests|FullyQualifiedName~CoreMailbox`.

**16 passed / 0 failed / 0 skipped.**

Server.Tests compiled, so File/Db agent test modules typecheck.

## 4. Complexity alert

Did not get fat: no second host type, no dual APIs, no `ofFileWithDbMirror`, no command façade.

The one extra vs “handlers / flush / ready / dispose” is `until` + `bindMailbox` on the filling. Those are persist-owned (Db prelude and mailbox bind). Host branches on `until` so File still uses `start` (no prelude poll) and Db uses `startWithPrelude`. That is the File vs Db delta, not a second program.
