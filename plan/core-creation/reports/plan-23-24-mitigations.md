# Plan 23 and 24 mitigations

Date: 2026-09-06

Locked refactor plan: [[../mitigations.md]]. No application source. No 21. Did not swallow 07 or 08. Did not start 17.

## Where

Standing plan: [[../mitigations.md]] (not reports-only). This file is the transactional pointer.

## Order

1. [[../issues/24-clarify-core-increment-boundary.md|24 (Clarify Core increment boundary)]] — instruction in [[../project.md]], one [[../map.md]] Notes sentence, optional scoped rule pointers. Then Status `done`.
2. [[../issues/23-close-core-object-seam.md|23 (Close Core object seam)]] — Core object at HTTP, `Credential` on every production post, typed admission Error. Blocked by 24 until 24 is done.
3. Then [[../issues/17-cancel-a-job.md|17 (Cancel a job)]].

24 first so the 23 worker has the Core vs Adapter vs Browser boundary before coding.

## Stage

[[../project.md]] Stage is `spec` (locked refactor plan). Return to `active` when 23 coding starts.

## Defaults (not blocking)

Process-lifetime Browser credential after cookie; `CoreAuth` wrap at production posts (agent mailboxes unchanged); Stage `spec` until 23 starts.
