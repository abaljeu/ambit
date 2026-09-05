# Core creation Wayfinder

## Destination

Define an implementation-ready initial Core increment that routes shared typed Changes through one Core-owned Server path in the existing projects. Retain the later Core API and Actor-pool decisions for future sessions without implementing or resolving them during charting.

## Notes

- The initial increment implements Changes only. Files, Query, Command, and the Actor pool remain later Core decision work.
- Core gets no new fsproj. Shared keeps the Browser-compatible apply implementation, and Server owns the typed produce path.
- Preserve current HTTP, database, file-authority, acknowledgement, timeout, and mirror behavior during extraction. Database authority and view-only file mode remain later work under [[plan/roadmap/epics/chapters/acid-apply.md]].
- Every runtime Change must reach the authoritative Server Graph and History through Core Changes. Named startup and repair paths may remain temporary exceptions until the ACID apply work.
- Future map sessions must follow [[.agents/skills/wayfinder/SKILL.md]]. Grilling tickets must also follow [[.agents/skills/grilling/SKILL.md]] and [[.agents/skills/domain-modeling/SKILL.md]], use [[CONTEXT.md]], and keep the Project current through [[.cursor/skills/project-work/SKILL.md]].

## Decisions so far

- [[plan/core-creation/issues/03-define-typed-core-changes-contract.md|Typed Core Changes contract]] — normal and Parse-only Graph-only operations accept typed Change lists and return typed acceptance facts or the current text Reject while preserving all existing behavior.

## Not yet specified

- Whether the final Core API composition needs a separate decision after the Files, Changes, Query, Command, and Actor-pool contracts are known.

## Out of scope

- The Parse Actor definition belongs to [[plan/roadmap/epics/chapters/actors-supported.md]].
- Advisory soft-lock policy and Browser UI belong to [[plan/event-sourced-ops/project.md]].
- Database authority, view-only file mode, timeout and mirror replacement, and startup or repair authority migration belong to [[plan/roadmap/epics/chapters/acid-apply.md]].
- Incremental Upload and Load belong to [[plan/roadmap/epics/chapters/incremental-operations.md]].
- These are exclusions from this map's initial implementation increment, not product-wide exclusions.
