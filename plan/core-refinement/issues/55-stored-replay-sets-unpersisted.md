# 55 — Stored replay sets Unpersisted

**Status:** `cancelled`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events
**Trackers:** architecture, project spec

## Context

Graph change apply has the Core-loop Node handlers. Stored replay applies stored Events and does not read files. Graph change stored replay marks the nearest owning special node Unpersisted and skips Node messages.

## What to build

Change the server and Browser stored-replay appliers. They apply Graph-changing Events and mark the nearest owning special node Unpersisted. They skip NodeUploading, NodeReceived, NodeParsing, NodeParsed, NodePersisting, and NodePersisted.

### 1. Stored replay

1. [ ] **Server stored replay** — Change `EventLog.recover` and its private `applyRecover` so that Events with an id ahead of the graph apply their Ops, mark each affected nearest owning special node Unpersisted, and skip NodeUploading, NodeReceived, NodeParsing, NodeParsed, NodePersisting, and NodePersisted.
2. [ ] **Browser stored replay** — Change `BootCache.foldLog` to receive the cached snapshot and stored Event delta, apply Graph-changing Events through `ResidentProjection`, and mark each affected nearest owning special node Unpersisted. The replay skips NodeUploading, NodeReceived, NodeParsing, NodeParsed, NodePersisting, and NodePersisted.

### Acceptance Criteria

1. [ ] **Replay test** — `stored replay sets Unpersisted and does not apply Node events`. The graph changes. NodeUploading, NodeReceived, NodeParsing, NodeParsed, NodePersisting, and NodePersisted from the stream are not applied.

## See also

- [Graph change stored replay](../arch.md#graph-change-replay)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [GLOSSARY](../../GLOSSARY.md)
