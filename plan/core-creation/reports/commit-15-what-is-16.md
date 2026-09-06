# Commit 15 and what is 16

Date: 2026-09-06

## Issue 15 commit

- Hash: pending
- Message: Core launches a registered Actor on a span; the caller gets a never-reused public number, not the send credential, and live Nodes show lock-present without SQL or History carrying lock.
- Script: [[scripts/commit.sh]] with an explicit file list under `src/`, `tests/`, and [[plan/core-creation/]]. [[plan/skills-cleanup/]] and [[.agents/skills/wayfinder/SKILL.md]] were not staged.

[[../issues/15-launch-actor-and-hold-span.md]] is Status done. Project Stage stays `active`.

## Issue 16

[[../issues/16-track-running-job.md]] — **16 Track running job**.

Status: `ready-for-agent`. Blocked by [[../issues/15-launch-actor-and-hold-span.md|15 Launch an Actor and hold the span]], which this commit delivers. No other blocker is named. [[../project.md]] one-liner: query a registered job by public number.

The Command caller must find a running job by the public number from launch. There is no job result to fetch. Query by that number succeeds while the Actor is registered, does not return a job result or job Error, and uses the public number as the query key (not the cancel argument). The number lasts until delete-actor ([[../issues/18-finish-and-drop.md]]).

This issue is ready to build at Core: 15 is done, Status is `ready-for-agent`, and the working tree has no leftover 16 implementation (the pool has launch and lock overlay, not query-by-number). HTTP Command query and Browser UI stay later issues (17–22). Do not implement 16 in this commit.
