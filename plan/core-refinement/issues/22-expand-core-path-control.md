# 22 — Expand Core path control

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture, user manual

## Context

Callers still build or hold a DataDir absolute path. Core does not yet own the four residency operations as the only way to name a file or a directory.

## What to build

Core stands read file, write file, read directory, and write directory beside today's DataDir use. A caller passes a node, or a relative path derived from a node. Core holds the absolute residency. This ticket does not move every caller.

### Expand

1. [ ] **Four operations** — Core owns read file, write file, read directory, and write directory.
2. [ ] **Caller names a node** — A caller passes a node, or a relative path derived from a node.
3. [ ] **Core holds the path** — Core holds the absolute residency for those operations.
4. [ ] **Old use remains** — Today's DataDir use remains until the migrate.

### Obligations

1. [ ] **Parse and persist claim** — The Parse and persist claim names the four operations and the node or relative-path protocol.
2. [ ] **Door test** — A test calls each of the four operations with a node or a relative path derived from a node.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement map](plan/core-refinement/map.md)
- [core-refinement](plan/core-refinement/project.md)
- [DataDir caller inventory](plan/core-refinement/reports/datadir-caller-inventory.md)
- [transport-layer map](plan/transport-layer/map.md)
- [Parse and persist](doc/current/parse-persist.md)
