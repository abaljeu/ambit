# Name clash → `amb-conflict`

Worker report. Stage still `charting`. No software.

## Decision (accepted)

Concurrent `SetName` is **Merge success**, not HTTP Reject. Server arrival is first: that name stays on the Node. The newest Change drops `SetName` and **Add Node** as first child, class `amb-conflict`, text = the new name. Same family as same-text (`amb-conflict` child with the losing text).

The conflict child is a **Normal Node**. Conflict-ness is the `amb-conflict` name/role, not a Kind. Do not invent a Conflict Kind.

`amb-conflict` is still not an edge-edit device (children Accept Both stays bag approximation).

## Remaining Reject

Auth, malformed POST, and similar request failures. Name is not Reject.

## Files changed

- [[merge.md]], [[conflict-kinds.md]], [[vocab.md]], [[collab-vocab.md]], [[goal.md]] — Name + Normal Node
- This file
