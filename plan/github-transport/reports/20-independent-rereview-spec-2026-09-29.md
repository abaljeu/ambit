# Spec — Ticket 20 — State axes on special nodes

## Checked

1. Pass. `persistGraphChange` and `persistGraphOps` pass `persistedContentIds` (path-move ids plus `writeDocumentsSoft` success ids) to `stampExistingDocuments`; `persistGraphChange leaves an untouched sibling root Unpersisted` keeps an `enumerateDocumentRoots` sibling Unpersisted.
2. Pass. That sibling test keeps file B and the workspace on their prior Unpersisted axis. `persistGraphChange leaves a failed write Unpersisted when the old file remains` keeps Unpersisted when the old file stays on disk.
3. Pass. Live write success is `successful persistGraphOps marks the written special Persisted` (`SetPersistState` on the event source via `PersistStamp.opsBetween`). Live write fail is the two failed-write tests. Disk parse is `disk parse leaves the file Parsed and Persisted` and `disk parse of a directory leaves that directory Parsed and Persisted`. Directory File exclusion is `directory file amb node is excluded from state axes` (`Node.carriesStateAxes`).
4. Pass. [20-spec-agent-2026-09-29.md](20-spec-agent-2026-09-29.md) names Discovery with item 3 and git pull finish with item 4, and quotes Client Load on Directory with item 8 and Client Load on File with item 9 on the same line; [20-independent-code-review-2026-09-29.md](20-independent-code-review-2026-09-29.md) cites item 3 Discovery, item 4 git pull finish, item 8 Client Load on Directory, and item 9 Client Load on File.

## Findings

No findings.
