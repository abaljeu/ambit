# View

Category: Reference

See Also:

[Commands](doc/user/reference/commands.md)

These commands change which node is the root of the page.

## Zoom in

Keys: Ctrl+`]` and `]`. While you are selecting or editing.

What it does: Makes the first selected node the view root. A node with no children zooms to its parent instead.

## Zoom out

Keys: Ctrl+`[` and `[`. While you are selecting or editing.

What it does: Moves the view root one level up and selects the child you came from. At the root of the outline, Zoom out does nothing.

## Zoom owner

Keys: `{` and Alt+`[`. While you are selecting or editing.

What it does: Makes the owner of the focused node the view root and selects that node there. A node with no owner does not move.
