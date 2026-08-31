# transport-layer

Stage: charting
Summary: Cross-cutting transport layer — inbound, outbound, and round-trip patterns for moving information between outside sources and the Graph while Graph stays authority; Parse/Persist as the shared text-processing unit; module contract for connector Actors; `plan` until promoted to `doc/`.
Updated: 2026-08-31

## Objective

Chart how arbitrary outside sources connect to Gambol: materialize external data into the Graph, push Graph slices outward, and round-trip editable copies without a second truth. Every transport module plans from a Local Graph and emits **Changes** through the ESO Actor path.

## Dependencies

- **Depends on:** [[plan/event-sourced-ops/project.md]] — Actor produce path, merge, job identity, soft-lock.
- **Uses (legs):** [[plan/document-formats/map.md]] (codec Parse/Persist), [[plan/llm-connector/project.md]] (agent Actor), [[plan/selective-client-loading/project.md]] (Load/residency), workspace file sync Projects (file channel).
- **Enables:** [[plan/roadmap/epics/operate-a-pkm.md]] — PKM consumes and navigates transported material; it does not implement the transport layer.

## Out of scope (this Project)

- PKM find/navigate, graph view, expression-language.
- Generate-from-data (reports, derived content, LLM output pipelines beyond connector contract).
- ESO merge semantics, wire protocol, permanent history.
- Promotion to [[doc/]] — not there yet.

## Notes

- Start at [[overview.md]] — what transport-layer is and the three flows.
- Map of legs and future connectors: [[map.md]].
- Parse/Persist primitive: [[details/parse-persist.md]].
- Framing report: [[plan/roadmap/reports/hub-epic-framing.md]].
- Scope vs product commitment: [[doc/agents/scope-vs-commitment.md]].
