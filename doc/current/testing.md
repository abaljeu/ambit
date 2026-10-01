# Testing

Category: Capability

See Also:

[Operations](operations.md)
[Persistence model](persistence-model.md)
[Gambol.Shared.Tests](gambol-shared-tests.md)
[Gambol.CloudAgents.Tests](gambol-cloud-agents-tests.md)
[Gambol.Server.Tests](gambol-server-tests.md)

Testing is TDD where that is valuable, and the tests stay fast and layered.

## Job

[x] Test major functionality where that test is easy.
[x] Prefer [src/Shared](../../src/Shared) as the location of the code under test.
[x] Prefer pure functions in Shared: ops, ViewModel planners, and serialization.
[x] Test `DbAgent` and store logic with `TEST_DB_CONNECTION_STRING` when Postgres is available.
[x] Avoid browser automation until it pays off.
[x] Tool: xUnit in [tests/Shared.Tests](../../tests/Shared.Tests), [tests/CloudAgents.Tests](../../tests/CloudAgents.Tests), and [tests/Server.Tests](../../tests/Server.Tests).

## Shared ops

[x] Shared tests cover `applyOp`, `Change.apply`, and undo invariants.
[x] Shared tests cover graph invariants after op batches: child refs exist, the root exists, and ownership rules hold.

## Serialization

[x] Serialization tests cover JSON round-trip for `Op`, `Change`, `Graph`, and API DTOs. File: [SerializationTests.fs](../../tests/Shared.Tests/SerializationTests.fs).

## Persistence

[x] [DbAgentTests.fs](../../tests/Server.Tests/DbAgentTests.fs) runs against real Postgres when `TEST_DB_CONNECTION_STRING` is set.
[x] Replay tests load persisted state, apply changes, and match the expected graph and revision.

## Server

[x] Server tests cover command handlers behind endpoints: revision increment, change append, and conflict behavior when that behavior is added.
[ ] Optional in-memory ASP.NET Core integration tests cover the server.

## Browser

[x] Browser tests cover pure MVU and update helpers where those helpers are extracted.
[x] Baseline: no Playwright.
