# Resolve Server-side search

Recorded the HITL lock on [[plan/expression-language/issues/14-server-side-search.md|Server-side search]]. Status is resolved.

## Changed

- Ticket Answer: Find/Move language matcher (`=`) evals on the server Graph. Run stays on the client Graph. Word search without `=` stays today’s client word search.
- Amendment: all eval is local; server postponed.
- Map [[plan/expression-language/map.md]] Decisions so far: 14 gist added.
- [[plan/expression-language/spec-draft.md]] top-level context and Unloaded walks.
- [[plan/expression-language/issues/06-top-level-context-node-versus-text.md|Top-level context]] Search/Move paragraph.

## Frontier

No open expression-language decision tickets. Later: quoted path segments; number-returning functions and shell `> …`.
