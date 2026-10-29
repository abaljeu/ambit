# 15 — Load target omits the Workspace scope

Slice: The isLoaded call in loadTargetIntent is deleted. includeWorkspace stays false on every target until the field delete.

## Signatures

[ ] `UpdateHelpers.loadTargetIntent` in `src/Client/UpdateHelpers.fs` sets `includeWorkspace` true when the target Header is present and `Graph.isLoaded` is false.
[ ] `LoadTarget` is `{ targetId; includeWorkspace }` in `src/Shared/ApiResponses.fs`.

## Match sites

[ ] `tryStartLoadFetch` maps selection ids through `loadTargetIntent`.
[ ] The field stays on the record until 30 — Delete includeWorkspace.

## Existing tests

[ ] No current fact calls `loadTargetIntent`. `includeWorkspace = true` still appears in [[tests/Shared.Tests/LoadCaptureTests.fs]] as a request fixture.

## Copy-from

[ ] `tryStartLoadFetch` in `src/Client/UpdateHelpers.fs` is the public caller.

## write-ticket

### Source files

[ ] [[src/Client/UpdateHelpers.fs]] — `loadTargetIntent`.

### Tests

[ ] Add ``tryStartLoadFetch sets includeWorkspace false`` in `tests/Server.Tests/LoadSaveCommandClientTests.fs`.

## Size

[ ] Source files: 1. Under the limit of 8.
[ ] Tests added or changed: 1. Under the limit of 4.
