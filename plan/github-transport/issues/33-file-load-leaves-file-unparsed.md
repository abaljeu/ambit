# 33 — File Load leaves the File Unparsed

**Status:** `done`
**Type:** coding
**Blocked by:** None

**Binding spec:** [github-transport spec](../spec.md) Solution 5 — Load and Save, and User Story 14 — Plain Load prefers git.

## Bug

A person runs Load on a File Node. The file is not parsed, no child nodes appear, and the File Node stays Unparsed. When the Workspace has a git remote, the same Load also pulls the whole Workspace.

## Cause

The breaking commit is `d57b5be6`, Run the Load actor for Git and Neither (pull request 591).

- Before that commit, `PathPick.resolveCommand` kept a plain Load of a Directory, a File, or a non-subject on the Desk path. `DeskLoadPush.runLoadSave` then sent a mailbox Load for that node, and `CoreMailboxFilesAxis.markUnparsedThenPush` marked it Unparsed and pushed it onto the parse stack.
- That commit made `PathPick.resolveCommand` return Git for every plain Load, so a plain File Load starts the Load actor.
- The Load actor checks the work tree for a remote. With a remote it pulls the whole Workspace. With no remote it takes the Neither arm. Both arms then call `loadNode` on the enclosing Workspace, never on the File.
- The parse thread reconciles that Workspace as a Directory. `requeueOnUnparsed` is `false` in `ParseThread`, so the reconcile does not push the File. The File stays Unparsed.
- On the client, `PathPick.plainLoadPath` turns a plain Load into a git Load when the Workspace has a remote and a local mapping, for any subject. Since explicit git Load now pulls the whole Workspace, a plain File Load from the desktop also pulled.

The git operations did not stop the parse by themselves. The route change sent File Loads to code that only loads the Workspace.

## Deleted or weakened tests

The same commit `d57b5be6` inverted or replaced every test that held this rule. No test file was deleted. The later suspect commits (desk Load cancel in `19ccd32e`, plain Save routing in `9e1ec14b`, and the CoreMsg removals in `4273ef22`, `80cad19c`, `a66cdded`, `9a6c2e91`, and `b19181a7`) changed no test that asserts a File Load parses.

- `Load on a file parses that file` in `LoadSaveCommandTests.fs` asserted that a plain Load of a File through `Api.postLoadSaveCommand` returns Desk and that the File ends Current with a child whose text is the file content. It was replaced by `plain File Load with no Git is Neither through the Load actor`, which asserted only that the actor started and stopped and did not pull or push. Nothing checked the parse.
- `plain Load of a File or Directory stays Desk when a remote exists` in `LoadSaveCommandTests.fs` asserted Desk and no actor start for a File and a Directory in a work tree with a remote. It became `plain Directory Load with a remote pulls through the Load actor`, which asserted the pull.
- `plain Load of a non-Workspace returns Desk when a remote exists` in `LoadSaveCommandTests.fs` asserted Desk and no git call. It became `plain Load starts the Load actor when a remote is reported`.
- `plain Load of a File or Directory ignores a Git path` in `ParseThreadLoadTests.fs` asserted that the Directory and the File are pushed onto the parse stack. It became `plain Load does not coerce a Git path onto the parse stack`, which asserted that nothing is pushed.
- `Desk Load of a Directory does not leave a cancelable actor` in `ParseThreadLoadTests.fs` became `Neither Load of a Directory starts the Load actor and then stops`, which asserted that `resolvePath` returns Git for a plain Directory Load.
- `plain Load of a Directory, File, or non-subject stays Desk` in `PathPickTests.fs` became `plain Load of a Directory or a File chooses the Load actor`.
- `effective path forces Desk for plain File Load` in `PathPickTests.fs` was deleted with `PathPick.effectiveLoadPath`.

## Fix

