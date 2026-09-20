# Code review re-review — 52 Run abort when commit fails

Range: three-dot `origin/staging...HEAD` after fetch (merge-base `bd1e8059`, tip `90d0da2e`). Spec: [52 — Run must not launch when edit commit fails](plan/core-creation/issues/52-run-abort-when-commit-fails.md). Type bug-fixing. Status `coded` (unchanged). Prior review: [code-review-52-run-abort-when-commit-fails](code-review-52-run-abort-when-commit-fails.md) (Needs changes). Axis reports: [Standards](code-review-standards-52-rereview.md), [Spec](code-review-spec-52-rereview.md).

Verdict (axes separate; not a single winner): **Standards: Needs changes**. **Spec: Good**.

## Claim check (parent)

Scan `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` has no FILE hit on [UpdateHelpers.fs](src/Client/UpdateHelpers.fs) or [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs). Tip lengths: UpdateHelpers 447 (not in the three-dot name list); CoreMailboxBackend 421. [CommandRequest.fs](src/Shared/CommandRequest.fs) holds `commitIfEditingForRun` and `execRunOp`. [RunLaunch.fs](src/Client/RunLaunch.fs) holds `afterEditCommit`. [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) holds `dropAndReply`. [CommandRequestTests.fs](tests/Shared.Tests/CommandRequestTests.fs) `failed Editing SetText CAS does not SubmitCommand` calls Shared `execRunOp`. `commandSubmitEffects` is absent from product and test F#.

## Standards

Range: three-dot `origin/staging...HEAD` (tip `90d0da2e`). [52 — Run must not launch when edit commit fails](plan/core-creation/issues/52-run-abort-when-commit-fails.md).

### Mechanical scan (documented hits)

- [code-review-52-run-abort-when-commit-fails](code-review-52-run-abort-when-commit-fails.md):60 [refer-by-name](.agents/rules/refer-by-name.md) BARE_ID (`ticket 51`, `item 1`, `item 2`); :64 BARE_ID (`item 3`).
- [code-review-spec-52-run-abort-when-commit-fails](code-review-spec-52-run-abort-when-commit-fails.md):11 same `ticket 51` / `item 1` / `item 2`; :15 `item 3`.
- [CoreMailboxDoorTests](tests/Server.Tests/CoreMailboxDoorTests.fs) FILE 594→627: [fsharp-source](.agents/rules/fsharp-source.md) 800/400 split. Tests are exempt. Not a fail.
- measure-fs-size: listed bindings at most 25 lines (40-line cap). Pass. Surgical under-100-line preference is not a script fail ([core-agent-behavior](.agents/rules/core-agent-behavior.md)).

Earlier FILE hits [UpdateHelpers](src/Client/UpdateHelpers.fs) 447→460 and [CoreMailboxBackend](src/Server/Core/CoreMailboxBackend.fs) 423→425 are absent. Tip: UpdateHelpers 447 (no net change vs staging). CoreMailboxBackend 421 (down from 423). `commandSubmitEffects` is gone.

Those BARE_ID lines are write-once reports. [no-retrofit](.agents/rules/no-retrofit.md) keeps them. They are not a product edit.

### Additional hard (documented)

1. Bare issue number — [52 — Run must not launch when edit commit fails](plan/core-creation/issues/52-run-abort-when-commit-fails.md) Non-goals: `Stream / Focus≠Command encode (51 done)` names [51 — Browser Run Focus vs Command](plan/core-creation/issues/51-browser-run-focus-vs-command.md) by number only. [refer-by-name](.agents/rules/refer-by-name.md) requires the name with the number.

Product F#: no added `mutable`; fail paths use Error; added lines ≤100 characters; new publics are multi-word (`commitIfEditingForRun`, `mayLaunchAfterEditCommit`, `execRunOp`, `afterEditCommit`, `dropAndReply`). [core-api](.agents/rules/core-api.md): Core pool still owns live drop. Stage stays `build`.

### Judgement (smells, not hard)

1. Mysterious Name and Duplicated Code — Shared [CommandRequest.execRunOp](src/Shared/CommandRequest.fs) (tests only; ActorStart only) vs Browser [Commands.execRunOp](src/Client/Commands.fs) (Amble plus ActorStart). Same SubmitCommand shape:

