# Cursor

Category: Reference

See Also:

[Commands](doc/user/reference/commands.md)

These commands move the cursor through the visible outline.

## Cursor up

Keys: Up arrow and `,`. While you are selecting.

What it does: Moves the selection to the previous visible node. In a wide selection, the first press moves the focus to the start of the range. On the first visible node, the selection clears.

## Cursor down

Keys: Down arrow and `o`. While you are selecting.

What it does: Moves the selection to the next visible node. In a wide selection, the first press moves the focus to the end of the range. With no selection, Cursor down selects the first visible node.

## Cursor to Start

Keys: Page Up. While you are selecting.

What it does: Selects the first sibling under the same parent.

## Cursor to End

Keys: Page Down. While you are selecting.

What it does: Selects the last visible node under the last sibling of the same parent.

## Cursor to Top of View

Keys: Home. While you are selecting.

What it does: Selects the first child of the view root.

## Cursor to End of View

Keys: End. While you are selecting.

What it does: Selects the last child of the view root.

## Cursor right to child

Keys: Right arrow, `e`, and `E`. While you are selecting.

What it does: Opens the focused node when it is folded and has children. When that node is already open, the selection moves to its first child.

Related: [Cursor left to parent](folding.md#cursor-left-to-parent).

## Move to previous node

Keys: Left arrow. While you are editing.

What it does: At the start of the text, commits the edit and edits the previous visible node. Elsewhere, the key only moves the caret.

## Move to next node

Keys: Right arrow. While you are editing.

What it does: At the end of the text, commits the edit and edits the next visible node. Elsewhere, the key only moves the caret.

## Edit cursor up

Keys: Up arrow and `,`. While you are editing.

What it does: On the first visual line, commits the edit and edits the previous visible node at the same horizontal position. On a later line, the key only moves the caret.

## Edit cursor down

Keys: Down arrow and `o`. While you are editing.

What it does: On the last visual line, commits the edit and edits the next visible node at the same horizontal position. On an earlier line, the key only moves the caret.
