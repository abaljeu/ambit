# 138 — Deterministic index.lock wait test

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

`GitRunTests.second git waits instead of failing on index.lock` (tests/Shared.Tests/GitRunTests.fs) shows that a second `GitRun.gitExec` waits while `index.lock` is held.

- A clean filter holds `index.lock` until the test releases it.
- The second add starts only while that lock exists.
- The assertion is that the second call blocks, then succeeds.

## 4. Change

- Hold `index.lock` until this test releases it.
- Start the second git only while that lock is held.
- Assert that the second git blocks and then succeeds, with no elapsed-time threshold.

Source: `GitRunTests.fs`.

## 5. Tests

[x] `second git waits instead of failing on index.lock` passes 20 consecutive runs under `--filter`.
[x] The full suite passes.

## 6. Done

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[x] All executed tests pass.
