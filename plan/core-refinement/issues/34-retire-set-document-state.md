# 34 — Retire SetDocumentState

**Status:** `defined`
**Type:** coding
**Blocked by:** [29 — Graph change crosses Core loop](29-graph-change-crosses-core-loop.md)
**Trackers:** architecture

## Context

[29 — Graph change crosses Core loop](29-graph-change-crosses-core-loop.md) moves parse and reconcile off `Op.SetDocumentState`. Upload and import still emit that Op. [§3 Expand-contract sequence](plan/core-refinement/arch.md#3-expand-contract-sequence) step 1 Contract removes `DocumentStateEnum` after its callers migrate. [§10 Module map](plan/core-refinement/arch.md#10-module-map) names `GraphMutate.setParseState` as the field write of the parsed axis. Parsed, Unparsed, and NoServerFile are one three-way parse state ([GLOSSARY.md](GLOSSARY.md)).

## What to build

`Op.SetDocumentState` leaves. `DocumentStateEnum` and its converters leave. Upload and import write the three-way parse state through `GraphMutate.setParseState`. That function is the one parse-state writer.

### Retire

1. [ ] **Op leaves** — `Op.SetDocumentState` leaves [History](src/Shared/History.fs) (lines 14-17), including apply, undo, and the Op JSON codec in [Serialization](src/Shared/Serialization.fs).
2. [ ] **Enum leaves** — `DocumentStateEnum` leaves [Model](src/Shared/Model.fs) (lines 70-73). These converters leave with it: `DocumentState.fromParts`, `DocumentState.toLegacy`, `AsEnum`, `ParseState.ofDocumentState`, `ParseState.toDocumentState` (lines 94-124), `Node.documentStateEnum` (line 264), and `Node.withDocumentState` (lines 267-273). `ParseState`, `PersistState`, and the `DocumentState` record stay.
3. [ ] **Second writer leaves** — `GraphMutate.setDocumentState` and the `Graph.setDocumentState` facade in [Graph ops](src/Shared/GraphOps.fs) (lines 62-63) leave. Every remaining match of the Op case leaves with the case.
4. [ ] **Upload** — [Workspace upload structure](src/Shared/WorkspaceUploadStructure.fs) `markUnparsed`, `markNoServerFile`, `markParentCurrent` (lines 143-160), and `planServerFilePresentOps` (line 319) write through `GraphMutate.setParseState`.
5. [ ] **Import** — [Import text](src/Shared/ImportText.fs) `markDocumentCurrentBeforeParse` (line 47) writes through `GraphMutate.setParseState`.
6. [ ] **One writer** — `GraphMutate.setParseState` writes Parsed, Unparsed, and NoServerFile. No other function writes that three-way state.
7. [ ] **Keys stay for their tickets** — Call sites that encoded `DocumentStateEnum` read the three-way parse state from the node. Node JSON keys stay for [35 — Wire three-way parse state](35-wire-three-way-parse-state.md). The column `document_state` and its stored words stay. [36 — Add the persist column](36-add-the-persist-column.md) adds the persist column beside that column.

### Obligations

1. [ ] **Op claim** — [Op](doc/current/op.md) shows no `Op.SetDocumentState`. The parsed axis has one writer, `GraphMutate.setParseState`.
2. [ ] **Writer test** — A test shows Upload and import do not emit `Op.SetDocumentState`, and `GraphMutate.setParseState` is the write that sets Parsed, Unparsed, and NoServerFile.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [§3 Expand-contract sequence](plan/core-refinement/arch.md#3-expand-contract-sequence)
- [§10 Module map](plan/core-refinement/arch.md#10-module-map)
- [core-refinement](plan/core-refinement/project.md)
- [29 — Graph change crosses Core loop](29-graph-change-crosses-core-loop.md)
- [35 — Wire three-way parse state](35-wire-three-way-parse-state.md)
- [Op](doc/current/op.md)
- [GLOSSARY](GLOSSARY.md)
