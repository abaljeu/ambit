# Split-node origin TRACE

Stage: build
Summary: Name the origin of obscure line deletion on Enter node-split, then correct Poll/sync recoverable mismatch to undo all pending Graph ops and apply the Server merge.
Updated: 2026-09-28
Started: 2026-09-28
Actual: 3h40m

**Part of:** [[plan/roadmap/epics/robust-outliner.md]]

Frontier: none. [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](issues/01-poll-sync-cas-undo-all-pending-apply-merge.md) — Type `coding`, Status `coded`.

## Notes

- Origin TRACE (PR [#160 — TRACE: childMap refactor does not break node-split](https://github.com/abaljeu/ambit/pull/160)): [split-node-origin-trace](reports/split-node-origin-trace.md). Truncated line (`hel`, suffix gone) is soft-skip plus same-field pending invert (`84d19416` then `8309e9db`). Want overwrite (sibling dropped, old full text restored) is a stale Want `childMap` install (`52f79c49`..`b41c5dea`). childMap first-apply is not the break. No product fix in that TRACE.
- 2026-09-28 — Alan locked grill (chat). Never hard-fail the Poll fold. General rule: undo client posted/optimistic Graph edits (all pending), apply the Server merge (which includes posted edits), then proceed. Post all ops in normal use; only an uncommitted edit-box draft stays local — leave `#edit-input` alone on conflict unless focus or node is gone. Soft-skip-without-apply of a mismatched authoritative payload is wrong. Corrected handling is the undo-all-pending → apply-Server-merge family. Keep the Want overwrite proving test; do not keep a prior wrong fix. Ticket: [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](issues/01-poll-sync-cas-undo-all-pending-apply-merge.md).
- Consume architecture stays [Client correction — rewind and replay](plan/event-sourced-ops/details/client-consume.md). Related live tickets on that Project: [16 — Fix pending+merged-events: undo-then-apply instead of DataOutdated reload](plan/event-sourced-ops/issues/16-fix-pending-merged-events-undo-then-apply.md), [17 — Instrument apply-error → DataOutdated with op type and mismatch reason](plan/event-sourced-ops/issues/17-instrument-apply-error-dataoutdated.md). This Project is not [github-transport](plan/github-transport/project.md).
- 2026-09-28 — [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](issues/01-poll-sync-cas-undo-all-pending-apply-merge.md) coded. Poll/sync apply undoes all pending Graph ops, then applies the Server merge. Soft-skip-without-apply after undo is removed for those field mismatches.
- 2026-09-28 — Review Must-fix on [01 — Poll/sync recoverable mismatch: undo all pending, then apply Server merge](issues/01-poll-sync-cas-undo-all-pending-apply-merge.md): Event tails do not rewind pending Graph ops. Want install still undoes pending, then applies Want.
