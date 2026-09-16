# Single event source

Stage: chart
Summary: Event is the sole durable source of truth for Graph history and apply — persist only [[src/Server/EventLogFile.fs]] `gambol.events` and the `events` table; Change is an EventBody subtype; `postGraphOnlyChange` is `postChange` minus the file write; no parallel Change log or dual persist.
Updated: 2026-09-16

Home: [[plan/roadmap/epics/robust-outliner.md]] Required for done (Live). Chart map: [[map.md]].

## Related work

- [[plan/core-creation/project.md]] / [[plan/core-creation/arch.md]] — Event stories **Event, EventLog, and ClientHistory** and **Caller, persist, and Poll** are already coded. Do not rewrite those arch checkboxes here.
- [[plan/event-sourced-ops/project.md]] — Actor Change merge semantics. Different Project. Do not duplicate that work.
