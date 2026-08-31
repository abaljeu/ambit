# Correct Undo Slice 2 to submitted-only History

## Changed files

- [[src/Shared/ClientHistory.fs]] removes confirmation and pending-lineage ownership.
  Its public module seam is now exactly `record`, `undo`, `redo`, and `clear`.
- [[src/Shared/History.fs]] shares one private reversible-Op mapping between
  `Change.inverse` and the legacy `Change.invert`, while preserving inversion of a
  client-submitted `SetUpdateTime`.
- [[tests/Shared.Tests/ClientHistoryTests.fs]] specifies the four-operation,
  submitted-only seam and removes confirmation-amendment and dependent-rewrite tests.
- [[correct-undo-slice-2-submitted-history.md]] records this correction.

No Slice 3 queue, planner, synchronization, ACK, or runtime wiring was added.

## Red evidence

After changing only the ClientHistory tests to require a directly returned stable
`recordId`, the focused test command failed to compile against the old API:

```bash
dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~ClientHistoryTests"
```

Expected failure: `FS0041` at `ClientHistoryTests.fs(153,12)` because the implementation
returned `PendingTransition` where the revised public seam required `int`.

## Green implementation and public API

`ClientHistory` stores only private `past`, `future`, and `nextRecordId` state.
`HistoryRecord.applied` is replaced only by a locally generated Undo or Redo Change,
so it remains exactly the last client-submitted local Change for that logical record.

- `clear : unit -> ClientHistory`
- `record : string -> Change -> ClientHistory -> ClientHistory * int`
- `undo : Revision -> Guid -> ClientHistory ->
  (Change * string * ClientHistory * int) option`
- `redo : Revision -> Guid -> ClientHistory ->
  (Change * string * ClientHistory * int) option`

The returned `int` is the stable logical record identity. Undo and Redo retain it
while replacing `applied` with their freshly identified submitted inverse Change.
Normal recording still folds the applied future into Undo order without duplicating
logical records.

## Review-finding disposition

- Quadratic pending append: deleted with `pendingByRecord` and all ClientHistory
  confirmation lineage. No replacement append path exists in Slice 2.
- Duplicated inversion mapping: resolved in [[src/Shared/History.fs]] by one private
  `invertOp`; ordinary inversion still omits create Ops, while legacy inversion keeps
  them under its existing policy.
- Undo/Redo duplication was a judgement-call finding, not a hard violation. The two
  short stack-direction operations remain explicit and within source limits.

## Focused verification

```bash
dotnet build tests/Shared.Tests -c Debug
```

Passed with 0 warnings and 0 errors.

```bash
dotnet test tests/Shared.Tests -c Debug --no-build \
  --filter "FullyQualifiedName~ClientHistoryTests|FullyQualifiedName~HistoryTests"
```

Passed: 62 of 62 tests, 0 failed, in 224 ms.

Focused source searches found no `confirm`, `pendingByRecord`, `PendingTransition`,
confirmation validation, amendment, or dependent-rewrite symbol in ClientHistory or
its tests. IDE diagnostics reported no errors in the three edited F# files.

## Blocker

None.

## Proposed WORK.md mutations

- `remove` [[correct-undo-slice-2-submitted-history.md]] from Active: the correction
  is implemented and focused verification is green.
- `remove` [[review-undo-slice-2.md]] from Pending: both hard standards findings are
  resolved by this correction.
