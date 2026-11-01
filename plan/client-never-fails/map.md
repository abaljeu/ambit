# Client never fails on server data

## 1. Problem

The Browser can refuse server data. Then it stops Sync and shows "Data changed on server — click to reload". Only a reload clears that state. The Server sends every Event in order, so a refusal is a gap in the Browser or a Server bug. It is not missing data.

The Browser Graph is three collections:

- ids: the NodeId set (possibly not stored)
- nodes: map from NodeId to Node
- children: map from NodeId to NodeId list option

Today the Server can send a Node without its child list. That Node is Unloaded: it is in nodes and not in children. The word is confused with the Load process. A NodeId in a child list that is not in nodes is Absent. Today the Browser refuses that case with "new child not found".

The Browser refuses server data in these places:

- 1 — Notices: a Notice is a logged Event, so it moves the server event id. Each Change inside a File Node Body makes the Server log NodeUnpersisted, NodePersisting, and NodePersisted. A Poll that carries only these Notices while the edit box holds new text sets DataOutdated (`Update.fs` PollDone, `isAutoSyncBlocked`). A Load reply that follows only Notices is refused with "raced Load answer" (`SyncLogic.applyLoadResponse`). A Notice that `NoticeGraph.writeGraph` rejects stops the Poll (`SyncLogic.applyNoticeGraph`).
- 2 — Moved child the Browser never had: a move under a parent with its child list names a child that is not in nodes.
- 3 — Pending changes during a Poll: the Browser refuses the Poll Events while it holds changes it has not posted.
- 4 — Typing during a server change: the Browser refuses the Events while the edit box holds text the Graph does not.
- 5 — Edit during a Load: an expand followed by an edit sets the same refusal as 4.
- 6 — Raced Load answer: a Load reply with no Change Events is refused with "raced Load answer" when the event id moved.
- 7 — Server write order: the Server writes the Graph before the Event. A crash between the two leaves the Graph ahead of EventLog. After a restart the Server reports an event id with no Events behind it.

## 2. Solution

The Browser applies every server reply. Each fix removes a refusal where it starts.

- Remove Loaded and Unloaded: each state return for a NodeId includes both its Header and its child list. A child list holds ids only. A Resident Node always has its child list, so Loaded and Unloaded go away.

- Absent display: an Absent child shows as a placeholder Bullet drawn in color #444444. It selects like any row. Expand of a placeholder requests its Node. When its Node arrives, the next render draws it as a normal row. Commands that won't run with an absent node:
  - Edit
  - Copy
  - Cut
  - Edit classes
  - Exec
- Absent Fetch: Poll is the one mechanism that gets data. The active process that looks for nearby Nodes looks for Absent ids, not Unloaded Nodes. Expand of a Node makes its Absent children nearby. The whole-Workspace request is unhooked and deleted.
- 1 — Notices: when the Browser has no pending changes, it accepts each Notice. It applies the Notice and takes its event id. A Notice never blocks the Browser, never sets DataOutdated, and never refuses a Load reply. A post reply applies its Notices too.
- 2 — Moved child: the Browser accepts the child list as the Server sends it. Its Absent children show as placeholders.
- 3 — Protocol: the Browser posts every change. The Server merges and sends back revised Events. A post reply undoes the posted changes and replays the server Events. A Poll is the same with zero changes to undo, so a Poll always applies. A post reply with our own changes as Events is accepted. A post reply with different changes while more changes are pending is a merge in progress, not a reload. The Browser ignores that reply, posts the rest, and waits for a clean merge.
- 4 — Typing: the Browser posts the edit after a one-second pause in typing. Edit mode stays on. When a Poll removes the Node in edit mode, the Browser leaves edit mode and the view renders as usual.
- 5 — Expand is not a change. An edit during a Load is case 4.
- 6 — Load: the Browser sends Load and does not wait on it. The "raced Load answer" guard is deleted. A Load reply applies like any server reply.
- 7 — Server write order: the Server writes the Event first, then the Graph. Then the Server never reports an event id with no Events behind it, and the Browser needs no handling for that case.
- Reload prompt: "Data changed on server — click to reload" is removed after 1 to 6 are fixed. "New version available" stays, because the protocol can change.

## 3. Scope

This Project covers:

- the placeholder display and Fetch for an Absent child
- the Browser refusals 1 to 6
- the server write order 7
- the removal of Loaded and Unloaded
- removal of the "Data changed on server" prompt

"New version available" and the Server refusal of a Browser post stay as they are. A separate ticket repurposes the hollow circle to mark Absent Nodes.
