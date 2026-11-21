pass

Check for [102 — Place Parse changes as Order.Change](../issues/102-place-parse-changes-as-order-change.md).

- `scripts/agent-test.sh` with no arguments ran `Issue41CoreMailboxTests`: 10 passed, 0 failed, 0 skipped.
- `scripts/agent-test.sh --filter` on `tests/Server.Tests` for `a graphOnly post stores the Change without a document file write`, `A graph-only change pushes nothing`, and `postGraphOnly updates Graph and EventLog without file write`: 3 passed, 0 failed, 0 skipped.
- Server.Tests full suite: 729 passed, 0 failed, 1 skipped.
- Shared.Tests full suite: 1913 passed, 0 failed, 3 skipped.
