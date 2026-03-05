# 1. Paste (External)

## Summary

Ctrl+V pastes external clipboard content into the outline. Works in both select
and edit modes. The browser's native `paste` event is intercepted on the focused
input element — no `navigator.clipboard` permission required.

## Input Formats

1. **Plain text** — each line becomes a node. Leading tab characters indicate depth.
2. **Tab-indented hierarchy** — matches the snapshot file format: one `\t` per depth level.
3. **HTML (rich text)** — `text/html` is tried first; block/break elements (`<p>`, `<div>`,
   `<br>`, `<tr>`, `<li>`, `<td>`) are mapped to newlines via `div.innerText` on a temporary
   DOM element. All other tags are stripped. Result is treated as plain text.

`text/html` is tried first; `text/plain` is the fallback.

## Blank Lines

Runs of 2 or more consecutive blank lines are collapsed to 1 blank node.
A single blank line between content sections is preserved as an empty node.

## Select Mode Behavior

The current selection is **replaced** by the pasted nodes.

- Top-level pasted lines (depth 0) replace the selected range as siblings.
- `Op.Replace(parentId, range.start, selectedChildIds, topLevelPastedIds)` performs the splice.
- Sub-nodes from tab-indented input are attached to their parents via `Op.Replace` before the graft.
- Post-paste selection covers the newly inserted top-level nodes.

## Edit Mode Behavior

The first pasted line is **spliced into the current node at the cursor position**
(text before cursor + first pasted line + text after cursor).
Remaining pasted lines become **siblings below** the current node.

- `Op.SetText` updates the current node with the first line merged in.
- `Op.Replace(parentId, focusIdx+1, [], remainingTopLevelIds)` inserts the rest.
- If the clipboard contains only one line, it behaves like a plain text insert — no new nodes.

## Implementation

### New Msg
`PasteNodes of pastedText: string` in `src/Shared/ViewModel.fs`.

### New helpers in `src/Client/View.fs`
- `stripHtmlToText : string -> string` — DOM-based HTML → plain text via `div.innerHTML` /
  `innerText`
- `getClipboardData : Event -> string -> string` — `[<Emit>]` wrapper around
  `ev.clipboardData.getData(format)`
- `onPaste : Event -> (Msg -> unit) -> unit` — reads clipboard, strips HTML if present,
  dispatches `PasteNodes text`
- Wired to both `hidden-input` (select mode) and `edit-input` (edit mode) via `"paste"` event

### New helpers in `src/Client/Update.fs`
- `readEditInputCursor : unit -> int` — reads `selectionStart` from `#edit-input`
- `parsePasteText : string -> (string * int) list` — splits lines, counts leading tabs,
  collapses consecutive blank lines
- `buildPasteOps : (string * int) list -> NodeId list * Op list` — depth-stack traversal
  producing `NewNode` + `Replace` ops; returns top-level ids and the full op list
- `pasteNodes : string -> Model -> (Msg -> unit) -> Model` — branches on select vs edit mode,
  assembles `Change`, calls `applyAndPost`

### Handler in `src/Client/Update.fs`
`| PasteNodes pastedText -> pasteNodes pastedText model dispatch` in the `update` function.

## Key Files

| What | Where |
|---|---|
| Msg type | `src/Shared/ViewModel.fs` |
| HTML stripping, paste handler | `src/Client/View.fs` |
| Text parsing, op building, handler | `src/Client/Update.fs` |


# 2. Paste (internal or between two windows, deep)

## Summary

`Ctrl+C` / `Ctrl+X` in select mode write the selected **visible** nodes and
their visible (unfolded) children to the system clipboard as tab-indented text
(the snapshot format). Folded children are not included. Pasting in any Gambol
window — same or different tab — re-instantiates exactly that visible subtree
with fresh NodeIds. This is the deep-copy path; no internal-only channel is needed.

## Copy / Cut

Handled via the native `copy` and `cut` DOM events on `hidden-input` (select
mode). No key table entries for `Ctrl+C` / `Ctrl+X` are needed. The handler
calls `e.preventDefault()` and writes to the clipboard via
`e.clipboardData.setData('text/plain', serialized)` — no `navigator.clipboard`
permission required, works on non-HTTPS and in Firefox.

In Elmish, the `view` function receives `model` as a parameter, so event
handlers defined within it close over the current model. The `copy`/`cut`
handlers exploit this: `e.clipboardData` is only valid synchronously during
the event, so serialization must happen inside the handler before it returns —
it cannot be deferred to `update`.

The handler does two things synchronously, then dispatches:

1. Calls `serializeSubtree model.graph model.selection` → writes the result to
   `e.clipboardData.setData('text/plain', ...)` immediately.
2. Dispatches `CopySelection` / `CutSelection` so `update` can call
   `collectSubtree` and store the result in `model.clipboard`.

- `copy` event on `hidden-input` → (serialize synchronously) → dispatch `CopySelection`
- `cut` event on `hidden-input` → (serialize synchronously) → dispatch `CutSelection`

- `serializeSubtree : Graph -> NodeId list -> string` — preorder walk over
  selected nodes and their **visible children only** (respects fold state);
  called synchronously in the event handler.
- `collectSubtree : Graph -> NodeId list -> ClipboardContent` — same visible
  traversal, captures node map; called in `update`, stored in `model.clipboard`
  (reserved for Phase 3 link-paste).

`CutSelection` additionally applies `Op.Replace` to remove the selected nodes,
then selects sibling-after > sibling-before > parent.

Edit mode: `copy` / `cut` events on `edit-input` are **not** intercepted —
browser-native clipboard handles text within the edit input.

## Paste (deep)

The `paste` event fires on `hidden-input` and is handled by the existing
`onPaste → PasteNodes text` path. Because `serializeSubtree` produces
tab-indented text identical to external paste input, `parsePasteText` +
`buildPasteOpsFromClipboard` (remaps to fresh NodeIds) handle it. The pasted
result reflects exactly the visible subtree that was copied — folded children
are not re-instantiated.

Only standard `Ctrl+V` is used for paste — no custom keybinding. In Phase 3,
when clipboard content is detected as Gambol snapshot format, a program
option (checkbox in settings) controls whether it pastes as deep-copy (new
nodes) or as links. See Section 3.

## `buildPasteOpsFromClipboard`

Unlike `buildPasteOps` (which parses text), this function accepts a
`ClipboardContent` and remaps every NodeId to a fresh one, preserving tree
structure. Produces the same `NewNode + Replace` op list.

## Key Files

| What | Where |
|---|---|
| `serializeSubtree`, `collectSubtree`, `buildPasteOpsFromClipboard` | `src/Shared/Paste.fs` |
| `CopySelection`, `CutSelection` msgs | `src/Shared/ViewModel.fs` |
| `copy`/`cut` event handlers, `e.clipboardData.setData` interop | `src/Client/View.fs` |
| `CopySelection` / `CutSelection` handlers, post-cut selection | `src/Client/Update.fs` |

# 3. Paste Link (internal, option-controlled)

## Summary

When clipboard content is detected as Gambol snapshot format, a program option
(settings checkbox: **"Paste Gambol content as links"**) controls behaviour:

- **Off (default):** deep-copy — new nodes are created as in Section 2.
- **On:** link-paste — existing nodes are re-used by NodeId; new occurrences
  reference the same nodes.

No custom keybinding is required. Detection and branching happen inside the
existing `onPaste` handler after reading `e.clipboardData.getData('text/plain')`.
The setting is part of app/user preferences and exposed in the settings UI.
