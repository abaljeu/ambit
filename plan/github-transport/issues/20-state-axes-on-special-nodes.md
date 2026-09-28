# 20 — State axes on special nodes

**Type:** coding
**Status:** defined
**Blocked by:** None — can start immediately

## Context

A person works a Graph that already has Workspace, Directory, and File special nodes. Later Parse (disk → Graph) and Persist (Graph → disk) need two independent markers on those nodes. Today `DocumentState` is `Current` | `Unparsed` | `NoServerFile`. Parsed is the other pole of Unparsed (`Current` is today's name). Persisted | Unpersisted is not present. This ticket adds the axes as Graph state that can be set and read. It does not start workers.

## What to build

Special nodes carry **Parsed | Unparsed** and **Persisted | Unpersisted**. Code can set and read each axis on a Workspace Node, a Directory Node, and a File Node. No Parse actor, no Persist stack, no git Load retarget, no Upload or selection Parse, no path-control change, and no retirement of old hops.

### 1. Special-node state axes

Add the two axes as Graph markers on special nodes only. Point of lock: [19 — Parsed/Unparsed and Persisted/Unpersisted](19-file-newer-graph-newer.md). Migration home: [Here→There — step 1 locked](../here-to-there.md).

- [ ] 1.1 Carry Parsed | Unparsed — Each Workspace Node, Directory Node, and File Node has this axis. Parsed is the other pole of Unparsed.
- [ ] 1.2 Carry Persisted | Unpersisted — Each Workspace Node, Directory Node, and File Node has this axis. The axis is independent of Parsed | Unparsed.
- [ ] 1.3 Set and read both axes — Tests prove a special node can set and read each axis. No Parse or Persist work starts.
- [ ] 1.4 Leave workers unbuilt — Do not stand up the Parse actor or stack, the Core Persist stack, git Load retarget, Upload or selection Parse, path-control migration, or retirement of old hops.

## See also

- [19 — Parsed/Unparsed and Persisted/Unpersisted](19-file-newer-graph-newer.md)
- [Here→There — step 1 locked](../here-to-there.md)
