# transport-layer

Stage: chart
Summary: Cross-cutting transport layer — inbound, outbound, and round-trip patterns for moving information between outside sources and the Graph while Graph stays authority; Parse/Persist as the shared text-processing unit; module contract for connector Actors; `plan` until promoted to `doc/`.
Updated: 2026-10-07

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

- 2026-10-07 — Alan lock: auto-upload as a sibling Project is withdrawn. Keep-files-current outbound is UPDATE = Persist (Graph → text) then push. File-channel implementation stays under Chapter [[plan/roadmap/epics/chapters/automatic-upload-and-download.md]], on the Projects this chart names. [07 — Chart Automatic upload and download onto Projects](plan/roadmap/issues/07-chart-automatic-upload-and-download.md) is done.
- 2026-09-26 — Addressed leftover `doc/roadmap/workspace-file-sync.md` and deleted it. Implemented WebDAV Upload / Download is [[doc/current/workspace-file-sync.md]]. File-channel redesign chart stays on this Project. Auto-download remainder stays [[plan/auto-download-persisted-files/project.md]]. Leftover DAV / client-structure detail: [[doc/roadmap/workspace-webdav.md]], [[doc/roadmap/workspace-upload-client-structure.md]]. External GitHub detail stays on [[plan/github-transport/project.md]].
- 2026-09-26 — Connector leg [[plan/github-transport/project.md]]: external GitHub remote pull/push Actor (FF-only, Server-side). WebDAV Upload/Download stay Ambit↔Ambit on this Project / [[plan/auto-download-persisted-files/project.md]]. github-transport is not a second WebDAV transit Project.
- 2026-09-19 — **Owns** the Workspace file-channel redesign chart: FETCH (Unparsed → pull → push onto the one parse thread → Changes → done) and UPDATE (graph edit → Unpersisted → Core Persist stack → push → done); ORGANIZE stays Graph authority. Persist is not an Actor. Nobody starts an Actor after pull. Roadmap home: Chapter [[plan/roadmap/epics/chapters/automatic-upload-and-download.md]] on [[plan/roadmap/epics/work-with-text-files-from-anywhere.md]]. Implementation stays on file-channel Projects under that Chapter (auto-download; UPDATE = Persist then push; Parse File / ESO). Auto-upload is not a separate Project. Do not invent a Client-only sync path that bypasses ActorStart/Stop.
- 2026-09-28 — Alan lock: Core alone knows where files reside. Everyone else has a relative path. One hardened control point. Map: [[map.md]] Decisions so far.
- Start at [[overview.md]] — what transport-layer is and the three flows.
- Map of legs and future connectors: [[map.md]].
- Parse/Persist primitive: [[details/parse-persist.md]].
- Framing report: [[plan/roadmap/reports/hub-epic-framing.md]].
- Scope vs product commitment: [[doc/agents/scope-vs-commitment.md]].
