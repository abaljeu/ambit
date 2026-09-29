# Independent re-review — Ticket 20 — State axes on special nodes

Range `origin/staging...3fe7d9f01cb6edff0602856236dd057ea36dc508`. Base `88436e6a3fc99baa594b4fc1edeb571723e83a9e`. Commits `bb40fac9` and `3fe7d9f0`. Spec: [Ticket 20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md).

**Verdict:** Needs work

This report is not approval. [Ticket 20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md) stays `coded`.

Axis notes: [Standards agent](20-rereview-standards-agent-2026-09-29.md) and [Spec agent](20-rereview-spec-agent-2026-09-29.md).

## Standards

### Refer by name

[.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) requires a number and a name for every list item. These added report lines name [Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4 items by number only:

- [20 — independent code review](20-independent-code-review-2026-09-29.md) line 31: “§4 item 3 wants the Directory Node Unparsed.”
- Same file line 32: “§4 item 3 describes that Unparsed write.”
- Same file line 33: “§4 item 8 and item 9 say mark the Directory Node or File Node Unparsed.”
- [20 — Spec agent](20-spec-agent-2026-09-29.md) line 79: “No separate axis path (spec §4 item 4).”

Other scan hits on those files put the item title on the same line. Those lines obey the rule.

Added F# bindings stay at or under the limits in [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) (40 lines per function, 100 characters per line, 800 lines per file).

### Judgement call

Possible Mysterious Name in [DocumentParseOps.fs](src/Shared/dotnet/DocumentParseOps.fs). `marksOwningSpecialUnpersisted` returns a bool and does not mark a node.

```
    let private marksOwningSpecialUnpersisted =
        function
        | Op.SetText _
        | Op.SetClasses _
        | Op.SetName _
        | Op.Replace _ -> true
        | _ -> false
```

## Spec

### Directed checks

1. **Successful live write.** A successful `writeDocumentCore` reaches [stampWrittenNode](src/Server/DocumentPersistPath.fs). That function sets Persisted on a content special. [PersistStamp.opsBetween](src/Shared/History.fs) emits `SetPersistState` for the event source. The server test `successful persistGraphOps marks the written special Persisted` covers this path.
2. **Failed live write.** Partial. See the finding below.
3. **Disk parse ends Parsed and Persisted.** [finishDiskParse](src/Shared/dotnet/DocumentParseOps.fs) appends `SetPersistState` to Persisted when the content node is Unpersisted or the parse edits it. The test `disk parse leaves the file Parsed and Persisted` ends Parsed and Persisted.
4. **Directory File.** Exact `.amb` name is excluded by [carriesStateAxes](src/Shared/Model.fs). Graph set of either axis returns an error. A graph edit of that node leaves it Persisted and marks the owning Directory Unpersisted. The test `directory file amb node is excluded from state axes` covers this.

### (c) Implementation looks wrong

**A failed write of a moved content node can still mark that node Persisted.** Spec: “A successful artifact write marks that content node Persisted in the event source. A failed write leaves Unpersisted.”

[persistGraphOps](src/Server/DocumentPersistChange.fs) writes the affected roots with `writeDocumentsSoft`. A failed `writeDocumentCore` stays out of that stamp map. The same function then calls [stampExistingDocuments](src/Server/DocumentPersistPath.fs) on the path-move ids. [stampWrittenNode](src/Server/DocumentPersistPath.fs) sets Persisted for every id whose file still exists. A moved node whose content write failed still has a file on disk, so this path marks it Persisted. [PersistStamp.opsBetween](src/Shared/History.fs) can then emit `SetPersistState` into the event source.

[persistGraphChange](src/Server/DocumentPersistChange.fs) uses the same stamp on every existing document root. [DbAgent.writeLiveSnapshot](src/Server/Core/DbAgent.fs) takes that graph. A failed write of a file that still exists is marked Persisted there too.

A failed write that is not a path-move id stays Unpersisted. The test `persistGraphOps soft-fails illicit write and returns could-not-save message` covers that narrower case.

## Summary

Standards: 4 hard violations, 1 judgement call. Worst on this axis: prior reports on this branch name Here→There list items by number only.

Spec: 1 finding. Worst on this axis: a failed write of a moved content node can still enter the event source as Persisted.
