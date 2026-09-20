# Standards — 52 Run abort when commit fails

Range: three-dot `origin/staging...HEAD` (merge-base `bd1e8059`, tip `d36c65ef`). Ticket [52 — Run must not launch when edit commit fails](plan/core-creation/issues/52-run-abort-when-commit-fails.md).

## Hard violations

1. File length — [UpdateHelpers.fs](src/Client/UpdateHelpers.fs)

Scan: `FILE 447->460 already over 400 or new file over 400; change increased it`. Rule: [fsharp-source.md](.agents/rules/fsharp-source.md) (800/400 split; if a file is already longer, split only when the change would increase it). `commitIfEditingForRun` added 13 lines to a file already over 400.

2. File length — [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs)

Scan: `FILE 423->425`. Same [fsharp-source.md](.agents/rules/fsharp-source.md) file-length rule. Persist-Error `context.pool.drop secret` increased a file already over 400.

Scan also printed [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `FILE 594->627`. Same rule then says this rule does not apply to tests. Not a hard hit.

Mechanical size on changed bindings is at most 12 lines (40-line cap). No added line over 100 characters. No `mutable`. Fail paths use Error, not exceptions. New publics are multi-word (`commitIfEditingForRun`, `mayLaunchAfterEditCommit`, `commandSubmitEffects`).

## Judgement (smells, not hard)

1. Possible Duplicated Code — [Commands.fs](src/Client/Commands.fs) `execRunOp` and [UpdateAmbleRun.fs](src/Client/UpdateAmbleRun.fs) `runAmbleOp` share this abort shape:

```
let committed, commitEffects, mayLaunch = commitIfEditingForRun model
if not mayLaunch then
    committed, commitEffects
else
    match committed.selectedNodes with
```

2. Possible Speculative Generality — [CommandRequest.fs](src/Shared/CommandRequest.fs) `commandSubmitEffects` re-gates `mayLaunch` and `Result` after `execRunOp` already returned when `not mayLaunch`, and the only Client call passes `Ok request`:

```
@ CommandRequest.commandSubmitEffects
    mayLaunch (Ok request)
```

Shared tests need the empty-list proof, so this is a call, not a standard miss.

Surgical Client/Server edits match the ticket. Plan notes name [52 — Run must not launch when edit commit fails](plan/core-creation/issues/52-run-abort-when-commit-fails.md). No EventId-serial issue. No retrofit of old tickets.

Standards: Needs changes (2 hard, 2 judgement)
