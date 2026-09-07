---
name: add-shared-test
description: Adds pure xUnit tests in tests/Shared.Tests with fixtures and fsproj registration. Use when adding test coverage, writing repro tests, or extending Shared domain test suites.
---

# Add Shared Test

Augments [[.agents/skills/implement/SKILL.md]] for Shared.Tests coverage. Follow [[.agents/rules/fsharp-source.md]]. Run tests per [[.agents/skills/implement-fsharp-feature/SKILL.md]].

## Project setup

- Location: `tests/Shared.Tests/` — references `src/Shared/` only.
- Register new files in [[tests/Shared.Tests/Gambol.Shared.Tests.fsproj]] `<Compile Include="..."/>` in dependency order.
- Database, HTTP, or FileAgent behavior → `tests/Server.Tests/`, not here.

## Reuse fixtures

Before building graphs by hand, check existing helpers:

| Helper | Use for |
|--------|---------|
| [[tests/Shared.Tests/RefExprTestTree.fs]] | Workspace + file subtree, ref expressions |
| [[tests/Shared.Tests/SpecialNodeTestHelpers.fs]] | Special nodes |
| `ModelBuilder` / `Graph.create ()` | Minimal trees |
| Module-local helpers in nearby test files | Local patterns |

Match naming: backtick F# test names, xUnit `[<Fact>]` / `[<Theory>]`.

## Workflow

1. Find the closest existing test file for the behavior.
2. Add the failing case (or new file if the concern is distinct).
3. Update the fsproj if you added a file.
4. Run tests per [[.agents/skills/implement-fsharp-feature/SKILL.md]].
