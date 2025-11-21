# Selection Management

## Goal
Wrap all direct uses of `window.getSelection()` and selection manipulation APIs into a new module to enable more precise control over selection and its interpretation. No code should directly call `window.getSelection()` or directly manipulate selection ranges.

## Affected Functions

### In `src/web/cell.ts`

1. **`caretOffset()`** (line 50)
   - Uses: `window.getSelection()`, `selection.rangeCount`, `selection.getRangeAt(0)`, `selection.focusNode`, `selection.focusOffset`
   - Purpose: Get current caret position within cell (HTML offset)

2. **`getHtmlSelectionRange()`** (line 73)
   - Uses: `window.getSelection()`, `selection.rangeCount`, `selection.getRangeAt(0)`
   - Purpose: Get selection range within cell (returns visible text offsets)

3. **`setSelection(start, end)`** (line 93)
   - Uses: `window.getSelection()`, `selection.removeAllRanges()`, `selection.addRange(range)`, `document.createRange()`, `range.setStart()`, `range.setEnd()`
   - Purpose: Set selection within cell (HTML offsets)

4. **`getAnchorOffset()`** (line 130)
   - Uses: `window.getSelection()`, `selection.rangeCount`, `selection.anchorNode`, `selection.anchorOffset`, `selection.direction`
   - Purpose: Get anchor offset for extending selection

5. **`offsetAtX(x)`** (line 167)
   - Uses: `document.createRange()`, `range.setStart()`, `range.collapse(true)` (indirectly related to selection)
   - Purpose: Find text offset at X coordinate (creates temporary range for measurement)

### In `src/web/row.ts`

1. **`removeCarets()`** (line 26)
   - Uses: `window.getSelection()?.removeAllRanges()`
   - Purpose: Clear all selections/carets

2. **`getCurrentParagraphWithOffset()`** (line 483)
   - Uses: `window.getSelection()`, `selection.rangeCount`, `selection.getRangeAt(0)`, `selection.anchorNode`
   - Purpose: Find current row and offset from selection

3. **`currentSelection()`** (line 562)
   - Uses: `window.getSelection()`, `selection.rangeCount`, `selection.getRangeAt(0)`, `selection.focusNode`
   - Purpose: Get current selection as `PureTextSelection`

4. **`caretX()`** (line 604)
   - Uses: `window.getSelection()`, `selection.rangeCount`, `selection.getRangeAt(0)`, `range.cloneRange()`, `range.collapse(true)`
   - Purpose: Get X coordinate of caret position

## Selection API Usage Summary

### Direct `window.getSelection()` calls: 8 locations
- `cell.ts`: 4 calls (lines 50, 73, 97, 133)
- `row.ts`: 4 calls (lines 27, 484, 563, 605)

### Selection manipulation methods:
- `selection.removeAllRanges()`: 2 locations (cell.ts:126, row.ts:27)
- `selection.addRange()`: 1 location (cell.ts:127)
- `selection.rangeCount`: 6 locations
- `selection.getRangeAt(0)`: 6 locations
- `selection.focusNode`: 3 locations
- `selection.focusOffset`: 1 location
- `selection.anchorNode`: 2 locations
- `selection.anchorOffset`: 1 location
- `selection.direction`: 1 location

### Range manipulation (related):
- `document.createRange()`: 2 locations (cell.ts:117, cell.ts:185)
- `range.setStart()`: 2 locations (cell.ts:120, cell.ts:186)
- `range.setEnd()`: 1 location (cell.ts:121)
- `range.collapse()`: 2 locations (cell.ts:187, row.ts:608)
- `range.cloneRange()`: 1 location (row.ts:607)

## Functions Used Outside `web/`

These functions are part of the public API and are called from code outside `src/web/`:

### Exported Functions from `row.ts`
1. **`removeCarets()`** - Used in `src/scene.ts`
2. **`currentSelection()`** - Used in `src/controller.ts`
3. **`caretX()`** - Used in `src/controller.ts`

### Public Methods on `Row` class
4. **`Row.caretOffset`** (getter) - Used extensively in `src/controller.ts`
5. **`Row.setSelectionInRow()`** - Used in `src/controller.ts`
6. **`Row.offsetAtX()`** - Used in `src/controller.ts`

### Public Methods on `Cell` class
7. **`Cell.caretOffset()`** - Used in `src/controller.ts`
8. **`Cell.getHtmlSelectionRange()`** - Used in `src/controller.ts`

## Functions Only Used Within `web/`

These functions are internal implementation details:

1. **`getCurrentParagraphWithOffset()`** - Private function in `row.ts`, only used internally by `currentRow()`
2. **`Cell.setSelection()`** - Only used internally within `cell.ts` and `row.ts`
3. **`Cell.getAnchorOffset()`** - Only used internally within `row.ts` (in `currentSelection()` function)
4. **`Cell.offsetAtX()`** - Only used internally by `Row.offsetAtX()`

## Next Steps

1. Create new module (e.g., `src/web/selection.ts`) to encapsulate all selection operations
2. Define abstraction layer for selection state and manipulation
3. Replace all direct `window.getSelection()` calls with module functions
4. Replace all direct selection manipulation with module functions
5. Consider whether range creation/manipulation should also be abstracted

### Proposed Selection.ts Methods
setSelection(PureTextSelection)
    - focus default 0, anchor defaults to focus.  if out of range use default.

currentSelection() : PureTextSelection|null
collapse() - set anchor to focus
remove() - remove cursors

getFocusX()
setCursorToX(row, x)

Adjusters: None of these will alter the current cell.  (If none, do nothing)
left(), right()
extend_left(), extend_right()
start(), end()
extend_start(), extend_end()
extend_all()

up(), down()
extend_up(), extend_down()

### Gaps Resolution
Row.caretOffset - use get cursor()
Row.setSelectionInRow - deleted
Cell.getHtmlSelectionRange - use get cursor()
Cell.setSelection - use setCursor
Cell.getAnchorOffset - use get cursor()
Row.offsetAtX and caretX() - added getFocusX and setCursorToX to proposal.
getCurrentParagraphWithOffset() - moved into Selection.ts.
currentSelection() - moved into Selection.ts.
