# 14 — Server tracks credentials

**Status:** done
**Blocked by:** None — can start immediately.
**Actual:** 50m

## Context

The Server admits messages only from known senders. Core Changes already apply a Change from a test Actor ([[01-generalized-server-actor-produce-path.md]]). The Actor pool must use one credential set for the Server process.

## What to build

Core holds a set of credentials for the lifetime of the Server process. A credential stays in the set until Core removes it. Every message to Core must present one credential from that set. Auth refuse is one family: Adapter cookie fail and inactive sender are the same refuse. A TCP or Database system error is not that refuse. The Actor pool reuses this set.

- [x] Core holds credentials for the Server process until Core removes a credential.
- [x] A message that does not present a live credential is auth-refused and is not enqueued.
- [x] Adapter cookie fail and inactive sender are the same refuse family.
- [x] A TCP or Database failure is not that auth refuse.

## See also

[[10-define-actor-cancellation-and-output-admission.md]], [[plan/core-creation/reports/to-build-09-12-actor-pool.md]], [[plan/core-creation/reports/implement-issue-14-credentials.md]]

## Comments

- 2026-09-06 — Implementation started on `dev`.
- 2026-09-06 — Delivered Core credential set, `CoreAuth.post` admission, and Adapter mapping of Unauthorized to HTTP 401. Browser HTTP still uses the Adapter cookie then `CoreChanges.postChange`. Issue 20 presents a Browser credential. See [[plan/core-creation/reports/implement-issue-14-credentials.md]].

## Time

- 2026-09-06 50m — Core credential set and one auth-refuse family (from chat)
