# Single event source

Stage: build
Summary: Core creation is suspended. This Project creates a single Event source with one event id, then corrects [[plan/core-creation/arch.md]] to match what was created.
Updated: 2026-09-16
Started: 2026-09-16
Actual: 2h30m

Home: [[plan/roadmap/epics/robust-outliner.md]] Required for done (Live). Chart map: [[map.md]].

## Map

- [[plan/single-event-source/map.md]]

## Architecture

- [[plan/single-event-source/arch.md]] — Sequence expand-contract. Leftover Change and Revision.

## Issues

- [[plan/single-event-source/issues/05-expand-op-list-apply.md|05 — Expand Op-list apply]] — expand. Status `coded`.
- [[plan/single-event-source/issues/06-compile-preamble.md|06 — Compile preamble]] — migrate. Status `coded`.
- [[plan/single-event-source/issues/07-persist-apply.md|07 — Persist apply]] — migrate. Blocked by 05, 06.
- [[plan/single-event-source/issues/08-core-doors.md|08 — Core doors]] — migrate. Blocked by 05, 07.
- [[plan/single-event-source/issues/09-command-mint.md|09 — Command mint]] — migrate. Blocked by 05, 08.
- [[plan/single-event-source/issues/10-boot-indexeddb.md|10 — Boot IndexedDB]] — migrate. Blocked by 05.
- [[plan/single-event-source/issues/11-one-serial-event-id.md|11 — One serial event id]] — migrate. Blocked by 05.
- [[plan/single-event-source/issues/12-contract-leftover-change-and-revision.md|12 — Contract leftover Change and Revision]] — contract. Blocked by 06–11.
- [[plan/single-event-source/issues/04-write-core-creation-arch-md-last.md|04 — Write core-creation arch.md last]] — after contract.

## Related work

- [[plan/core-creation/project.md]] / [[plan/core-creation/arch.md]] — Event stories **Event, EventLog, and ClientHistory** and **Caller, persist, and Poll** are already coded. Do not rewrite those arch checkboxes here.
- [[plan/event-sourced-ops/project.md]] — Actor Change merge semantics. Different Project. Do not duplicate that work.
