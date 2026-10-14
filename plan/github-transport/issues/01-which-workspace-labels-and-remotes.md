# 01 — Which Workspaces and remotes

**Type:** grilling
**Status:** done
Blocked by: None
Actual: 5m

## 1. Question

- [x] Which Workspaces (DataDir work trees) connect to which GitHub remotes, and where does that config live?

The Destination says GitHub is the outside channel for key repos, and the Workspace’s DataDir work tree **is** the git home. It does not say which Workspaces, how many remotes per Workspace, or whether the operator’s `git remote` on that work tree is the only config.

Grill: one Workspace vs many, one remote vs many, config in git itself vs Server settings vs a Graph Node. Do not implement.

## 2. Answer

Locked 2026-09-26 (Alan, chat).

Prefer **Workspace**, not “label.” Every Workspace’s DataDir work tree is a git work tree.

Any / every Workspace connects (all have git). Server-git applies when a remote exists. No allowlist. No special label.

Config lives in git on that work tree: `git remote` + current branch / upstream. **No separate Server branch map** in v1.

Pull and push the **same** tracked branch. Fast-forward push and remote-wins pull are locked on [Decisions so far item 2 — Operator sets remote and credentials](../map.md) and on [Solution item 4 — Fast-forward push, remote-wins pull](../spec.md). An edit-versus-delete is remote-wins, the same rule as a content conflict: a Server edit and a remote delete remove the file; a Server delete and a remote edit leave the remote file. No conflict markers.

Checkout / switch branch / older commits are **out of this chart** (future). See [[../map.md]] Out of scope.

Map gist: [[../map.md]] Decisions so far item 7.

## Comments

- 2026-09-26: Alan locked in chat. Status `done`. Prefer Workspace, not label. Server-git vs desk is remote-exists.
- 2026-10-14: The parenthetical that treated Destination as fast-forward for pull is replaced. Pull and push still share the tracked branch. Pull is the remote-wins merge. An edit-versus-delete is remote-wins, the same rule as a content conflict: a Server edit and a remote delete remove the file; a Server delete and a remote edit leave the remote file. No conflict markers.

## Time

- 2026-09-26 5m — recorded lock from chat
