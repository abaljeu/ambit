# Open question G — decision report

Date: 2026-08-19

## Decision

G answered **YES** — weak-form contiguous-run matching via **client merge-sync** (slices 2–3), not server-side silent relocation in `Graph.replace`. Client replan preferred over server replan.

## Canonical references

- Protocol, slice layering, blockers: [[map.md#Client merge-sync — RESOLVED (G)]]
- Full client-vs-server rationale: [[design.md#Client vs server replan]]
- Slice 1 acceptance criteria unchanged: [[spec.md]]

## Open forks (slices 2–3)

Reject ack wire shape; partial batch vs fail-fast; optimistic graph rebuild before replan.