- `PathPick.resolveCommand` routes any Load (plain or explicit git) of a Directory, a File, or a non-subject to the Desk path. `DeskLoadPush.runLoadSave` sends the mailbox Load for that node, which marks it Unparsed and pushes it onto the parse stack. There is no git pull or push.
- A Load of a Workspace (plain or explicit git) enters the Load actor. A remote pulls; no remote is the Neither arm; a successful step reconciles the Workspace, and a failed step aborts before reconcile.
- `GithubTransportActor` drops the dead non-Workspace `reconcileId` tracking (`reconcileId` on `WorkspaceTarget`, `reconcileIdFor`, and `runOutsideWorkspace`). `startParse` reconciles `target.workspaceId`, and `runLoadBody` reads the Workspace via `readWorkspace workspaceFromFocus` (falling back to Neither with label `SYSTEM` when a Workspace such as `ROOT` has no work tree).
- `UpdateWorkspaceLoad.plannedUpload` returns `Some WorkspaceUploadAction.ReconcileServerDisk` for an explicit git Load of a non-Workspace so `holdStack` stays `false` and the client does not run a desktop push on the Desk response.
- Explicit desk Load and all Save routing are unchanged.
- `PathPick.plainLoadPath` turns a plain Load into a git Load only when the subject is a Workspace with a remote and a local mapping.

## Tests

- [x] `plain Load of a File parses that file from disk` sends a plain Load of a File through the production router on a host with a parse thread. The File ends Current with its child, and no Load actor starts. It failed before the fix. On worktrees it passes at `d57b5be6^` and fails at `d57b5be6`.
- [x] `plain Load of a File or Directory with a remote parses and does not pull` uses a real git remote with a new commit. The File is parsed, the remote file does not arrive, and no Load actor starts.
- [x] `explicit git Load of a File parses disk and does not pull` sends an explicit git Load of a File through the production router on a parsing host with a real git remote that has a newer commit. The File ends Current with its on-disk child, no pull happens, and no Load actor starts.
- [x] `explicit git Load of a Directory reconciles disk and does not pull` sends an explicit git Load of a Directory through the production router on a parsing host with a real git remote that has a newer commit. The Directory ends Current with its on-disk child, no pull happens, and no Load actor starts.
- [x] `explicit git Load of a Normal node in a Workspace does not pull` sends an explicit git Load of a Normal node inside a Workspace with a real git remote and verifies no pull happens and no Load actor starts.
- [x] `explicit git Load of a Workspace pulls and reconciles` sends an explicit git Load of a Workspace through the production router on a parsing host with a real git remote and verifies the pull arrives and the Workspace reconciles to Current.
- [x] `failed git Load of a Workspace does not reconcile` fails the git pull on a parsing host and verifies the Workspace stays Unparsed with no reconcile Load event.
- [x] Removed tests that asserted the old pull-for-non-Workspace behavior: `git Load pulls a staging checkout and does not reconcile`, `explicit Git Load of a non-repo Directory is Neither`, `git Load does not reconcile a disk file into the graph` in `LoadSaveCommandTests.fs`, and `git Load of a Normal node pulls without reconciling the Workspace` in `GithubTransportActorTests.fs`. Updated the remaining Workspace-Load tests in `LoadSaveCommandTests.fs`, `GithubTransportActorTests.fs`, and `DeskLoadParseStackTests.fs`.
- [x] `plain Load of a non-Workspace returns Desk when a remote exists` is restored.
- [x] `Load actor fails when git remote fails besides no repository` now loads the Workspace, which still enters the actor.
- [x] `Load of a ROOT file skips git and git Save uses the data directory` now expects Desk for the Load.
- [x] `plain Load of a Directory does not leave a cancelable actor` and `plain Load of a File or Directory pushes that node onto the parse stack` restore the two parse-stack tests through `LoadSaveRouting.resolvePath` and `LoadSaveStart.startCommand`.
- [x] `plain Load of a Directory, File, or non-subject stays Desk`, `explicit git Load of a Directory, File, or non-subject stays Desk`, and `explicit git Load of a Workspace chooses Git` cover `PathPick.resolveCommand`.
- [x] `a File in a workspace with both mappings keeps a plain Load with no git` and `plain Load of a Workspace with both mappings chooses Git` cover `PathPick.plainLoadPath`.
- [x] `LoadSaveCommandTests.fs` is split into `LoadSaveCommandFixtures.fs`, `LoadSaveCommandTests.fs`, and `LoadSaveSaveCommandTests.fs` so each file stays under the 800-line limit.

## Open

- [x] A plain Load of a Workspace with no remote runs the Load actor's Neither arm. Neither is the default "nothing" transport arm, and the Load then reconciles, as the Load algorithm states.
- [x] A plain Load with a non-Workspace subject or no subject (ROOT with nothing selected) only reconciles. It does no git pull or push, and nothing else changes.

The Load algorithm: a Workspace subject runs the transport step (desk when only a local mapping exists, git when a remote exists, nothing otherwise), then reconciles. Any other subject only reconciles. Reconcile runs whether or not the transport step did anything.
