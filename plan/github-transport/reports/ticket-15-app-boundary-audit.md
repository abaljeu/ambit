# Ticket 15 — App boundary audit

Date: 2026-09-27
Branch: `cursor/ticket-15-app-boundary-163d` at `3cde2784`
Ticket: [15 — Keep the App outside Peer Actor hosting](../issues/15-keep-app-outside-peer-actor-hosting.md)
Architecture: [github-transport architecture](../arch.md) module **App** and Story **Server Peer Actor does the round-trip**
Method: Static read of ticket plan, `src/Client`, `src/Desktop`, `src/Server` load/save wiring, Shared command types, and focused tests. No product edits. No broad test run (per audit request).

## 1. Question

Does the App (Browser Client plus Desktop host) host GitHub git transport, hold git remote / tracked-branch / credential state, or bypass the Server load/save command door? What is the smallest stable repository-boundary test set that proves ticket 15 without re-running the full Server git matrix?

## 2. Verdict

No App-side **GitHub hosting** or **Ambit credential store** leak was found in production Client or Desktop source. Person **Load** and **Save** (plain and pre-pick) enqueue **`SubmitLoadSaveCommand`** and POST `/{file}/load-save-command`; git pull/push and `WorkspaceGit.remoteExists` live on Server only. Desk WebDAV and desk Save HTTP continuations remain behind Server **`LoadSavePath.Desk`**. Ticket 15 checklist items **6.2.2 Prove the Server boundary** and the negative App checks are **not** yet locked by dedicated repository-boundary tests; behavior proof is mostly compositional (PathPick, Server routing, partial Client desk continuation).

## 3. App surface today

### 3.1 Browser Client (`src/Client`)

| Concern | Finding |
| --- | --- |
| Peer Actor host | No `GithubTransportActor`, `StartPeerActor`, or Server project reference. `Gambol.Client.fsproj` references Shared and Shared.Documents only. |
| Git process (GitHub pull/push) | No `GitRun`, `WorkspaceGit`, or `LoadSaveRouting` in any Client `.fs` file (ripgrep over `src/Client`). |
| Server request door | `loadOpFor` / `saveOpFor` emit `[ SubmitLoadSaveCommand (loadSaveCommandRequest …) ]` only ([UpdateWorkspaceLoad.fs](../../src/Client/UpdateWorkspaceLoad.fs), [UpdateSave.fs](../../src/Client/UpdateSave.fs)). `App.fs` routes that effect to [LoadSaveCommandClient.fs](../../src/Client/LoadSaveCommandClient.fs), which POSTs `/{fileName}/load-save-command`. |
| Run / `?git` door | Load/Save do not use `SubmitCommand` / `POST …/command`. That path remains for Run ([App.fs](../../src/Client/App.fs) `runSubmitCommand`). |
| Desk WebDAV | When response `path = Desk`, `LoadSaveCommandClient` calls `continueDesk` → `deskLoadOp` / `deskSaveOp` (WebDAV push chain and `ContinueDeskSave` → `/{file}/save`). |
| Pre-picks | [Commands.fs](../../src/Client/Commands.fs) registers plain Load/Save plus `git Load`, `desk Load`, `git Save`, `desk Save` via `LoadSavePrePick`. |
| Remote / branch / credential **state** | Client `VM` / ViewModel modules hold workspace mapping and path-sync ledger data for desk indicators, not a Server branch map or GitHub credential. No Client reference to `GitTokenIssue` or `WorkspaceGitRemote` APIs. |
| Shared types compiled into Client | [WorkspaceGitRemote.fs](../../src/Shared/WorkspaceGitRemote.fs) and `GitTokenIssue` in [ServerCapabilities.fs](../../src/Shared/ServerCapabilities.fs) ship in Shared for wire/helpers; Client has **no call sites**. This is shared codec surface, not App-hosted git config. |
| Desk Save gate | `canGitSave` on `serverCapabilities` gates **local** desk Save (`POST …/save` / Server `GitSave`), not GitHub push from the App. |

### 3.2 Desktop host (`src/Desktop`)

