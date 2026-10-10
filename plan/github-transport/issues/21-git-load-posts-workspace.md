# 21 — Git Load posts the Workspace

**Status:** `done`
**Actual:** `2h`
**Type:** coding
**Blocked by:** 23 — Directory or File Load posts to the parse stack
**Trial step:** 3. Next: 22 — Drop the Browser git Load after-step.
**Trackers:** project spec

## Context

A person runs git Load on a Workspace. The Actor pulls the tracked branch of that Workspace work tree. On that same turn, `runLoad` calls `reconcileWorkspace`, then `parseFocusFile`. Desk Directory Load and desk File Load post their own nodes in 23 — Directory or File Load posts to the parse stack. This ticket leaves those desk posts as they are. `requeueOnUnparsed` stays false. Pacing stays 25 — Pace server functions. The Browser after-step stays until 22 — Drop the Browser git Load after-step.

## What to build

After the pull, git Load posts that one Workspace node. The person still gets the tracked-branch pull. `parseFocusFile` stays on the Actor turn. The desk posts stay on 23 — Directory or File Load posts to the parse stack.

### 1. Git Load posts the Workspace

Module **Server Actor** pulls the whole Workspace work tree on the tracked branch. Focus names that work tree. The pull stays that whole work tree. Story path **git Load** informs Core after the pull. This ticket makes that inform a post of the one Workspace node. The workspace lock stays on core-refinement architecture.

The Load command sends a core message. The mailbox posts Events. The parse push is `markUnparsedThenPush`. That push marks the Workspace node Unparsed, then pushes that id. The mark writes the axis and does not enqueue (core-refinement architecture, **Parse push marks Unparsed**). `requeueOnUnparsed` stays false. parse-thread architecture **Load door** accepts a Workspace node. **Directory reconcile** walks that Workspace node after the post. This ticket posts the node. The walk stays on the parse thread. Pull does not start a second Actor (parse-thread architecture, **No Actor after pull**).

No new POST endpoints keeps the post on the existing mailbox door. The mailbox modules are `CoreMailboxEvents`, `CoreMailboxActors`, and `CoreMailboxLoad`. Core mailbox messages clear fast keeps the post a fast message. The Workspace walk stays off the Actor turn.

`runLoad` pulls, then calls `dependencies.continueLoad`, then `parseFocusFile`. `productionDependencies` sets `continueLoad` to `reconcileWorkspace`. That walk runs on the Actor turn. `parseFocusFile` calls `planParseFile` on that same turn.

1. [x] No inline reconcile — `continueLoad` does not call `reconcileWorkspace`. The function stays in its module.
2. [x] Workspace post — git Load posts that Workspace id through `markUnparsedThenPush`. The post is one node.
3. [x] File parse stays — The `parseFocusFile` call and `planParseFile` stay as they are.
4. [x] Pull stays — The tracked-branch pull still runs.
5. [x] Flag stays off — `requeueOnUnparsed` stays false.

### 2. Proof

1. [x] Workspace post test — A git Load posts that Workspace id and does not call `reconcileWorkspace`.

## Finish-up

1. [x] Piece answers — changes during implementation are written on this ticket, or the ticket records that none changed.
2. [x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.

## See also

1. [github-transport spec](plan/github-transport/spec.md) — story **git Load** and story **Inform Core after files land**.
2. [github-transport architecture](plan/github-transport/arch.md) — module **Server Actor** and story path **git Load**.
3. [core-refinement architecture](plan/core-refinement/arch.md) — the workspace lock and **Parse push marks Unparsed**.
4. [parse-thread architecture](plan/parse-thread/arch.md) — **Load door**, **Directory reconcile**, and **No Actor after pull**.
5. [No new POST endpoints](doc/Decisions/0005-no-new-post-endpoints.md) — the post uses the existing mailbox door.
6. [Core mailbox messages clear fast](doc/Decisions/0004-core-mailbox-messages-clear-fast.md) — the post stays a fast message.
7. [23 — Directory or File Load posts to the parse stack](23-directory-file-load-parse-stack.md) — desk Directory Load and desk File Load.
8. [22 — Drop the Browser git Load after-step](22-drop-browser-git-load-after-step.md) — the Browser after-step stays until this ticket.
9. [25 — Pace server functions](25-pace-server-functions.md) — pacing stays this ticket.

## Comments

1. Post door — git Load sends mailbox Load for the enclosing Workspace Node. The actor handle is `CoreChanges.load`. That call is `CoreMailbox.load`. The push is `markUnparsedThenPush`. The post is one id.
2. Pull error — A failed pull ends the git actor. That path posts no Workspace Node.
3. Reconcile — `reconcileWorkspace` stays in its module. The git Load path omits that call. `continueLoad` is gone from the actor dependencies.
4. Spec text — The project spec stays as written. Story **git Load** and story **Inform Core after files land** still say inform Core after the pull lands.

## Time

- 2026-10-10 1h30m — git Load posts the Workspace (from chat)
- 2026-10-10 30m — a failed pull ends the git actor (from chat)
