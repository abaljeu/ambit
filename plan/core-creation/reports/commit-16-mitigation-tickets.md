# Commit 16 and mitigation tickets

Date: 2026-09-06

## Issue 16/20 commit

- Hash: `9435f0841c1c7c70b0dce34ba8a7699ba28de69b`
- Message: Query a registered Actor by launch public number without a job result; Browser POST presents the session cookie.
- Script: [[scripts/commit.sh]] with an explicit file list. One commit: [[../issues/16-track-running-job.md|16 (Track running job)]], [[../issues/20-client-presents-credential.md|20 (Client presents credential)]], their tests and sources, [[implement-issue-16.md]], [[implement-issue-20.md]], [[core-api-boundary-review.md]], and [[../project.md]] report index.

[[../issues/16-track-running-job.md]] and [[../issues/20-client-presents-credential.md]] are Status `done`. Project Stage stays `active`. [[plan/index.md]] was not regenerated.

## Mitigation tickets

Filed from [[core-api-boundary-review.md]] (verdict **partial**). No code mitigations. Did not start [[../issues/21-client-shows-lock-present.md|21 (Client shows lock-present)]]. Did not swallow [[../issues/07-define-core-files-contract.md|07]] or [[../issues/08-define-core-query-contract.md|08]].

- [[../issues/23-close-core-object-seam.md|23 (Close Core object seam)]] — corrected code: production posts present `Credential`; callers hold a Core object and use typed Core API functions.
- [[../issues/24-clarify-core-increment-boundary.md|24 (Clarify Core increment boundary)]] — clarifying instruction: Core vs Adapter vs Client; no lock UI this increment; typed Core calls, not primitives. Canonical text in [[../project.md]]; optional scoped rule pointers only.

[[../issues/17-cancel-a-job.md|17 (Cancel a job)]] is now Blocked by 23 (sender-at-Post).

## Left unstaged

- [[.agents/skills/wayfinder/SKILL.md]]
- [[plan/index.md]] (skills-cleanup Stage row)
- [[plan/skills-cleanup/project.md]] and [[plan/skills-cleanup/reports/]]
