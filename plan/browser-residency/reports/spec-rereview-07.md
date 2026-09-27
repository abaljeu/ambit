# Spec re-review — origin/staging...HEAD

Range: `git diff origin/staging...HEAD` on [07 — Expand Want and edges/Nodes package](plan/browser-residency/issues/07-expand-want-and-edges-nodes-package.md).

## (c) Wrong implementation

[2. Module map](plan/browser-residency/arch.md) item [3. ResidentProjection](plan/browser-residency/arch.md) interface 1 says: "`visibleClosureGraph` is reserved-plus-ancestors beside `rootBootstrapGraph`: `childMap` for ROOT, TRASH, Workspaces Node, SYSTEM; ancestors of Zoom root. Not Included". [11 — Migrate Bullet, Included, and bootstrap wants](plan/browser-residency/issues/11-migrate-bullet-included-and-bootstrap-wants.md) keeps Included on bootstrap.

`loadedParentIds` appends `[ zoom ]` after reserved ids and owner ancestors. On a Server Graph that already has those `childMap` keys, `visibleClosureWantAnswer` copies Zoom `childMap`. Direct Children under Zoom become Resident. That set is Included, not ancestors of the Zoom root.

## Dishonest checkmarks

The `[x]` on that same [3. ResidentProjection](plan/browser-residency/arch.md) interface 1 line is false against the code. Shared segment 6 stays `[ ]`. [21. Edges in the answer](plan/browser-residency/arch.md) item 2 names `installWantAnswer` only. Item 4 stays `[ ]` for [08 — Migrate Shared wire](plan/browser-residency/issues/08-migrate-shared-wire.md). Ticket **Status:** coded.
