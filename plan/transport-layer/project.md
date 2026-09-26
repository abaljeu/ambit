# transport-layer

Stage: chart
Summary: Cross-cutting transport layer — inbound, outbound, and round-trip patterns for moving information between outside sources and the Graph while Graph stays authority; Parse/Persist as the shared text-processing unit; module contract for connector Actors; `plan` until promoted to `doc/`.
Updated: 2026-09-26

## Objective

Chart how arbitrary outside sources connect to Gambol: materialize external data into the Graph, push Graph slices outward, and round-trip editable copies without a second truth. Every transport module plans from a Local Graph and emits **Changes** through the ESO Actor path.

## Dependencies

- **Depends on:** [[plan/event-sourced-ops/project.md]] — Actor produce path, merge, job identity, soft-lock.
- **Uses (legs):** [[plan/document-formats/map.md]] (codec Parse/Persist), [[plan/llm-connector/project.md]] (agent Actor), [[plan/browser-residency/project.md]] (want-driven residency; successor to done [[plan/selective-client-loading/project.md]]), workspace file sync Projects (file channel), [[plan/github-transport/project.md]] (external GitHub remote).
- **Enables:** [[plan/roadmap/epics/operate-a-pkm.md]] — PKM consumes and navigates transported material; it does not implement the transport layer.

## Out of scope (this Project)

- PKM find/navigate, graph view, expression-language.
- Generate-from-data (reports, derived content, LLM output pipelines beyond connector contract).
- ESO merge semantics, wire protocol, permanent history.
- Promotion to [[doc/]] — not there yet.

## Notes

- 2026-09-26 — Leftover [[doc/roadmap/workspace-file-sync.md]] is a high-level pointer only. File-channel (WebDAV Upload/Download) detail stays on this Project, [[plan/auto-download-persisted-files/project.md]], [[doc/roadmap/workspace-webdav.md]], and [[doc/roadmap/workspace-upload-client-structure.md]]. External GitHub detail stays on [[plan/github-transport/project.md]]. Do not grow workspace-file-sync back into a dual-chapter dump.
- 2026-09-26 — Connector leg [[plan/github-transport/project.md]]: external GitHub remote pull/push Actor (FF-only, Server-side). WebDAV Upload/Download stay Ambit↔Ambit on this Project / [[plan/auto-download-persisted-files/project.md]]. github-transport is not a second WebDAV transit Project.
- 2026-09-19 — **Owns** the Workspace file-channel redesign chart: FETCH (Start Actor → pull → Parse → Changes → done) and UPDATE (Start Actor → Persist → push → done); ORGANIZE stays Graph authority. Roadmap home: Chapter [[plan/roadmap/epics/chapters/automatic-upload-and-download.md]] on [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]. Implementation stays on file-channel Projects (auto-download, future auto-upload, Parse File / ESO). Do not invent a Client-only sync path that bypasses ActorStart/Stop.
- Start at [[overview.md]] — what transport-layer is and the three flows.
- Map of legs and future connectors: [[map.md]].
- Parse/Persist primitive: [[details/parse-persist.md]].
- Framing report: [[plan/roadmap/reports/hub-epic-framing.md]].
- Scope vs product commitment: [[doc/agents/scope-vs-commitment.md]].
