# 20 — State axes on special nodes — independent code review

Review range: `origin/staging...bb40fac9` (one commit). Sources: [20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md), [Here→There — step 1 locked](plan/github-transport/here-to-there.md) §3–4, [19 — Parsed/Unparsed and Persisted/Unpersisted](plan/github-transport/issues/19-file-newer-graph-newer.md). Axis reports: [20 — Standards agent](20-standards-agent-2026-09-29.md), [20 — Spec agent](20-spec-agent-2026-09-29.md).

[20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md) stays `coded`.

## 1. Landing

**Needs work.** One Must-fix. One Should-fix.

The five in-scope pieces hold: markers, DocumentState dual-write, NewSpecialNode Unparsed and Persisted, nearest-special Unpersisted on a graph edit, and SQL `document_state` reload. The existing server artifact write leaves that special Unpersisted after the file is written.

## 2. Must-fix

1. **The existing server artifact write leaves the owning special Unpersisted.** [Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4 item 5: “Persist done — Mark that node Persisted only.” A graph edit sets Unpersisted in `GraphMutate.markOwningSpecialUnpersisted`. Live-save then runs `DocumentPersistChange.persistGraphOps` from [FileAgent](src/Server/Core/FileAgent.fs) `syncPersistChange` and from [DbAgent](src/Server/Core/DbAgent.fs) `persistLiveChange`. That path writes the artifact through `DocumentPersistWrite.writeDocumentsSoft` and stamps `updateTime` with `SetUpdateTime`. `SetUpdateTime` leaves Parsed|Unparsed and Persisted|Unpersisted as they were. After a successful file write the special is still Unpersisted. This expand step does not stand up the Persist stack. The write that already runs must set Persisted on that node only.

## 3. Should-fix

1. **A disk-to-Graph parse leaves the special Unpersisted.** [Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4 item 2 marks a graph edit Unpersisted only. §4 item 6: “Directory Parse done — Mark that Directory Node Parsed only.” `GraphMutate.setText`, `setClasses`, `setName`, and `replace` always call `markOwningSpecialUnpersisted`. Parse applies those ops and then `SetDocumentState` Current. The special ends Parsed and Unpersisted. The same shape is in `ImportDocument.planParseFile`, `DocumentPersistWrite.planParseFile`, `Api.postParseFile`, `GithubTransportActor.parseFocusFile` on the Server git Actor, `LazyLoadReconciliationApply.parseDirInfoIfPresent`, and `ImportText`. A disk-to-Graph update should leave Persisted. The parse-done write is Parsed only.

## 4. In-scope paths that hold

1. **Markers.** `ParseState` is Parsed|Unparsed. `PersistState` is Persisted|Unpersisted. Both fields sit on `Node` for a Workspace Node, a Directory Node, and a File Node. `NodeKind.artifact` is those three kinds. Workspaces and a Normal Node are rejected by `setParseState` and `setPersistState`.
2. **DocumentState dual-write.** `Node.withDocumentState` writes DocumentState and `ParseState.ofDocumentState` together. `GraphMutate.setDocumentState` is the apply path for `Op.SetDocumentState`. Current maps to Parsed. Unparsed and NoServerFile map to Unparsed. `Node.Create` derives the parse axis from DocumentState when the caller omits it. Direct `documentState =` writes in this range are `Node.Create` arguments or `withDocumentState` (`DocumentAssembly.seedUnparsedStub`).
3. **NewSpecialNode.** `Op.apply` builds the node with `documentState = Unparsed` and the Create defaults. The node starts Unparsed and Persisted. `FileNodeOps` create plans, upload `NewSpecialNode`, and cold stubs that pass Unparsed follow the same Create rule.
4. **Graph edit.** `markOwningSpecialUnpersisted` uses `GraphQuery.enclosing` with `NodeKind.artifact`, inclusive, and updates that one node. Ancestors stay as they were. The parse axis stays as it was. Undo of SetText, SetClasses, SetName, and Replace is a graph edit and sets Unpersisted again. `SetUpdateTime` and `SetDocumentState` do not use this helper.
5. **SQL reload.** `GraphProjection.graphFromPersistence` builds each node with `Node.Create` and the stored `document_state`. The parse axis comes from that value. Persist defaults to Persisted. `Database` still reads and writes `document_state` only. JSON `decodeNode` does the same when `parseState` or `persistState` is absent.

## 5. Called out and outside this expand step

1. **A new member still sets the parent to Current.** `LazyLoadReconciliationApply.markParentCurrent` and `WorkspaceUploadStructure.markParentCurrent` write DocumentState Current, so the dual-write is Parsed. [Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4 item 3 wants the Directory Node Unparsed. §3 Mikado says set the new axes and the old DocumentState together, and migrate old uses step by step. Today’s Current write is that dual-write. The Unparsed discovery value is the later migration.
2. **A deleted member does not write Unparsed on the Directory Node.** `LazyLoadReconciliation.planDeletedInfo` trash-moves the node. The Replace marks the nearest special Unpersisted. It does not write DocumentState Unparsed. §4 item 3 describes that Unparsed write. It is not one of the five in-scope pieces of this expand step. The Server git Actor git pull finish uses this same discovery path (§4 item 4). There is no second axis path.
3. **Client Load still ends on the old parse hop.** §4 item 8 and item 9 say mark the Directory Node or File Node Unparsed. The Parse-stack push is deferred. [Here→There — step 1 locked](plan/github-transport/here-to-there.md) §5 keeps today’s Load to Parse hop. `reconcileDirectory` and `planParseFile` still run that hop and set Current. Leaving the node Unparsed would retire that hop. This expand step keeps the hop.
4. **No deferred worker is started.** No Parse actor, no Persist stack, no Load-on-File stack push, and no retirement of DocumentState.

## Standards

No Standards-axis findings.

The mechanical scan listed new functions at 5 to 16 lines. The limit in [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) is 40 lines per function, 100 characters per line, and 800 lines per file. None of the printed lines break those limits. The exception rule does not apply to tests.

## Spec

The Spec axis reported six gaps. Quotes are from [Here→There — step 1 locked](plan/github-transport/here-to-there.md) §4. The full path inventory is [20 — Spec agent](20-spec-agent-2026-09-29.md).

### (a) Missing or partial

1. Persist-done does not mark Persisted. “Persist done — Mark that node Persisted only.” This review classifies that gap as the Must-fix in section 2.
2. A deleted member does not mark the Directory Node Unparsed. “A new member or a deleted member marks the Directory Node Unparsed.” This review classifies that gap as outside this expand step (section 5).
3. Client Load on a Directory Node does not leave that node Unparsed. “Client Load on Directory — Mark the Directory Node Unparsed (re-process).” This review classifies that gap as outside this expand step (section 5).
4. Client Load on a File Node does not leave that node Unparsed. “Client Load on File — Mark the File Node Unparsed.” This review classifies that gap as outside this expand step (section 5).

### (b) Behaviour not asked

None. The diff does not start the Parse actor, the Persist stack, a git Load retarget, or DocumentState retirement.

### (c) Implemented wrong

1. Parse apply marks Unpersisted. A graph edit is “Unpersisted only”, and “Directory Parse done — Mark that Directory Node Parsed only.” This review classifies that gap as the Should-fix in section 3.
2. Discovery of a new member marks the parent Parsed. “A new member or a deleted member marks the Directory Node Unparsed.” This review classifies that gap as outside this expand step (section 5). Today’s Current value is dual-written to Parsed.

## 6. Summary

Standards: no findings. Spec axis: six gaps; the worst is the artifact write that leaves the owning special Unpersisted. Landing: **Needs work**, one Must-fix (that artifact write), one Should-fix (disk-to-Graph parse leaves Unpersisted).
