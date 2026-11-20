# 33 — File Load leaves the File Unparsed
**Status**: coded

One job: A person who runs Load (plain or explicit git) on a File Node, a Directory Node, or another non-Workspace node reconciles that selected node from disk with no git pull or push; a Workspace Load runs its transport step (`desk -> desk Load | git -> git Load (pull) | default -> nothing`) and then reconciles the Workspace, aborting with no reconcile if a step fails. This implements [github-transport spec](../spec.md) Solution 5 — Load and Save and User Story 14 — Plain Load prefers git.

Type:

[x] bug-fixing

Blockers:

[x] none

## Inputs

### Working

[x] `PathPick.resolveCommand` and `PathPick.plainLoadPath` in `src/Shared/PathPick.fs` route non-Workspace Load (plain or explicit git) to `LoadSavePath.Desk` and Workspace Load (plain or explicit git) to `LoadSavePath.Git`; `plainLoadPath` chooses `Git` only for a Workspace with both a remote and a local mapping.
[x] `LoadSaveRouting.resolvePath` in `src/Server/LoadSaveRouting.fs` delegates request path resolution to `PathPick.resolveCommand`.
[x] `GithubTransportActor` in `src/Server/GithubTransportActor.fs` runs the Workspace Load actor: `readWorkspace workspaceFromFocus` resolves the Workspace work tree (falling back to the Neither arm with label `SYSTEM` when a Workspace such as ROOT has no work tree), `startParse` reconciles `target.workspaceId`, and a failed transport step aborts with no reconcile.
[x] `DeskLoadPush.runLoadSave` in `src/Server/Core/DeskLoadPush.fs` sends the mailbox Load for the selected node, marking it Unparsed and pushing it onto the parse stack.
[x] `UpdateWorkspaceLoad.plannedUpload` in `src/Client/UpdateWorkspaceLoad.fs` returns `Some WorkspaceUploadAction.ReconcileServerDisk` for an explicit git Load of a non-Workspace so `holdStack` stays `false` and the client does not run a desktop push on the Desk response.
[x] `tests/Server.Tests/LoadSaveCommandTests.fs`, `tests/Server.Tests/LoadSaveCommandFixtures.fs`, `tests/Server.Tests/LoadSaveSaveCommandTests.fs`, `tests/Server.Tests/GithubTransportActorTests.fs`, `tests/Server.Tests/DeskLoadParseStackTests.fs`, `tests/Server.Tests/ParseThreadLoadTests.fs`, and `tests/Shared.Tests/PathPickTests.fs` verify Load routing, reconcile behavior, abort-on-failure, and unchanged Save routing.

### Reference

[x] [github-transport architecture](../arch.md) Story paths 8, 10, 14, and 24.
[x] [github-transport map](../map.md) Decisions 8, 9, and 11.

## Do not load

[x] Save routing (`PathPick.plainSavePath`, `src/Client/UpdateSave.fs`, `tests/Server.Tests/LoadSaveSaveCommandTests.fs`).
[x] Known desk limitations (out of scope, not intended design):
  - Plain and desk Load of a File or Directory on Desktop with a local mapping still plan `DesktopPush` before reconcile.
  - Desk Load of a non-subject returns `None` from `DeskLoadPush.subjectToLoad` and reconciles nothing.
  - Plain Load of a local-only Workspace (no remote, local mapping only) enters the Load actor Neither arm and does not run desktop upload.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [x] `PathPick.resolveCommand` routes any Load (plain or explicit git) of a File, a Directory, or another non-Workspace node to `LoadSavePath.Desk`; `DeskLoadPush.runLoadSave` reconciles that selected node on the parse stack with no git pull or push.
