# 12 — Drop the residency children line
**Status**: 
One job: The outliner sees the document-state line on a row and no Loaded or Unloaded children line. The story Remove Loaded and Unloaded in [Client never fails on server data](plan/client-never-fails/spec.md) is the reason.

Type: coding
Blockers: [11 — Child list draws the Bullet](plan/client-never-fails/issues/11-child-list-draws-bullet/11-child-list-draws-bullet-ticket.md)

## Inputs

### Working

[ ] `ViewModelRowState.residencyText` in `src/Shared/ViewModelRowState.fs` formats `Residency: {children}, {document}` from `GraphChildren.status`.
[ ] `GraphChildren.status` returns `Loaded` or `Unloaded`.
[ ] Document state is `Current`, `Unparsed`, or `PullingFile`.
[ ] ``bulletTip minimal node lists guid residency and update time only`` in [[tests/Shared.Tests/ViewModelRowStateTests.fs]] expects `Residency: Loaded, Current`.
[ ] ``bulletTip disambiguates hollow by Unloaded versus Unparsed`` in `tests/Shared.Tests/ViewModelRowStateTests.fs` expects `Residency: Unloaded, Current` and `Residency: Loaded, Unparsed`.
[ ] ``bulletTip reads Loaded Current for a parsed leaf`` in `tests/Shared.Tests/ViewModelRowStateTests.fs` expects `Residency: Loaded, Current`.
[ ] `Node.documentStateEnum` is the document-state read the line keeps.
[ ] [[src/Shared/ViewModelRowState.fs]] — `residencyText`.

### Reference

[ ] Scope facts: [[plan/client-never-fails/reports/12-drop-residency-children-line-scope.md]]
[ ] Story order in [[plan/client-never-fails/arch.md]]

## Do not load

[ ] Closed: `GLOSSARY.md`. This ticket leaves the glossary, the architecture, and the spec unchanged.
[ ] Closed: `rowChildrenIndicator`. That Bullet is 11 — Child list draws the Bullet.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `residencyText` so the line names the document state. Delete the children word `Loaded` or `Unloaded`.
3. [ ] Change the three facts named in Outputs.

## Outputs

[ ] `src/Shared/ViewModelRowState.fs`
[ ] `tests/Shared.Tests/ViewModelRowStateTests.fs` — change the three bullet-tip facts

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/12-drop-residency-children-line-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``bulletTip minimal node lists guid residency and update time only``
  - `tests/Shared.Tests`: ``bulletTip disambiguates hollow by Unloaded versus Unparsed``
  - `tests/Shared.Tests`: ``bulletTip reads Loaded Current for a parsed leaf``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/12-drop-residency-children-line-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
