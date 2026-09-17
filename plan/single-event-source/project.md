# Single event source

Stage: build
Summary: Core creation is suspended. This Project creates a single Event source with one event id, then corrects [[plan/core-creation/arch.md]] to match what was created.
Updated: 2026-09-17
Started: 2026-09-16
Actual: 9h30m

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
- [10 — Boot IndexedDB](plan/single-event-source/issues/10-boot-indexeddb.md) — migrate. Status `defined`.
- [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md) — migrate. Status `defined`.
- [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md) — contract. Status `defined`.
- [04 — Write core-creation arch.md last](plan/single-event-source/issues/04-write-core-creation-arch-md-last.md) — after contract. Status `defined`.

## Related work

- [[plan/core-creation/project.md]] / [[plan/core-creation/arch.md]] — Event stories **Event, EventLog, and ClientHistory** and **Caller, persist, and Poll** are already coded. Do not rewrite those arch checkboxes here.
- [[plan/event-sourced-ops/project.md]] — Actor Change merge semantics. Different Project. Do not duplicate that work.
