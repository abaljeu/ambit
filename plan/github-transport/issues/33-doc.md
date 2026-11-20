# 33 — File Load leaves the File Unparsed

Status is `coded`.

## 1. Current

## 2. User

[[../../../doc/user/reference/commands/files.md]]

[x] A git pull happens only when the Load subject is a Workspace.
[x] Workspace Load runs the transport step (desk Load when only a local mapping exists, git Load pull when a git remote exists, nothing otherwise), then reconciles; a failed step aborts with no reconcile.
[x] ROOT is a Workspace with no work tree, so its Load runs no transport and then reconciles.
[x] Any Load (plain or git Load) of a File, a Directory, or another non-Workspace node does no git pull or push and only reconciles that selected node from disk.

## 3. Found elsewhere

[[../spec.md]]

[[../arch.md]]

[[../map.md]]

[[../project.md]]

[[33-file-load-leaves-file-unparsed.md]]

[[../../actor-as-client/issues/23-run-the-load-actor-for-git-and-neither/23-run-the-load-actor-for-git-and-neither-ticket.md]]

[x] Spec, architecture, map, project notes, 33 — File Load leaves the File Unparsed, and 23 — Run the Load actor for Git and Neither state the Workspace-only git pull rule, the `(desk | git | nothing)` Workspace Load transport step followed by reconcile, abort-on-failure before reconcile, ROOT as a Workspace Node with no work tree, and reconcile-only routing for any non-Workspace Load (plain or explicit git).
[x] User Story 2 and Solution 10 in the github-transport spec cite `PathPick.resolveCommand` and `PathPick.plainLoadPath`.
[x] Known out-of-scope desk limitations (Desktop `DesktopPush` on File/Directory plain and desk Load, `DeskLoadPush.subjectToLoad` returning `None` for non-subject desk Load, and local-only Workspace plain Load taking Neither without desktop upload) are recorded as known limitations, not intended design.
[x] 33 — File Load leaves the File Unparsed follows the ticket template with status `coded` and present-situation text.