| Concern | Finding |
| --- | --- |
| Server git / Peer Actor | No `GithubTransportActor`, `WorkspaceGit`, or Server reference. |
| Git process | [LocalProxy.fs](../../src/Desktop/LocalProxy.fs) calls `DesktopGit.isAvailable()` once at startup. [DesktopGit.fs](../../src/Shared/dotnet/DesktopGit.fs) uses `GitRun.isAvailable()` → host `git --version` in a temp directory. This is a **capability probe** for desktop JSON (`canGit`), not pull/push and not GitHub credential loading. |
| WebDAV | Existing `/_desktop/workspace-push`, inventory, mappings, and sync ledger endpoints unchanged; Client drives them after desk Load routing. |
| Login credentials | [LocalProxy.fs](../../src/Desktop/LocalProxy.fs) / [AuthStore.fs](../../src/Desktop/AuthStore.fs) hold **Ambit session** login for the cloud app, not git credential helper state for GitHub transport. |

### 3.3 Server boundary (context only)

Plain and explicit git Load/Save choices resolve on Server: [LoadSaveRouting.fs](../../src/Server/LoadSaveRouting.fs) runs `PathPick.resolve` and calls `WorkspaceGit.remoteExists` only inside the Plain chooser. Git path starts [GithubTransportActor.fs](../../src/Server/GithubTransportActor.fs) via mailbox → pool. This is the intended door; the App does not duplicate it.

## 4. Ticket 15 checklist vs evidence

| Item | Status | Evidence |
| --- | --- | --- |
| 6.2.2 Do not host Peer Actor | **Met in source** | No App construction or run of `GithubTransportActor`. |
| 6.2.1 Keep git off App (GitHub pull/push) | **Met in source** | No App `GitRun` pull/push; Desktop only `git --version` probe. |
| 6.1.1 Keep remote config off App | **Met in source** | No App remote map, tracked-branch map, or GitHub credential fields; Plain remote fact is Server-only inside `LoadSaveRouting`. |
| 6.3.1 Command surface + pre-picks | **Met in source** | [Commands.fs](../../src/Client/Commands.fs) and Shared `LoadSavePrePick`. |
| 6.3.2 Desk WebDAV | **Met in source + partial test** | Desk continuation code path; [LoadSaveCommandClientTests.fs](../../tests/Server.Tests/LoadSaveCommandClientTests.fs) proves desk Load → workspace push and desk Save → `/ambit/save`. |
| 6.2.2 Prove Server boundary (repo checks) | **Gap** | No automated forbidden-reference or “effects only SubmitLoadSaveCommand” guard; reliance on manual review and Server-side tests. |

Note: [arch.md](../arch.md) module **App** boxes are checked, but [15 — Keep the App outside Peer Actor hosting](../issues/15-keep-app-outside-peer-actor-hosting.md) leaves verification boxes open until boundary tests land.

## 5. Adjacent nuance (not classified as leaks)

1. **Desktop `git --version`** — Still a host `git` process on the App machine. It does not perform GitHub pull/push and matches ticket wording if “GitRun for GitHub pull or push” is read narrowly. Boundary tests should **allow** `DesktopGit` / `GitRun.isAvailable` on Desktop only, and **forbid** `GitRun` elsewhere under App trees.
2. **`WorkspaceGitRemote` in Shared compiled to Fable** — Dead weight for Browser bundle today, not git hosting. Splitting it out would be a product refactor, not required to close ticket 15 proof.
3. **Stale trace doc** — [ticket-14-routing-trace.md](ticket-14-routing-trace.md) §1 still describes Commands bypassing the load/save door; [project.md](../project.md) records ticket 14 **done** and current Client code uses `SubmitLoadSaveCommand`. Treat the trace as pre-wiring history.

## 6. Existing tests (reuse, do not duplicate)

| Test area | File | What it already proves |
| --- | --- | --- |
| PathPick + pre-pick bypass | [PathPickTests.fs](../../tests/Shared.Tests/PathPickTests.fs) | Plain git vs desk choice; explicit git/desk skip remote chooser. |
| Server git facts | [WorkspaceGitTests.fs](../../tests/Server.Tests/WorkspaceGitTests.fs) | `remoteExists`, tracked branch, pull/push (Server). |
| Peer Actor | [GithubTransportActorTests.fs](../../tests/Server.Tests/GithubTransportActorTests.fs) | Actor git Load/Save; no credential field on dependencies. |
| Command door (Server) | [LoadSaveCommandTests.fs](../../tests/Server.Tests/LoadSaveCommandTests.fs) | Desk vs routed Git through `postLoadSaveCommand` and Peer Actor. |
| Desk Client continuation | [LoadSaveCommandClientTests.fs](../../tests/Server.Tests/LoadSaveCommandClientTests.fs) | Desk response → WebDAV push and desk Save HTTP. |

