# 57 — Upload Actor posts Node notices

**Status:** `coded`
**Type:** coding
**Blocked by:** 44 — Apply document state from Node events; 54 — Parse and import stop the Parsed Set
**Trackers:** architecture, project spec

## Context

`Api.postEvents` is the client Change endpoint. `Api.inferredNodeEvents` reads `Op.SetDocumentState` on that Change and posts `Notice.NodeUploading`, `Notice.NodeReceived`, and `Notice.NodeParsed` through `CoreMailboxApi.postNotice`. Alan, 2026-10-19: client upload becomes an Actor. The core gives the Actor the notice function. The Actor posts `Notice.NodeUploading` and `Notice.NodeReceived`. Only a holder of the notice function posts a Notice. Today's notice function is `CoreMailboxApi.postNotice`. [54 — Parse and import stop the Parsed Set](54-parse-and-import-stop-the-parsed-set.md) moves the import `NodeParsed` post to `ParseThread.finishParse` before `inferredNodeEvents` is removed.

## What to build

Client upload runs as an Actor. The core gives the Actor the notice function. The Actor posts `Notice.NodeUploading` and `Notice.NodeReceived` through that function. Those cases become `Notice.NodeUploading` and `Notice.NodeReceived`. `Api.postEvents` stops inferring node events from client ops. Remove `inferredNodeEvents`.

### 1. Upload Actor

1. [ ] **Actor** — Client upload runs as an Actor. The core gives the Actor `CoreMailboxApi.postNotice`.
2. [ ] **Posts** — The Actor posts `Notice.NodeUploading` and `Notice.NodeReceived` through that function. [50 — Upload emits NodeUploading](50-upload-emits-nodeuploading.md) states the PullingFile and Unpersisted effect. [53 — Arrival and reconcile emit NodeReceived](53-arrival-and-reconcile-emit-nodereceived.md) states the Unparsed, Persisted, and clear PullingFile effect.
3. [ ] **Stop inference** — Remove `inferredNodeEvents`. `Api.postEvents` does not post `Notice.NodeUploading`, `Notice.NodeReceived`, or `Notice.NodeParsed` from the ops of a client Change.

### Acceptance Criteria

1. [ ] **Actor test** — `upload Actor posts Node notices`. The upload Actor posts `Notice.NodeUploading` and `Notice.NodeReceived` through `CoreMailboxApi.postNotice`. `Notice.NodeUploading` sets PullingFile and Unpersisted. `Notice.NodeReceived` sets Unparsed and Persisted and clears PullingFile.
2. [ ] **Inference test** — `postEvents does not infer node events`. `Api` has no `inferredNodeEvents`. A client Change does not gain `NodeUploading`, `NodeReceived`, or `NodeParsed` from its ops.

## See also

- [Graph change upload Actor](../arch.md#graph-change-upload-actor)
- [Document-state events](../spec.md)
- [core-refinement](../project.md)
- [44 — Apply document state from Node events](44-apply-document-state-from-file-events.md)
- [54 — Parse and import stop the Parsed Set](54-parse-and-import-stop-the-parsed-set.md)
- [45 — SetDocumentState leaves; NodeUnpersisted replaces it](45-remove-document-state-set-ops.md)
- [GLOSSARY](../../../GLOSSARY.md)