3. [x] `PathPick.resolveCommand` routes Workspace Load (plain or explicit git) to `LoadSavePath.Git` (the Load actor), which pulls when a remote exists or takes the Neither arm otherwise, then reconciles the Workspace; a failed step aborts with no reconcile. ROOT is a Workspace Node (`Special Workspace`) with no work tree, so its Load runs no transport and then reconciles.
4. [x] `PathPick.plainLoadPath` returns `Some LoadSavePath.Git` only when the subject is a Workspace with both a remote and a local mapping.
5. [x] `GithubTransportActor` reconciles `target.workspaceId` via `startParse` and reads the Workspace work tree via `readWorkspace workspaceFromFocus` (falling back to Neither with label `SYSTEM` when a Workspace has no work tree).
6. [x] `UpdateWorkspaceLoad.plannedUpload` returns `Some WorkspaceUploadAction.ReconcileServerDisk` for explicit git Load of a non-Workspace so `holdStack` stays `false` and no desktop push runs on the Desk response.
7. [x] Explicit desk Load and all Save routing stay unchanged.

## Outputs

[x] `src/Shared/PathPick.fs`
[x] `src/Server/LoadSaveRouting.fs`
[x] `src/Server/GithubTransportActor.fs`
[x] `src/Server/Core/DeskLoadPush.fs`
[x] `src/Client/UpdateWorkspaceLoad.fs`
[x] `tests/Server.Tests/LoadSaveCommandTests.fs`
[x] `tests/Server.Tests/LoadSaveCommandFixtures.fs`
[x] `tests/Server.Tests/LoadSaveSaveCommandTests.fs`
[x] `tests/Server.Tests/Gambol.Server.Tests.fsproj`
[x] `tests/Server.Tests/GithubTransportActorTests.fs`
[x] `tests/Server.Tests/DeskLoadParseStackTests.fs`
[x] `tests/Server.Tests/ParseThreadLoadTests.fs`
[x] `tests/Shared.Tests/PathPickTests.fs`
[x] `plan/github-transport/spec.md`
[x] `plan/github-transport/arch.md`
[x] `plan/github-transport/map.md`
[x] `plan/github-transport/project.md`
[x] `doc/user/reference/commands/files.md`
[x] `plan/actor-as-client/issues/23-run-the-load-actor-for-git-and-neither/23-run-the-load-actor-for-git-and-neither-ticket.md`
[x] `plan/github-transport/issues/33-doc.md`

## Stop when

[x] Status stays `coded` until review and document steps finish.

## Check

[x] Run `scripts/agent-test.sh` with no arguments.
[x] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Server.Tests`: ``plain Load of a File parses that file from disk``
  - `tests/Server.Tests`: ``plain Load of a File or Directory with a remote parses and does not pull``
  - `tests/Server.Tests`: ``explicit git Load of a File parses disk and does not pull``
  - `tests/Server.Tests`: ``explicit git Load of a Directory reconciles disk and does not pull``
  - `tests/Server.Tests`: ``explicit git Load of a Normal node in a Workspace does not pull``
  - `tests/Server.Tests`: ``explicit git Load of a Workspace pulls and reconciles``
  - `tests/Server.Tests`: ``failed git Load of a Workspace does not reconcile``
  - `tests/Server.Tests`: ``plain Load of a non-Workspace returns Desk when a remote exists``
  - `tests/Server.Tests`: ``Load actor fails when git remote fails besides no repository``
  - `tests/Server.Tests`: ``Load of a ROOT file skips git and git Save uses the data directory``
  - `tests/Server.Tests`: ``plain Load of a Directory does not leave a cancelable actor``
  - `tests/Server.Tests`: ``plain Load of a File or Directory pushes that node onto the parse stack``
  - `tests/Shared.Tests`: ``plain Load of a Directory, File, or non-subject stays Desk``
  - `tests/Shared.Tests`: ``explicit git Load of a Directory, File, or non-subject stays Desk``
  - `tests/Shared.Tests`: ``explicit git Load of a Workspace chooses Git``
  - `tests/Shared.Tests`: ``a File in a workspace with both mappings keeps a plain Load with no git``
  - `tests/Shared.Tests`: ``plain Load of a Workspace with both mappings chooses Git``
[x] All executed tests pass.
[x] A reader who reads only the One job line can say what the ticket builds.
