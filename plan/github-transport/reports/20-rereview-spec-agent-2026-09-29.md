# Ticket 20 — State axes on special nodes — Spec re-review

Range: `origin/staging...HEAD` (`88436e6a` to `3fe7d9f0`). Spec: [Ticket 20 — State axes on special nodes](plan/github-transport/issues/20-state-axes-on-special-nodes.md).

## (c) Implementation looks wrong

1. **`stampExistingDocuments` marks Persisted without a successful artifact write.** Spec: "A successful artifact write marks that content node Persisted in the event source. A failed write leaves Unpersisted." `stampWrittenNode` sets Persisted for every id passed to `stampNodes`. Live `persistGraphOps` then calls `stampExistingDocuments` on path-move ids after `writeDocumentsSoft`. A moved node whose `writeDocumentCore` failed still has a file on disk, so that path marks it Persisted and `PersistStamp.opsBetween` can emit `SetPersistState`. Snapshot `persistGraphChange` stamps every existing document root the same way. A failed non-moved write stays Unpersisted.
