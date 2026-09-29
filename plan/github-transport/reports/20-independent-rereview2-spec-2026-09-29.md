# Spec — Ticket 20 — State axes on special nodes

Sources: [20 — State axes on special nodes](../issues/20-state-axes-on-special-nodes.md) live-write note, and [Here→There — step 1 locked](../here-to-there.md) §4 item 5 Persist done (“Mark that node Persisted only”).

## Checked

1. Pass. `persistGraphChange` and `persistGraphOps` stamp Persisted through `persistedContentIds` (path-move ids plus `writeDocumentsSoft` success ids). On `persistGraphChange`, `enumerateDocumentRoots` is the mtime list. Persisted stays limited to `persistedContentIds`. `stampNode` writes Persisted only when that id is in the set and `Node.carriesStateAxes` is true.
2. Pass. `persistGraphChange leaves an untouched sibling root Unpersisted` keeps file B and the workspace on their prior Unpersisted axis. `persistGraphChange leaves a failed write Unpersisted when the old file remains` keeps Unpersisted while the old file stays on disk. `persistGraphOps soft-fails illicit write and returns could-not-save message` keeps Unpersisted and emits no `SetPersistState` for that file.
3. Pass. Live write success is `successful persistGraphOps marks the written special Persisted`. `PersistStamp.opsBetween` emits `SetPersistState`, and [FileAgent](src/Server/Core/FileAgent.fs) `preparePostChange` plus [DbAgent](src/Server/Core/DbAgent.fs) `preparePostChange` append those ops on the event source. Live write failure stays Unpersisted. Disk parse ends Parsed and Persisted in `disk parse leaves the file Parsed and Persisted` (`SetDocumentState` Current, then `DocumentParseOps.finishDiskParse`). A Directory File (exact `.amb` name) stays off both axes in `directory file amb node is excluded from state axes`.
4. Fail. [20 — independent code review](20-independent-code-review-2026-09-29.md) cites item 3 Discovery, item 4 git pull finish, item 8 Client Load on Directory, and item 9 Client Load on File by number and name. [20 — Spec agent](20-spec-agent-2026-09-29.md) does that on the finding lines. The same report’s path inventory still says “§4.8–9” with no item name.

## Findings

1. Prior Ticket 20 — State axes on special nodes reports must cite the checklist items by number and name. [20 — Spec agent](20-spec-agent-2026-09-29.md) line 105 cites item 8 Client Load on Directory and item 9 Client Load on File as “§4.8–9” only. Line 27 cites item 1 Writer target as “§4.1” only.

Product behavior in checks 1–3 matches the live-write note and §4 item 5 Persist done. This diff does not start a Parse actor, a Persist stack, a git Load retarget, or DocumentState retirement.
