# 32 — Workspace lock handoff

Ticket: [[../issues/32-workspace-lock-handoff.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- **Hard** — [[.agents/rules/fsharp-source.md]] 40 lines or less per function. `python3 scripts/standards-scan.py --diff HEAD` prints `WorkspaceWebDav.registerRoutes` lines 692-768 (77 lines) OVER 40. The signature gained `landFile`. The body was already over 40.
- **Judgement** — Argument Count. `landLocked` takes `handoff`, `graph`, `workspaceId`, `label`, `relative`, and `land`. Source: [[../../context/SMELLS.md]]

```fsharp
let private landLocked handoff graph workspaceId label relative land =
```

- **Judgement** — Argument Count. `dispatch` and `dispatchOpaque` gained a `land` parameter on an argument list that was already past four. Source: [[../../context/SMELLS.md]]

```fsharp
let private dispatch
    (land: WorkspaceFileLand)
    (isAuthenticated: HttpRequest -> bool)
    (dataDir: string)
```

- **Judgement** — Duplicated Code. `runLoadOn` and `saveFetched` both commit, fetch, then `lockMergeAndMark`. Source: [[../../context/SMELLS.md]]

```fsharp
match commitFetched dependencies.git locked "gambol: git Load" with
| Ok (tracked, _) ->
    match lockMergeAndMark dependencies.git locked target tracked with
```

- No GLOSSARY.md PHRASE line.

## 2. Spec

- **Unnamed** — `UploadHandoff` in [Workspace upload lock](../../../src/Server/WorkspaceUploadLock.fs) and `WorkspaceWebDav.WorkspaceFileLand` in [Workspace WebDAV](../../../src/Server/WorkspaceWebDav.fs). The ticket does not name those types.
- No Missing, Partial, or Wrong requirement. Fetch completes with no `withLock`. The Workspace lock then drains a held member, merges, marks Unparsed from the git changed list, and releases. `EventBody.Load` pushes the Parse stack after that release. Upload lands, posts `Notice.NodeReceived`, and releases inside one `withLock`, then loads the file.

## 3. Arch

- No findings.

## 4. Tests

- ``fetch completes before the workspace lock then the pull merges and marks`` in [GitHub transport load pull tests](../../../tests/Server.Tests/GithubTransportLoadPullTests.fs) calls `lockMergeAndMark`. The merge and the Unparsed notice run while `NodeLockPool.nodeIsHeld` is true for the Workspace. Fetch then runs while a Directory lock is held, and the merge starts after that release. `EventBody.Load` of the Workspace follows.
- ``upload lands and marks Unparsed inside the workspace lock`` calls `WorkspaceUploadLock.landMarkRelease`. Land and `Notice.NodeReceived` run while the caller holds the Workspace lock. The parse load runs after that lock releases.
- ``git Load commits before pull`` records commit, then fetch, then pull.
- `bash scripts/check-test.sh 13916` prints `DONE` and `/tmp/ambit-full-suite-20261110-220727.log`.
- Server.Tests passed 768, skipped 1, total 769.
- Shared.Tests passed 1937, skipped 3, total 1940.
- The same counts passed again after the review fixes and the refactor.

## 5. Miss

- No findings.

## 6. Found elsewhere

- **Problem.** [Workspace upload structure](../../../src/Shared/WorkspaceUploadStructure.fs) `planServerFilePresentOps` returns no ops for `PullingFile`, `Current`, and `Unparsed`.
- **Fix.** Restore the incoming-file ops for the state named in [core-refinement spec](../spec.md) **Upload and non-File create**, in a ticket that owns that planner.
- **Problem.** [07 — Git changed list sets Unparsed](../issues/07-git-changed-list-unparsed.md) is still `defined`. [12 — Git pull marks each received file Unparsed](../../parse-thread/issues/12-git-pull-marks-each-received-file-unparsed.md) is `done` and [13 — Git pull marks containing Directories Unparsed](../../parse-thread/issues/13-git-pull-marks-containing-directories-unparsed.md) is `coded`.
- **Fix.** Point 07 at 12 and 13, or mark 07 superseded. Alan decides.
- 2026-11-10 — Alan: 07 — Git changed list sets Unparsed is superseded by 12 — Git pull marks each received file Unparsed and 13 — Git pull marks containing Directories Unparsed.
