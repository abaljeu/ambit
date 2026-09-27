# 13 — Run git Load and Save through the Server Peer Actor

**Status:** defined
**Blocked by:** [12 — Run the Workspace git tracked-branch round-trip](12-workspace-git-tracked-branch-round-trip.md)

## Context

A person has started a git Load or git Save for a Workspace. The Server must run the request as a live Peer Actor through the actor pool. The Actor receives Focus, uses the Workspace work tree, and gives every device the same Server-hosted path without making the App a git host.

## What to build

### 1. Peer Actor

Build the **Peer Actor** from the Module map in [[../arch.md]]. This capability supports the **git Load**, **git Save**, **Load keeps Parse**, **Server Peer Actor does the round-trip**, **One Actor shape through Server**, **Actor start door**, and **Reject UX** Story paths.

- [ ] 4.1.1 Keep only live Actor state — The Actor row exists only while a person-started git Load or git Save runs.
- [ ] 4.1.2 Store no GitHub credential — The Actor has no durable or live Ambit credential field.
- [ ] 4.2.1 Use the Load/Save start door — A load/save command request reaches the mailbox and actor pool; the pool invokes the Peer Actor, not Run or a `?git` entrée.
- [ ] 4.2.2 Resolve the work tree from Focus — The Actor uses Focus only to identify the Workspace work tree; a subnode does not narrow git scope.
- [ ] 4.2.3 Use one door shape — Save uses the same mailbox-to-pool wiring as Load.
- [ ] 4.2.4 Run git Load — The Actor asks WorkspaceGit to pull the whole tracked branch, then continues through today's Load → Parse / graph-push coupling without expanding the later selection-parse nuance.
- [ ] 4.2.5 Run git Save — The Actor asks WorkspaceGit to commit all work-tree edits and then push the whole tracked branch; it does not invoke Persist.
- [ ] 4.2.6 Return matching rejects — Load and Save return the same condensed git error shape, and a conflict names at least one file path.
- [ ] 4.2.7 Serve every mapped device — The same Server Actor shape handles each device that maps through Server.
- [ ] 4.2.8 Use WorkspaceGit without a token — The Actor invokes the WorkspaceGit interface and supplies no Ambit credential.
- [ ] 4.3.1 Use the actor pool — The implementation uses CoreActorPool and ActorFn for lifecycle and invocation.
- [ ] 4.3.2 Depend on WorkspaceGit — Tests stub WorkspaceGit and prove pull for Load and commit-then-push for Save.
- [ ] 4.3.4 Preserve Load completion — Tests prove git Load continues to the existing Parse / graph-push hop after files land, with the selection behavior left at its current v1 boundary.

## See also

- [github-transport architecture](../arch.md)
- [07 — Actor start door](07-actor-start-door.md)
