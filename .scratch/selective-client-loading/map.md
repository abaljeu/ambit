# Selective client loading

## Destination

Resolve every product, domain, and architectural decision needed for `/to-spec` to produce a complete, coherent, implementation-ready selective client loading specification. Implementation itself is outside this map.

## Notes

- This is an independently shippable client-only phase of the broader [[doc/roadmap/on-demand-graph-residency.md]]. The server remains fully resident, and later server-residency work may replace this phase's Workspace granularity and protocol.
- [[doc/roadmap/selective client loading.amb]] is a preliminary historical concept, not a current requirement or decision store.
- Tickets 01–13 are resolved historical deliberation. [[.scratch/selective-client-loading/issues/14-simplify-selective-loading.md]] is the sole current decision and supersedes them wherever they differ.
- Ticket 14 is the simplified model.  Future developments may revisit 01-13 to add sophistication.
- Residency grows monotonically by complete Workspace within one webpage session. Refresh starts a new session; eviction and re-unloading are outside this effort.
- `Loaded` means the client received an authoritative complete direct-child list, including an empty one. Unloaded nodes remain distinct from loaded leaves.

## Decisions so far

- [[.scratch/selective-client-loading/issues/14-simplify-selective-loading.md]]: use monotonic complete-Workspace residency, explicit full-selection Load, serialized projected synchronization, and one shared structural guard for every local Change plan including MoveSelected; the Move dialog does not offer Unloaded destinations.

## Not yet specified
- [[.scratch\event-sourced-ops\overview.md]] is a standard of behavior, established after this project began, but to be met by this project.  The local spec does not yet take it into account.

## Out of scope

- Implementing selective client loading or producing implementation slices.
- Partial server residency, lazy server cache admission, server startup de-residency, or server eviction. A server endpoint needed by the client mechanism remains eligible while the server graph stays fully resident.
- Client eviction, re-unloading, or passive reclamation during a webpage session.
- A configurable loading-policy framework, alternative policies, or speculative future loading scopes.
