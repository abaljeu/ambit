# 13 — Run git Load and Save through the Server Peer Actor

**Type:** coding
**Status:** done
**Blocked by:** [12 — Run the Workspace git tracked-branch round-trip](12-workspace-git-tracked-branch-round-trip.md)
Actual: 1h35m

## Context

A person has started a git Load or git Save for a Workspace. The Server must run the request as a live Peer Actor through the actor pool. The Actor receives Focus, uses the Workspace work tree, and gives every device the same Server-hosted path without making the App a git host.

## What to build

### 1. Peer Actor

Build the **Peer Actor** from the Module map in [[../arch.md]]. This capability supports the **git Load**, **git Save**, **Load keeps Parse**, **Server Peer Actor does the round-trip**, **One Actor shape through Server**, **Actor start door**, and **Reject UX** Story paths.

- [x] 4.1.1 Keep only live Actor state — The Actor row exists only while a person-started git Load or git Save runs.
- [x] 4.1.2 Store no GitHub credential — The Actor has no durable or live Ambit credential field.
- [x] 4.2.1 Use the Load/Save start door — A load/save command request reaches the mailbox and actor pool; the pool invokes the Peer Actor, not Run or a `?git` entrée.
- [x] 4.2.2 Resolve the work tree from Focus — The Actor uses Focus only to identify the Workspace work tree; a subnode does not narrow git scope.
- [x] 4.2.3 Use one door shape — Save uses the same mailbox-to-pool wiring as Load.
- [x] 4.2.4 Run git Load — The Actor asks WorkspaceGit to pull the whole tracked branch, then continues through today's Load → Parse / graph-push coupling without expanding the later selection-parse nuance.
- [x] 4.2.5 Run git Save — The Actor asks WorkspaceGit to commit all work-tree edits and then push the whole tracked branch; it does not invoke Persist.
- [x] 4.2.6 Return matching rejects — Load and Save return the same condensed git error shape, and a conflict names at least one file path.
- [x] 4.2.7 Serve every mapped device — The same Server Actor shape handles each device that maps through Server.
- [x] 4.2.8 Use WorkspaceGit without a token — The Actor invokes the WorkspaceGit interface and supplies no Ambit credential.
- [x] 4.2.9 Acquire and release the work-tree gate — The Actor acquires the Workspace gate before git Load pull or git Save commit and releases it after that work-tree change; a second caller waits until release. **Superseded 2026-09-28.** [16 — Persist/git work-tree gate](16-persist-git-work-tree-gate.md) revoked that exclusive gate (#151). Unparsed / Unpersisted replace it ([19 — Parsed/Unparsed and Persisted/Unpersisted](19-file-newer-graph-newer.md)). This line records what shipped; it is not current required acceptance.
- [x] 4.3.1 Use the actor pool — The implementation uses CoreActorPool and ActorFn for lifecycle and invocation.
- [x] 4.3.2 Depend on WorkspaceGit — Tests stub WorkspaceGit and prove pull for Load and commit-then-push for Save.
- [x] 4.3.4 Preserve Load completion — Tests prove git Load continues to the existing Parse / graph-push hop after files land, with the selection behavior left at its current v1 boundary.

## See also

- [github-transport architecture](../arch.md)
- [07 — Actor start door](07-actor-start-door.md)
- [16 — Persist/git work-tree gate](16-persist-git-work-tree-gate.md)

## Time

- 2026-09-27 1h30m — implemented and tested the peer-only mailbox door, Server Peer Actor, WorkspaceGit composition, work-tree gate order, Load Parse continuation, and matching rejects
- 2026-09-28 5m — annotated 4.2.9 exclusive-gate acceptance as superseded by Unparsed/Unpersisted (#151 / [16 — Persist/git work-tree gate](16-persist-git-work-tree-gate.md) revoke)
