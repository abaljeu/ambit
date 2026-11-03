# 03 — See a moved Node

Ticket: [[03-see-moved-node-ticket.md]]

Spec: [[plan/client-never-fails/spec.md]]

Arch: [[plan/client-never-fails/arch.md]]

Review: [[03-review.md]]

## Stage

Stage is refactor in [03 — See a moved Node](CONTEXT.md).

Ticket status is `coded`.

## 1. planPatchDOM

`python3 scripts/measure-fs-size.py` counts each binding below. The count is the same span the review used. [[.agents/rules/fsharp-source.md]] limits a function to 40 lines.

- **planPatchDOM.** `planPatchDOM` in `src/Shared/ViewModelDomPlan.fs` is 11 lines (330–340).
- **removalMutations.** `removalMutations` is 6 lines (191–196) and builds the `RemoveRow` list.
- **workspaceRemoteArrived.** `workspaceRemoteArrived` is 6 lines (197–202) and is the non-root Workspace remote check.
- **ResidentRowCompare.** `ResidentRowCompare` holds the old row, the new row, and the instance id for one cached Resident Header.
- **classNamePatch.** `classNamePatch` is 22 lines (203–224) and returns the class patch.
- **fileIndicatorPatch.** `fileIndicatorPatch` is 12 lines (225–236) and returns the file-indicator patch.
- **outlineTextPatch.** `outlineTextPatch` is 8 lines (238–245) and returns the text patch.
- **nodeNamePatch.** `nodeNamePatch` is 8 lines (246–253) and returns the name patch.
- **textClassesPatch.** `textClassesPatch` is 8 lines (254–261) and returns the text-class patch.
- **foldArrowPatch.** `foldArrowPatch` is 13 lines (262–274) and returns the fold-arrow patch.
- **residentRowPatches.** `residentRowPatches` is 11 lines (275–285) and keeps that patch order.
- **residentNeedsRecreate.** `residentNeedsRecreate` is 14 lines (286–299) and is the RecreateRow check.
- **residentCachedMutation.** `residentCachedMutation` is 6 lines (300–305) and returns `RecreateRow` or `PatchRow`.
- **visibleRowMutation.** `visibleRowMutation` is 21 lines (306–326) and returns `CreateRow`, the placeholder mutation, or the Resident mutation.

## 2. Row frame

- **buildRowFrame.** `buildRowFrame` in `src/Client/RowView.fs` is 21 lines (133–153). It appends the indent, the Bullet, the text element, the name element, the file indicator, and the Cancel button.
- **buildAbsentRowElement.** `buildAbsentRowElement` is 21 lines (155–175). It sets the Absent row classes and passes the placeholder Bullet to `buildRowFrame`.
- **buildResidentRowElement.** `buildResidentRowElement` is 90 lines (177–266). It sets the Resident row classes, builds the Bullet, passes that Bullet to `buildRowFrame`, then fills the text, the name, and the file indicator.

## 3. Left in place

Alan keeps these two bindings as they are:

- `replace` in `src/Shared/GraphMutate.fs`
- `buildResidentRowElement` past the shared frame

## 4. Suite

- **Command.** `bash scripts/check-test.sh 3891` printed `DONE` and `/tmp/ambit-full-suite-20261030-182309.log`. The log ends `full suite finished`.
- **Server.** `tests/Server.Tests` passed 728, skipped 1, failed 0.
- **Shared.** `tests/Shared.Tests` passed 1909, skipped 3, failed 0.
- **Scan.** `python3 scripts/standards-scan.py --diff HEAD` printed each new binding inside the 40-line limit.
