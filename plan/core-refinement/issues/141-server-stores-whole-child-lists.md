# 141 — Server stores whole child lists

**Status:** `defined`

One job: When the server records a change to the order or membership of a node's children, the record always holds the node's full list of children before the change and the full list after it. Then any page can copy the new list as it is, without checking it ([[plan/core-refinement/reports/missing-changes-reload-findings.md]] §1 Model).

Type: coding

Blockers: none

## Inputs

### Working

- [[src/Server/Core/CoreEventDispatch.fs]]
  - `persistNew` runs `persist`, then `store`
  - `withConfirmedOps` writes the server-confirmed ops into the stored event
  - the stored event is the place to normalize
- [[src/Shared/ChangeAmendment.fs]]
  - `applyMarked` returns the applied ops
  - `tryAmendReplace` already emits a whole-list `ChildListWire.replace parentId current target`
- [[src/Shared/GraphMutate.fs]]
  - `replace` applies the old span at index 0
  - a prefix span is legal today, so a stored `Replace` can name part of a list
- [[src/Shared/ChildListWire.fs]]: the whole-list `replace` helper; a convention only
- [[src/Shared/History.fs]]: `Op.Replace`, and `Op.invert` (Undo flips old and new)
- [[tests/Server.Tests/DbAgentTests.fs]], or a test file under [[tests/Server.Tests/]] that already drives `CoreEventDispatch.postEvent`

### Reference

- [[plan/core-refinement/reports/missing-changes-reload-findings.md]] §1, §2.2, and §3
- [[plan/core-refinement/cqrs-messages.md]]

## Do not load

- client code
- `GraphMutate.replace` placement checks
- parse producers ([[src/Shared/documents/DocumentColdParse.fs]] and others); the server normalizes what they post

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Expand the confirmed-ops step: each applied `Op.Replace (parent, old, new)` is stored as `Op.Replace (parent, fullBefore, fullAfter)`. `fullBefore` is the parent's child list before that op. `fullAfter` is the list after it. Read both from the graph the server applied the op to.
3. [ ] Keep every other op as applied.
4. [ ] Expand the tests in Outputs.

## Outputs

- Changed: [[src/Server/Core/CoreEventDispatch.fs]], or the confirmed-ops helper in [[src/Shared/ChangeAmendment.fs]]
- Added tests:
  - `Server`: ``Stored Replace holds the whole child list before and after``
  - `Server`: ``A prefix Replace is stored as the whole list``
  - `Server`: ``Undo of a stored Replace restores the whole list``

## Stop when

[ ] Record the stop at `plan/core-refinement/reports/141-server-stores-whole-child-lists-stop.md`, then ask.

- the fix needs a file outside Inputs
- the stored op must differ from the op the server applied for a reason that the code states
- a client test depends on a stored prefix `Replace`

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `Server`: ``Stored Replace holds the whole child list before and after``
  - `Server`: ``A prefix Replace is stored as the whole list``
  - `Server`: ``Undo of a stored Replace restores the whole list``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/core-refinement/reports/141-server-stores-whole-child-lists-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
