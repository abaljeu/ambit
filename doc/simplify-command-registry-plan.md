# Simplify Command Registry — Implementation Plan

> This plan should be followed by the next agent implementing the Controller refactor.

## Goal

Refactor [src/Client/Controller.fs](src/Client/Controller.fs) so that:

1. **One name = one operation** — Each `CommandEntry` has exactly one `op: Op` (no `Op option`).
2. **Modes: sel, edit, or both** — A command applies in Selection and/or Editing mode.
3. **Keys are flat** — Each command has `keys: string list`; the same keys apply in whatever mode(s) the command operates in.

## Out of Scope (unchanged)

- **Printable key handling** — Stays separate from the table; `tryResolveOperation` continues to special-case `printableKeyToken` when resolving keys. The registry does not include printable as a key.
- **Palette mode** — `paletteKeyTable` and palette key handling (Escape, ArrowUp, ArrowDown, Enter) stay as-is, not part of the command registry.

## New Data Model

```fsharp
type KeyContext =
  | Selection of SelectionKeyContext
  | Editing of EditingKeyContext

type CommandEntry = {
    name: string
    op: Op
    sel: bool      // operates in selection mode
    edit: bool     // operates in editing mode
    keys: string list
    handler: KeyContext -> Op option   // single field — returns Some when applicable in that mode
}
```

**Rules:**

- `op` is always present.
- `handler` is a single field; it receives `KeyContext` (Selection or Editing) and returns `Op option`.
- For sel-only: `handler` returns `Some` when given `Selection _`, `None` for `Editing _` (or ignore).
- For edit-only: `handler` returns `Some` when given `Editing _`, `None` for `Selection _`.
- For both (same op): `handler = fun _ -> Some op`.
- Keys do not include `printableKeyToken` — printable is handled separately in key resolution.
- **Invariant:** When the same key does different things in sel vs edit (e.g. ArrowLeft), use separate entries: one sel-only, one edit-only (e.g. "Move focus left" and "Move to previous node").

## Table Construction

Only selection and editing tables are computed from `commandRegistry`:

| Table               | Source                   | Derived handler                                |
| ------------------- | ------------------------ | ---------------------------------------------- |
| `selectionKeyTable` | Entries with `sel=true`  | `fun s -> entry.handler (Selection s)`         |
| `editingKeyTable`   | Entries with `edit=true` | `fun e -> entry.handler (Editing e)`           |

First binding per key wins when flattening.

`paletteKeyTable` remains a separate literal (Escape, ArrowUp, ArrowDown, Enter).

## Palette List (filteredCommands)

`paletteCommands` = commands where `sel || edit` (they have key bindings in sel or edit and are invokable by name). The palette displays these. `op` must be directly invokable — context-dependent commands like "Split at cursor" may stay key-only and not appear in the palette.

## File Changes

1. **[Controller.fs](src/Client/Controller.fs)**
   - Add `KeyContext` union type. Replace `KeyMap` and current `CommandEntry` with the new `CommandEntry` (single `handler: KeyContext -> Op option`, no `paletteOp`).
   - Rewrite `commandRegistry` into the new format (sel/edit only; no palette entries).
   - Split "Move focus left" into: "Move focus left" (sel, ArrowLeft, arrowLeftSelectionOp) and "Move to previous node" (edit, ArrowLeft + Ctrl+ArrowLeft, handleArrowLeft). Same for "Move focus right" → "Move focus right" (sel) and "Move to next node" (edit).
   - Update `selectionKeyTable` / `editingKeyTable` to derive mode-specific handlers by wrapping `entry.handler` with `Selection` or `Editing`.
   - Restore `paletteKeyTable` as a literal (remove derivation from registry).
   - Keep printable-key handling in `tryResolveFromNamed` as a special case.
   - Update `paletteCommands` to filter by `sel || edit`.
2. **[View.fs](src/Client/View.fs)**
   - No API changes; `handleKey`, `filteredCommands`, `paletteRunOp` stay the same.

## Data Flow

```
commandRegistry (CommandEntry list)
  → sel=true  → selectionKeyTable (wrap handler with Selection)
  → edit=true → editingKeyTable   (wrap handler with Editing)
  → sel||edit → paletteCommands  → filteredCommands (palette list)
```
