# Implement issue 32

Invert the persist-agent door: FileAgent and DbAgent live under Core, CoreMailbox is the only mailbox API.

## Done

- Moved FileAgent and DbAgent into `src/Server/Core/`. Same module names.
- `CoreMsg` lives with the loop in CoreMailboxBackend. `MailboxHost` compiles before the agents. `CoreMailbox` compiles after them.
- Agents keep PersistHandlers and Backend start; they do not call `CoreMailbox`. They expose `mailboxHost`. File keeps `initialState`.
- CoreMailbox door: tryGetState, getState, getRevision, getChangesSince, coreChanges, isReady, flushSnapshot, dispose, plus `createFile` / `createDb` / `createDbWithDataDir`.
- CoreRuntime and DatabaseSetup use only CoreMailbox. DatabaseSetup cache is `MailboxHost`.
- Only FileAgentFailureTests, DbAgentTests, and DbAgentFailureTests name the agent modules.

## Tests

- Focused Server filters (FileAgentFailure, DbAgent, CoreChanges, CoreActorPool, CoreRuntime): 42 passed.
- Retargeted suites (GraphOnlyChangePost, LazyLoad, IgnoredDestination, DatabaseProjectionContract): 41 passed.
- Full `./scripts/test.sh all`: Server 369 passed; Shared 1566 passed, 1 skipped.

## Review

Standards: no hard violations. Spec: residual FileAgent strings in error-message tests; posts stay public on CoreMailbox.
