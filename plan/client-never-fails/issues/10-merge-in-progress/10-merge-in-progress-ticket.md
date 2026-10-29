# 10 — Merge in progress
**Status**: 
One job: The outliner with more changes pending sees a server success reply accepted and the rest of the pending changes posted, with no code change for a server reject. The story Merge in progress in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: none

## Inputs

### Working

[ ] The wire is synchronous. A post gets the reply to that same post, so a reply that carries different changes cannot come from a normal server success.
[ ] `SyncLogic.reconcileAck` in `src/Shared/SyncLogic.fs` accepts a reply that carries our own changes. That path already works.
[ ] `Update.reconcileSubmit` and `Update.applySubmitResponse` in `src/Client/Update.fs` handle the success reply, then post the rest of the pending changes.
[ ] ``Notice in the reply keeps the confirmation echo`` in [[tests/Shared.Tests/AckReconcileTests.fs]] covers a matching reply.

### Reference

[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md` and the architecture.
[ ] Closed: the server reject path. A reject keeps the existing reload message, and its code is not modified.
[ ] Closed: a network error. It is not a server reject and is not part of this ticket.
[ ] Closed: showing the error and undoing on the client after a reject. That needs its own ticket.
[ ] Closed: the one-second typing pause. That pause is 11 — Type while others change.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Confirm that a server success reply, while more changes are pending, is accepted and the rest of the pending changes post. Change code only if that success path fails.
3. [ ] Do not change how any error reply is handled.

## Outputs

[ ] None unless step 2 finds the success path fails.

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/10-merge-in-progress-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit would change how an error reply is handled.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/10-merge-in-progress-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
