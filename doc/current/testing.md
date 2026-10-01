# Testing

Category: Architecture
See Also: [[doc/current/arch.md]], [[doc/current/operations.md]], [[doc/current/persistence-model.md]]

Goal: TDD where valuable; keep tests fast and layered.

## Is

All major functionality should be tested where easy; prefer `src/Shared` for code location.

Workflow: smallest failing test → minimal implementation → refactor.

Bias:

- Prefer pure functions in Shared (ops, ViewModel planners, serialization)
- Server: test `DbAgent` / store logic with `TEST_DB_CONNECTION_STRING` when Postgres available
- Avoid browser automation until it pays off

Domain/ops unit tests:

- `applyOp` / `Change.apply` / undo invariants (Shared.Tests)
- Graph invariants after op batches (child refs exist, root exists, ownership rules)

Serialization tests:

- JSON round-trip for `Op`, `Change`, `Graph`, API DTOs (`SerializationTests.fs`)

Persistence tests:

- DB: `DbAgentTests.fs` against real Postgres when `TEST_DB_CONNECTION_STRING` is set
- Legacy file: snapshot + log replay (`FileAgent` / document loader tests — rollback path only)
- Replay: load persisted state → apply changes → matches expected graph/revision

Server tests:

- Command handlers behind endpoints (revision increment, change append, conflict behavior when added)

Browser tests:

- Pure MVU/update helpers where extracted; no Playwright in baseline

## Should Become

- [ ] Optional later: in-memory ASP.NET Core integration tests

## Where

Tooling: **xUnit** in [[tests/Shared.Tests]] and [[tests/Server.Tests]].

- [[tests/Shared.Tests/SerializationTests.fs]]
- [[tests/Server.Tests/DbAgentTests.fs]]
