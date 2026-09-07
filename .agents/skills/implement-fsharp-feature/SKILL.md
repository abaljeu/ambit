---
name: implement-fsharp-feature
description: Implements Gambol F# features with Shared-first logic and surgical diffs. Use when adding or changing behavior in src/Shared, src/Client, or src/Server.
---

# Implement F# Feature

Augments [[.agents/skills/implement/SKILL.md]] for F# layout and test commands. TDD quality: [[.agents/skills/tdd/SKILL.md]]. Follow [[.cursor/rules/fsharp-source.mdc]] and [[.cursor/rules/core-agent-behavior.mdc]].

See [[doc/arch.md]] for layer boundaries.

## Implementation layout

1. src/Shared/ pure functions and ops.  Used by both .net and fable compilers, and by tests.  
ALL non-interacting logic belongs here.
- based on library dependencies
- so it can reused by the different layers of the application.
- so that it is possible to unit-test.
- This does not apply to web UI.

- Rarely, code that only compiles in .net will go here if it's required by multiple consumers.
2. src/Desktop/ - .net browser wrapper providing host machine access.
3. src/Client/ - Fable-compiled F# webpages.
4. src/Server/ - .net backend.
5. src/Server/wwwroot - target of Client build, and permanent residence of web-native sources
6. Tests/ - .net tests for shared and server.
Shared.Tests coverage — use [[.agents/skills/add-shared-test/SKILL.md]] when adding tests.

**Foreground** (related tests only). Do not use vscode's test runner; it hangs. Use `dotnet test` with a filter.

```bash
dotnet build tests/Shared.Tests -c Debug
dotnet test tests/Shared.Tests -c Debug --no-build --filter "FullyQualifiedName~YourTestModule"
```

When Client dependencies changed (`src/Shared/`, `src/Client/`, or anything the Client fsproj references, including Shared documents when that project is in the Client graph), `dotnet test` on Shared.Tests is not enough. Run `./scripts/client.sh build` (Fable and esbuild). `/ambit` serves `Program.bundle.js`; `dotnet fable` alone is not sufficient. A Fable failure is a real failure, not a skip.

```bash
./scripts/client.sh build
```

**Background** (full suite — slow):

```bash
./scripts/test.sh shared
```

Use `./scripts/test.sh all` when Server tests may be affected.

## Escalation

- Browser-only or ambiguous DOM behavior → [[.agents/skills/investigate-fable-client/SKILL.md]].
- Large cross-layer change → [[.agents/skills/plan-roadmap-change/SKILL.md]] first.
