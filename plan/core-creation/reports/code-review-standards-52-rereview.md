# Standards re-review — 52 Run abort when commit fails

Range: three-dot `origin/staging...HEAD` (tip `90d0da2e`). [52 — Run must not launch when edit commit fails](plan/core-creation/issues/52-run-abort-when-commit-fails.md).

## Mechanical scan (documented hits)

- [code-review-52-run-abort-when-commit-fails](code-review-52-run-abort-when-commit-fails.md):60 [refer-by-name](.agents/rules/refer-by-name.md) BARE_ID (`ticket 51`, `item 1`, `item 2`); :64 BARE_ID (`item 3`).
- [code-review-spec-52-run-abort-when-commit-fails](code-review-spec-52-run-abort-when-commit-fails.md):11 same `ticket 51` / `item 1` / `item 2`; :15 `item 3`.
- [CoreMailboxDoorTests](tests/Server.Tests/CoreMailboxDoorTests.fs) FILE 594→627: [fsharp-source](.agents/rules/fsharp-source.md) 800/400 split. Tests are exempt. Not a fail.
- measure-fs-size: listed bindings at most 25 lines (40-line cap). Pass. Surgical under-100-line preference is not a script fail ([core-agent-behavior](.agents/rules/core-agent-behavior.md)).

Earlier FILE hits [UpdateHelpers](src/Client/UpdateHelpers.fs) 447→460 and [CoreMailboxBackend](src/Server/Core/CoreMailboxBackend.fs) 423→425 are absent. Tip: UpdateHelpers 447 (no net change vs staging). CoreMailboxBackend 421 (down from 423). `commandSubmitEffects` is gone.

Those BARE_ID lines are write-once reports. [no-retrofit](.agents/rules/no-retrofit.md) keeps them. They are not a product edit.

## Additional hard (documented)

1. Bare issue number — [52 — Run must not launch when edit commit fails](plan/core-creation/issues/52-run-abort-when-commit-fails.md) Non-goals: `Stream / Focus≠Command encode (51 done)` names [51 — Browser Run Focus vs Command](plan/core-creation/issues/51-browser-run-focus-vs-command.md) by number only. [refer-by-name](.agents/rules/refer-by-name.md) requires the name with the number.

Product F#: no added `mutable`; fail paths use Error; added lines ≤100 characters; new publics are multi-word (`commitIfEditingForRun`, `mayLaunchAfterEditCommit`, `execRunOp`, `afterEditCommit`, `dropAndReply`). [core-api](.agents/rules/core-api.md): Core pool still owns live drop. Stage stays `build`.

## Judgement (smells, not hard)

1. Mysterious Name and Duplicated Code — Shared [CommandRequest.execRunOp](src/Shared/CommandRequest.fs) (tests only; ActorStart only) vs Browser [Commands.execRunOp](src/Client/Commands.fs) (Amble plus ActorStart). Same SubmitCommand shape:

```
| Ok request ->
    committed, commitEffects @ [ SubmitCommand request ]
```

2. Middle Man — [CoreActorPool.dropAndReply](src/Server/Core/CoreActorPool.fs) only `pool.drop` then `reply.Reply(Error err)` so CoreMailboxBackend does not grow. [RunLaunch.commitIfEditingForRun](src/Client/RunLaunch.fs) is a one-line wrap of Shared `commitIfEditingForRun`.

3. Divergent Change — [CommandRequest](src/Shared/CommandRequest.fs) now runs VM Editing commit and SubmitCommand, not only ActorStart construction.

The abort `if not mayLaunch` shape now lives in [afterEditCommit](src/Client/RunLaunch.fs). Adjacent `dispatchPostGraphOnly` wrap was compacted to keep CoreMailboxBackend from growing.

Standards: Needs changes (1 hard, 3 judgement)
