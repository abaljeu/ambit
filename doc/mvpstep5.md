# Step 5: Client editing – text

## Design overview

Two UI modes, analogous to Excel's selection and edit modes:

| | Selection mode | Edit mode |
|---|---|---|
| **Visual** | Row highlighted (`.selected`) | Inline `<input>` visible in row, cursor blinking |
| **Keyboard focus** | Hidden off-screen `<input>` | Inline `<input>` in the selected row |
| **Printable key** | Enter edit mode (text = that char) | Character typed into input |
| **F2** | Enter edit mode (text = current node text) | — |
| **Enter** | No-op (Step 6 adds new-sibling) | Commit edit → selection mode |
| **Escape** | Deselect row | Cancel edit → revert → selection mode |
| **Click another row** | Select that row | Commit current edit, select clicked row |

### The Excel analogy

In Excel, a cell can be *selected* (blue border, value displayed) or *being edited* (cursor blinking inside cell). The distinction matters:

- **Selection mode → type a character**: replaces cell content with that character (destructive start).
- **Selection mode → F2**: enters edit mode with the cursor at the end of existing content (non-destructive).
- **Edit mode → Enter**: commits the edit and moves selection down.
- **Edit mode → Escape**: reverts the cell to its original value.

This outliner follows the same pattern, adapted for rows instead of cells.

## Model

```fsharp
type Mode =
    | SelectionMode
    | EditMode of originalText: string

type Model =
    { graph: Graph
      revision: int
      selectedNode: NodeId option
      mode: Mode }
```

- `selectedNode` — which row is highlighted (`None` on initial load until first click)
- `mode` — `SelectionMode` (default) or `EditMode originalText`
  - `originalText` is the node's text at the moment edit mode was entered; used for revert on Escape

## Messages

```fsharp
type Msg =
    | StateLoaded of Graph * int
    | SelectRow of NodeId
    | EnterEditMode of prefill: string   // F2 → current text; printable char → that char
    | EditInput of string                // oninput from inline <input>
    | CommitEdit                         // Enter in edit mode, or click-away
    | CancelEdit                         // Escape in edit mode
    | SubmitResponse of Graph * int      // POST /submit success
    | SubmitError of string              // POST /submit failure
```

## DOM structure

```
#app
  .row [.selected]              ← click → SelectRow
    .indent  (× depth)
    .text                       ← shown in selection mode
    input.edit-input            ← shown in edit mode (replaces .text)
  .row
    ...
  input#hidden-input            ← off-screen, always present, focus in selection mode
```

### Hidden input

A single `<input id="hidden-input">` positioned off-screen (`position: absolute; left: -9999px`) always exists inside `#app`. In selection mode it holds keyboard focus so keystrokes are captured:

- `keydown` with a printable character (not a modifier key alone) → `EnterEditMode(String char)`
- `keydown` F2 → `EnterEditMode(currentNodeText)`
- `keydown` Escape → deselect (clear `selectedNode`)
- `keydown` Up/Down → (no-op in Step 5; Step 6 may add row navigation)

When edit mode activates, focus moves to the inline `<input>`. When edit mode ends, focus returns to the hidden input.

### Inline input

In edit mode, the selected row's `.text` div is replaced by `<input class="edit-input">` pre-filled with text. This input:

- Receives focus immediately (via `el.focus()` after insertion)
- `oninput` → `EditInput newValue`
- `onkeydown`:
  - Enter → `CommitEdit`
  - Escape → `CancelEdit`
  - All other keys → default input behavior
- `onblur` → `CommitEdit` (covers click-outside and tab-away)

## Update logic

| Current state | Message | New state | Side effects |
|---|---|---|---|
| `SelectionMode, None` | `SelectRow id` | `SelectionMode, Some id` | Re-render (highlight row) |
| `SelectionMode, Some _` | `SelectRow id` | `SelectionMode, Some id` | Re-render (move highlight) |
| `SelectionMode, Some id` | `EnterEditMode prefill` | `EditMode orig, Some id` | Show inline input with `prefill`, focus it |
| `EditMode _, Some id` | `EditInput text` | Update input value in DOM | — |
| `EditMode orig, Some id` | `CommitEdit` | `SelectionMode, Some id` | If text ≠ orig: `SetText` op → apply → re-render → POST |
| `EditMode _, Some id` | `CancelEdit` | `SelectionMode, Some id` | Re-render row with original text |
| `EditMode orig, Some id` | `SelectRow newId` | triggers CommitEdit first, then `SelectionMode, Some newId` | Commit + re-render + select new row |

## Rendering

### Full re-render (simple MVP approach)

After any model change, re-render the entire outline:

1. Clear `#app` children
2. For each visible node (depth-first from root's children), create `.row` div
3. If `selectedNode = Some id` and this is that node's row, add `.selected` class
4. If `mode = EditMode _` and this is the selected row, render `<input class="edit-input">` instead of `.text` span
5. Append hidden input at end of `#app`
6. Set focus: inline input if edit mode, hidden input if selection mode

This is acceptable for MVP (outlines are small). Incremental DOM updates can be added later.

### CSS additions

```css
.row.selected {
    background: #bde;
}
.edit-input {
    flex: 1;
    font: inherit;
    padding: 0.15rem 0.3rem;
    border: 1px solid #89a;
    outline: none;
}
#hidden-input {
    position: absolute;
    left: -9999px;
}
```

## Server sync

The client uses an **optimistic post** model: apply locally first, then confirm with the server.

On commit (when text actually changed):

1. Build `Change { id = 0; ops = [ SetText(nodeId, oldText, newText) ] }`
2. Apply to local graph via shared `Op.apply` / `Change.apply`
3. Re-render (local state is already correct)
4. `POST /submit` with `{ clientRevision: model.revision, change }` in background
5. On success response: update local `revision` to server's `newRevision` (graph is already correct locally)
6. On no response / network error: repost with usual retry protocol
7. On error (400): log to console (MVP — no user-facing error UI yet)

**MVP assumption**: single client, so the server always accepts what we post. No conflict, no merge. The response is a confirmation, not a correction.

**Future (multi-client)**: if the server's latest revision is ahead of ours, the response will include a change set for the client to merge before retrying. Not needed for MVP.

## Interaction with Step 6

Step 6 (structural editing) will extend the key handling:

| Key | Step 5 behavior | Step 6 changes |
|---|---|---|
| Enter (selection mode) | No-op | Create new sibling below selected row |
| Enter (edit mode) | Commit edit | Commit edit + create new sibling |
| Tab (selection mode) | No-op | Indent selected row |
| Shift+Tab (selection mode) | No-op | Outdent selected row |
| Up/Down (selection mode) | No-op | Move selection between rows |

Step 5 lays the groundwork — mode management, hidden input, inline editing, server sync — and Step 6 plugs structural operations into the same framework.

## Key decisions

- **Hidden input pattern**: Avoids `contenteditable` complexity. A single hidden input for selection-mode key capture; a standard `<input>` for edit mode. Clean, predictable, testable.
- **Typing replaces vs appends**: Following Excel convention — typing in selection mode starts with just the typed character (replaces). F2 preserves existing text (appends). This gives the user both options.
- **Full re-render**: Acceptable for MVP. Avoids incremental DOM diffing. Optimize later if outlines grow large.
- **No arrow-key row navigation in Step 5**: User clicks to select. Up/Down navigation deferred to Step 6 or later.
- **`onblur` commits**: Prevents edits from being silently lost if the user clicks outside the input.
