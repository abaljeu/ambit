# 21 — Client shows lock-present

**Status:** ready-for-agent
**Blocked by:** [[15-launch-actor-and-hold-span.md|15 Launch an Actor and hold the span]], [[16-track-running-job.md|16 Track running job]], [[20-client-presents-credential.md|20 Client presents credential]]

## Context

A person needs to see that Nodes in a live span are locked. Lock-present is on the live Node. It is not a job flag and it is not History.

## What to build

The Browser shows lock-present through state, Fetch, or Query.

- [ ] After launch, the Browser shows lock-present on the live Nodes in the span through state, Fetch, or Query.
- [ ] History does not show lock.
- [ ] Lock-present is on the live Node, not a job flag on the public number.

## See also

[[11-define-actor-finish-and-failure-behavior.md]], [[12-define-actor-pool-shutdown-behavior.md]]
