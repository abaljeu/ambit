# 02 — Actor command surface

**Type:** grilling
**Status:** done
Blocked by: None
Actual: 5m

## 1. Question

- [x] What Commands does the Server Actor receive for remote, pull, and push?

The Destination locks a Server-side Peer Actor that does pull and push (round-trip v1). It does not lock the command names or whether a person in the Browser starts them.

Prior spec [[plan/workspace-git/project.md]] proposes Git Remote, Git Push, and Git Pull on WorkspaceGit. Grill that surface as a candidate. Do not inherit that spec’s non-FF accept. Do not implement.

## 2. Answer

Locked 2026-09-26 (Alan, chat).

Person-facing Commands: **Load** and **Save** (same Ambit command names).

Explicit secondary pre-picks: **git Load** / **git Save** and **desk Load** / **desk Save**.

Plain Load/Save = git* when valid (a remote exists), else desk*.

Do **not** inherit old workspace-git’s Git Remote / Git Pull / Git Push as the primary surface (those names are not the lock). Do not inherit non-FF accept.

Map gist: [[../map.md]] Decisions so far item 8.

## Comments

- 2026-09-26: Alan locked in chat. Status `done`. Load/Save plus git* and desk* pre-picks. workspace-git command names are not the lock.

## Time

- 2026-09-26 5m — recorded lock from chat
