# 15 — Keep the App outside Peer Actor hosting — independent standards recheck

Review range: `git diff 0a532cb1...HEAD` (HEAD `498c2b38`). Files in range: [AppGithubTransportBoundaryTests.fs](tests/Server.Tests/AppGithubTransportBoundaryTests.fs), [LoadSaveCommandTests.fs](tests/Server.Tests/LoadSaveCommandTests.fs), [LoadSaveCommandClientTests.fs](tests/Server.Tests/LoadSaveCommandClientTests.fs), and [15 — independent code review](plan/github-transport/reports/15-independent-code-review-2026-09-27.md). Ticket Status was not changed.

Repo-root `SMELLS.md` is absent. Standards used: [fsharp-source](.agents/rules/fsharp-source.md), [core-agent-behavior](.agents/rules/core-agent-behavior.md), [markdown-writing](.agents/rules/markdown-writing.md), [refer-by-name](.agents/rules/refer-by-name.md). Smells are only those that remain defensible without that baseline. Mechanical scan `python3 .agents/skills/code-review/scripts/standards-scan.py --diff 0a532cb1` produced no stdout (exit 0). An independent check of tabs, F# line length, and binding size on the same range also produced no over-limit output.

## 1. Hard violations

None.

## 2. Judgment calls

None.

## 3. Summary

Standards findings: 0 hard, 0 judgment. Worst issue: none.
