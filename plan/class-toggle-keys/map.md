# Class toggle keys

## 1. Destination

A person in Selecting can toggle cssClasses `b`, `i`, and `check` on the current Selection with the keys Alan named. Those keys use the existing `cssClasses` / `Op.SetClasses` path. The way is clear; this map indexes the one coding ticket.

## 2. Notes

Domain: Browser command keys, Selection, cssClasses. Consult  (Selection, Focus, Node), [CommandEntry](src/Shared/CommandEntry.fs), [CssClass.toggle](src/Shared/CssClass.fs), [submitCssClassPromptOp](src/Client/UpdateOps.fs), [handleKey](src/Client/Controller.fs).

This Project homes on [[plan/roadmap/epics/robust-outliner.md]]. It is a tiny slice. There is no spec.md and no arch.md. Implementation reads this map and [01 — Toggle cssClasses b, i, and check from keys](issues/01-toggle-cssclasses-b-i-check-from-keys.md).

## 3. Decisions so far

1. Alan 2026-09-28 — Ctrl+B and bare `b` toggle class `b`. Ctrl+I and bare `i` toggle class `i`. Space toggles class `check`. Apply to the current Selection (the selected / focused Node or Nodes). Reuse SetClasses / cssClasses. Do not invent a new style system.
2. Existing command-key convention — [handleKey](src/Client/Controller.fs) skips bare one-character keys while Editing so `#edit-input` receives the character. [editingKeyBindings](src/Client/Controller.fs) also drops single-character registry keys. Modifier combos (Ctrl+Z, Ctrl+P, Ctrl+S) still run in Editing when `keyScope` is `SelectionOrEditing`. Overlay modes (palette, cssClass prompt, rename, Find, Insert) keep their own tables and do not run Selecting letter keys.

## 4. Not yet specified

None. The way to the destination is clear.

## 5. Out of scope

1. A new style system, new CSS tokens, or new visual meaning for `b` / `i` / `check`. Those tokens already exist in [style.css](src/Server/wwwroot/style.css).
2. Changing [Edit classes](src/Shared/CommandEntry.fs) (`.` / `Alt+.`) or the cssClass prompt overlay.
3. Extra class-toggle keys beyond `b`, `i`, and `check`.
4. Product code in this chart session.
