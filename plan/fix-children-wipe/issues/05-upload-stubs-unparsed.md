# 05 — Desktop Upload Directory stubs start Unparsed

**Status**: pending confirmation from 01
Blockers: [01 — Run the event-log queries](01-run-event-log-queries.md) — this ticket does not start before the event-log results confirm Rank 4 on [[../project.md]].

## Scope

`WorkspaceUploadStructure.planStubOps` creates a Desktop Upload Directory stub as Parsed, with no children, before any bytes have landed. `ChangeAmendment.markCreatedUnparsed` marks only a "Parse" change Unparsed, so the stub stays Parsed with an empty child list — the filled-circle glyph — until a later Load reparses it. A Directory stub must start Unparsed instead.

## Done

[ ] `planStubOps` creates a Directory stub as Unparsed, not Parsed.
[ ] The stub draws the slashed-hollow glyph (`rowChildrenIndicator`) until a Load parses it, not the filled-circle glyph.
[ ] A test uploads a Directory stub with no children yet received and asserts it is Unparsed.

## Stop

[ ] Stop before ticket 01's results confirm the life-root loss matches this path; this ticket does not start before that confirmation.
[ ] Stop if the fix changes a File Upload stub; only Directory stubs are in scope here.
[ ] Stop if the change reaches outside `WorkspaceUploadStructure.fs` plus its direct callers and tests.