```
| Ok request ->
    committed, commitEffects @ [ SubmitCommand request ]
```

2. Middle Man — [CoreActorPool.dropAndReply](src/Server/Core/CoreActorPool.fs) only `pool.drop` then `reply.Reply(Error err)` so CoreMailboxBackend does not grow. [RunLaunch.commitIfEditingForRun](src/Client/RunLaunch.fs) is a one-line wrap of Shared `commitIfEditingForRun`.

3. Divergent Change — [CommandRequest](src/Shared/CommandRequest.fs) now runs VM Editing commit and SubmitCommand, not only ActorStart construction.

The abort `if not mayLaunch` shape now lives in [afterEditCommit](src/Client/RunLaunch.fs). Adjacent `dispatchPostGraphOnly` wrap was compacted to keep CoreMailboxBackend from growing.

Standards: Needs changes (1 hard, 3 judgement)

## Spec

Range: three-dot `origin/staging...HEAD` (merge-base `bd1e8059`, tip `90d0da2e`). Spec: [52 — Run must not launch when edit commit fails; no orphan live](plan/core-creation/issues/52-run-abort-when-commit-fails.md). Type bug-fixing. Status `coded` (unchanged). Prior axis: [code-review-spec-52-run-abort-when-commit-fails](code-review-spec-52-run-abort-when-commit-fails.md).

### (a) Missing or partial

None. Spec Client Run item 1: “If `commitIfEditing` was required (mode was Editing) and commit failed … **do not** `SubmitCommand` / Amble Run. Keep the commit error visible.” [RunLaunch.fs](src/Client/RunLaunch.fs) `afterEditCommit` uses Shared `commitIfEditingForRun`; a new commit Error sets `mayLaunch` false and returns that model. [Commands.fs](src/Client/Commands.fs) `execRunOp` and [UpdateAmbleRun.fs](src/Client/UpdateAmbleRun.fs) `runAmbleOp` both abort there.

Spec Client Run item 2: “Proof: Run while Editing with a SetText that fails CAS does not POST `/command`.” [CommandRequestTests.fs](tests/Shared.Tests/CommandRequestTests.fs) `failed Editing SetText CAS does not SubmitCommand` now calls [CommandRequest.fs](src/Shared/CommandRequest.fs) `execRunOp` with a SetText CAS-fail commit (same `withMoveError` path as `commitTextEdit`) and asserts no `SubmitCommand`. [App.fs](src/Client/App.fs) POSTs `/{file}/command` only for that Effect, so no `SubmitCommand` means that POST does not run. Browser Run is the private `execRunOp` that shares this gate through `afterEditCommit`; Shared `execRunOp` is the Run-after-commit the proof calls. `commandSubmitEffects` is gone.

Spec Server start item 1: “If `putLive` succeeds and durable `ActorStart` (or schedule) fails, **drop** the live row … Reply Error.” [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `dispatchStartActor` persist Error calls [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) `dropAndReply`. Item 2 prefer: still putLive, then durable ActorStart Event; drop on persist Error (existing mailbox order). Item 3: [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) `ActorStart persist Error leaves Focus out of liveFocusIds`.

### (b) Behaviour not asked for

None in product behaviour. Amble abort is spec Client Run item 1. Shared `actorStartEffects` is only the success arm of Shared `execRunOp`. Non-goals (CAS root cause, [51 — Browser Run Focus vs Command](plan/core-creation/issues/51-browser-run-focus-vs-command.md) encode, Undo) are not in the product diff.

### (c) Implemented but wrong

None. Failed Editing commit keeps the commit Error and emits no `SubmitCommand`. Forced ActorStart Event Error leaves Focus out of `liveFocusIds`.

### Counts

(a) 0 / (b) 0 / (c) 0.

Spec: Good (0 missing, 0 creep, 0 wrong)

## Summary

Standards 4 findings (1 hard refer-by-name, 3 judgement); worst: ticket Non-goals names [51 — Browser Run Focus vs Command](plan/core-creation/issues/51-browser-run-focus-vs-command.md) as `51 done`. Spec 0 findings; no worst issue.
