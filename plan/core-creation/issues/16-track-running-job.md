# 16 — Track running job

**Status:** done
**Blocked by:** [[15-launch-actor-and-hold-span.md|15 Launch an Actor and hold the span]]
**Actual:** 45m

## Context

The Command caller must find a running job by the public number from launch. There is no job result to fetch. The number lasts until delete-actor ([[18-finish-and-drop.md]]).

## What to build

Query by public number succeeds while the Actor is registered. Query does not return a job result. The public number is the query key for the registered Actor.

- [x] Query by the public number identifies the Actor while it is registered.
- [x] Query does not return a job result or job Error.
- [x] The public number is the query key and is not the cancel argument.

## See also

[[09-define-core-command-launch-contract.md]], [[11-define-actor-finish-and-failure-behavior.md]]

## Comments

- 2026-09-06 — Implementation started on `dev`.
- 2026-09-06 — Delivered Core pool query by public number. Lookup returns the retained launch identity (name, Revision, span) while the Actor is registered. It does not return a job result, job Error, or send credential. An unknown number fails. HTTP Command query stays later. The number lasts until delete-actor ([[18-finish-and-drop.md]]). See [[plan/core-creation/reports/implement-issue-16.md]].

## Time

- 2026-09-06 45m — query-by-number on the Core Actor pool (from chat)
