# Commit 24 and 23

Date: 2026-09-06

## Commit

- Hash: pending `commit.sh`
- Message: Clarify Core vs Adapter vs Client for workers, and hold CoreRuntime as the Core object with Credential on every production post.
- Script: [[scripts/commit.sh]] with an explicit file list. One commit: the locked pair [[../issues/24-clarify-core-increment-boundary.md|24 (Clarify Core increment boundary)]] then [[../issues/23-close-core-object-seam.md|23 (Close Core object seam)]].

[[../issues/24-clarify-core-increment-boundary.md]] and [[../issues/23-close-core-object-seam.md]] are Status `done`. Project Stage stays `active`. [[plan/index.md]] was not staged: the diff is skills-cleanup summary and row order, not a Core creation Stage change.

## What landed

- 24: Agent instruction in [[../project.md]]; map Notes pointer; scoped [[.cursor/rules/core-api.mdc]] indexed from [[.cursor/rules/gambol.mdc]].
- 23: [[src/Server/Core/CoreRuntime.fs]] is the Core object. Production posts present a live Credential through `CoreAuth`. HTTP holds Core, not an unpacked runtime.
- Locked plan [[../mitigations.md]], [[plan-23-24-mitigations.md]], [[implement-24-then-23.md]], [[actor-core-and-mailbox-check.md]].
- Leftover ticket from the seam: [[../issues/25-bind-changes-at-core-seam.md|25 (Bind Changes at the Core seam)]] (Status `needs-triage`), from [[improve-codebase-architecture.md]].

Did not start [[../issues/17-cancel-a-job.md|17 (Cancel a job)]]. Did not start [[../issues/21-client-shows-lock-present.md|21 (Client shows lock-present)]].

## Left unstaged

- [[.agents/skills/implement/SKILL.md]]
- [[.agents/skills/wayfinder/SKILL.md]]
- [[plan/index.md]] (skills-cleanup Stage row)
- [[plan/skills-cleanup/project.md]] and [[plan/skills-cleanup/reports/]]
