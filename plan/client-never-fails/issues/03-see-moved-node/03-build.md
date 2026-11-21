# 03 — See a moved Node build

## Stage

[ ] Stage is build in [03 — See a moved Node](CONTEXT.md).

## Edits

[ ] `installWantAnswer` in `src/Shared/ResidentProjection.fs` returns `Ok` and keeps a child id that has no Header on the edge.
[ ] `replace` in `src/Shared/GraphMutate.fs` returns `Ok` for a new child id with no Header. The parent `GraphChildren.isLoaded` gate stays.
[ ] `planPatchDOM` in `src/Shared/ViewModelDomPlan.fs` reads a Header with `Map.tryFind`. A missing Header is a placeholder row.
[ ] `rowChildrenIndicatorForId` in `src/Shared/ViewModelChildrenIndicator.fs` returns the placeholder Bullet when the Header is absent.
[ ] `RowView.Layout.buildRowElement` in `src/Client/RowView.fs` draws that row with class `amb-row-absent`.
[ ] `.amb-row-absent .amb-fold-toggle` in `src/Server/wwwroot/style.css` uses color #444444.
[ ] ``installWantAnswer refuses a dangling edge`` expects `Ok` and the child id on the parent list.
[ ] ``applySyncResponse refuses dangling Want edges`` expects `Ok` and the child id on the parent list.
[ ] ``Graph.replace accepts new child id with no Header`` expects `Ok`, the child id on the parent list, and no Header.
[ ] ``planPatchDOM CreateRow for visible Absent child id`` expects `CreateRow` for that visible id.

## Check

[ ] Verdict is `pass` in [03 — See a moved Node check](plan/client-never-fails/reports/03-see-moved-node-check.md).

## Status

[ ] Ticket status is `coded`.
