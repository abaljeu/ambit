# Ticket 20 — State axes on special nodes — independent re-review 2

Review of [Ticket 20 — State axes on special nodes](https://github.com/abaljeu/ambit/pull/177). Range `origin/staging...HEAD`. Tip `3dd5b28e` matches `origin/cursor/state-axes-special-nodes-6aa0` and the pull-request head. Merge-base with `origin/staging` is `88436e6a`. The branch is five commits ahead. [20 — State axes on special nodes](../issues/20-state-axes-on-special-nodes.md) stays `coded`.

Prior re-review: [Ticket 20 — State axes on special nodes — independent re-review](https://github.com/abaljeu/ambit/pull/183). That pass said Spec was clean and Standards Needs work was four tests over 40 lines. This pass measures the shortened tests and re-checks the persist rules. It does not reuse that verdict.

Sources: [20 — State axes on special nodes](../issues/20-state-axes-on-special-nodes.md) and [Here→There — step 1 locked](../here-to-there.md) §4 item 5 Persist done. Axis notes: [20 — Standards re-review 2](20-independent-rereview2-standards-2026-09-29.md), [20 — Spec re-review 2](20-independent-rereview2-spec-2026-09-29.md).

**Needs work.**

## Standards

One hard violation of [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md). [20 — Spec agent](20-spec-agent-2026-09-29.md) line 105 cites “§4.1, §4.8–9” and omits item 1 Writer target, item 8 Client Load on Directory, and item 9 Client Load on File. Line 27 cites “§4.1” and omits item 1 Writer target.

The four previously long tests are now 31, 24, 26, and 25 lines. The 800-line file rule does not exempt function length. Helpers from the shorten stay under 40 lines. Assertions in those four tests match the prior bodies. No smell in that extraction is worth a finding. Mechanical-scan function lines are 4 to 17 lines. Scan BARE_ID lines that already carry the item name on the same line stay clear.

## Spec

One finding. It is the same citation miss. [20 — Spec agent](20-spec-agent-2026-09-29.md) line 105 cites item 8 Client Load on Directory and item 9 Client Load on File by number only. The finding lines in that report, and [20 — independent code review](20-independent-code-review-2026-09-29.md), already cite item 3 Discovery, item 4 git pull finish, item 8 Client Load on Directory, and item 9 Client Load on File by number and name.

Checked against [Here→There — step 1 locked](../here-to-there.md) §4 item 5 Persist done and the live-write note on [20 — State axes on special nodes](../issues/20-state-axes-on-special-nodes.md):

1. Pass. `persistGraphChange` and `persistGraphOps` stamp Persisted through `persistedContentIds` (path-move ids plus `writeDocumentsSoft` success ids). `enumerateDocumentRoots` supplies mtime roots. It does not stamp every on-disk root Persisted.
2. Pass. An untouched sibling root keeps its prior persist axis. A failed write whose older file remains stays Unpersisted.
3. Pass. Live write success marks Persisted on the event source (`SetPersistState` via `PersistStamp.opsBetween`). Live write failure stays Unpersisted. Disk parse ends Parsed and Persisted. A Directory File (exact `.amb` name) stays off both axes.
4. Fail on the path-inventory line named above. The primary citations of item 3 Discovery, item 4 git pull finish, item 8 Client Load on Directory, and item 9 Client Load on File already include the name.

## Summary

Standards: 1 finding. Worst: [20 — Spec agent](20-spec-agent-2026-09-29.md) line 105 cites item 8 Client Load on Directory and item 9 Client Load on File by number only. Spec: 1 finding. Worst: that same citation. Product persist behavior matches §4 item 5 Persist done and the live-write note.

Landing: **Needs work**.
