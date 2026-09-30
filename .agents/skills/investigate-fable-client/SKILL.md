---
name: investigate-fable-client
description: Investigates Gambol Fable client MVU and DOM issues while keeping logic in Shared when possible. Use when debugging src/Client, browser behavior, rendering, keys, sync, or Fable compilation.
---

# Investigate Fable Client

Follow [[.agents/rules/fsharp-source.md]], [[.agents/skills/implement-fsharp-feature/SKILL.md]], and [[doc/arch.md]]. Client paths and layer roles live in [[doc/arch.md]]; Fable output is served from `src/Server/wwwroot` at `/ambit`.

## Investigation order

1. **Reproduce** — note URL, file, selection, and message sequence if known. Done: the repro steps are written down.
2. **Classify** — pure logic, client wiring, or server response? Done: the layer class is named.
3. **Shared first** — add a Shared.Tests case and fix in Shared when possible; thin the Client change. Done: Shared holds the fix, or you recorded why the bug is Client-only.
4. **Client only when necessary** — DOM measurement, focus, fetch lifecycle, desktop capabilities. Done: Client edits are limited to what Shared cannot express.

## Common splits

| Symptom | Likely layer | First look |
|---------|--------------|------------|
| Wrong ops / undo / graph state | Shared | `Change.apply`, ViewModel ops |
| Wrong line rendering / fold | Shared ViewModel + Client View | `siteMap`, `View.fs` |
| Sync / poll / POST failures | Client App + Server | `Update*.fs`, `/ambit` API |
| Desktop file paths | Shared + Desktop proxy | `DesktopCapabilities`, `/_desktop/*` |

## Dev commands

VS Code default: Fable watch + server. Manual: `./scripts/client.sh` (default action is watch).

Shared edits that must ship to `/ambit` are Client dependencies. After those edits, run the Client compile gate. Shared.Tests do not compile the Client. Policy: [[.agents/skills/implement-fsharp-feature/SKILL.md]]. Invocation: [[.agents/skills/implement-fsharp-feature/TEST-COMMANDS.md]].

## Escalation

- Cross-cutting feature or multi-file Client refactor → [[.agents/skills/plan-roadmap-change/SKILL.md]].
- New Shared behavior → [[.agents/skills/implement-fsharp-feature/SKILL.md]].
