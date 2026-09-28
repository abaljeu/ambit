# 01 — Toggle cssClasses b, i, and check from keys

**Type:** coding
**Status:** coded
**Blocked by:** None — can start immediately
**Estimate:** 2h
**Actual:** 2.5h

## Context

A person works in the Browser with a Selection: a range of sibling Nodes, with Focus as the first or last of that range. They already set user cssClasses through [Edit classes](src/Shared/CommandEntry.fs) (`.` / `Alt+.`), which opens the cssClass prompt and posts `Op.SetClasses`. Tokens `b` (bold), `i` (italic), and `check` (line-through) already exist in [style.css](src/Server/wwwroot/style.css). There is no key that toggles those tokens. While Selecting, letter keys already run commands (`z` Undo, `p` palette). While Editing, `#edit-input` must keep those letters and Space as typed text.

## What to build

While Selecting, the named keys toggle the named user class on every Node in the current Selection. Toggle means add the class when it is absent and remove it when it is present, through [CssClass.toggle](src/Shared/CssClass.fs) and `Op.SetClasses`. The existing Edit-classes prompt stays. Do not invent a new style system.

### 1. Selection toggles

The keys act on the current Selection, the same set [submitCssClassPromptOp](src/Client/UpdateOps.fs) already writes with `Op.SetClasses`. Focus is the first or last of that Selection; do not invent a second target.

1. [x] 1.1 Bold — Ctrl+B and bare `b` toggle class `b` on each selected Node.
2. [x] 1.2 Italic — Ctrl+I and bare `i` toggle class `i` on each selected Node.
3. [x] 1.3 Check — Space toggles class `check` on each selected Node. Register the Space key as the space character that [formatKeyCombo](src/Client/Controller.fs) emits (`ke.key` is `" "`, not the word `Space`).
4. [x] 1.4 Existing path — Each toggle posts `Op.SetClasses` with the prior `cssClasses` and the toggled list. Reuse [CssClass.toggle](src/Shared/CssClass.fs). Preserve reserved `amb-` classes. No new style store and no new CSS tokens.

### 2. Edit-mode and overlay gate

Bare `b`, bare `i`, and Space must not fire as class toggles when the person is typing. Follow the current command-key convention. Do not add a second gate.

1. [x] 2.1 Editing field — While mode is Editing, bare `b`, bare `i`, and Space stay text in `#edit-input`. [handleKey](src/Client/Controller.fs) already skips a one-character key with no Ctrl/Alt/Meta while Editing. [editingKeyBindings](src/Client/Controller.fs) already drops single-character registry keys.
2. [x] 2.2 Modifier combos in Editing — Ctrl+B and Ctrl+I may run while Editing. Other modifier commands already do (Ctrl+Z, Ctrl+P, Ctrl+S) when `keyScope` is `SelectionOrEditing`. The resolved handler must `preventDefault` so the contentEditable field does not apply native browser bold or italic.
3. [x] 2.3 Overlays — While the command palette, cssClass prompt, rename prompt, Find, or Insert dialog is open, bare `b`, bare `i`, and Space stay with that overlay input. Those modes already use their own key tables.

### 3. Registry and proof

1. [x] 3.1 Command registry — The new keys live in [CommandEntry](src/Shared/CommandEntry.fs) with the same key-string and `keyScope` pattern as peer commands. They do not collide with existing first-wins bindings (`b`, `i`, and Space are free today).
2. [x] 3.2 Shared proof — Tests prove `CssClass.toggle` plus `Op.SetClasses` for `b`, `i`, and `check` on one Node and on a multi-Node Selection. [CommandEntryTests](tests/Shared.Tests/CommandEntryTests.fs) records the key lists.

## See also

[CommandEntry](src/Shared/CommandEntry.fs), [CssClass.toggle](src/Shared/CssClass.fs), [submitCssClassPromptOp](src/Client/UpdateOps.fs), [handleKey](src/Client/Controller.fs), [style.css user classes](src/Server/wwwroot/style.css)

## Comments

- 2026-09-28 — Charted from Alan’s easy ticket. Status `defined`. Home is a new slice Project on [Robust outliner](plan/roadmap/epics/robust-outliner.md). No existing keybind Project. Gate for bare `b` / `i` / Space is the existing Editing single-character skip plus overlay tables.
- 2026-09-28 — Alan accepted this chart PR. Status `done`.
- 2026-09-28 — Implemented product code. Status `coded`.

## Time

- 2026-09-28 45m — charted coding ticket from Alan ask (from chat)
- 2026-09-28 10m — Status `done` on accepted chart land (from chat)
- 2026-09-28 1.5h — wired Toggle bold / italic / check keys and Shared proof (from chat)
