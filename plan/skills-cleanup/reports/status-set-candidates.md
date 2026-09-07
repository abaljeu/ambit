# Status set candidates

Grill analysis for [[plan/skills-cleanup/project.md]]. Lists will live in [[doc/agents/]] (Q3). Not a locked definition.

Evidence: [[plan/skills-cleanup/reports/status-surface-inventory.md]]. Live triage list is five roles and does not include `done`, though implementation issues already use `Status: done`.

## What each kind is asking

**Stage** (Project, Epic): where is this effort on the plan-to-delivery arc?

**Status** (issue): what happens next on this work item?

Those are different questions. Shared words (`grilling`, `blocked`, `done`) make a merge look cheaper than it is.

## Set A — Stage only (Project / Epic)

`grilling` | `charting` | `steering` | `spec` | `tickets` | `active` | `blocked` | `done` | `dead`

- `grilling` is a directive, then the Project moves to `charting`.
- `steering` is Roadmap-only. An Epic must not use it.
- `blocked` is a wait state. `Blocked by:` is a different field (dependency list).

## Set B — Status only (every issue `Status:` writer, unified)

`grilling` | `needs-triage` | `needs-info` | `ready-for-agent` | `ready-for-human` | `claimed` | `done` | `wontfix`

| Today | Maps to |
| --- | --- |
| Triage five roles | same strings, plus `done` (already in use, missing from [[doc/agents/triage-labels.md]]) |
| Wayfinder `open` | `ready-for-agent` or `ready-for-human` (from `Type:`, not a second Status enum) |
| Wayfinder `claimed` | `claimed` |
| Wayfinder `resolved` | `done` |
| Issue `Status: grilling` / `Stage: grilling` | `Status: grilling` only (stop putting Stage on an issue) |

Frontier scan: Status is `ready-for-agent` or `ready-for-human`, and every `Blocked by:` entry is `done`. No `open`. Git place `ready` is not in this set.

Optional later: issue-level `blocked` vs keep wait only as `Blocked by:`.

## Set M — one enum for Stage and Status

Shared without lying: `grilling`, `blocked`, `done`. Near: `dead` ≈ `wontfix`.

Does not fit:

- `charting`, `spec`, `tickets`, `steering` are not issue next-actions.
- `ready-for-agent` / `ready-for-human` / `needs-triage` / `needs-info` are not a Project arc.
- `claimed` vs Project `active` are both "work underway" on different entities.
- `open` vs `charting` are not the same: takeable ticket vs unnamed destination.

A single list that is honest for both entities does not exist. Encoding arc and next-actor in one atomic state still needs two axes; that is a cleaner split, not a merge.

## Other Status fields (not Sets A/B)

Keep named, not folded into A or B, unless a later round says otherwise:

- ADR: `proposed` | `accepted` | `deprecated` | `superseded by …`
- Learning record: `active` | `superseded by …` (`active` already collides with Stage)
- Git places: `dev` | `ready` | `master` (not a `Status:` field)

## Recommendation

Keep Set A and Set B. Do not adopt Set M. Unify all issue `Status:` writers onto Set B. Define both lists in [[doc/agents/]].
