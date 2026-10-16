# 20 — Contract special-node state inside DocumentState

**Status:** `defined`
**Type:** coding
**Blocked by:** 19 — Migrate special-node state onto the DocumentState record
**Trackers:** architecture, user manual

## Context

After the migrate, the Node stores the DocumentState record and still stores standalone parseState and persistState fields. The enum inside the record is still Current, Unparsed, or NoServerFile.

## What to build

The standalone axes leave the Node. ParseState and PersistState remain the field types inside the DocumentState record. DocumentState remains. The enum remains, including NoServerFile. Current keeps its name.

### Contract

1. [ ] **Standalone axes leave** — The Node has no standalone parseState field and no standalone persistState field.
2. [ ] **Record remains** — The Node field documentState is the DocumentState record. Its fields are parseState, persistState, and documentState.
3. [ ] **Enum remains** — DocumentStateEnum keeps Current, Unparsed, and NoServerFile. NoServerFile stays its own case. Current keeps its name.
4. [ ] **DocumentState remains** — DocumentState is not deleted.

### Obligations

1. [ ] **Workspace graph** — The Workspace graph claim shows parse and persist only inside the DocumentState record.
2. [ ] **Read test** — A test reads parse and persist from the record after the standalone fields are gone.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [core-refinement architecture](plan/core-refinement/arch.md)
- [core-refinement map](plan/core-refinement/map.md)
- [core-refinement](plan/core-refinement/project.md)
- [19 — Migrate special-node state onto the DocumentState record](plan/core-refinement/issues/19-migrate-special-node-state-onto-the-document-state-record.md)
- [Workspace graph](doc/current/graph.md)
- [Model](src/Shared/Model.fs)
