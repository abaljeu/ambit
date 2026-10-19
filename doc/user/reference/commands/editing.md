# Editing

Category: Reference

See Also:

[Commands](doc/user/reference/commands.md)

These commands change node text, classes, or the last change.

## Edit node

Keys: Enter. While you are selecting.

What it does: Starts editing the focused node's text, with the caret at the end.

## Split at cursor

Keys: Enter. While you are editing.

What it does: Splits the node at the caret. Text after the caret becomes the next node.

## Delete

Keys: Delete and Backspace. While you are selecting.

What it does: Delete removes that link to the node. When the link is the owner and another link exists, that other link becomes the owner. When it is the last link, Trash becomes the owner, and deleting it from Trash removes the node for good.

## Join with previous

Keys: Backspace. While you are editing.

What it does: At the start of the text, appends this text to the previous visible node and removes this node. Elsewhere, Backspace deletes text.

## Join with next

Keys: Delete. While you are editing.

What it does: At the end of the text, appends the next visible node to this text and removes that node. A node that has children does not join. Elsewhere, Delete deletes text.

## Escape

Keys: Escape. While you are selecting or editing.

What it does: While editing, commits the text and returns to selecting. While selecting, reduces the selection to the focused node.

## Undo

Keys: Ctrl+Z and `z`. While you are selecting or editing.

What it does: Reverses the last change. When nothing can be undone, Ambit reports that.

## Redo

Keys: Ctrl+Y and `y`. While you are selecting or editing.

What it does: Applies the last undone change again. When nothing can be redone, Ambit reports that.

## Edit classes

Keys: `.` and Alt+`.`. While you are selecting or editing.

What it does: Opens a prompt of the classes on the selected nodes, except names that start with `amb-`. Submitting replaces those classes and keeps the `amb-` classes.

## Toggle bold

Keys: Ctrl+B and `b`. While you are selecting or editing.

What it does: Turns the class `b` on or off for every selected node.

## Toggle italic

Keys: Ctrl+I and `i`. While you are selecting or editing.

What it does: Turns the class `i` on or off for every selected node.

## Toggle check

Keys: Space. While you are selecting.

What it does: Turns the class `check` on or off for every selected node.
