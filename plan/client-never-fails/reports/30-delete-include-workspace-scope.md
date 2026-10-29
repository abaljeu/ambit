# 30 — Delete includeWorkspace scope

Slice: LoadTarget loses includeWorkspace. Encode and decode lose the field. loadTargetIntent builds a target id only. wantAnswerForTargets answers the target ids. The includeWorkspace filter is deleted. LoadRefuse.MultiWorkspace stays.

## Signatures

[ ] `LoadTarget` in `src/Shared/ApiResponses.fs`.
[ ] `encodeLoadTarget` and `decodeLoadTargetDecoder` in `src/Shared/ApiResponseSerialization.fs`.
[ ] `UpdateHelpers.loadTargetIntent` in `src/Client/UpdateHelpers.fs` sets the flag false.
[ ] `ResidentProjection.wantAnswerForTargets` filters on `includeWorkspace` in `src/Shared/ResidentProjection.fs`.
[ ] `LoadRefuse.MultiWorkspace` stays on the refusal union.

## Match sites

[ ] `tryStartLoad` and `tryStartLoadFetch` pass `LoadTarget` lists through.

## Existing tests

[ ] ``tryStartLoad emits LoadServer when idle with empty queue`` and ``tryStartLoad returns no effects when already loading`` in [[tests/Shared.Tests/SyncPlannerTests.fs]] build a target with `includeWorkspace`.

## Copy-from

[ ] Target-id answers in `wantAnswer` are the copy for `wantAnswerForTargets` after the flag goes.

## write-ticket

### Source files

[ ] [[src/Shared/ApiResponses.fs]] — delete `includeWorkspace` from `LoadTarget`.
[ ] [[src/Shared/ApiResponseSerialization.fs]] — encode and decode the target id only.
[ ] [[src/Client/UpdateHelpers.fs]] — `loadTargetIntent` builds a target id only.
[ ] [[src/Shared/ResidentProjection.fs]] — `wantAnswerForTargets` answers the target ids. Delete the `includeWorkspace` filter.

### Tests

[ ] Change the two `tryStartLoad` facts so they build a target id only.

## Size

[ ] Source files: 4. Under the limit of 8.
[ ] Tests added or changed: 2. Under the limit of 4.
