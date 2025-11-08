# Cell Conversion - Notes to Self

## Current Status

### Completed Phases
- **Phase 1**: Fix constants and setContent() - COMPLETE
  - Replaced old constants (`RowIndentAtt`, `TextCellAtt`) with new ones (`RowIndentClass`, `TextCellClass`)
  - Fixed `setContent()` to preserve rowContent wrapper (cells appended to rowContent, not directly to newEl)

- **Phase 2**: Fix cell access methods - COMPLETE
  - `getContentSpans()` now returns ALL cells (indent + text) from rowContent
  - `Row.cells` getter returns all cells wrapped in Cell class
  - Added `Row.contentCells` getter that returns only text cells (editableCell)
  - `Cell` class handles both indent and text cells

- **Phase 3**: Fix content access - COMPLETE
  - `htmlContent` uses cells abstraction
  - `visibleText` uses cells abstraction
  - Both properly handle tab character conversion

- **Phase 4**: Add Cell-level interaction methods - COMPLETE
  - Added `Cell.caretOffset()`, `setCaret()`, `getSelectionRange()`, `setSelection()`, `getAnchorOffset()`, `offsetAtX()`, `containsNode()`, `focus()`, `moveCaretToThisCell()`, `getHtmlOffset()`
  - All methods work on individual cells, only operate on text cells

- **Phase 5**: Update caret/selection at Row level - COMPLETE
  - `caretOffset` returns `{ cell: Cell, offset: number } | null` (includes cell context)
  - `setCaretInRow()` finds cell by offset, calculates cell-local offset, delegates to cell
  - `setSelectionInRow()` finds cell by offset, delegates to cell
  - `getSelectionRange()` finds active cell, delegates to cell
  - `offsetAtX()` returns `{ cell: Cell, offset: number } | null`
  - Deleted `getAnchorOffset()` from Row
  - `moveCaretToThisRow()` delegates to cell's `moveCaretToThisCell()`
  - `getHtmlOffset()` delegates to active cell's `getHtmlOffset()`

### Next Phase
- **Phase 6**: Update Controller to Use Cell-Centric Model - NOT STARTED

## Key Architectural Decisions

### Structure
- Row structure: `newEl` (div) → fold-indicator + rowContent → cells (indentation/editableCell)
- rowContent wrapper MUST be preserved - it contains all cells
- Each row has multiple cells (indent or text), not a singular content element

### Cell-Centric Model
- **Always singular cell**: Cursor/selection always in a single editable cell
- **Use `currentRow.activeCell`**: This is the target for most operations
- **Offsets need cell context**: Offsets are meaningless without knowing which cell they're in
- **Row methods delegate**: Row-level methods find the active cell and delegate to Cell methods

### Tab Character Handling
- Source text uses `\t` as delimiter
- Editor displays tabs as `VISIBLE_TAB` (`→`) in indent spans
- `Cell.visibleText` returns `\t` for indent cells (conversion is internal detail)
- `Cell.htmlContent` returns `\t` for indent cells
- `Row.htmlContent` includes leading indent characters (as `\t`) - used for DocLine conversion

### Deprecated Methods
- `Row.htmlContent` and `Row.visibleText` are deprecated for external use
- User interactions will be at the Cell level, not Row level
- Selection/cursor code should resolve to a single Cell

## Important Code Patterns

### Finding Active Cell
```typescript
const activeCell = this.cells.find(cell => cell.active);
```

### Getting Caret with Cell Context
```typescript
const caret = this.caretOffset; // Returns { cell: Cell, offset: number } | null
if (!caret) return;
// Use caret.cell and caret.offset
```

### Converting Row-Level Offset to Cell-Local Offset
```typescript
let cumulativeLength = 0;
for (const cell of this.cells) {
    const cellLength = cell.visibleTextLength;
    if (rowOffset <= cumulativeLength + cellLength) {
        const cellLocalOffset = rowOffset - cumulativeLength;
        cell.setCaret(cellLocalOffset);
        return;
    }
    cumulativeLength += cellLength;
}
```

### Finding Cell by X Coordinate
```typescript
const result = this.offsetAtX(x); // Returns { cell: Cell, offset: number } | null
if (result) {
    result.cell.moveCaretToThisCell(x);
}
```

## Phase 6 Requirements

### Must Add
- `Row.activeCell` getter - returns active cell or null

### Must Update
- All functions using `caretOffset` (now returns `{ cell, offset } | null`)
- `handleArrowLeft` - navigate to previous editable cell when at left end
- `handleArrowRight` - navigate to next editable cell when at right end
- Functions using `getSelectionRange()` (returns cell-local offsets)
- Functions using `htmlContent` for DocLine conversion

### New Behavior
- **Arrow Left from left end**: Go to previous editable cell (current row or previous row)
- **Arrow Right from right end**: Go to next editable cell (current row or next row)
- **Selection**: Always within single cell (controller prevents cross-cell selection)

## Helper Functions Needed

- `findPreviousEditableCell(row: Row, fromCell: Cell): Cell | null`
- `findNextEditableCell(row: Row, fromCell: Cell): Cell | null`

## Files Modified

- `src/editor.ts` - Main conversion work
- `doc/cell-conversion-plan.md` - Detailed plan

## Files to Modify Next

- `src/editor.ts` - Add `Row.activeCell` getter
- `src/controller.ts` - Update all keyboard handlers

## Current Issues

- None known - Phase 5 is complete with no linter errors


