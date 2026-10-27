# 139 — Plain Save prefers git push

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

In a workspace with both a GitHub mapping and a local mapping, a plain Save downloads to the local folder instead of pushing to GitHub.

## 4. Change

- When a node's workspace has a GitHub remote, a plain Save takes the Git path (git push), whether or not a local mapping exists.
- A plain Save takes the Desk path only when the workspace has no GitHub remote.
- An explicit Desk or Git pick is unchanged.

Source: `PathPick.fs`, and the client or server Save routing that leads to the download.

## 5. Tests

[x] A plain Save in a workspace with a GitHub remote and a local mapping takes the Git path and starts no download.
[x] A plain Save in a workspace with no GitHub remote takes the Desk path.
[x] An explicit Desk Save in a workspace with a GitHub remote takes the Desk path.

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
