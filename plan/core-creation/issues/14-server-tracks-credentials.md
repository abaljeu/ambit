# 14 — Server tracks credentials

**Status:** ready-for-agent
**Blocked by:** None — can start immediately.

## Context

The Server admits messages only from known senders. Core Changes already apply a Change from a test Actor ([[01-generalized-server-actor-produce-path.md]]). The Actor pool must use one credential set for the Server process.

## What to build

Core holds a set of credentials for the lifetime of the Server process. A credential stays in the set until Core removes it. Every message to Core must present one credential from that set. Auth refuse is one family: Adapter cookie fail and inactive sender are the same refuse. A TCP or Database system error is not that refuse. The Actor pool reuses this set.

- [ ] Core holds credentials for the Server process until Core removes a credential.
- [ ] A message that does not present a live credential is auth-refused and is not enqueued.
- [ ] Adapter cookie fail and inactive sender are the same refuse family.
- [ ] A TCP or Database failure is not that auth refuse.

## See also

[[10-define-actor-cancellation-and-output-admission.md]], [[plan/core-creation/reports/to-build-09-12-actor-pool.md]]
