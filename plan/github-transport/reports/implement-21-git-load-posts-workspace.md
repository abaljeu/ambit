# 1. Implement 21 — Git Load posts the Workspace

## 1. Ticket

1. Name — [21 — Git Load posts the Workspace](plan/github-transport/issues/21-git-load-posts-workspace.md)
2. Path — `plan/github-transport/issues/21-git-load-posts-workspace.md`

## 2. Binding architecture

Binding architecture agreed. Coding proceeded.

1. [github-transport architecture](plan/github-transport/arch.md) — story path **git Load**, module **Server Actor**, seam **Server Actor git Load/Save**. The pull stays on the whole Workspace work tree. The inform after the pull is the Workspace Node post.
2. [core-refinement architecture](plan/core-refinement/arch.md) — **Parse push marks Unparsed**. The mark writes the axis and leaves the enqueue flag false. The workspace lock stays on that architecture.
3. [parse-thread architecture](plan/parse-thread/arch.md) — **Load door** accepts a Workspace Node. **Directory reconcile** walks that node on the parse thread. **No Actor after pull** stays: the post is mailbox Load, and the pull starts no second Actor.

## 3. What was built

git Load pulls the tracked branch, then posts that one Workspace Node, then parses a focused File. `reconcileWorkspace` stays in its module. The git Load path omits that call.

1. [GithubTransportActor.fs](src/Server/GithubTransportActor.fs) — `runLoad` posts the enclosing Workspace id through `CoreChanges.load`. `parseFocusFile` and `planParseFile` stay. The tracked-branch pull stays. `continueLoad` is removed. `productionDependencies` calls the git facts only.
2. [CoreChanges.fs](src/Server/Core/CoreChanges.fs) — the actor handle gains `load` for one node id.
3. [CoreMailbox.fs](src/Server/Core/CoreMailbox.fs) — `coreChanges` wires `load` to the existing `CoreMailbox.load` door. That door runs `markUnparsedThenPush`.
4. [CoreRuntime.fs](src/Server/Core/CoreRuntime.fs) — the read-only handle rejects `load`.
5. [RouteRegistration.fs](src/Server/RouteRegistration.fs) — production dependencies are the git facts only.
6. Server tests — the Workspace post test records one pushed id, the Workspace id. Actor and Load/Save tests follow the new dependencies. Test records that only constructed `CoreChanges` gained the `load` field.

The post uses the same slot as the old `continueLoad` call. When the pull returns an error, the post still runs, and the actor result is that pull error.

## 4. Tests

1. `scripts/gitstatus.sh` — workplace `dev`. The tree was already dirty. Those other edits were left alone.
2. `dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~posts that Workspace"` — fail. `netcoredbg.exe` held Debug outputs. The command started. The copy into `src/Server/bin` failed. A later build of the same project succeeded.
3. `dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~GithubTransportActorTests"` — pass. 8 passed, 0 failed.
4. `dotnet test tests/Server.Tests -c Debug --no-build --filter "FullyQualifiedName~LoadSaveCommandTests"` — pass. 11 passed, 0 failed.
5. `scripts/test.sh all` — pass. Server tests: 644 passed, 0 failed. Shared tests: 1870 passed, 1 skipped, 0 failed.
6. Client compile gate — not run. This change edits Server only.

## 5. Code review

Range: `git diff HEAD` limited to the files in section 3. Pre-existing dirty files outside that list were left out of the review.

Mechanical scan stdout:

```
--- measure-fs-size ---
src/Server/GithubTransportActor.fs::productionDependencies: lines 212-213 (2 lines)
```

That line is under the function limit. It is not a finding.

### 1. Standards

No findings.

### 2. Spec

1. Pull error — `runLoad` posts the Workspace Node when `pullTracked` returns an error, then returns that pull error. [21 — Git Load posts the Workspace](plan/github-transport/issues/21-git-load-posts-workspace.md) puts the post after the pull. Story **git Load** and story **Inform Core after files land** in the project spec inform Core after the pull lands and files changed. A failed pull still marks that Workspace Node Unparsed and pushes it.
2. Handle member — `CoreChanges` gains `load`. The ticket names `markUnparsedThenPush` on the existing mailbox Load door. The member name is new on the actor handle. The member is the actor's call to that door.

### 3. Arch

No findings. Every touched rule holds. The step 4 re-check found no contradiction.

Totals: Standards 0. Spec 2, worst is the post on a failed pull. Arch 0.

## 6. Ticket status

**Status:** `coded`. The arch re-check found no contradiction. Review approval is what sets `done`.

## 7. Time

1. Ticket — `## Time` on [21 — Git Load posts the Workspace](plan/github-transport/issues/21-git-load-posts-workspace.md): 2026-10-10 1h30m (from chat). **Actual:** `1h30m`.
2. Project — [github-transport](plan/github-transport/project.md) `Started:` is 2026-09-27. `Finished:` is unset while the project is live. `Actual:` is 16h15m (prior 14h45m plus 1h30m). `Stage:` is `build`. `Updated:` is 2026-10-10.

## 8. Left for the human

1. Commit — no commit. Git protocol commits on `dev` only when Alan asks. Nothing was pushed or merged.
2. Failed pull — Alan can say whether the post should wait for a successful pull. The code posts in the old `continueLoad` slot, including when the pull returns an error.
3. Pre-existing dirty files — left unstaged and unedited by this run, except the github-transport files this ticket and the project stage required.
