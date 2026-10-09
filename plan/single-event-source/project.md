# Single event source

Stage: build
Summary: One Event source means one ordered stream, with one event id. Connected Events are queued in order, posted in that order, and processed in that order. A later Event does not pass an earlier connected Event. A posted list is applied in order, as one unit; lists from different clients do not interleave. The only rejection is the credential check. That check refuses the whole list before anything applies. Otherwise every posted list applies whole. Then [[plan/core-creation/arch.md]] matches what this Project created.
Updated: 2026-10-09
Started: 2026-09-16
Actual: 33h30m

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
- [15 — Client pending = zero + submissionId](plan/single-event-source/issues/15-client-pending-zero-submissionid.md) — redo 11b. Status `done`.
- [16 — Approve / merge stamp + beforeAll](plan/single-event-source/issues/16-approve-merge-stamp-beforeall.md) — redo 11c. Status `done`.
- [17 — Delete Revision aliases](plan/single-event-source/issues/17-delete-revision-aliases.md) — redo 12a. Status `done`.
- [18 — Delete leftover Change wrapping](plan/single-event-source/issues/18-delete-leftover-change-wrapping.md) — redo 12b. Status `done`.
- [19 — Delete unused EventId.fs](plan/single-event-source/issues/19-delete-unused-eventid-fs.md) — redo 12c. Status `done`.
- [04 — Write core-creation arch.md last](plan/single-event-source/issues/04-write-core-creation-arch-md-last.md) — after contract. Status `coded`.
- [20 — SES smell-cleanup quality criteria](plan/single-event-source/issues/20-ses-smell-cleanup-quality-criteria.md) — Type `research`. Status `needs-info`. Naming + improper type usage; file/function length out of scope except functions a follow-on cleanup touches. Sources: SES 11/11-repair/12 reviews + 34b review.
- [21 — SES smell-cleanup](plan/single-event-source/issues/21-ses-smell-cleanup.md) — apply [SES smell-cleanup quality criteria](plan/single-event-source/reports/ses-smell-cleanup-quality-criteria.md). Status `coded`.
- [22 — One ordered event stream](plan/single-event-source/issues/22-ordered-event-stream.md) — expand ActorStart and Cancel on the events list, migrate `execRunOp` and Cancel onto `syncInfo.pending`, then contract `POST /ambit/command` and `POST /ambit/cancel`. Status `coded`. This ticket lands first. [11 — Server evaluates](plan/online-search/issues/11-server-evaluates.md) rebases onto this ticket.
- [23 — Endpoint switch](plan/single-event-source/issues/23-endpoint-switch-events.md) — client posts the ordered list to `POST /ambit/events`. Remove `POST /ambit/changes`. Status `defined`.

## Notes

- 2026-09-17 — Charted [[plan/single-event-source/issues/20-ses-smell-cleanup-quality-criteria.md|20 — SES smell-cleanup quality criteria]] (research; Status `needs-info`).
- 2026-09-17 — [20 — SES smell-cleanup quality criteria](plan/single-event-source/issues/20-ses-smell-cleanup-quality-criteria.md) Answer filled; checklist [SES smell-cleanup quality criteria](plan/single-event-source/reports/ses-smell-cleanup-quality-criteria.md). Status stays `needs-info` until Alan accepts.
- 2026-10-08 — Alan. One Event source means one ordered stream. Connected Events are queued in order, posted in that order, and processed in that order. A later Event does not pass an earlier connected Event. A posted list is applied in order, as one unit; lists from different clients do not interleave. The only rejection is the credential check. That check refuses the whole list before anything applies. Otherwise every posted list applies whole. Ticket: [22 — One ordered event stream](plan/single-event-source/issues/22-ordered-event-stream.md).
- 2026-10-09 — [23 — Endpoint switch](plan/single-event-source/issues/23-endpoint-switch-events.md) Status `defined`. The ordered list posts to `POST /ambit/events`. `POST /ambit/changes` goes away. Stage stays `build`.

## Related work

- [[plan/core-creation/project.md]] / [[plan/core-creation/arch.md]] — Event stories **Event, EventLog, and ClientHistory** and **Caller, persist, and Poll** are already coded. Do not rewrite those arch checkboxes here.
- [[plan/event-sourced-ops/project.md]] — Actor Change merge semantics. Different Project. Do not duplicate that work.
- [[plan/single-event-source/reports/replan-11-12-smaller-increments.md]] — Alan-approved redo sequence; tickets 13–19.
