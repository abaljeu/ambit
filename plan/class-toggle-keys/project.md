# Class toggle keys

Stage: slice
Summary: Bind keys so a person can toggle cssClasses `b`, `i`, and `check` on the current Selection through the existing SetClasses path.
Updated: 2026-09-28

**Part of:** [[plan/roadmap/epics/robust-outliner.md]]

Frontier: none. [01 — Toggle cssClasses b, i, and check from keys](issues/01-toggle-cssclasses-b-i-check-from-keys.md) — Type `coding`, Status `done`.

## Notes

- No existing keybind Project. [Edit classes](src/Shared/CommandEntry.fs) (`.` / `Alt+.`) already opens the cssClass prompt and posts `Op.SetClasses`. This slice adds three toggles on that same `cssClasses` path.
- Style tokens `b`, `i`, and `check` already exist in [style.css](src/Server/wwwroot/style.css). Do not invent a new style system.
