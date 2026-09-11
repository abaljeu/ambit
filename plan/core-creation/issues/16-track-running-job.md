# 16 — Track running job

**Status:** blocked
**Blocked by:** [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]]
**Actual:** 45m

## Context

This issue records historical query-by-public-number delivery. The claim that there is no terminal result is superseded by [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Public Actor identity remains durable through ActorStarted and ActorFinished even after the live registry entry is removed.

## What to build

The existing live-registry query may identify the Actor while it runs. Durable lifecycle state is an Event projection, not a retained task object: ActorStarted exposes public identity, and exactly one ActorFinished records Succeeded, safe Failed, Cancelled, or Interrupted. Universal Core responses carry `{ nodes; events; latestId }`.

- [x] Query by the public number identifies the Actor while it is registered.
- [x] Focus NodeId, not public identity, is the cancel argument.
- [ ] ActorStarted and ActorFinished remain the durable lifecycle result after live-registry query ends.

## See also

[[plan/core-creation/issues/09-define-core-command-launch-contract.md]], [[plan/core-creation/issues/11-define-actor-finish-and-failure-behavior.md]]

## Comments

- 2026-09-06 — Implementation started on `dev`.
- 2026-09-06 — Delivered Core pool query by public number. Lookup returns the retained launch identity (name, Revision, span) while the Actor is registered. It does not return a job result, job Error, or send credential. An unknown number fails. HTTP Command query stays later. The number lasts until delete-actor ([[18-finish-and-drop.md]]). See [[plan/core-creation/reports/implement-issue-16.md]].
- 2026-09-11 — [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] keeps live public-identity query and adds durable terminal lifecycle Events. Its replacement contract directs the live-query part of the provider-neutral rebuild.
- 2026-09-11 — Reopened as `blocked` by [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]] because durable lifecycle projection is not delivered.

## Time

- 2026-09-06 45m — query-by-number on the Core Actor pool (from chat)
