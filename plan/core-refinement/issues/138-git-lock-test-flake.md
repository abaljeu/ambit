# 138 — Deterministic index.lock wait test

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

`GitRunTests.second git waits instead of failing on index.lock` (tests/Shared.Tests/GitRunTests.fs) sometimes fails under the full suite and passes on rerun. It depends on timing: a slow clean filter (`sleep 2`) holds `index.lock`, the test polls for the lock for about 3 s, and it then expects the second `git add` to take at least 500 ms.

## 4. Change

- Find the failing assertion and its cause under full-suite load.
- Make the test deterministic: the second git starts only while the lock is known to be held, and the assertions don't depend on scheduler timing.
- If the cause is in `GitRun` rather than the test, fix `GitRun`.

Source: `GitRunTests.fs`, and `GitRun.fs` if the cause is there.

## 5. Tests

[ ] `second git waits instead of failing on index.lock` passes 20 consecutive runs under `--filter`.
[ ] The full suite passes.

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
