# Standards rereview — graph childMap follow-up

Range `c76009ee...origin/cursor/graph-childmap-ea38` (`06b8e2fb`). Mechanical scan: none.

## 1 — mergeReadResult exceeds 40 lines (hard)

[`mergeReadResult`](src/Shared/documents/DocumentFormat.fs) spans lines 125–185 (61 lines). [fsharp-source.md](.agents/rules/fsharp-source.md) requires 40 lines or less per function. This follow-up grew the binding from 58 lines to 61 when it wrapped `graphWithRead` and assigned `readResult.childMap`.

The prior 100-character findings do not remain: the [`loadPackages`](src/Server/Api.fs) signature and the [`SerializationTests.fs`](tests/Shared.Tests/SerializationTests.fs) JSON fixtures are under 100 characters. No smell findings.
