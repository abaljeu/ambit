# Single event source

Stage: build
Summary: Core creation is suspended. This Project creates a single Event source with one event id, then corrects [[plan/core-creation/arch.md]] to match what was created.
Updated: 2026-09-18
Started: 2026-09-16
Actual: 26h30m

Home: [[plan/roadmap/epics/robust-outliner.md]] Required for done (Live). Chart map: [[map.md]].

## Map

- [[plan/single-event-source/map.md]]

## Architecture

- [[plan/single-event-source/arch.md]] — Sequence expand-contract. Leftover Change and Revision.

## Issues

- [05 — Expand Op-list apply](plan/single-event-source/issues/05-expand-op-list-apply.md) — expand. Status `done`.
- [06 — Compile preamble](plan/single-event-source/issues/06-compile-preamble.md) — migrate. Status `done`.
- [07 — Persist apply](plan/single-event-source/issues/07-persist-apply.md) — migrate. Status `done`.
- [08 — Core doors](plan/single-event-source/issues/08-core-doors.md) — migrate. Status `done`.
- [09 — Command mint](plan/single-event-source/issues/09-command-mint.md) — migrate. Status `done`.
- [10 — Boot IndexedDB](plan/single-event-source/issues/10-boot-indexeddb.md) — migrate. Status `done`.
- [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md) — migrate. Status `coded`. Historical land; redo is 13–19.
- [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md) — contract. Status `coded`. Historical land; redo is 13–19.
- [13 — Revision always 0 (diagnostic)](plan/single-event-source/issues/13-revision-always-zero.md) — redo 11z. Status `done`.
- [14 — EventId serial on Shared + Server](plan/single-event-source/issues/14-eventid-serial-shared-server.md) — redo 11a. Status `coded`.
- [15 — Client pending = zero + submissionId](plan/single-event-source/issues/15-client-pending-zero-submissionid.md) — redo 11b. Status `coded`.
- [16 — Approve / merge stamp + beforeAll](plan/single-event-source/issues/16-approve-merge-stamp-beforeall.md) — redo 11c. Status `defined`.
- [17 — Delete Revision aliases](plan/single-event-source/issues/17-delete-revision-aliases.md) — redo 12a. Status `defined`.
- [18 — Delete leftover Change wrapping](plan/single-event-source/issues/18-delete-leftover-change-wrapping.md) — redo 12b. Status `defined`.
- [19 — Delete unused EventId.fs](plan/single-event-source/issues/19-delete-unused-eventid-fs.md) — redo 12c. Status `defined`.
- [04 — Write core-creation arch.md last](plan/single-event-source/issues/04-write-core-creation-arch-md-last.md) — after contract. Status `coded`.
- [20 — SES smell-cleanup quality criteria](plan/single-event-source/issues/20-ses-smell-cleanup-quality-criteria.md) — Type `research`. Status `needs-info`. Naming + improper type usage; file/function length out of scope except functions a follow-on cleanup touches. Sources: SES 11/11-repair/12 reviews + 34b review.

## Notes

- 2026-09-17 — Charted [[plan/single-event-source/issues/20-ses-smell-cleanup-quality-criteria.md|20 — SES smell-cleanup quality criteria]] (research; Status `needs-info`).
- 2026-09-17 — [20 — SES smell-cleanup quality criteria](plan/single-event-source/issues/20-ses-smell-cleanup-quality-criteria.md) Answer filled; checklist [SES smell-cleanup quality criteria](plan/single-event-source/reports/ses-smell-cleanup-quality-criteria.md). Status stays `needs-info` until Alan accepts.

## Related work

- [[plan/core-creation/project.md]] / [[plan/core-creation/arch.md]] — Event stories **Event, EventLog, and ClientHistory** and **Caller, persist, and Poll** are already coded. Do not rewrite those arch checkboxes here.
- [[plan/event-sourced-ops/project.md]] — Actor Change merge semantics. Different Project. Do not duplicate that work.
- [[plan/single-event-source/reports/replan-11-12-smaller-increments.md]] — Alan-approved redo sequence; tickets 13–19.
