# Mandate Client compile on Shared edits

Branch: `w/expr`. Instruction amendment after HITL: agents proved Shared with `dotnet test` while `/ambit` served stale Fable JS. Did not rename `ExprAnswer.Node`. Did not change Full Stack preLaunch.

## Command (confirmed)

`./scripts/client.sh build` in [[scripts/client.sh]] runs `dotnet fable src/Client` then `npm run bundle` (esbuild). `/ambit` serves `Program.bundle.js`. Bare `./scripts/client.sh` defaults to watch. `dotnet fable` alone is not the gate.

## Exact gate wording

Canonical policy is in [[.cursor/rules/testing-workflow.mdc]]:

> If you edited Client dependencies (`src/Shared/`, `src/Client/`, or anything the Client fsproj references, including Shared documents when that project is in the Client graph), `dotnet test` on Shared.Tests is not enough. Run `./scripts/client.sh build` (Fable and esbuild). `/ambit` serves `Program.bundle.js`; `dotnet fable` alone is not sufficient. A Fable failure is a real failure, not a skip.

Browser tests stay forbidden.

## Files changed

- [[.cursor/rules/testing-workflow.mdc]] — gate plus globs `src/Shared/**`, `src/Client/**` so the rule attaches on Client-dependency edits
- [[.cursor/skills/implement-fsharp-feature/SKILL.md]] — after Shared tests, run the same command when those layers changed; links the rule
- [[.cursor/skills/investigate-fable-client/SKILL.md]] — Shared edits that must ship to `/ambit` require `build`, not watch
- [[.cursor/rules/core-agent-behavior.mdc]] — the compile gate is not “all tests”; subagents must not skip it
- [[.cursor/rules/gambol.mdc]] — catalog line only (scope now includes Shared and Client)

No bridge-file bodies. [[AGENTS.md]], [[.cursor/codex-context.md]], and [[.cursor/copilot-instructions.md]] still point at [[.cursor/rules/gambol.mdc]].

## WORK.md mutations (for the root)

None. This amendment is complete. Leave the existing HITL reload item in Pending.
