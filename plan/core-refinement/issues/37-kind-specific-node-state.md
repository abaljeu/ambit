# 37 — Kind-specific node state

**Status:** `defined`
**Type:** coding
**Blocked by:** [35 — Wire three-way parse state](35-wire-three-way-parse-state.md)
**Trackers:** architecture

## Context

[§1 Special-node Parse and Persist axes](plan/core-refinement/arch.md#1-special-node-parse-and-persist-axes) gives parse state and persist state to a Workspace Node, a Directory Node, and a File Node. A Normal Node carries neither ([GLOSSARY.md](GLOSSARY.md)). Only a File Node carries PullingFile. Today every Node holds `documentState` ([Model](src/Shared/Model.fs) line 138). `Node.Create` takes `documentState` (line 190) and fills the field for every kind (lines 196-207). The `DocumentState` record (lines 87-90) puts `pullingFile` on Directory and Workspace as well as File. [49 — Rename NoServerFile to PullingFile](49-rename-noserverfile-to-pullingfile.md) renames that field and does not change which Kind carries it. This ticket follows the wire in [35 — Wire three-way parse state](35-wire-three-way-parse-state.md).

## What to build

Node state is kind-specific. A Normal Node does not hold parse state or persist state, and node JSON for a Normal Node does not send them. Only a File Node carries PullingFile. PullingFile is memory-only.

### Kind

1. [ ] **Normal** — A Normal Node has no parse state and no persist state. `Node.Create` for Kind Normal does not store them.
2. [ ] **Wire** — Node JSON for a Normal Node omits `parseState` and `persistState`.
3. [ ] **File only** — PullingFile exists only on a File Node. A Directory Node and a Workspace Node carry parse state and persist state and do not carry PullingFile. The code field is `pullingFile`.
4. [ ] **Already without state** — A Workspaces node and a Directory File stay without parse state and persist state.

### Obligations

1. [ ] **Workspace graph** — [Workspace graph](doc/current/graph.md) says a Normal Node has no parse state and no persist state, and PullingFile is only on a File Node.
2. [ ] **Kind test** — A test shows a Normal Node stores neither state and its JSON omits both keys. A test shows a File Node can be PullingFile. A test shows a Directory Node and a Workspace Node have parse state and persist state and have no PullingFile.

## See also

- [§1 Special-node Parse and Persist axes](plan/core-refinement/arch.md#1-special-node-parse-and-persist-axes)
- [core-refinement](plan/core-refinement/project.md)
- [35 — Wire three-way parse state](35-wire-three-way-parse-state.md)
- [Model](src/Shared/Model.fs)
- [Workspace graph](doc/current/graph.md)
- [GLOSSARY](GLOSSARY.md)
