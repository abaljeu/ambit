# Editing

Category: Reference

See Also:

[Commands](doc/user/reference/commands.md)

These commands change node text, classes, or the last change.

## Edit node

Keys: Enter. Selection Mode.

What it does: Starts Edit Mode on the focused node's text, with the caret at the end.

## Split at cursor

Keys: Enter. Edit Mode.

What it does: Splits the node at the caret. Text after the caret becomes the next node.

## Delete

Keys: Delete and Backspace. Selection Mode.

What it does: Delete removes that link to the node. When the link is the owner and another link exists, that other link becomes the owner. When it is the last link, Trash becomes the owner, and deleting it from Trash removes the node for good.

## Join with previous

Keys: Backspace. Edit Mode.

What it does: At the start of the text, appends this text to the previous visible node and removes this node. Elsewhere, Backspace deletes text.

## Join with next

Keys: Delete. Edit Mode.

What it does: At the end of the text, appends the next visible node to this text and removes that node. A node that has children does not join. Elsewhere, Delete deletes text.

## Escape

Keys: Escape. Any Mode.

What it does: In Edit Mode, commits the text and returns to Selection Mode. In Selection Mode, reduces the selection to the focused node.

## Undo

Keys: Ctrl+Z and `z`. Any Mode.

What it does: Reverses the last change. When nothing can be undone, Ambit reports that.

## Redo

Keys: Ctrl+Y and `y`. Any Mode.

What it does: Applies the last undone change again. When nothing can be redone, Ambit reports that.

## Edit classes

Keys: `.` and Alt+`.`. Any Mode.

What it does: Opens a prompt of the classes on the selected nodes, except names that start with `amb-`. Submitting replaces those classes and keeps the `amb-` classes.

## Toggle bold

Keys: Ctrl+B and `b`. Any Mode.

What it does: Turns the class `b` on or off for every selected node.

## Toggle italic

Keys: Ctrl+I and `i`. Any Mode.

What it does: Turns the class `i` on or off for every selected node.

## Toggle check

Keys: Space. Selection Mode.

What it does: Turns the class `check` on or off for every selected node.
