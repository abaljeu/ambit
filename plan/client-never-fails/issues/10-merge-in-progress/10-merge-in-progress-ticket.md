# 10 — Merge in progress
**Status**: coded
One job: The outliner with more changes pending sees a different post reply ignored, then the Browser posts the rest and waits for a reply that matches. The story Merge in progress in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: none

## Inputs

### Working

[x] `SyncLogic.reconcileAck` returns `AckReconcile` in `src/Shared/SyncLogic.fs`. `Ok "ignore"` becomes `AckReconcile.Ignored`.
[x] `Update.reconcileSubmit` calls `reconcileAck` in `src/Client/Update.fs`.
[x] `isConfirmationEcho` accepts a reply that matches the submitted changes.
[x] ``late duplicate response is ignored when identities are retired`` already expects `AckReconcile.Ignored` for a retired id.
[x] ``late duplicate response is ignored when identities are retired`` in [[tests/Shared.Tests/AckReconcileTests.fs]].
[x] ``Notice in the reply keeps the confirmation echo`` in `tests/Shared.Tests/AckReconcileTests.fs` covers a matching reply.
[x] The `Ignored` arm in `reconcileAck` is the result this ticket uses for a different reply while more changes are pending.
[x] [[src/Shared/SyncLogic.fs]] — `reconcileAck`.
[x] [[src/Client/Update.fs]] — after `Ignored`, post the rest and wait for a matching reply.

### Reference

[x] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: the one-second typing pause. That pause is 11 — Type while others change.

## Process

1. [x] If an input is missing or does not match this ticket, stop and name that input.
2. [x] Change `reconcileAck` so a reply that carries our own changes is accepted, and a reply with different changes while more changes are pending is `AckReconcile.Ignored`.
3. [x] Change `Update.reconcileSubmit` so after `Ignored` the Browser posts the rest and waits for a reply that matches.
4. [x] Add the two facts named in Outputs.

## Outputs

[x] `src/Shared/SyncLogic.fs`
[x] `src/Client/Update.fs`
[x] `tests/Shared.Tests/AckReconcileTests.fs` — add ``different post reply while more changes are pending is Ignored``
[x] `tests/Server.Tests/BrowserResidencyClientTests.fs` — add ``Ignored reply posts the rest of the pending changes``

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/10-merge-in-progress-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``different post reply while more changes are pending is Ignored``
  - `tests/Server.Tests`: ``Ignored reply posts the rest of the pending changes``
[x] All executed tests pass.
[x] Write the verdict to `plan/client-never-fails/reports/10-merge-in-progress-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[x] A reader who reads only the One job line can say what the ticket builds.
