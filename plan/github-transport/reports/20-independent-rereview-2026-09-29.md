# 20 — State axes on special nodes — independent re-review

Review of [20 — State axes on special nodes](https://github.com/abaljeu/ambit/pull/177). Range `origin/staging...HEAD`. Tip `4486e474` matches `origin/cursor/state-axes-special-nodes-6aa0` and the pull-request head. Merge-base with `origin/staging` is `88436e6a` (`origin/staging`). The branch is four commits ahead. [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md) stays `coded`.

Sources: [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md) and [Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4 item 5 Persist done. Axis notes: [20 — Standards re-review](20-independent-rereview-standards-2026-09-29.md), [20 — Spec re-review](20-independent-rereview-spec-2026-09-29.md).

**Needs work.**

## Standards

Hard violations of [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md): 40 lines or less per function. The sentence that exempts tests is the 800-line file rule. Function length stays 40 lines. The mechanical scan skips function discovery under `tests/`. Counts run from the `let` to the next binding at the same indent. `origin/staging` sizes are the prior lengths.

1. New test `successful persistGraphOps marks the written special Persisted` in [DocumentOpPersistenceTests.fs](tests/Server.Tests/DocumentOpPersistenceTests.fs) is 42 lines (132–173).
2. New test `graph edit marks only the nearest owning special Unpersisted` in [SpecialNodeStateAxesTests.fs](tests/Shared.Tests/SpecialNodeStateAxesTests.fs) is 42 lines (115–156).
3. Existing test `persistGraphOps soft-fails illicit write and returns could-not-save message` in [DocumentOpPersistenceTests.fs](tests/Server.Tests/DocumentOpPersistenceTests.fs) grew from 37 lines to 46 lines (80–125).
4. Existing test `planParseFile after Insert Ref reaches Current` in [ImportDocumentTests.fs](tests/Shared.Tests/ImportDocumentTests.fs) grew from 45 lines to 54 lines (1208–1261).

Scan BARE_ID lines in the earlier Ticket 20 — State axes on special nodes reports already give the item number and the item name on the same line. Those lines are not findings. No smell is worth a finding.

## Spec

No findings.

Checked against [Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4 item 5 Persist done (“Mark that node Persisted only”) and the live-write note on [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md):

1. Pass. `persistGraphChange` and `persistGraphOps` stamp Persisted through `persistedContentIds` (path-move ids plus `writeDocumentsSoft` success ids). On `persistGraphChange`, `enumerateDocumentRoots` is the mtime list. Persisted stays limited to `persistedContentIds`.
2. Pass. `persistGraphChange leaves an untouched sibling root Unpersisted` keeps file B and the workspace on their prior Unpersisted axis. `persistGraphChange leaves a failed write Unpersisted when the old file remains` keeps Unpersisted while the old file stays on disk. `persistGraphOps soft-fails illicit write and returns could-not-save message` keeps Unpersisted and emits no `SetPersistState` for that file.
3. Pass. Live write success is `successful persistGraphOps marks the written special Persisted` (`SetPersistState` on the event source). Disk parse is `disk parse leaves the file Parsed and Persisted`. A Directory File (exact `.amb` name) stays off both axes in `directory file amb node is excluded from state axes` (`Node.carriesStateAxes`).
4. Pass. The review reports cite item 3 Discovery, item 4 git pull finish, item 8 Client Load on Directory, and item 9 Client Load on File by number and name.

## Summary

Standards: 4 findings. Worst: `planParseFile after Insert Ref reaches Current` is 54 lines (limit 40). Spec: no findings.

Landing: **Needs work**.
