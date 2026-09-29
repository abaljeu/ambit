# Standards — Ticket 20 — State axes on special nodes

Range `origin/staging...HEAD`. Tip `3dd5b28e`. Count is from the `let` to the next same-indent binding. The 800-line file rule in [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) exempts tests. The 40-line function rule stays in force for tests. The mechanical scan skips `tests/`.

## Function length

The four tests from the prior re-review are at or under 40 lines.

1. `successful persistGraphOps marks the written special Persisted` in [DocumentOpPersistenceTests.fs](tests/Server.Tests/DocumentOpPersistenceTests.fs) is 31 lines (151–181).
2. `graph edit marks only the nearest owning special Unpersisted` in [SpecialNodeStateAxesTests.fs](tests/Shared.Tests/SpecialNodeStateAxesTests.fs) is 24 lines (139–162).
3. `persistGraphOps soft-fails illicit write and returns could-not-save message` in [DocumentOpPersistenceTests.fs](tests/Server.Tests/DocumentOpPersistenceTests.fs) is 26 lines (100–125).
4. `planParseFile after Insert Ref reaches Current` in [ImportDocumentTests.fs](tests/Shared.Tests/ImportDocumentTests.fs) is 25 lines (1236–1260).

Helpers from that shorten stay under 40 lines: `illicitSystemFile` (17), `assertWrittenFileStamp` (15), `unparsedFileAfterInsertRef` (24), `persistedOwnedFile` (20), `markPersisted` (2), `markDocumentCurrent` (2). The moved assertions match the prior bodies. No smell in that extraction is worth a finding. Other changed test bindings in the range are 40 lines or under. Source functions in the mechanical scan are 4 to 17 lines. No added line exceeds 100 characters.

## Findings

1. Hard violation of [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md). [20 — Spec agent](20-spec-agent-2026-09-29.md) still cites checklist items by number alone. Line 105 says “§4.1, §4.8–9” and omits item 1 Writer target, item 8 Client Load on Directory, and item 9 Client Load on File. Line 27 says “§4.1” and omits item 1 Writer target.

Scan lines that already place the item name on the same line stay clear. Those lines are [20 — independent code review](20-independent-code-review-2026-09-29.md) item 3 Discovery, item 4 git pull finish, item 8 Client Load on Directory, and item 9 Client Load on File, and the finding lines in [20 — Spec agent](20-spec-agent-2026-09-29.md) that quote those names beside the item number.
