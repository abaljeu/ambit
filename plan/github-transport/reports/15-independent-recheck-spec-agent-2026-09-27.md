# Independent Spec recheck — [15 — Keep the App outside Peer Actor hosting](../issues/15-keep-app-outside-peer-actor-hosting.md)

Range `0a532cb1...HEAD` (`498c2b38`). Spec: [15 — Keep the App outside Peer Actor hosting](../issues/15-keep-app-outside-peer-actor-hosting.md) item **6.2.2 Prove the Server boundary**, and Must-fix **1** and **2** in [15-independent-code-review-2026-09-27.md](15-independent-code-review-2026-09-27.md). Source: current tests and project files. Tests were not run.

## 1. Landing

**No Must-fix remains.** Neither prior landing blocker remains on this Spec axis.

## 2. Prior Must-fix 1 — closed

Prior requirement: “Tests or repository-boundary checks prove every git choice crosses the Server request door.” And: add a Server integration matrix for Load and Save with explicit Git and plain resolved to Git, through real `Api.postLoadSaveCommand` / `CoreMailbox.startLoadSaveCommand` / Peer Actor start, not a local post stub.

[LoadSaveCommandTests.fs](tests/Server.Tests/LoadSaveCommandTests.fs) `every Git choice starts the Server Peer Actor` is a 2×2 theory (`load`/`save` × `git`/`plain`). Each case posts through [Api.postLoadSaveCommand](src/Server/Api.fs), starts through [CoreMailbox.startLoadSaveCommand](src/Server/Core/CoreMailbox.fs), and registers [GithubTransportActor.actorFn](src/Server/GithubTransportActor.fs) on the pool. [PathPick.resolve](src/Shared/PathPick.fs) with `remoteExists` true maps Plain to Git. The test waits for the Actor git callback and asserts `ActorStart` for the Focus. The old Client local `post` stub is gone.

## 3. Prior Must-fix 2 — closed

Prior requirement: “Tests or repository-boundary checks prove … no App Actor host is added.” And: resolve and scan every `<Compile Include>` for the Browser and App projects, including linked and nested files, and check the relevant project dependency boundary.

[AppGithubTransportBoundaryTests.fs](tests/Server.Tests/AppGithubTransportBoundaryTests.fs) reads `<Compile Include>` from [Gambol.Client.fsproj](src/Client/Gambol.Client.fsproj) and [Gambol.Desktop.fsproj](src/Desktop/Gambol.Desktop.fsproj), resolves each path with `Path.GetFullPath`, and scans those files. Nested and linked Includes are visible. `projectClosure` walks `<ProjectReference>` and must equal the approved set (Client, Desktop, Shared, Documents, DotNet). An extra host project fails. Current Client and Desktop Compile items are same-directory files; the resolver still covers a nested or linked Include if one is added. Client and Desktop sources do not name `GithubTransportActor`, `CoreActorPool`, `registerPeer`, `GitRun`, or `WorkspaceGit`. Desktop calls `DesktopGit.isAvailable` only as a capability probe.

## 4. Residual Should-fix limitations

1. **State proof is still a heuristic.** Ticket item **6.1.1 Keep remote config off the App** asks that verification prove the App adds no remote map, tracked-branch map, or GitHub credential state. The test forbids record field names that contain `github`, `remote`, or `branch`, plus selected source phrases. Equivalent state under other names in un-scanned closure sources can still pass. This is the prior Should-fix **3**, not a landing blocker.
2. **Identifier scan does not read closure Compile Includes.** Shared, Documents, and DotNet are in the approved project set, but their `<Compile Include>` files are not scanned. [GitRun.fs](src/Shared/dotnet/GitRun.fs) stays in DotNet. This matches the stated Must-fix 2 surface (Browser and App projects plus dependency closure). It does not make the spelling check a complete host-source proof.
3. **The 2×2 matrix stubs git.** Actor start is real. [GithubTransportGit](src/Server/GithubTransportActor.fs) in the matrix is a test double. Full production pull/push remains on the skippable facts for explicit Git Save and Plain Load only. The Must-fix asked for Peer Actor start, not four live git round-trips.

## 5. Summary

Must-fix findings: 0. Residual Should-fix limitations: 3. Worst remaining Spec limitation: the remote/branch/credential check is still a name heuristic, not a durable state model.
