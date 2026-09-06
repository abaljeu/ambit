# 15 — Launch an Actor and hold the span

**Status:** done
**Blocked by:** [[14-server-tracks-credentials.md|14 Server tracks credentials]]
**Actual:** 1h30m

## Context

A person starts long-running work on a span of Nodes. Core must start the Actor off the apply mailbox, give the caller a public number, and mark the live Nodes in that span.

## What to build

Launch takes a registered name, a Revision, a parent NodeId, and a non-empty span. Core extracts that subgraph and starts the Actor. The caller receives a public number that Core never reuses. The send credential is only in the Actor and is added to the set from [[14-server-tracks-credentials.md]]. Launch that shares any NodeId with a live span is refused. Core writes lock-present on the live Nodes in the span. SQL create, update, and select omit the lock field. History never carries lock.

- [x] Launch with registered name, Revision, parent NodeId, and a non-empty span starts an Actor and returns a never-reused public number.
- [x] The Command caller does not receive the send credential; the Actor does, and that credential is in the Core set.
- [x] Launch that shares any NodeId with a live span is refused.
- [x] Live Nodes in the span show lock-present; SQL and History do not carry lock.

## See also

[[09-define-core-command-launch-contract.md]], [[plan/core-creation/reports/to-build-09-12-actor-pool.md]], [[plan/core-creation/reports/commit-14-implement-15.md]]

## Comments

- 2026-09-06 — Implementation started on `dev`.
- 2026-09-06 — Delivered Core Actor pool launch, span extract, never-reused public number, send credential in the Actor and the Core set, overlap refuse, and lock-present overlay on live Nodes. Node JSON, History Changes, and projection SQL rows omit lock. HTTP Command launch, query, cancel, and Browser lock UI stay 16–22. See [[plan/core-creation/reports/commit-14-implement-15.md]].

## Time

- 2026-09-06 1h30m — launch pool, span extract, and lock-present overlay (from chat)
