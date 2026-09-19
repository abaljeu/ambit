# Code review — 21 Client shows live Actor

Range: `2f34407ebc14a3377d6fb7d7f555d062b26aaa6b...HEAD`. Spec: [21 — Client shows live Actor](../issues/21-client-shows-lock-present.md).

## Standards

Verbatim from [Standards axis](code-review-standards-21-live-actor-chrome.md):

1. Hard: already-over-400 [RowView.fs](../../../src/Client/RowView.fs), [Update.fs](../../../src/Client/Update.fs), [SyncLogic.fs](../../../src/Shared/SyncLogic.fs) grew. Tests exempt.
2. Hard: `applyRowPatches` / `buildRowElement` were already over 40 lines.
3. Hard: `BootGraphApplied` grew a tuple. Smell: Parameter Explosion.
4. Judgement: duplicated `actor-live` class/aria; Shotgun Surgery of the live set; `aria-busy` speculative.

Follow-up after that review: renamed the Client field to `actorLiveFocusIds` so it does not collide with Server `CoreActorPool.liveFocusIds`; removed `aria-busy`.

## Spec

Verbatim from [Spec axis](code-review-spec-21-live-actor-chrome.md):

1. Missing: none on the Poll / Load apply path.
2. Scope creep: `aria-busy` (removed after review).
3. Wrong: none on Poll / Load. Command POST still logs only; Poll carries Actor Events.

## Summary

Standards: 3 hard, 3 judgements; worst was growing already-over-400 files. Spec: 0 missing, 1 creep (aria, now removed), 0 wrong.
