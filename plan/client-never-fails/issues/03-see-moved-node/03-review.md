# 03 — See a moved Node

Ticket: [[03-see-moved-node-ticket.md]]
Spec: [[plan/client-never-fails/spec.md]]
Arch: [[plan/client-never-fails/arch.md]]

## 1. Standards

- **planPatchDOM length.** **Hard.** `planPatchDOM` in `src/Shared/ViewModelDomPlan.fs` is 113 lines (184–296). [[.agents/rules/fsharp-source.md]] limits a function to 40 lines. This diff raises that function from 92 lines to 113 lines. The added arm is the missing-Header match.
- **buildResidentRowElement length.** **Hard.** `buildResidentRowElement` in `src/Client/RowView.fs` is 117 lines (172–288). The same 40-line limit applies. `python3 scripts/standards-scan.py --diff origin/staging` printed this binding over the limit. This diff moves the old `buildRowElement` body (118 lines) into this function. The new `buildRowElement` dispatcher is 7 lines.
- **replace length.** **Hard.** `replace` in `src/Shared/GraphMutate.fs` is 205 lines (257–461). The same 40-line limit applies. This diff adds the missing-Header `Map.tryFind` match inside that function. The function was already past 40 lines before this diff. The line count stays 205.
- **Two row skeletons.** **Judgement.** Duplicated Code. `buildAbsentRowElement` and `buildResidentRowElement` each build the indent, the text element, the name element, the file indicator, and the Cancel button. Source: [[plan/context/SMELLS.md]]. The hunk is `cancelBtn.classList.add ActorLive.cancelControlClass` in both functions (`src/Client/RowView.fs` lines 161 and 281).

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

- **Suite.** `bash scripts/check-test.sh 3648` printed `DONE` and `/tmp/ambit-full-suite-20261030-162056.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 728, skipped 1, failed 0. `tests/Shared.Tests` passed 1909, skipped 3, failed 0.
- **Want edge.** ``installWantAnswer refuses a dangling edge`` expects `Ok`, the child id on the parent list, and no Header for that id.
- **Sync edge.** ``applySyncResponse refuses dangling Want edges`` expects `Ok`, the child id on the parent list, and no Header for that id.
- **Replace fact.** ``Graph.replace accepts new child id with no Header`` expects `Ok`, the child id on the parent list, and no Header.
- **Row fact.** ``planPatchDOM CreateRow for visible Absent child id`` expects `CreateRow` for that visible id.

## 5. Miss

- **planPatchDOM length.** Hard. Standards item 1. `planPatchDOM` is 113 lines. The limit is 40.
- **buildResidentRowElement length.** Hard. Standards item 2. `buildResidentRowElement` is 117 lines. The limit is 40.
- **replace length.** Hard. Standards item 3. `replace` is 205 lines. The limit is 40.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

The Found elsewhere section takes the PR body's Found elsewhere list. That list is empty.

- No findings.
