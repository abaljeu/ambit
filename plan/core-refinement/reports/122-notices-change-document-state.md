# 122 — Notices change document state

Branch: dev.

Each Notice that changes document state already has a ticket. No new ticket.

## Effects

- [50 — Upload emits NodeUploading](../issues/50-upload-emits-nodeuploading.md) already states `Notice.NodeUploading`: PullingFile and Unpersisted.
- [53 — Arrival and reconcile emit NodeReceived](../issues/53-arrival-and-reconcile-emit-nodereceived.md) now states `Notice.NodeReceived`: Unparsed and Persisted, and PullingFile clears.
- [54 — Parse and import stop the Parsed Set](../issues/54-parse-and-import-stop-the-parsed-set.md) now states `Notice.NodeParsed`: Parsed when PullingFile is clear; Unparsed when PullingFile is set; persist state stays unchanged; a Parsed Unpersisted node is pushed.
- [57 — Upload Actor posts Node notices](../issues/57-upload-actor-posts-document-state.md) posts `Notice.NodeUploading` and `Notice.NodeReceived`. The effects stay on 50 — Upload emits NodeUploading and 53 — Arrival and reconcile emit NodeReceived.
- [73 — Add the NodeParsing and NodePersisting Notices](../issues/73-add-the-nodeparsing-and-nodepersisting-notices.md) already states `Notice.NodePersisting`: Unpersisted, parse state unchanged.
- [100 — Core loop gates Persisted on the written version](../issues/100-core-loop-gates-persisted-on-the-written-version.md) already states `Notice.NodePersisted`: `persistedVersion`, `updateTime`, and Persisted when the written version has reached `changedVersion`.

`Notice.NodeParsing` and `Notice.PersistFailed` leave document state unchanged. [121 — MarkUnparsed leaves Notice](../issues/121-markunparsed-leaves-notice.md) already covers `Notice.MarkUnparsed`.

## Index

[122 — Document state moves by Notice](../issues/122-document-state-moves-by-notice.md) states that Load, Parse, and Persist push a Notice. The effect is idempotent and matches `Op.SetDocumentState` for that same change. Changes points at the six tickets above.

[123 — Prepare document state Notice context](../issues/123-prepare-document-state-notice-context.md) keeps its still-open producers. It no longer replaces the context of 122 — Document state moves by Notice.
