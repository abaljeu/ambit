# 22 — Drop the Browser git Load after-step

**Status:** `done`
**Type:** coding
**Actual:** `45m`
**Blocked by:** 21 — Git Load posts the Workspace
**Trackers:** architecture, project spec

## Context

[ ] A person invokes git Load.
[ ] The Server Actor pulls the whole Workspace work tree.
[ ] When the files land, Load informs Core.
[ ] After git Load returns, the Browser runs a second reconcile or parse.
[ ] That second reconcile or parse is the git Load after-step in the Browser.
[ ] The App is thin.
[ ] The App does not host the Server Actor.

## What to build

[x] After git Load returns, the Browser does not run a second reconcile or parse.
[x] The App stays thin.
[x] Core was already informed when the files landed.

### 1. Drop the Browser git Load after-step

[x] This capability is on the App.
[x] The App stays thin.
[x] The App does not invoke git.
[x] The App does not host the Server Actor.
[x] The App has no git remote map.
[x] When the files land, the Server Actor informs Core.
[x] After git Load returns, the Browser does not run a second reconcile.
[x] The Browser does not run a second Parse.
[x] Fetch and Poll stay.
[x] Desk WebDAV stays.
[x] Desk Load and desk Save continue to use Desk WebDAV.
[x] This Project does not specify the Parse stack.
[x] This Project does not specify the Persist stack.
[x] The core-refinement Project is the home of those stacks.
[x] The parse-thread Project is the home of the parse thread.

## Finish-up

[x] Piece answers — changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.

## See also

[github-transport spec](../spec.md)
[github-transport architecture](../arch.md)
[21 — Git Load posts the Workspace](21-git-load-posts-workspace.md)
[core-refinement Project](../../core-refinement/project.md)
[parse-thread Project](../../parse-thread/project.md)

## Comments

1. After-step — A git Load response completes the command. The Browser does not start a second reconcile or a second Parse. Desk Load and desk Save still continue on Desk WebDAV.
2. Server Actor — That Actor is unchanged. When the files land, it still informs Core.
3. Basis — Spec stories git Load and Inform Core after files land stay as written. Architecture module App already keeps the App thin. Fetch and Poll stay. This ticket does not specify the Parse stack or the Persist stack.

## Time

- 2026-10-10 45m — drop the Browser git Load after-step (from chat)
