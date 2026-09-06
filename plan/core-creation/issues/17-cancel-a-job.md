# 17 — Cancel a job

**Status:** ready-for-agent
**Blocked by:** [[14-server-tracks-credentials.md|14 Server tracks credentials]], [[15-launch-actor-and-hold-span.md|15 Launch an Actor and hold the span]], [[23-close-core-object-seam.md|23 Close Core object seam]]

## Context

A person stops further work on a span. Changes that already merged stay. Cancel is not Undo.

## What to build

Cancel takes a NodeId. Core finds the job by span membership. Core signals the Actor with a CancellationToken and removes the job credential from the set. Mailbox items that are already enqueued still apply.

- [ ] Cancel by NodeId finds the job by span membership.
- [ ] The Actor receives a CancellationToken and Core removes the job credential from the set.
- [ ] Mailbox items that are already enqueued still apply.
- [ ] Cancel does not Undo merged Changes.

## See also

[[10-define-actor-cancellation-and-output-admission.md]], [[02-core-actor-pool.md]], [[23-close-core-object-seam.md|23 (Close Core object seam)]]

## Comments

- 2026-09-06 — Blocked by [[23-close-core-object-seam.md|23 (Close Core object seam)]]. Cancel needs sender-at-Post; production Changes still post with no Credential.
