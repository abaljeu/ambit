# 16 — Track running job

**Status:** ready-for-agent
**Blocked by:** [[15-launch-actor-and-hold-span.md|15 Launch an Actor and hold the span]]

## Context

The Command caller must find a running job by the public number from launch. There is no job result to fetch. The number lasts until delete-actor ([[18-finish-and-drop.md]]).

## What to build

Query by public number succeeds while the Actor is registered. Query does not return a job result. The public number is the query key for the registered Actor.

- [ ] Query by the public number identifies the Actor while it is registered.
- [ ] Query does not return a job result or job Error.
- [ ] The public number is the query key and is not the cancel argument.

## See also

[[09-define-core-command-launch-contract.md]], [[11-define-actor-finish-and-failure-behavior.md]]
