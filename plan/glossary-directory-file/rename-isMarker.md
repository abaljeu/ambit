# Rename isMarker → Directory File APIs

## Done

`DocumentArtifactPath.isMarker` and related Directory File “Marker” identifiers renamed. CONTEXT.md glossary already defined Directory File / deprecated Marker.

## Renames

| Old | New |
| --- | --- |
| `DocumentArtifactPath.isMarker` | `isDirectoryFile` |
| `DocumentArtifactPath.tryMarkerOwnerParts` | `tryDirectoryFileOwnerParts` |
| `Filename.isAmbMarkerName` | `isDirectoryFileBasename` |
| `Filename.isAmbMarkerFilename` | `isDirectoryFileFilename` |
| `refuseAmbMarkerNamedDocument` | `refuseDirectoryFileNamedDocument` |
| locals `markerRelatives` / `markerOwnerParts` / `markerPathsFromChanges` | `directoryFileRelatives` / `directoryFileOwnerParts` / `directoryFilePathsFromChanges` |

Error strings that said “amb marker basename…” now say “directory file basename…”. Comments in cold-bootstrap / DocumentAssembly updated to Directory File.

## Left alone (not glossary Marker)

- AmbDocument owner-line / kind markers (`parseOwnerKindMarker`, WorkspaceMarker tests)
- WebDAV URL path `marker` locals
- SyncLogicTests conflict “marker” nodes
- Snapshot “hash markers”

## Tests

- Shared focused filter (LazyLoad / DocumentAssembly / Model / SystemDirectoryPersist / FileNodeOps / WorkspaceUpload / History): **385 passed**
- Server focused (DocumentPersistence / ChangeEndpointResilience): **56 passed**

## WORK.md

Narrow Pending speech/doc sweep: `isMarker` API rename is complete; remaining is informal docs/comments elsewhere (and AmbDocument format “marker” is unrelated).
