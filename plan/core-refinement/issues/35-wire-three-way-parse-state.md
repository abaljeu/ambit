# 35 — Wire three-way parse state

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

Node JSON sends a derived `documentState` key beside `parseState` and `persistState`. [§4 Step 1 Commitment](plan/core-refinement/arch.md#4-step-1-commitment) keeps `DocumentStateEnum` for JSON until those callers migrate. Parsed, Unparsed, and PullingFile are one three-way parse state. PullingFile is memory-only. The column `document_state` stores Parsed or Unparsed only. Persist state is separate ([GLOSSARY.md](GLOSSARY.md)). [§2 Starting point](plan/core-refinement/arch.md#2-starting-point) records today's JSON and SQL encoding.

## What to build

The node wire sends the three-way parse state once, plus persist state. The derived `documentState` key leaves.

### Wire

1. [ ] **One parse value** — [Serialization](src/Shared/Serialization.fs) `encodeNode` (lines 142-148) and `nodeFromFields` (lines 169-177) send `parseState` as `parsed`, `unparsed`, or PullingFile. The JSON word is `noServerFile`. They send `persistState` as `persisted` or `unpersisted`. The column `document_state` does not store PullingFile.
2. [ ] **Derived key leaves** — The key `documentState` leaves the node object. `encodeDocumentState` (lines 13-17) and `decodeDocumentState` (lines 43-49) leave with that key.
3. [ ] **Three-way restore** — Node JSON with `parseState` and `persistState` restores Parsed, Unparsed, or PullingFile, and restores persist state. Reload from `document_state` does not restore PullingFile.

### Obligations

1. [ ] **HTTP contract** — [HTTP contract](doc/current/http-contract.md) shows `parseState` and `persistState` on a node and shows no `documentState` key.
2. [ ] **Workspace graph** — [Workspace graph](doc/current/graph.md) shows the node wire as one three-way parse state plus persist state.
3. [ ] **Round-trip test** — A test encodes and decodes a node for each of Parsed, Unparsed, and PullingFile, with Persisted and with Unpersisted. The decoded node matches. The JSON object has no `documentState` key.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [§4 Step 1 Commitment](plan/core-refinement/arch.md#4-step-1-commitment)
- [§2 Starting point](plan/core-refinement/arch.md#2-starting-point)
- [core-refinement](plan/core-refinement/project.md)
- [34 — Upload and import use the parse-state writer](34-upload-and-import-use-the-parse-state-writer.md)
- [37 — Kind-specific node state](37-kind-specific-node-state.md)
- [HTTP contract](doc/current/http-contract.md)
- [Workspace graph](doc/current/graph.md)
- [GLOSSARY](GLOSSARY.md)
