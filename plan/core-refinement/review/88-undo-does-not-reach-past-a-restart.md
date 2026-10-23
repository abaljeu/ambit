# 88 — Undo does not reach past a restart

Ticket: [[../issues/88-undo-does-not-reach-past-a-restart.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- 1. **Standards scan** — `python3 scripts/standards-scan.py --diff origin/staging` exited 0. `python` is absent on this machine, so the scan ran under `python3`. The printed sizes are `targetBeforeRestart` at 4 lines and `completeAction` at 33 lines. Each sits under the 40-line function limit in `.agents/rules/fsharp-source.md`.

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/agent-test.sh --full` wrote `/tmp/ambit-full-suite-20261023-035250.log` and printed `full suite finished`. Server.Tests passed 746, failed 0, skipped 0. Shared.Tests passed 1928, failed 0, skipped 1 (`WorkspaceLocalInventoryTests.listForPush without git still returns walk`).
- 2. **Refuse** — `Undo after restart refuses a target from before the restart` in `tests/Server.Tests/MailboxHistoryDurabilityTests.fs` runs the mixed hello on `hostDb`, disposes it, and starts a second `hostDb` on the same database. A name-only Undo of the hello Change returns `Error` containing `before the restart`. A Redo with ops naming that Change returns that `Error`. The test uses `hostDb` only. It passed inside the server run.
- 3. **Accept** — `Undo after restart accepts a target after the restart` in the same file posts a new Change on the second `hostDb`. A name-only Undo of that Change returns `Ok` with ops. It passed inside the server run.
- 4. **Name-only** — `name-only Undo and Redo store completed Events with submission ids` in `tests/Server.Tests/Issue41CoreMailboxTests.fs` is unchanged in the diff. It stores the completed Undo and Redo with their submission ids and ops. It passed inside the server run.

## 5. Miss

- No findings.

## 6. Found elsewhere

- No findings.
