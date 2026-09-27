# 02 — Graph handoff

**Type:** grilling
**Status:** defined
Blocked by: None

## 1. Question

How does an Actor receive Graph at start: full Graph plus start ids, or a remaining extract? Who holds the live Graph versus a snapshot?

Today ActorStart carries `graphIds` (Included expand of Zoom). CoreActorPool ([[src/Server/Core/CoreActorPool.fs]]) requires that extract and looks up Command inside those ids. Provisional aim (Alan 2026-09-27 chat, not yet a Decision): stop requiring a subgraph extract; handing an immutable Graph is O(1) reference share. Start ids (Zoom, Focus, Command or equivalent) select where work begins.

Grill:

1. **Full Graph** — Is the start Graph Core's live Graph reference, a snapshot at ActorStart, or a later snapshot the Actor Polls?
2. **Start ids** — Which ids are required (Zoom, Focus, Command)? What is equivalent if Command is not a Node?
3. **Remaining extract** — Does any extract remain (Included, File Node body, Workspace work tree)? If yes, who builds it — Command, Actor, or Core?
4. **Live vs snapshot** — After start, does the Actor keep the handed Graph, Poll Events and apply locally, or ask Core for State again?

Do not implement.

## Comments

- 2026-09-27 — Filed with the [Actor as client](../map.md) chart. Status `defined`.
