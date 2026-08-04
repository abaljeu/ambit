# Define unloaded navigation and UI semantics

Type: grilling
Status: resolved
Blocked by: 01, 03

## Question

How should rendering distinguish unloaded nodes, loaded leaves, collapsed loaded parents, active loading, and load failure, and for expand, zoom, and keyboard navigation when should the client load and resume versus reject or no-op with explicit feedback?

## Answer

- An `Unknown` child list renders as a hollow circle matching the existing solid-circle loaded-leaf indicator. A loaded parent keeps the existing collapsed or expanded fold chevron.
- Single-clicking the hollow circle requests `Direct` for that node. Success unfolds it when the returned list is non-empty and renders it as a leaf when the list is empty. Double-clicking performs `Direct`-backed Zoom using the existing deferred single-click/double-click interaction pattern; fold state does not apply to the zoomed node.
- While that node is loading, a spinner replaces its hollow circle. Failure restores the hollow circle, reports through the existing command-result feedback, and abandons any pending framing continuation.
- Fold, Space/Right, deep-unfold, sibling traversal, and range selection operate only on resident child lists. An unloaded or loading node contributes zero visible children, so these operations naturally no-op or continue without extra feedback and never load implicitly.
- Zoom In, Zoom Out, and breadcrumb navigation request `Direct` for missing destination data and resume if their framing intent is still current. The current frame remains visible until success. Resumed Zoom re-evaluates the loaded state: a parent frames itself, while a leaf uses the existing owner-frame behavior.
- A valid current load response still installs when its initiating intent is no longer current. Identical in-flight mode and normalized-target requests coalesce and carry each waiting command continuation; overlapping but non-identical requests remain separate.
- The existing pre-first-render bootstrap loading and failure behavior remains unchanged. Search handoff and the entity and scope of explicit Load remain for their dedicated tickets.
- These semantics reuse established controls and feedback closely enough that a runnable UI prototype is not required before specification.
