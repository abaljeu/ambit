# Paste (External)

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
