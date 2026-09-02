# Fix 02: Delete of Owned Node with a self-Ref

Product fix. No commit. [[WORK.md]] not edited.

## Outcome

Delete of the Owned appearance no longer treats a self-Ref as another home. Classify ignores occurrences whose parent is the same Node. If no other appearance remains, the action is Move to TRASH. History and Browser apply finish. The owner is TRASH, not the Node itself.

If a real Ref exists elsewhere, Delete still promotes that Ref. `buildPromoteOps` also skips a self-Ref so promotion cannot pick the loop.

The owner-chain visited set in [[src/Shared/GraphQuery.fs]] `enclosing` stays as a safety net.

## Files changed

- [[src/Shared/ViewModelDeleteOps.fs]] — filter self-occurrences in both classify paths; skip self-Ref in promote
- [[tests/Shared.Tests/DeleteOpsTests.fs]] — failing then passing facts for classify, History, Browser apply, and mixed real Ref
- [[plan/delete-ref/project.md]] — Stage `active`
- [[plan/index.md]] — Delete Ref row Stage `active`
- [[plan/delete-ref/issues/02-delete-owned-self-ref-hangs.md]] — Status `agent-done`

## Tests

- `dotnet build tests/Shared.Tests -c Debug` — succeeded
- `dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~DeleteOpsTests"` — 21 passed (two diagnose facts replaced; two new facts added)
- `./scripts/client.sh build` — Fable and esbuild succeeded

## Remaining risk

- A self-Ref Child stays on the Node after Move to TRASH. The issue allowed that. It is not dropped.
- `isOwnerUnderTrash` still walks owners without a visited set. Not this repro.
- No Browser HITL of the Delete gesture.
- Existing Owned cycles in a stored Graph remain [[plan/owner-edge-db-repair/spec.md]].

## Suggested [[WORK.md]] mutations

- `move` [[plan/delete-ref/issues/02-delete-owned-self-ref-hangs.md]] Pending → Active
- `remove` [[plan/delete-ref/issues/02-delete-owned-self-ref-hangs.md]] after this report (tests and Client compile verified)