## 7. Recommended smallest stable repository-boundary tests

Add **one** new test module under `tests/Server.Tests` (project already references [Gambol.Client.fsproj](../../src/Client/Gambol.Client.fsproj) for seam tests). Keep each test function under 40 lines. Prefer reading repo files from disk (stable across refactors that preserve boundaries) over brittle full-string file snapshots.

### 7.1 Module: `AppGitBoundaryTests.fs`

1. **`Client_fsproj_does_not_reference_Server_or_Shared_DotNet`** — Parse [Gambol.Client.fsproj](../../src/Client/Gambol.Client.fsproj); assert no `ProjectReference` to `Gambol.Server` or `Gambol.Shared.DotNet`.
2. **`Client_sources_forbid_git_host_identifiers`** — For every `Compile Include` under Client fsproj, read the `.fs` file; assert no substring `GithubTransportActor`, `GitRun`, `WorkspaceGit.`, `LoadSaveRouting`, or `StartPeerActor`.
3. **`Desktop_sources_forbid_Server_git_host_identifiers`** — Same scan for [Gambol.Desktop.fsproj](../../src/Desktop/Gambol.Desktop.fsproj) sources with the same forbidden list (Desktop may reference Shared.DotNet; that is not a leak by itself).
4. **`Desktop_git_probe_is_allowlisted`** — Assert the only Desktop `.fs` that mentions `DesktopGit` is [LocalProxy.fs](../../src/Desktop/LocalProxy.fs) (or allowlist at most LocalProxy + no other `GitRun` string in Desktop `.fs`).

These four tests directly prove **6.2.2**, **6.2.1**, and **6.1.1** at repo boundary with minimal maintenance.

### 7.2 Module: `LoadSaveCommandDoorTests.fs` (or two facts appended to `LoadSaveCommandClientTests.fs`)

5. **`plain_Load_and_Save_enqueue_only_SubmitLoadSaveCommand`** — Build a minimal `VM` via existing [VmTestHelpers.fs](../../tests/Shared.Tests/VmTestHelpers.fs); run `loadOpFor LoadSavePrePick.Plain` and `saveOpFor LoadSavePrePick.Plain`; assert effects are exactly one `SubmitLoadSaveCommand` each (no direct `ContinueDeskSave`, no `ContinueWorkspacePush` on the first hop).
6. **`Git_response_does_not_call_continueDesk`** — In `LoadSaveCommandClient.runWith`, stub `path = Git` with `command = Some …`; assert `continueDesk` never runs and `commandDone` receives events (mirror desk tests’ style).

Test 5 proves **every git choice crosses the Server request door** at the App entry. Test 6 proves git completion does not silently fall back to desk WebDAV.

### 7.3 Optional single scan (if you want one test instead of a Commands unit test)

7. **`Commands_fs_registers_git_and_desk_pre_picks`** — Read [Commands.fs](../../src/Client/Commands.fs) as text; assert substrings `prePickCmd Load "git Load"`, `prePickCmd Load "desk Load"`, and the Save pair exist. Proves **6.3.1** without executing Fable.

### 7.4 What not to add for ticket 15

- Do not re-prove `WorkspaceGit.pullTracked` / push in App tests (stay in [WorkspaceGitTests.fs](../../tests/Server.Tests/WorkspaceGitTests.fs)).
- Do not add Fable Browser build gates to Server.Tests unless CI already runs them; source scans plus Client project reference checks are enough for **stable** boundary proof.
- Do not require Desktop to drop `git --version` unless product redefines `canGit`; document allowlist instead.

## 8. Conclusion

The App is thin relative to ticket 15: git Load/Save and remote-exists routing are Server-side; the Client sends load/save command requests and keeps desk WebDAV and pre-picks. The remaining work for ticket 15 is **verification**, not product boundary repair — a small `AppGitBoundaryTests` scan suite plus two load/save door effect tests (and optionally one Commands.fs scan) would close the open checklist items with stable repo-boundary proof.
