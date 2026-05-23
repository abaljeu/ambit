# Cursor & Selection Movement

Gambol has two modes: **Select mode** (a node or range is highlighted but not being typed into) and **Edit mode** (the cursor is inside a node's text).

---

## Select Mode

In Select mode a node or contiguous range of siblings is highlighted. No text cursor is visible.

### Navigating the selection

| Key | Behaviour |
|-----|-----------|
| **UP** | Move the selection to the node above |
| **DOWN** | Move the selection to the node below |
| **Shift+UP** | Extend the selection upward by one sibling |
| **Shift+DOWN** | Extend the selection downward by one sibling |

### Structural editing

| Key | Behaviour |
|-----|-----------|
| **Alt+UP** or **Ctrl+UP** | Move the selected node (or range) up past its preceding sibling |
| **Alt+DOWN** or **Ctrl+DOWN** | Move the selected node (or range) down past its following sibling |
| **Tab** | Indent: make the selected node (or range) a child of its preceding sibling |
| **Shift+Tab** | Outdent: promote the selected node (or range) up one level |

### View

| Key | Behaviour |
|-----|-----------|
| **Ctrl+.** | Collapse or expand the selected node |

### Clipboard

| Key | Behaviour |
|-----|-----------|
| **Ctrl+C** | Copy the selected node (or range) to the clipboard |
| **Ctrl+X** | Cut the selected node (or range): copy to clipboard then remove from the outline |
| **Ctrl+V** | Paste clipboard contents as siblings below the current selection |

### History

| Key | Behaviour |
|-----|-----------|
| **Ctrl+Z** | Undo the last change |
| **Ctrl+Y** | Redo the last undone change |

### Entering Edit mode

| Key | Behaviour |
|-----|-----------|
| **Enter** or **F2** | Begin editing the selected node; cursor placed at the end |
| *Any printable character* | Begin editing the selected node; the typed character replaces any selection and pre-fills the input |

---

## Edit Mode

In Edit mode a text cursor is active inside a single node's text. The node being edited is the current selection.

### Cursor movement within the node (browser-handled)

The app does not intercept these keys; the browser input handles them natively.

| Key | Behaviour |
|-----|-----------|
| **LEFT / RIGHT** | Move the cursor one character left or right. At the start of a node, LEFT moves to the node above; at the end, RIGHT moves to the node below |
| **Ctrl+LEFT / Ctrl+RIGHT** | Jump the cursor one word left or right. At the start of a node, Ctrl+LEFT moves to the node above; at the end, Ctrl+RIGHT moves to the node below |
| **Home** | Move the cursor to the start of the text |
| **End** | Move the cursor to the end of the text |
| **Shift+LEFT / Shift+RIGHT** | Extend the in-node text selection one character |
| **Ctrl+Shift+LEFT / Ctrl+Shift+RIGHT** | Extend the in-node text selection one word |
| **Shift+Home / Shift+End** | Extend the in-node text selection to the start or end |

### Deleting text

| Key | Behaviour |
|-----|-----------|
| **Backspace** (cursor not at start) | Delete the character to the left of the cursor — browser handles |
| **Backspace** (cursor at start of node) | Merge this node's text onto the end of the previous node; the cursor lands at the join point |
| **Delete** (cursor not at end) | Delete the character to the right of the cursor — browser handles |
| **Delete** (cursor at end of node) | Merge the next node's text onto the end of this node; the cursor stays at the join point |

### Splitting and moving between nodes

| Key | Behaviour |
|-----|-----------|
| **Enter** | Split the node at the cursor: text to the right becomes a new sibling below; cursor moves to it |
| **UP** | Move to the node above, keeping the cursor at the same character position (clamped to that node's length) |
| **DOWN** | Move to the node below, keeping the cursor at the same character position (clamped to that node's length) |

### Structural editing

| Key | Behaviour |
|-----|-----------|
| **Alt+UP** or **Ctrl+UP** | Move the current node up past its preceding sibling (text is committed first) |
| **Alt+DOWN** or **Ctrl+DOWN** | Move the current node down past its following sibling (text is committed first) |
| **Tab** | Indent: make the current node a child of its preceding sibling |
| **Shift+Tab** | Outdent: promote the current node up one level |

### View, history, and leaving Edit mode

| Key | Behaviour |
|-----|-----------|
| **Ctrl+.** | Collapse or expand the node being edited |
| **Ctrl+Z** | Undo the last change |
| **Ctrl+Y** | Redo the last undone change |
| **Escape** | Discard any in-progress edit and return to Select mode |
