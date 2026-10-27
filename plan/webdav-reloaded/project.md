# webdav-reloaded

Stage: map
Summary: Holds the WebDAV and desktop upload breakage left by removing desktop-side parse, upload node creation and reconcile in [[plan/core-refinement/project.md]]. Nothing here is being fixed yet.
Updated: 2026-10-26
Started: 2026-10-26

**Split from:** [[plan/core-refinement/project.md]]
**Steps:** [[plan/context/project-work.md]] default sequence.

## Purpose

For now, this project only stashes what we've identified as broken. The fixes are tabled.

## Broken

1. **Upload no longer guarantees nodes.** Desktop-side node creation for uploaded files is gone. Parse-before-upload was deleted, and the upload structure markers went with 128/129. `planStubOps` now returns only create ops. Anything that expected upload to create or settle nodes is not guaranteed.
2. **WebDAV file-present handler sets no document state.** `planServerFilePresentOps` dropped its `SetDocumentState` (127). Nothing replaces it yet.
3. **A parent goes Unparsed to Current without a parse.** Alan answered core-refinement 131 §6 as No: `applyNodeParsed`'s `pushParsedUnpersisted` and `noteParsed` are not right for an upload parent that becomes Current without a parse. The intended direction is to route such uploads through the existing Load channel, which invokes parse at the right moments. It is unknown whether Load supports everything that needs.
4. **`upload emits NodeUploading` is order-dependent.** It fails when run alone, even on staging, so it stays disabled.

## Tentative design (Alan, 2026-10-26)

Not locked.

- Upload sends files to a receiving actor. That actor constructs or updates the directory tree the same way git Load does.
- At minimum, when the files arrive, it marks the root Workspace Unparsed and pushes it onto the parse stack.
- It also marks received files Unparsed.
- It marks pending files PullingFile. Open: a possible design change drops PullingFile.

## Disabled tests

| Test | File | Disabled by |
|---|---|---|
| `incoming file posts NodeReceived beside SetDocumentState` | `tests/Server.Tests/NodeEventExpandTests.fs:389` | 127 |
| `incoming file emits NodeReceived` | `tests/Server.Tests/NodeEventExpandTests.fs:416` | 127 |
| `planServerFilePresentOps transitions only matching absent files` | `tests/Shared.Tests/WorkspaceUploadStructureTests.fs:246` | 127 |
| `upload posts NodeUploading beside SetDocumentState` | `tests/Server.Tests/NodeEventExpandTests.fs:205` | 128/129 |
| `upload emits NodeUploading` | `tests/Server.Tests/NodeEventExpandTests.fs:236` | 128/129 (also order-dependent) |
| `reuses existing owned path without duplicating` | `tests/Shared.Tests/WorkspaceUploadStructureTests.fs:117` | 128 |
| `upload structure persistence preserves existing file mtime` | `tests/Server.Tests/DocumentPersistenceTests.fs:249` | 128 |

## Notes

- 2026-10-26 — Alan opened this project to stash the identified breakage. Fixing is tabled.
- 2026-10-26 — Alan recorded a tentative design: a receiving actor for upload, modeled on git Load (see Tentative design).
