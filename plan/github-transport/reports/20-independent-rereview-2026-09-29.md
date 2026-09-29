# Ticket 20 — State axes on special nodes — independent re-review

Review range: `origin/staging...3fe7d9f01cb6edff0602856236dd057ea36dc508`.

Commits: `bb40fac9` Add parse and persist axes on special nodes. `3fe7d9f0` Mark content specials Persisted after a successful artifact write.

Spec: [Ticket 20 — State axes on special nodes](../issues/20-state-axes-on-special-nodes.md). Axis-write names: [Here→There — step 1 locked](../here-to-there.md) §4. Vocabulary: [Here→There — step 1 locked](../here-to-there.md) §6.

[Ticket 20 — State axes on special nodes](../issues/20-state-axes-on-special-nodes.md) stays `coded`. This report is not approval.

**Verdict: Needs work.**

## Directed locks

1. **Live write.** `persistGraphOps` stamps a content special Persisted after `writeDocumentsSoft` records a successful write. `PersistStamp.opsBetween` emits `SetPersistState` on that stamp, and the event source applies it. A failed write with an empty path-move list leaves Unpersisted and emits no `SetPersistState` for that node. Tests: `successful persistGraphOps marks the written special Persisted` and `persistGraphOps soft-fails illicit write and returns could-not-save message` in [DocumentOpPersistenceTests.fs](../../../tests/Server.Tests/DocumentOpPersistenceTests.fs).
2. **Disk parse.** `DocumentParseOps.finishDiskParse` appends `SetPersistState` to Persisted when the content node is Unpersisted or the parse ops edit it. The node ends Parsed and Persisted. Tests: `disk parse leaves the file Parsed and Persisted` in [SpecialNodeStateAxesTests.fs](../../../tests/Shared.Tests/SpecialNodeStateAxesTests.fs). `disk parse of a directory leaves that directory Parsed and Persisted` in [LazyLoadReconciliationTests.fs](../../../tests/Shared.Tests/LazyLoadReconciliationTests.fs). The Workspace ancestor in that directory test stays Unpersisted.
3. **Directory File.** `Node.carriesStateAxes` is a Workspace, Directory, or File content node, and an exact `.amb` name is excluded. A graph edit uses `GraphQuery.enclosing` with that predicate, so the Directory File stays free of both axes and the owning Directory becomes Unpersisted. Test: `directory file amb node is excluded from state axes` in [SpecialNodeStateAxesTests.fs](../../../tests/Shared.Tests/SpecialNodeStateAxesTests.fs).

## Standards

Hard violations of [.agents/rules/refer-by-name.md](../../../.agents/rules/refer-by-name.md): a list item needs its number and its name. These sentences use the item number alone.

[20 — independent code review](20-independent-code-review-2026-09-29.md) §5:

1. **Discovery.** “§4 item 3 wants the Directory Node Unparsed.” The name is Discovery.
2. **Discovery.** “§4 item 3 describes that Unparsed write.” The name is Discovery.
3. **Client Load on Directory and Client Load on File.** “§4 item 8 and item 9 say mark the Directory Node or File Node Unparsed.” The names are Client Load on Directory and Client Load on File.

[20 — Spec agent](20-spec-agent-2026-09-29.md) path inventory:

1. **git pull finish.** “No separate axis path (spec §4 item 4).” The name is git pull finish.

The mechanical scan also printed `item N` lines whose same sentence quotes the item title. Those lines meet the rule. Every `measure-fs-size` line is under the 40-line function limit in [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md). `History.fs` stays under the 800-line file limit. No new ESO, CAS, or Peer wording appears in the product F#, the tests, or [Ticket 20 — State axes on special nodes](../issues/20-state-axes-on-special-nodes.md).

Smells: none to stand behind.

## Spec

1. **Shared mtime stamp marks Persisted on a node this call did not write.** `stampWrittenNode` in [DocumentPersistPath.fs](../../../src/Server/DocumentPersistPath.fs) sets Persisted for every content special that `stampNodes` stamps. `persistGraphChange` in [DocumentPersistChange.fs](../../../src/Server/DocumentPersistChange.fs) then calls `stampExistingDocuments` with `enumerateDocumentRoots`, so each document root whose file already exists becomes Persisted on the returned graph. That set includes a root this call did not write, and a root whose latest write failed while an older file remains. [DbAgent.fs](../../../src/Server/Core/DbAgent.fs) `writeLiveSnapshot` is that path. `handleSnapshotDone` stores the returned graph as `persistedGraph` when `graphEquals` holds, and `graphEquals` ignores `persistState`. Quote from [Ticket 20 — State axes on special nodes](../issues/20-state-axes-on-special-nodes.md): “Persist done — That node Persisted only.” Quote: “A failed write leaves Unpersisted.” `persistGraphOps` passes only path-move ids into `stampExistingDocuments`. A failed live write with no path move stays Unpersisted, and that part of the lock holds.

## Summary

Standards: 4 findings. Worst: bare item numbers in the two prior review reports.

Spec: 1 finding. Worst: `persistGraphChange` marks every existing on-disk document root Persisted, including a root this call did not write.
