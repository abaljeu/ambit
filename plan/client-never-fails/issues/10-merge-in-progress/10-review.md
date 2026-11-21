# 10 — Merge in progress

Ticket: [[10-merge-in-progress-ticket.md]]
Spec: [[plan/client-never-fails/spec.md]]
Arch: [[plan/client-never-fails/arch.md]]

## SPEC DEFECTS

A spec defect means the code misses the story Merge in progress or the ticket One job.

- No spec defects.

The story and the One job hold in the diff:

1. **Own changes.** `finishReconcileAck` returns `AckReconcile.Applied` when `identityError` is none and `collectSuffixes` succeeds. ``retry ACK removes only the submitted prefix and resubmits the remainder`` is that reply while pending extends the submitted prefix.
2. **Different reply.** A reply whose body differs, while `pendingExtendsSubmitted` is true, returns `AckReconcile.Ignored`. ``different post reply while more changes are pending is Ignored`` expects that result.
3. **Post the rest.** `applyIgnoredSubmitAnswer` calls `postRemainingPending`. `retireSubmittedPrefix` emits `SubmitPendingBatch` for the pending suffix and sets Sync `Sending`.
4. **Wait for a match.** Sync stays `Sending`. A later reply that matches the posted remainder uses that same `Applied` arm. ``Ignored reply posts the rest of the pending changes`` expects the remainder batch, Sync `Sending`, an empty catch-up, and the local graph text.
5. **No reload.** That post-the-rest arm leaves `DataOutdated` unset.

## 1. Standards

1. **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` printed four bindings inside the 40-line limit: `acknowledged` 7, `postRemainingPending` 13, `pendingExtendsSubmitted` 4, `ignoreDifferentReply` 10. The scan printed no long line, no glossary phrase, and no refer-by-name hit.
2. **Two Ignored meanings.** **Judgement.** Overcomplication. `AckReconcile.Ignored` is the retired-id result and the different-reply result. `applyIgnoredSubmitAnswer` calls `pendingExtendsSubmitted` again to choose `postRemainingPending` or `SyncAnswer.applyChangeSuccess`. Source: [[plan/context/SMELLS.md]]. The hunk is `if SyncLogic.pendingExtendsSubmitted submitted model.syncInfo.pending then postRemainingPending submitted response model`.

## 2. Spec

1. **Extra** — confirmation errors other than a different body. `ignoreDifferentReply` returns `AckReconcile.Ignored` for every `identityError` and every `collectSuffixes` error when pending extends the submitted prefix. The story Merge in progress and the One job name a post reply with different changes. A missing confirmation and a reordered confirmation take that arm too. `applyIgnoredSubmitAnswer` then retires the submitted prefix and posts the suffix.

## 3. Arch

- No findings.

## 4. Tests

1. **Suite.** `bash scripts/check-test.sh 3554` printed `DONE` and `/tmp/ambit-full-suite-20261029-185835.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 729, skipped 1, failed 0. `tests/Shared.Tests` passed 1908, skipped 3, failed 0.
2. **Different reply.** ``different post reply while more changes are pending is Ignored`` expects `AckReconcile.Ignored`.
3. **Post the rest.** ``Ignored reply posts the rest of the pending changes`` expects pending `[ later ]`, Sync `Sending 1`, `catchUp` none, graph text `local`, and `SubmitPendingBatch` of `[ later ]`.

## 5. Miss

- No findings.

## 6. Found elsewhere

The Found elsewhere section takes the PR body's Found elsewhere list. That list is empty.

- No findings.
