# Update glossary: Directory File replaces Marker

## What changed

### Glossary (CONTEXT.md) — already done

1. **Directory File** defined under About the Software (after Directory Node).
2. **Marker** listed under Additional Unwanted terms → use Directory File.
3. Directory File `_Avoid_` covers former Marker speech.

### Code renames (this follow-up)

| Old | New | File |
| --- | --- | --- |
| `markerRelativePath` | `directoryFileRelativePath` | `LazyLoadReconciliationApply.fs` |
| `markerMoves` | `directoryFileMoves` | `LazyLoadReconciliation.fs` |
| `markers` (local) | `directoryFiles` | `LazyLoadReconciliation.fs`, `LazyLoadReconciliationReport.fs` |
| `markerPairs` (param) | `directoryFilePairs` | `coveredByDirInfoMove` in `LazyLoadReconciliation.fs` |

Left alone: `DocumentArtifactPath.isMarker` / `tryMarkerOwnerParts` and other persistence “marker” identifiers.

### Tests

Renamed four LazyLoadReconciliationTests titles from “marker” to “Directory File”. Filter `FullyQualifiedName~LazyLoadReconciliation`: 33 passed.

### WORK.md

Added Pending: optional speech/doc sweep (artifact: this report). `isMarker` API renames out of scope unless decided.

## Exact glossary definitions

**Directory File**:
The `.amb` Document that belongs to a Directory Node or Workspace Node (root `.amb` or `DirName/.amb`). It is that node's Document artifact, not a File Node child. Cold bootstrap that reads only Directory Files leaves other File bodies Unparsed until Parse.
_Avoid_: Marker (for this concept), marker file, directory marker, amb marker, marker-only load (prefer Directory-File-only / Directory File cold load)

**Marker** (unwanted / deprecated):
For `.amb` Directory/Workspace documents, or “marker-only” cold bootstrap — deprecated; say **Directory File**.

## Leftover “marker” (optional later sweep)

| Location | Notes |
| --- | --- |
| `src/Shared/DocumentArtifactPath.fs` | `isMarker`, `tryMarkerOwnerParts` |
| `src/Shared/dotnet/DocumentAssembly.fs` | comment “directory markers” |
| `src/Server/DocumentPersistence.fs` | marker-path / marker-outline comments and locals |
| `src/Server/LazyLoadReconciliationServer.fs` | `markerPathsFromChanges` |
| `src/Shared/WorkspaceUploadStructure.fs` | `markerOwnerParts` |
| Informal docs | e.g. `doc/roadmap/lazy-load.md`, `doc/roadmap/workspace-file-persistence.md` |

Unrelated (do not retarget): conflict markers, markdown list markers, `@` disk-marker history, Bullet “node marker”, WebDAV URL markers.
