# 12 — Drop the residency children line scope

Slice: residencyText keeps the document-state line. The children line is deleted.

## Signatures

[ ] `ViewModelRowState.residencyText` in `src/Shared/ViewModelRowState.fs` formats `Residency: {children}, {document}` from `GraphChildren.status`.

## Match sites

[ ] `GraphChildren.status` returns `Loaded` or `Unloaded`.
[ ] Document state is `Current`, `Unparsed`, or `PullingFile`.

## Existing tests

[ ] ``bulletTip minimal node lists guid residency and update time only`` in [[tests/Shared.Tests/ViewModelRowStateTests.fs]] expects `Residency: Loaded, Current`.
[ ] ``bulletTip disambiguates hollow by Unloaded versus Unparsed`` in `tests/Shared.Tests/ViewModelRowStateTests.fs` expects `Residency: Unloaded, Current` and `Residency: Loaded, Unparsed`.
[ ] ``bulletTip reads Loaded Current for a parsed leaf`` in `tests/Shared.Tests/ViewModelRowStateTests.fs` expects `Residency: Loaded, Current`.

## Copy-from

[ ] `Node.documentStateEnum` is the document-state read the line keeps.

## write-ticket

### Source files

[ ] [[src/Shared/ViewModelRowState.fs]] — `residencyText`.

### Tests

[ ] Change the three bullet-tip facts so the line names the document state and omits Loaded and Unloaded.

## Size

[ ] Source files: 1. Under the limit of 8.
[ ] Tests added or changed: 3. Under the limit of 4.
