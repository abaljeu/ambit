# Pending audit — Bullet tooltip HITL

Date: 2026-08-15

## Verdict

`not established`

Board advice: `block`

Block on a named human browser check of all three Bullet variants and their existing interactions.
If the root applies that block, set this project's stage to `blocked` and regenerate
[[.scratch/index.md]].

## Evidence

- [[.scratch/node-bullet-tooltip/issues/02-client-bullet-tip-wiring.md]] still says
  `ready-for-agent`; every checklist item is unchecked, including the explicit Manual/HITL check
  for chevron, solid circle, hollow circle, and unchanged click/double-click/fold/zoom behavior.
- Commit `21d1cb501837a0b627bdc546184a4412a2087769` (`bullet tips`, 2026-08-08) added
  the implementation and recorded the project as `active`, “awaiting HITL hover verification.” It
  also added the same verification item to WORK.md.
- Commit `b07275cb12db0cc327826c9673b118cfc6dc740d` later changed the project to `done`
  and removed the board item, but its message and diff contain no browser result, operator
  attestation, completed checklist, or verification note. Commit
  `d1d46d874946fdd3e50a68402b6b5d2ab0f26d61` restored the exact HITL item 25 minutes
  later, confirming that the requirement was still considered outstanding.
- The current `done` statements in [[.scratch/node-bullet-tooltip/project.md]] and
  [[.scratch/index.md]] are delivery assertions, not evidence of the requested interaction check.
  Repository and local transcript searches found only the outstanding board wording, not a pass
  result. The repository has no Git notes ref.
- [[src/Client/RowView.fs]] builds one `nodeBullet` from all three
  `RowChildrenIndicator` variants and attaches the generated `title` to that element.
  The implementation diff renamed the listener parameter but left the fold, zoom, activation, and
  double-click listener logic unchanged. [[src/Client/JsInterop.fs]] supplies the browser-local time
  formatter.
- [[tests/Shared.Tests/ViewModelRowStateTests.fs]] verifies formatter content and ordering,
  including hollow-state disambiguation and chevron-versus-leaf stability. It does not exercise a
  browser native tooltip or pointer interactions.

No test was run for this audit: another Shared test cannot close the explicit browser-HITL gap.

## Smallest closing check

In one browser outline containing a non-root chevron, a solid circle, and a hollow circle:

1. Hover each glyph until its native title appears and confirm all three show the Bullet facts.
2. Confirm the chevron still single-clicks to fold/unfold and double-clicks to zoom; confirm each
   circle's single-click and double-click behavior is unchanged.
3. Record one durable line here or in issue 02:
   `PASS — <date/browser/build>; all three tips shown; click/double-click/fold/zoom unchanged.`
