# Plan initial Core Changes implementation

Date: 2026-09-05

## Changed

- Added [[plan/core-creation/initial-core-changes-implementation.md]] as the durable Project-root implementation plan for resolved issues 03–06.
- Corrected the plan to make [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] and [[plan/core-creation/issues/13-delete-runtime-mirror-and-remove-production-persistence-mode.md]] later work. The direct typed seam tests only prove that issue 01 can start later.
- Kept the plan link on issue 01 and labeled it as enabling work. Removed the 5m planning entry from issue 01 and assigned it to [[plan/core-creation/issues/06-ready-the-initial-core-changes-increment.md]].
- Kept [[plan/core-creation/project.md]] at Stage `spec` with Updated 2026-09-05. Restored Project Actual to 4h10m: the pre-session 3h20m historical aggregate plus 45m for issue 06 grilling and 5m for this implementation plan. Current explicit issue Actual fields do not represent all historical Project work, so no unsupported issue-level allocation was added.
- Kept the existing Stage `spec` row in [[plan/index.md]]. The correction changed no metadata shown in the overview, so it did not require another regeneration.

## Verification

- Read the resolved issues, current Core reports, Graph-agent source, compile lists, HTTP routes, and focused Server test modules before writing the plan.
- Confirmed the plan has Goal, scope, non-goals, settled constraints, evidence, four ordered implementation steps, focused commands, and an acceptance gate for issues 03–06 only.
- Confirmed the plan does not deliver issue 01, add a production Server Actor, implement issue 13, change mode selection, delete the mirror, or add mirror tests.
- Confirmed the obsolete mirror wrapper appears only as the minimum mechanical typed-signature adaptation needed for the issues 03–06 signature change to compile with behavior unchanged.
- Confirmed [[plan/core-creation/project.md]] and [[plan/index.md]] both record Stage `spec`, and the overview still has 35 Project rows.
- Confirmed issue 06 remains at 50m and Project Actual preserves the 4h10m historical aggregate.
- Confirmed the plan is linked from issue 01 as enabling work and from the Project as the issues 03–06 implementation plan.
- Confirmed no two consecutive empty Markdown lines and no whitespace errors in the tracked diff.
- Reviewed the focused diff for the plan, issue 01 link, issue 06 time, Project, overview, and this report. No source, test, map, later issue content, or commit work was performed.
