# 24 — Command Load/Save

**Status:** `defined`
**Type:** coding
**Blocked by:** 10 — Add discoverImmediateMembers, 11 — Load reconciles walk one level, 12 — Git pull marks each received file Unparsed.
**Trackers:** architecture, project spec

## Context

A person uses the Load Command and the Save Command. The spoken names are Load and Save. A Command is a user operation. A Command is not sent. The Workspace DataDir work tree is a git work tree. These Commands do not bring files from GitHub. These Commands do not send files to GitHub. Upload and Download move files between the App and the Server. Upload and Download do not talk to the GitHub remote. When the work tree has a remote, no Server Actor pulls the tracked branch from Load or Save. No Server Actor pushes the tracked branch from Load or Save.

PathPick chooses git when a remote exists. PathPick chooses desk when a remote does not exist. The Server Actor does pull and push. Desk Load and desk Save are the desk path. 26 — Desk WebDAV keeps desktop-mapped WebDAV. The core-refinement Project is the home for how Core works through changes. 09 — Grill reconcile discovery depth is done. This ticket waits on 10 — Add discoverImmediateMembers, 11 — Load reconciles walk one level, and 12 — Git pull marks each received file Unparsed.

## What to build

24 — Command Load/Save keeps the spoken names Load and Save. A Command is not sent. Plain Load asks PathPick. Plain Save asks PathPick. An explicit git Load pre-pick skips PathPick. An explicit git Save pre-pick skips PathPick. An explicit desk Load pre-pick skips PathPick. An explicit desk Save pre-pick skips PathPick. The Load command sends a core message through the mailbox to the Actor pool. Run does not start that door. A ?git entrée does not start that door. Save uses that same door. When the path is git, the mailbox starts the Server Actor. The mailbox posts Events. Load informs Core after files land. The core-refinement Project works through those changes. No schedule starts this work. No post-Persist step starts this work. No post-Download step starts this work.

### 1. Command Load/Save

Command Load/Save is the person Command for Load and Save. A plain path asks PathPick. An explicit git or desk pre-pick skips PathPick. A git path uses the Server Actor through the mailbox and the actor pool. A desk path uses Desk Load/Save. The App stays thin. Load informs Core after the files land.

1. [ ] **Spoken names** — The person invokes Command Load and Command Save. The spoken names stay Load and Save. A Command is a user operation. A Command is not sent.
2. [ ] **Core message** — The Load command sends a core message through the mailbox to the actor pool. Save uses that same door. Load sends a core message, not a Command or an Event. The mailbox posts Events. A Node does not send a message, a Command, or an Event.
3. [ ] **Start door** — 07 — Actor start door is this door. The actor pool starts the Server Actor when the path is git. Run does not start the Server Actor. A ?git entrée does not start the Server Actor.
4. [ ] **Path pre-pick** — The core message carries a path pre-pick of Plain, Git, or Desk. The explicit pre-picks are git Load, git Save, desk Load, and desk Save. 02 — Actor command surface locks these pre-picks. Load and Save stay the command names.
5. [ ] **Plain Load** — Plain Load asks PathPick. Plain Load of a Workspace Node uses the PathPick result. A git result starts the Server Actor through the actor pool. A desk result uses Desk Load/Save. Plain Load of a File Node stays Desk. Plain Load of a Directory Node stays Desk. A remote on the Workspace does not start the Server Actor for that File Node or that Directory Node.
6. [ ] **Plain Save** — Plain Save asks PathPick. A git result uses the same door as Load, and the actor pool starts the Server Actor. A desk result uses Desk Load/Save.
7. [ ] **Explicit pre-pick** — An explicit git or desk pre-pick skips PathPick. Explicit git Load and explicit git Save start the Server Actor through the actor pool. The Server Actor uses Focus as the Workspace work tree. 05 — Git Load/Save are Workspace-scoped keeps that work tree for a subnode. Explicit desk Load and explicit desk Save use Desk Load/Save.
8. [ ] **Load subject** — The subject of a Load core message is one Workspace Node, one Directory Node, or one File Node. Nodes inside one File Node share that File Node as the single subject. The subject list is the whole selection. Several selected nodes in one File Node make one core message.
9. [ ] **Desk path** — The desk path uses Desk Load/Save. 26 — Desk WebDAV keeps desktop-mapped WebDAV. WebDAV Upload and WebDAV Download stay on that path. This ticket leaves that seam as it is.
10. [ ] **Inform Core** — Load informs Core after the files land. On the git path, Load informs Core after the pull lands. On the desk path, Load informs Core after the desk files land. Fetch and Poll stay. The core-refinement Project owns how Core works through the changes. The parse-thread Project stays the home of the parse thread. This ticket does not specify the Parse stack or the Persist stack.
11. [ ] **No automatic start** — 03 — When pull and push fire locks person Commands only. A schedule does not start a git pull or a git push. A post-Persist step does not start a git pull or a git push. A post-Download step does not start a git pull or a git push. The person starts the work with Load, Save, or an explicit git or desk pre-pick.
12. [ ] **App** — The App stays thin. The Server hosts the Server Actor. The Load or Save core message reaches the mailbox on the Server.

## Finish-up

1. [ ] **Piece answers** — Changes during implementation are written on this ticket, or the ticket records that none changed.
2. [ ] **Basis and reference docs** — The ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
3. [ ] **Current and user docs** — Where those sources reference current docs or user docs, update those referents to match the finished state of this ticket. Delete statements that are no longer true. Check off statements that are newly achieved.

## See also

1. [github-transport spec](../spec.md)
2. [github-transport architecture](../arch.md)
3. [02 — Actor command surface](02-actor-command-surface.md)
4. [03 — When pull and push fire](03-when-pull-and-push-fire.md)
5. [05 — Git Load/Save are Workspace-scoped](05-git-load-save-workspace-scoped.md)
6. [07 — Actor start door](07-actor-start-door.md)
7. [core-refinement](../../core-refinement/project.md)
8. [Parse thread](../../parse-thread/project.md)
9. [09 — Grill reconcile discovery depth](../../parse-thread/issues/09-grill-reconcile-discovery-depth.md)
10. [10 — Add discoverImmediateMembers](../../parse-thread/issues/10-add-discover-immediate-members.md)
11. [11 — Load reconciles walk one level](../../parse-thread/issues/11-load-reconciles-walk-one-level.md)
12. [12 — Git pull marks each received file Unparsed](../../parse-thread/issues/12-git-pull-marks-each-received-file-unparsed.md)
