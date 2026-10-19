# Folding

Category: Reference

See Also:

[Commands](doc/user/reference/commands.md)

[Infinite Outliner](doc/user/guide/infinite-outliner.md)

These commands open and close nodes.

## Cursor fold left

Keys: Shift+Left arrow and `A`. While you are selecting.

What it does: Closes the deepest open level under the selection, one level per press. Does nothing when nothing in that scope is open.

Related: [Cursor unfold right](#cursor-unfold-right).

## Cursor unfold right

Keys: Shift+Right arrow. While you are selecting.

What it does: Opens the nearest folded level under the selection, one level per press. Does nothing when nothing in that scope is folded.

Related: [Cursor fold left](#cursor-fold-left).

## Cursor left to parent

Keys: Left arrow and `a`. While you are selecting.

What it does: Closes the selected nodes when any of them is open. Otherwise moves the selection to the parent.

Related: [Cursor fold left](#cursor-fold-left).

## Fold / unfold

Keys: Ctrl+`.`. While you are selecting or editing.

What it does: Closes the selected nodes when any of them is open. Otherwise opens the selected nodes.
