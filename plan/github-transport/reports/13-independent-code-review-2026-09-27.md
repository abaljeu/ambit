# 13 — Peer Actor git Load/Save independent code review

Date: 2026-09-27
PR: [Run git Load and Save through the Server Peer Actor](https://github.com/abaljeu/ambit/pull/140)
Range: `origin/staging...9397f52d58e3fe9e8d213081c40da003592fe6dc`
Overall: **Must-fix** for one repository documentation-standard violation. The implementation has no functional Must-fix against [13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md).

## 1. Standards

### 1.1 Must-fix — Code map uses number-only references

[Peer Actor code map for 13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/reports/peer-actor-code-map-2026-09-27.md) repeatedly refers to 13 — Run git Load and Save through the Server Peer Actor, 07 — Actor start door, 14 — Route Load and Save by path pre-pick, 06 — Selection-scoped Parse after whole-tree git Load, and checklist items by number without their names throughout lines 1–145. This breaks [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md): every human-readable ticket and item reference must include its name. The mechanical standards scan reports 41 occurrences. This added report must not enter the reviewed change in its current form.

### 1.2 Nice-to-have — Peer Actor repeats WorkspaceGit orchestration

[GithubTransportActor.fs](src/Server/GithubTransportActor.fs) lines 67–81 reconstructs pull and save from `withWorkTreeGate`, `trackedBranch`, `pullTracked`, `commitAll`, and `pushTracked`, while [WorkspaceGit.fs](src/Server/WorkspaceGit.fs) lines 153–208 already composes `pullTracked` and `saveTracked`. This is possible Duplicated Code and Feature Envy under [.agents/skills/code-review/SMELLS.md](.agents/skills/code-review/SMELLS.md). The split makes gate order observable to the Actor tests, and the behavior is correct, so this is not a functional blocker. It does create two places that must preserve the same tracked-branch and gate rules.

## 2. Specification

### 2.1 Good — Mailbox start door and pool registry

[CoreMsg.fs](src/Server/Core/CoreMsg.fs), [CoreMailbox.fs](src/Server/Core/CoreMailbox.fs), [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs), and [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) add a dedicated `StartPeerActor` door and a separate `peerDefs` registry. `startActor` still resolves only the existing Run actor registry, and [GithubTransportActorTests.fs](tests/Server.Tests/GithubTransportActorTests.fs) proves that the Peer Actor is unavailable through Run. This matches [07 — Actor start door](plan/github-transport/issues/07-actor-start-door.md). Production person-command routing is not present, but [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md) owns that caller and is out of scope here.

### 2.2 Good — Workspace scope and git operation order

[GithubTransportActor.fs](src/Server/GithubTransportActor.fs) resolves a subnode Focus through `GraphQuery.enclosingWorkspace` and `DocumentPersistPath.workspaceRootFor`, so Focus does not become a git pathspec. Load runs tracked-branch pull and then Server Parse reconciliation. Save commits all work-tree edits, releases the gate, and then pushes. No Save path invokes Persist.

### 2.3 Good — Gate acquisition, release, and waiting

[GithubTransportActorTests.fs](tests/Server.Tests/GithubTransportActorTests.fs) proves acquire → pull → release → Parse and acquire → commit → release → push. [WorkspaceGit.fs](src/Server/WorkspaceGit.fs) uses `SemaphoreSlim.Wait` with `finally` release. Existing [WorkspaceGitTests.fs](tests/Server.Tests/WorkspaceGitTests.fs) proves that a second caller, Persist, pull, and save wait and continue without overlap.

### 2.4 Good — Stubbed operations, Parse continuation, rejects, and credentials

[GithubTransportActorTests.fs](tests/Server.Tests/GithubTransportActorTests.fs) stubs the complete `GithubTransportGit` record and proves Load pull, Save commit-then-push, Parse continuation, matching conflict rejects, live-row lifetime, and no credential-shaped dependency field. [GithubTransportActor.fs](src/Server/GithubTransportActor.fs) supplies only host-side `WorkspaceGit` functions and has no GitHub credential argument or store.

### 2.5 Good — Status and architecture boundary

[13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) remains `coded`. [github-transport architecture](plan/github-transport/arch.md) keeps person-command routing, PathPick routing, App hosting, and the full selection-aware Load pipeline unchecked. It does not claim [14 — Route Load and Save by path pre-pick](plan/github-transport/issues/14-route-load-save-by-pre-pick.md) or [15 — Keep the App outside Peer Actor hosting](plan/github-transport/issues/15-keep-app-outside-peer-actor-hosting.md).

### 2.6 Nice-to-have — Parse completion wording has two boundaries

[13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/issues/13-peer-actor-runs-git-load-save.md) checks the Parse-continuation items, while [github-transport architecture](plan/github-transport/arch.md) leaves the full Load command pipeline and selection-aware Parse items unchecked. [GithubTransportActor.fs](src/Server/GithubTransportActor.fs) does call `LazyLoadReconciliationServer.reconcileWorkspace`, and the Actor test proves that continuation is invoked. It does not prove the later Browser `parseFileOp` / Fetch+Poll path or selection behavior. The current checkmarks are defensible only if the ticket means the Server reconciliation boundary; a short clarification would prevent readers from treating it as completion of the work owned by 14 — Route Load and Save by path pre-pick or 06 — Selection-scoped Parse after whole-tree git Load.

## 3. Verification

1. **Focused Peer Actor and WorkspaceGit tests** — `dotnet test tests/Server.Tests/Gambol.Server.Tests.fsproj --filter "FullyQualifiedName~GithubTransportActorTests|FullyQualifiedName~WorkspaceGitTests"` passed 37 of 37.
2. **Full solution gate** — `dotnet test gambol.sln` passed Cloud Agents 62 of 62 and Server 553 of 553. Shared passed 1,722, failed 1, and skipped 1. The failure is the pre-existing [AmbDocumentTests.fs](tests/Shared.Tests/AmbDocumentTests.fs) test `read ambiguous owner-link candidates keeps map order`, outside this PR’s diff and ticket seams.
3. **Mechanical standards scan** — F# function and file limits passed. The scan found only the number-only references in [Peer Actor code map for 13 — Run git Load and Save through the Server Peer Actor](plan/github-transport/reports/peer-actor-code-map-2026-09-27.md).

## 4. Summary

Standards: one Must-fix and one Nice-to-have; worst issue is the required name-reference rule violation. Specification: no Must-fix and one Nice-to-have; the Peer Actor behavior, pool registry, gate, Workspace scope, Parse continuation, Persist independence, reject shape, credential boundary, Status, and architecture scope are Good.
