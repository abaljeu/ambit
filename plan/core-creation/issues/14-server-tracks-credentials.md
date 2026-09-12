# 14 — Server tracks credentials

**Status:** blocked
**Blocked by:** [[plan/core-creation/issues/02-core-actor-pool.md]]
**Actual:** 1h

## Context

The Server admits messages only from known Authorities. Core Changes already apply a Change from a test Actor through [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]]. The Actor pool must validate live identity without persisting secrets.

## What to build

Implement Authority admission from [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] on the live Core mailbox. Validate the public-and-secret pair on each request. Persist only the readable Authority name. Keep authentication refusal distinct from TCP or Database system failure.

- [x] Core holds live credentials for the Server process until Core removes them.
- [x] A message whose public Authority and secret credential do not match is auth-refused and creates no Event.
- [x] Adapter cookie fail and inactive sender are the same refuse family.
- [x] A TCP or Database failure is not that auth refuse.
- [x] Accepted Events persist the readable Authority name but never the secret credential.

## See also

[[10-define-actor-cancellation-and-output-admission.md]], [[plan/core-creation/reports/to-build-09-12-actor-pool.md]], [[plan/core-creation/reports/implement-issue-14-credentials.md]]

## Comments

- 2026-09-06 — Implementation started on `dev`.
- 2026-09-06 — Delivered Core credential set, `CoreAuth.post` admission, and Adapter mapping of Unauthorized to HTTP 401. Browser HTTP still uses the Adapter cookie then `CoreChanges.postChange`. [[plan/core-creation/issues/20-client-presents-credential.md]] presents a Browser credential. See [[plan/core-creation/reports/implement-issue-14-credentials.md]].
- 2026-09-11 — Reconciled with the one-mailbox rebuild. Credential admission remains a valid Core responsibility and this historical delivery stays `done`. The separate credential mailbox implementation shape is superseded: the one Changes/apply mailbox owns credential state and admit-and-enqueue.
- 2026-09-11 — [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] added public Authority identity, durable readable Authority on Events, and the rule that secret credentials never persist. The provider-neutral rebuild is owned by [[plan/core-creation/issues/02-core-actor-pool.md]].
- 2026-09-11 — Reopened as `blocked`: the historical credential-set delivery remains recorded, but public/secret Authority validation and Event attribution require the rebuilt mailbox and Event model.
- 2026-09-11 — Marked locked admission and Event-attribution facts complete. Status stays `blocked`; the one-mailbox rebuild is not this mark.

## Time

- 2026-09-06 50m — Core credential set and one auth-refuse family (from chat)
- 2026-09-11 5m — drop restated identity and Event rules; keep unique admission gates (from chat)
- 2026-09-11 5m — mark locked admission and Event-attribution facts complete (from chat)
