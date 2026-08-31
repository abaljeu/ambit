# Auto-download on persist — runtime verification tabled

Status: tabled — not yet verified

Board advice: `remove` the [[doc/roadmap/workspace-file-sync.md]] auto-download item. The implementation is delivered; the unresolved runtime checks remain durable in this project and should return to the board only if resumed.

## Tabled project items

- [ ] **Own-edit mapped-folder refresh — not yet verified.** In the desktop-hosted app, edit a mapped File and wait for persistence plus the 400 ms debounce; confirm the corresponding local file content refreshes.
- [ ] **Remote-poll refresh — not yet verified.** Edit that File from a second client; after the desktop receives the remote poll, confirm the same local file refreshes.
- [ ] **No feedback loop — not yet verified.** For either edit, observe at least two poll intervals: one bounded `POST /_desktop/workspace-download`, no `GET /_desktop/workspace-download?id=...`, no download-caused `POST /ambit/changes`, and no repeating download.
- [ ] **Plain-web no-op — not yet verified.** In plain web, edit the same File and confirm persistence succeeds with no `/_desktop/workspace-download` request, folder prompt, or sync error.

Record the date and pass/failure result if these checks are resumed.

## Expected outcome audited

A persisted mapped-workspace File must refresh the local mapped folder after both this client's own edit and a remote edit received by poll, without a feedback loop. Plain web must do nothing.

## Durable evidence found

### Implementation and automated checks

- Own-edit wiring exists: [[src/Client/Update.fs]] applies `SubmitResponse` `stampOps`, then passes them to `UpdateWorkspaceDownload.accumulateAutoDownloadFromOps`.
- Remote-poll wiring exists in both applied `PollDone` paths: after applying the server tail, [[src/Client/Update.fs]] passes the polled Changes to `accumulateAutoDownloadFromChanges`.
- [[src/Shared/WorkspaceUploadStructure.fs]] extracts stamped File targets, and [[src/Shared/WorkspaceSyncScope.fs]] coalesces them into File, Directory, or Workspace download scopes.
- [[src/Client/UpdateWorkspaceDownload.fs]] gates accumulation on `DesktopCapabilities.canWorkspaceSync`, checks for an existing mapping, posts `/_desktop/workspace-download`, ignores the job response, and clears the pending targets.
- The desktop manager calls `WorkspaceFileSync.getStaged`; that path downloads into staging, promotes files into the mapped root, and preserves the server mtime.
- The auto path does not emit `ContinueWorkspaceDownload`, poll a job, call `planAlignFileStampOps`, or post `applyAndPostSync`. This is implementation evidence for the intended no-feedback-loop design.
- [[tests/Shared.Tests/WorkspaceSyncScopeTests.fs]] covers target extraction and coalescing only. It does not exercise either Client handler, desktop download/promotion, repeated network behavior, or plain-web behavior.
- The implementation report, `tmp/implement-auto-download-persisted-files.md`, records a green Client build and 19 focused Shared tests, then explicitly says: “HITL verification pending. No browser/e2e test.”
- The later filesize-refactor report, `tmp/implement-refactor-filesize.md`, records builds/tests and says the project remains active because “HITL auto-download verify [is] still pending.”
- Commit `b07275cb12db0cc327826c9673b118cfc6dc740d` delivered the feature and tests; commit `d1d46d874946fdd3e50a68402b6b5d2ab0f26d61` reorganized it without intended behavior change. Neither commit is a manual verification record.

### Verification evidence still absent

- Own edit refresh: no durable record that a user edited a mapped File in the owning desktop session and observed the corresponding local file refresh.
- Remote-poll refresh: no durable record that a second client edited the File, the desktop received the change through polling, and the mapped local file refreshed.
- No feedback loop: no durable runtime observation showing the resulting download stopped after one bounded enqueue, with no download-job poll or download-caused `/ambit/changes` post.
- Plain-web no-op: no durable browser observation showing an edit persisted with no desktop-download request, mapping prompt, or related error.

The roadmap describes all four behaviors, the original plan marks its implementation todos complete, and the code review found the feature path matched that plan. Those records prove implementation/spec conformance, not that the requested HITL scenarios were run.
