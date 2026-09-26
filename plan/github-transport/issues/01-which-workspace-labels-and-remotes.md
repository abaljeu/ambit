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

Pull and push the **same** tracked branch (FF-only already locked on Destination).

Checkout / switch branch / older commits are **out of this chart** (future). See [[../map.md]] Out of scope.

Map gist: [[../map.md]] Decisions so far item 7.

## Comments

- 2026-09-26: Alan locked in chat. Status `done`. Prefer Workspace, not label. Server-git vs desk is remote-exists.

## Time

- 2026-09-26 5m — recorded lock from chat
