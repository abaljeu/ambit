# 48 — Browser applies Node events

**Status:** `defined`
**Type:** coding
**Blocked by:** 43 — Expand Node events beside the Set output
**Trackers:** architecture, project spec

## Context

Graph change expand posts `Notice.NodeUploading` and `Notice.NodeReceived` through `CoreMailboxApi.postNotice`, and emits NodeParsing, NodeParsed, NodePersisting, and NodePersisted. `CoreMailboxLoad.applyNotice` applies `Notice.NodeUploading` and `Notice.NodeReceived` and logs them. The Browser applies the messages the handler logged. The server writer is today's `Op.apply` of `Op.SetDocumentState` and `GraphMutate.setPersistState`, or the Core-loop handlers from Graph change apply.

## What to build

The Browser event applier updates the resident Browser Graph from the messages `CoreMailboxLoad.applyNotice` logged. The browser updates only that graph. Display reads from it.

### 1. Browser apply

1. [ ] **Browser event applier** — Extend `SyncLogic.foldProjectedEvents` to receive every log message, including `Notice.NodeUploading` and `Notice.NodeReceived` that `CoreMailboxLoad.applyNotice` logged, and apply the effect in [Document-state events](../spec.md) for each Node message to the resident Browser Graph. There is no filter. Display reads that graph. NodeParsing means parse started and does not set Unparsed. NodePersisting sets Unpersisted. NodeUploading sets PullingFile in memory.

### 2. Browser replay, undo and redo mark Unpersisted

1. [ ] **Replay** — When Browser replay changes a node in the resident Browser Graph, the browser sets that node's `Node.documentState.persistState : PersistState` to `Unpersisted` with `Node.withUnpersistedAt`.
2. [ ] **Undo** — When Browser undo changes a node, the browser sets that node's `persistState` to `Unpersisted` with `Node.withUnpersistedAt`.
3. [ ] **Redo** — When Browser redo changes a node, the browser sets that node's `persistState` to `Unpersisted` with `Node.withUnpersistedAt`.

### Acceptance Criteria

1. [ ] **Client test** — `client applies Node events for parse state and persist state`. The resident Browser Graph has the parse state and persist state from those messages, including `Notice.NodeUploading` and `Notice.NodeReceived`. Display reads that graph. The client receives every log message.
2. [ ] **Client test** — `browser replay marks changed node Unpersisted`. The changed node's `persistState` is `Unpersisted`.
3. [ ] **Client test** — `browser undo marks changed node Unpersisted`. The changed node's `persistState` is `Unpersisted`.
4. [ ] **Client test** — `browser redo marks changed node Unpersisted`. The changed node's `persistState` is `Unpersisted`.

## See also

- [Graph change browser apply](../arch.md#graph-change-browser)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [43 — Expand Node events beside the Set output](43-expand-file-events-beside-the-set-output.md)
- [GLOSSARY](../../GLOSSARY.md)
