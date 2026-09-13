---
name: implement-fsharp-feature
description: Implements Gambol F# features with Shared-first logic and surgical diffs. Use when adding or changing behavior in src/Shared, src/Client, or src/Server.
---

# Implement F# Feature

Augments [[.agents/skills/implement/SKILL.md]] for F# layout and test commands. TDD quality: [[.agents/skills/tdd/SKILL.md]]. Follow [[.agents/rules/fsharp-source.md]] and [[.agents/rules/core-agent-behavior.md]].

Layer paths and boundaries: [[doc/arch.md]].

## Implementation layout

1. Put new non-interacting logic in Shared first (pure functions and ops). Shared compiles for .NET and Fable, and for tests. This does not apply to web UI. Rarely, put .NET-only code in Shared when more than one consumer requires it. Done: that logic is in Shared.
2. Then edit Client, Server, or Desktop as the feature needs those layers. Done: other-layer edits match the feature.
3. When you add tests, use [[.agents/skills/add-shared-test/SKILL.md]]. Done: new Shared.Tests files follow that skill.

## Tests

Invocations for each class: [[TEST-COMMANDS.md]].

1. **Foreground.** Run related tests only. Done: those tests pass.
2. **Client compile gate.** When Client dependencies changed (`src/Shared/`, `src/Client/`, or anything the Client fsproj references, including Shared documents when that project is in the Client graph), run the Client compile gate. Shared.Tests is not enough. A Fable failure is a real failure, not a skip. Done: the gate succeeds.
3. **Background.** After coding is complete, run the full suite as a background task. Use the shared suite unless Server tests may be affected. Done: the suite is running in the background.

## Escalation

- Browser-only or ambiguous DOM behavior → [[.agents/skills/investigate-fable-client/SKILL.md]].
- Large cross-layer change → [[.agents/skills/plan-roadmap-change/SKILL.md]] first.
