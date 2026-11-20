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

- `PathPick.resolveCommand` keeps a plain Load of a Directory, a File, or a non-subject on the Desk path. `DeskLoadPush.runLoadSave` sends the mailbox Load for that node, which marks it Unparsed and pushes it onto the parse stack. There is no git pull or push.
- A plain Load of a Workspace still enters the Load actor. A remote pulls. No remote is the Neither arm.
- `GithubTransportActor` tracks `reconcileId` separately from the enclosing `workspaceId` on `WorkspaceTarget`. After a successful git step (pull or Neither), `startParse` reconciles the selected File, Directory, or Workspace via `loadNode`, and skips `loadNode` for a non-subject node rather than reconciling the enclosing Workspace. A failed git step aborts before reconcile.
- Explicit desk Load and all Save routing are unchanged.
- `PathPick.plainLoadPath` turns a plain Load into a git Load only when the subject is a Workspace with a remote and a local mapping.

## Tests

- [x] `plain Load of a File parses that file from disk` sends a plain Load of a File through the production router on a host with a parse thread. The File ends Current with its child, and no Load actor starts. It failed before the fix. On worktrees it passes at `d57b5be6^` and fails at `d57b5be6`.
- [x] `plain Load of a File or Directory with a remote parses and does not pull` uses a real git remote with a new commit. The File is parsed, the remote file does not arrive, and no Load actor starts.
- [x] `explicit git Load of a File pulls and parses that file` sends an explicit git Load of a File through the production router on a host with a parse thread and a real git remote. The File ends Current with the pulled child.
- [x] `explicit git Load of a Directory pulls and reconciles it` sends an explicit git Load of a Directory through the production router on a host with a parse thread and a real git remote. The Directory ends Current with the pulled child.
- [x] `failed git Load of a File does not reconcile` fails the git pull on a parsing host and verifies the File stays Unparsed with no children and no reconcile Load event.
- [x] `git Load of a Normal node pulls without reconciling the Workspace` in `GithubTransportActorTests.fs` replaces `git Load posts that Workspace id and does not reconcile` and asserts no node is pushed onto the parse stack for a non-subject focus.
- [x] `plain Load of a non-Workspace returns Desk when a remote exists` is restored.
- [x] `Load actor fails when git remote fails besides no repository` now loads the Workspace, which still enters the actor.
- [x] `Load of a ROOT file skips git and git Save uses the data directory` now expects Desk for the Load.
- [x] `plain Load of a Directory does not leave a cancelable actor` and `plain Load of a File or Directory pushes that node onto the parse stack` restore the two parse-stack tests through `LoadSaveRouting.resolvePath` and `LoadSaveStart.startCommand`.
- [x] `plain Load of a Directory, File, or non-subject stays Desk` is restored.
- [x] `a File in a workspace with both mappings keeps a plain Load with no git` and `plain Load of a Workspace with both mappings chooses Git` cover `PathPick.plainLoadPath`.
- [x] `LoadSaveCommandTests.fs` is split into `LoadSaveCommandFixtures.fs`, `LoadSaveCommandTests.fs`, and `LoadSaveSaveCommandTests.fs` so each file stays under the 800-line limit.

## Open

- [x] A plain Load of a Workspace with no remote runs the Load actor's Neither arm. Neither is the default "nothing" transport arm, and the Load then reconciles, as the Load algorithm states.
- [x] A plain Load with a non-Workspace subject or no subject (ROOT with nothing selected) only reconciles. It does no git pull or push, and nothing else changes.

The Load algorithm: a Workspace subject runs the transport step (desk when only a local mapping exists, git when a remote exists, nothing otherwise), then reconciles. Any other subject only reconciles. Reconcile runs whether or not the transport step did anything.
