---
name: investigate-fable-client
description: Investigates Gambol Fable client MVU and DOM issues while keeping logic in Shared when possible. Use when debugging src/Client, browser behavior, rendering, keys, sync, or Fable compilation.
---

# Investigate Fable Client

Follow [[.cursor/rules/fsharp-source.mdc]], [[.cursor/rules/testing-workflow.mdc]], and [[doc/arch.md]].

## Client layout

| Area | Path | Role |
|------|------|------|
| MVU update | `src/Client/Update*.fs` | Messages, cmds, op dispatch |
| View / DOM | `src/Client/View.fs`, `*View.fs` | Render and event wiring |
| Runtime | `src/Client/App.fs`, `Program.fs` | Dispatch, polling, boot |
| JS interop | `src/Client/JsInterop.fs` | Fetch, timers, console |
| Browser DOM types | `other/fable.browser.dom.fs` | When Fable.Browser.Dom is insufficient |

Fable output goes to `src/Server/wwwroot`; the server serves `/ambit`.

## Investigation order

1. **Reproduce** — note URL, file, selection, and message sequence if known.
2. **Classify** — pure logic, client wiring, or server response?
3. **Shared first** — add a Shared.Tests case and fix in Shared when possible; thin the Client change.
4. **Client only when necessary** — DOM measurement, focus, fetch lifecycle, desktop capabilities.

## Common splits

| Symptom | Likely layer | First look |
|---------|--------------|------------|
| Wrong ops / undo / graph state | Shared | `Change.apply`, ViewModel ops |
| Wrong line rendering / fold | Shared ViewModel + Client View | `siteMap`, `View.fs` |
| Sync / poll / POST failures | Client App + Server | `Update*.fs`, `/ambit` API |
| Desktop file paths | Shared + Desktop proxy | `DesktopCapabilities`, `/_desktop/*` |

## Dev commands

VS Code default: Fable watch + server. Manual: `./scripts/client.sh` (default action is watch).

Shared edits that must ship to `/ambit` are Client dependencies. After those edits, run `bash ./scripts/client.sh build` (Fable and esbuild). Shared.Tests do not compile the Client. A Fable failure is a real failure, not a skip. Policy: [[.cursor/rules/testing-workflow.mdc]].

## Escalation

- Cross-cutting feature or multi-file Client refactor → [[.cursor/skills/plan-roadmap-change/SKILL.md]].
- New Shared behavior → [[.cursor/skills/implement-fsharp-feature/SKILL.md]].
