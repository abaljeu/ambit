# Publish tickets — delegated report

Parent-facing summary of `/to-tickets` step 5 publish. Branch: `w/event-sourced-ops` ([[git.md]]). Approved source: [[to-tickets-draft.md]].

## Published files (12)

| ID | File |
| --- | --- |
| 01 | [[issues/01-shared-success-envelope-expand.md]] |
| 02 | [[issues/02-independent-concurrent-changes-succeed.md]] |
| 03 | [[issues/03-server-amends-recoverable-field-collisions.md]] |
| 04 | [[issues/04-client-consumes-merge-success-without-reload.md]] |
| 05 | [[issues/05-child-list-accept-both.md]] |
| 06 | [[issues/06-recovery-safety-decisions.md]] |
| 07 | Moved to [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] |
| 08 | [[issues/08-parse-file-realignment-tracer.md]] |
| 09 | [[issues/09-job-identity-with-advisory-soft-lock.md]] |
| 10 | [[issues/10-child-list-approximation-polish.md]] |
| 11 | [[issues/11-completing-ops-pattern-beyond-timing.md]] |
| 12 | [[issues/12-unrestricted-undo-desirability-decision.md]] |

Each issue: Context, What to build, Blocked by, See also, Status `ready-for-agent`, acceptance checkboxes.

Current ownership differs from this historical publish: [[plan/core-creation/project.md]] owns former ESO 07 and the Core pool machinery extracted from ESO 09. ESO retains Parse in 08 and advisory soft-lock behavior with Browser-facing job access in 09.

## Dependency graph

- `01 ∥ 02 → 03 → 04 → 05`
- `03+04 → 06` (decision only)
- `03+04 → Core 01 → ESO 08`
- `Core 01 → Core 02 → ESO 09`; ESO 08 also blocks ESO 09
- `05 → 10`
- `Core 01 → 11`
- `04 → 12` (decision only)

## Frontier (unblocked, ready-for-agent)

- **01** — Shared success envelope expand (behavior-identical)
- **02** — Independent concurrent Changes succeed

## Stage and index

- [[project.md]] Stage set to `tickets`; Updated 2026-08-22; issues index pointer added.
- [[../index.md]] regenerated: 18 live projects; event-sourced-ops row shows `tickets`.

## Verification

- 12 issue files under `plan/event-sourced-ops/issues/`.
- Numbers, titles, and Blocked-by edges match approved draft.
- All Status lines `ready-for-agent`.
- No software edits; WORK.md not edited; no commit.

## Recommended WORK.md mutations (parent)

- **remove** Active [[plan/event-sourced-ops/to-tickets-draft.md]] quiz/publish entry (publish complete).
- **add** Pending frontier: [[plan/event-sourced-ops/issues/01-shared-success-envelope-expand.md]] and [[plan/event-sourced-ops/issues/02-independent-concurrent-changes-succeed.md]] — start critical-flaw elimination (parent: [[plan/event-sourced-ops/project.md]]).
- **note** [[plan/relaxed-concurrency/]] is a build-upon layer (Stage done); gate removal delivered in issue 02.
