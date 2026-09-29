# Standards — Ticket 20 — State axes on special nodes

Hard violations of [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) “40 lines or less per function”. The 800-line file exemption for tests does not apply to function size. The mechanical scanner does not measure `tests/`.

1. New test `successful persistGraphOps marks the written special Persisted` in [DocumentOpPersistenceTests.fs](tests/Server.Tests/DocumentOpPersistenceTests.fs) is 42 lines (132–173).
2. New test `graph edit marks only the nearest owning special Unpersisted` in [SpecialNodeStateAxesTests.fs](tests/Shared.Tests/SpecialNodeStateAxesTests.fs) is 42 lines (115–156).
3. Existing test `persistGraphOps soft-fails illicit write and returns could-not-save message` in [DocumentOpPersistenceTests.fs](tests/Server.Tests/DocumentOpPersistenceTests.fs) grew from 37 lines to 46 lines (80–125).
4. Existing test `planParseFile after Insert Ref reaches Current` in [ImportDocumentTests.fs](tests/Shared.Tests/ImportDocumentTests.fs) was already 45 lines and grew to 54 lines (1208–1261).

Scan BARE_ID lines in [20-independent-code-review-2026-09-29.md](20-independent-code-review-2026-09-29.md) and [20-spec-agent-2026-09-29.md](20-spec-agent-2026-09-29.md) already give the item number and the item name on the same line. Those lines are not findings.
