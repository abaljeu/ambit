# 34 — Upload and import use the parse-state writer

**Status:** `defined`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

Upload and import still plan `Op.SetDocumentState`. [§10 Module map](plan/core-refinement/arch.md#10-module-map) names `GraphMutate.setParseState` as the field write of the parsed axis. [§3 Expand-contract sequence](plan/core-refinement/arch.md#3-expand-contract-sequence) step 1 Contract removes `DocumentStateEnum` after its callers migrate. `Op.SetDocumentState` stays. It carries the three-way parse state so the client can display document state from an Event. Parsed, Unparsed, and PullingFile are that state ([GLOSSARY.md](GLOSSARY.md)). PullingFile is memory-only. The column `document_state` stores Parsed or Unparsed only.

## What to build

`DocumentStateEnum` and its converters leave. Upload and import write the three-way parse state through `GraphMutate.setParseState`. That function is the one parse-state writer. `Op.SetDocumentState` stays and carries that same three-way state.

### Writer

1. [ ] **Enum leaves** — `DocumentStateEnum` leaves [Model](src/Shared/Model.fs). These converters leave with it: `DocumentState.fromParts`, `DocumentState.toLegacy`, `AsEnum`, `ParseState.ofDocumentState`, `ParseState.toDocumentState`, `Node.documentStateEnum`, and `Node.withDocumentState`. `ParseState`, `PersistState`, and the `DocumentState` record stay.
2. [ ] **Op stays** — `Op.SetDocumentState` stays in [History](src/Shared/History.fs). Apply, undo, and the Op JSON codec in [Serialization](src/Shared/Serialization.fs) stay. The old value and the new value are the three-way parse state: Parsed, Unparsed, or PullingFile. PullingFile is not written to `document_state`.
3. [ ] **One field writer** — `GraphMutate.setDocumentState` and the `Graph.setDocumentState` facade in [Graph ops](src/Shared/GraphOps.fs) leave. Apply of `Op.SetDocumentState` calls `GraphMutate.setParseState`.
4. [ ] **Upload** — [Workspace upload structure](src/Shared/WorkspaceUploadStructure.fs) `markUnparsed`, `markNoServerFile` [o] obsolescent, `markParentCurrent`, and `planServerFilePresentOps` write through `GraphMutate.setParseState`. [49 — Rename NoServerFile to PullingFile](49-rename-noserverfile-to-pullingfile.md) renames `markNoServerFile` and does not change when the state is set.
5. [ ] **Import** — [Import text](src/Shared/ImportText.fs) `markDocumentCurrentBeforeParse` writes through `GraphMutate.setParseState`.
6. [ ] **Three values** — `GraphMutate.setParseState` writes Parsed, Unparsed, and PullingFile. No other function writes that three-way state. The code value is `NoServerFile` [o] obsolescent until [49 — Rename NoServerFile to PullingFile](49-rename-noserverfile-to-pullingfile.md).
7. [ ] **Keys stay for their tickets** — Call sites that encoded `DocumentStateEnum` read the three-way parse state from the node. Node JSON keys stay for [35 — Wire three-way parse state](35-wire-three-way-parse-state.md). The column `document_state` stores Parsed or Unparsed only. [36 — Add the persist column](36-add-the-persist-column.md) adds the persist column beside that column and keeps PullingFile out of it.

### Obligations

1. [ ] **Op claim** — [Op](doc/current/op.md) keeps `Op.SetDocumentState`. The Op carries the three-way parse state. The parsed axis has one writer, `GraphMutate.setParseState`.
2. [ ] **Writer test** — `upload and import write through setParseState` shows Upload and import call `GraphMutate.setParseState` for Parsed, Unparsed, and PullingFile.
3. [ ] **Round-trip test** — `Op.SetDocumentState round-trip` still passes. The payload is Parsed, Unparsed, or PullingFile.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [§3 Expand-contract sequence](plan/core-refinement/arch.md#3-expand-contract-sequence)
- [§10 Module map](plan/core-refinement/arch.md#10-module-map)
- [core-refinement](plan/core-refinement/project.md)
- [35 — Wire three-way parse state](35-wire-three-way-parse-state.md)
- [36 — Add the persist column](36-add-the-persist-column.md)
- [Op](doc/current/op.md)
- [GLOSSARY](GLOSSARY.md)
