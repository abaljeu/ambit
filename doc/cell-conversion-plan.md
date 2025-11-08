# Cell Structure Conversion Plan

## Overview
Converting from a structure with indent elements and a singular content element to a structure with an array of cells (indent or content), where multiple content elements are possible.

## Target Structure

The row element should consist of:
- **fold-indicator**: A span for fold/unfold indicators
- **rowContent**: A span that contains all cells
  - Cells can be either:
    - **indentation**: Indent cells (class `indentation`)
    - **editableCell**: Text cells (class `editableCell`)
    - Future-expandable to other cell types

## Current State Analysis

### Structure Issues
1. **Inconsistent DOM structure**: 
   - `createRowElement()` correctly creates `.rowContent` wrapper span
   - `setContent()` clears `newEl.innerHTML`, which removes the rowContent wrapper
   - `setContent()` then appends cells directly to `newEl` instead of to rowContent
   - `getContentSpans()` looks for `.rowContent` but it doesn't exist after `setContent()`

2. **Inconsistent constant usage**:
   - New constants defined: `RowIndentClass` ('indentation'), `TextCellClass` ('editableCell')
   - Old constants still used: `RowIndentAtt` ('indent'), `TextCellAtt` ('editableText')
   - `setContent()` uses old constants, `getContentSpans()` uses new constant

3. **Missing methods**:
   - `getContentSpan()` (singular) is called but doesn't exist
   - Methods expect a single content element but structure has multiple cells

4. **Cell access**:
   - `getContentSpans()` only returns text cells (`.editableCell`)
   - Indent cells (`.indentation`) are not included
   - `Row.cells` getter exists but only wraps text cells

### Tab Character Handling
- Source text uses `\t` as delimiter
- Editor displays tabs as `VISIBLE_TAB` (`→`) in indent spans
- `htmlContent` converts `VISIBLE_TAB` back to `\t` for source representation
- `SceneRowCells` splits source by `\t` to create cells

## Conversion Steps

### Phase 1: Fix Constants and setContent()
**Goal**: Use correct constants and ensure rowContent wrapper is preserved

2. **Fix `setContent()` in `src/editor.ts`** (lines 103-127):
   - Does not clear `newEl.innerHTML` (this would remove rowContent)
   - Gets the rowContent element: `this.newEl.querySelector(`.${RowContentClass}`)`
   - Clears only rowContent's innerHTML: `rowContent.innerHTML = ''`
   - Appends cells to rowContent, not to newEl
   - Uses correct class names: `RowIndentClass` and `TextCellClass`
   - Preserves fold-indicator (does not clear newEl.innerHTML)

3. **Verify `createRowElement()`** (lines 17-33):
   - Already correctly creates rowContent wrapper
   - No changes needed

### Phase 2: Fix Cell Access Methods
**Goal**: Make `getContentSpans()` return ALL cells (indent + text) from rowContent

1. **Update `getContentSpans()` in `src/editor.ts`** (lines 78-85): ✅ COMPLETE
   - Keeps dependency on `.rowContent` wrapper (it's correct)
   - Queries rowContent children for both `.indentation` and `.editableCell` cells
   - Returns all cells in document order
   - Uses `RowIndentClass` and `TextCellClass` constants

2. **Update `Row.cells` getter** (lines 66-74): ✅ COMPLETE
   - Returns all cells (indent + text), not just text cells
   - Each cell wrapped in `Cell` class
   - Added `contentCells` getter that returns only text cells (editableCell)

3. **Update `Cell` class** (lines 44-73): ✅ COMPLETE
   - Handles both indent and text cell elements via `isIndent` and `isText` helpers
   - `visibleText` returns `\t` for indent cells, actual text for text cells
   - `htmlContent` returns `\t` for indent cells, innerHTML for text cells (preserves HTML structure)

### Phase 3: Fix Content Access Methods
**Goal**: Update methods that access content to work with multiple cells

1. **Fix `htmlContent` getter** (lines 121-125): ✅ COMPLETE
   - Gets rowContent element directly using `RowContentClass`
   - Extracts rowContent's innerHTML to preserve HTML structure

2. **Fix `visibleText` getter** (lines 113-116): ✅ COMPLETE
   - Uses `cells` getter to get all cells (indent + text)
   - Uses `Cell.visibleText` which returns `\t` for indent cells, actual text for text cells
   - Properly handles both cell types

### Phase 4: Add Cell-Level Interaction Methods
**Goal**: Move caret/selection methods from Row to Cell level, enabling cell-centric interactions

1. **Add `Cell.caretOffset()` method** (lines 74-93): ✅ COMPLETE
   - Gets caret offset within this specific cell
   - Returns 0 if cursor is not in this cell
   - Uses `getTextOffsetFromNode()` helper with this cell's element
   - Only works for text cells (indent cells return 0)

2. **Add `Cell.setCaret(offset: number)` method** (lines 95-120): ✅ COMPLETE
   - Sets caret at offset within this cell
   - Focuses the cell first
   - Uses `getNodeAndOffsetFromTextOffset()` helper
   - Only works for text cells (indent cells are ignored)
   - Based on `Row.setCaretInParagraph()` private method

3. **Add `Cell.getSelectionRange()` method** (lines 122-143): ✅ COMPLETE
   - Gets selection range within this specific cell
   - Returns {start: 0, end: 0} if selection not in this cell
   - Uses `getTextOffsetFromNode()` helper
   - Only works for text cells

4. **Add `Cell.setSelection(start: number, end: number)` method** (lines 145-169): ✅ COMPLETE
   - Sets selection range within this cell
   - Focuses the cell first
   - Uses `getNodeAndOffsetFromTextOffset()` helper
   - Only works for text cells
   - Based on `Row.setSelectionInRow()` method

5. **Add `Cell.getAnchorOffset()` method** (lines 171-192): ✅ COMPLETE
   - Gets anchor offset within this cell
   - Returns 0 if anchor not in this cell
   - Only works for text cells
   - Based on `Row.getAnchorOffset()` method

6. **Add `Cell.offsetAtX(x: number)` method** (lines 194-227): ✅ COMPLETE
   - Finds text offset at X coordinate within this cell
   - Returns 0 if X is outside cell bounds
   - Only works for text cells
   - Based on `Row._offsetAtX()` method

7. **Add `Cell.containsNode(node: Node)` helper method** (lines 229-231): ✅ COMPLETE
   - Checks if a DOM node is within this cell's element
   - Used to determine if cursor/selection is in this cell

8. **Add `Cell.focus()` method** (lines 233-237): ✅ COMPLETE
   - Focuses this cell's element
   - Only works for text cells (indent cells are not focusable)

### Phase 5: Update Caret and Selection Methods (Row Level)
**Goal**: Find the current cell with cursor/selection and delegate to Cell methods. Controller prevents selection from expanding beyond one cell. **Important**: Offsets need cell context to be meaningful - ensure cell is identified before using offsets.

1. **Update `caretOffset` getter**:
   - Find active cell using `Cell.active()` getter (checks if cell contains `document.activeElement`)
   - Delegate to that cell's `caretOffset()` method (returns cell-local offset)
   - Return 0 if no cell is active
   - Note: `getContentSpan()` is deprecated but temporarily added for compatibility

2. **Update `setCaretInRow(visibleOffset: number)`**:
   - **Find cell context first**: Determine which cell should contain the cursor based on row-level offset
   - Calculate cell-local offset by subtracting preceding cells' cumulative text lengths
   - Delegate to that cell's `setCaret(cellLocalOffset)` method
   - Handle edge cases (offset at cell boundaries, offset beyond row length)

3. **Update `setSelectionInRow(visibleStart: number, visibleEnd: number)`**:
   - **Find cell context first**: Determine which cell contains the selection based on row-level offsets
   - Verify start and end are in the same cell (controller ensures this)
   - Calculate cell-local offsets by subtracting preceding cells' cumulative text lengths
   - Delegate to that cell's `setSelection(cellLocalStart, cellLocalEnd)` method

4. **Update `getSelectionRange()`**:
   - Find active cell using `Cell.active()` getter
   - Delegate to that cell's `getSelectionRange()` method (returns cell-local offsets)
   - Return {start: 0, end: 0} if no cell is active
   - Note: Returned offsets are cell-local, cell context is implicit (the active cell)

5. **Update `offsetAtX(x: number)`**:
   - **Return cell context with offset**: Return `{ cell: Cell, offset: number }` or `null`
   - Find which cell contains the X coordinate using `getBoundingClientRect()` on each cell
   - Delegate to that cell's `offsetAtX()` method to get cell-local offset
   - Return `null` if X is not in any cell
   - This ensures offset always has cell context

6. **Delete `getAnchorOffset()`**:
   - Remove this method as it's no longer needed


### Phase 6: Update Controller to Use Cell-Centric Model
**Goal**: Update controller functions to work with cell-centric model. Always have singular cell containing cursor/selection. Use `currentRow.activeCell` as target for operations.

**Prerequisites**:
- Add `Row.activeCell` getter that returns the active cell (or null)
- Controller ensures cursor/selection always in a single editable cell

1. **Add `Row.activeCell` getter**:
   - Returns the active cell using `Cell.active()` check
   - Returns `Cell | null` if no cell is active
   - Used throughout controller as the target for operations

2. **Update functions using `caretOffset`**:
   - `caretOffset` now returns `{ cell: Cell, offset: number } | null`
   - Update all usages to extract cell and offset from the result
   - Functions affected: `handleBackspace`, `handleDelete`, `handleArrowLeft`, `handleArrowRight`, `handleShiftArrowLeft`, `handleShiftArrowRight`, `handleWordLeft`, `handleWordRight`, `handleShiftWordLeft`, `handleShiftWordRight`, `handleHome`, `handleEnd`, `handleShiftHome`, `handleShiftEnd`, `handleTab`, `handleShiftTab`, `insertChar`, `handleInsertChar`, `extendSelectionInRow`

3. **Update `handleArrowLeft`** - New behavior:
   - Get active cell from `currentRow.activeCell`
   - If cursor at left end of cell (offset === 0):
     - Find previous editable cell in current row (iterate backwards through `contentCells`)
     - If found, move to end of that cell
     - If not found in current row, find last editable cell in previous row
     - If found, move to end of that cell
   - If cursor not at left end:
     - Move cursor left within current cell using `activeCell.setCaret(offset - 1)`

4. **Update `handleArrowRight`** - New behavior:
   - Get active cell from `currentRow.activeCell`
   - If cursor at right end of cell (offset === activeCell.visibleTextLength):
     - Find next editable cell in current row (iterate forwards through `contentCells`)
     - If found, move to start (offset 0) of that cell
     - If not found in current row, find first editable cell in next row
     - If found, move to start (offset 0) of that cell
   - If cursor not at right end:
     - Move cursor right within current cell using `activeCell.setCaret(offset + 1)`

5. **Update functions using `htmlContent` for DocLine conversion**:
   - `Row.htmlContent` includes leading indent characters (as `\t`)
   - Separations between cells should be captured with a \t
   - the cell elements themselves should not appear in htmlContent.
   - Functions that convert between Editor and DocLine use `currentRow.htmlContent`
   - Functions affected: `handleEnter`, `deleteSelection`, `deleteVisibleCharBefore`, `deleteVisibleCharAt`, `insertCharAtPosition`, `handleAddMarkup`
   - Ensure conversion preserves tab characters correctly by examining the sources involved, not by adding validation code.

6. **Update functions using `getSelectionRange()`**:
   - `getSelectionRange()` returns cell-local offsets
   - Get active cell to understand cell context
   - Functions affected: `handleBackspace`, `handleDelete`, `handleInsertChar`, `handleAddMarkup`
   - Selection is always within a single cell (controller ensures this)

7. **Update functions using `setCaretInRow` with offsets**:
   - When setting caret, ensure cell context is maintained
   - Functions that calculate offsets need to account for cell boundaries
   - Functions affected: `handleBackspace`, `handleDelete`, `handleTab`, `handleShiftTab`, `insertCharAtPosition`

8. **Update `extendSelectionInRow`**:
   - Takes row-level offset, but selection must stay within single cell
   - Find which cell contains the offset, ensure selection stays in that cell
   - Use `activeCell.setSelection()` for cell-local operations

9. **Add helper functions to find previous/next editable cell**:
   - `findPreviousEditableCell(row: Row, fromCell: Cell): Cell | null`
   - `findNextEditableCell(row: Row, fromCell: Cell): Cell | null`
   - Used by arrow key handlers to navigate between cells

### Phase 7: Update Helper Functions
**Goal**: Make helper functions work with cell structure

1. **Update `getNodeAndOffsetFromTextOffset()`**:
   - Currently takes `CellElement` (single cell)
   - Should work with row and find correct cell(s)
   - Or take array of cells and search across them

2. **Update `getTextOffsetFromNode()`**:
   - Similar to above
   - Calculate offset across multiple cells

### Phase 8: Update Text Extraction and Conversion
**Goal**: Ensure proper tab character handling

1. **Verify `visibleText`**:
   - Indent cells should contribute `VISIBLE_TAB`
   - Text cells contribute their text
   - Join in correct order

2. **Verify `htmlContent`**:
   - Extract HTML from all cells
   - Convert `VISIBLE_TAB` back to `\t`
   - Preserve HTML structure in text cells

3. **Verify `setContent()`**:
   - Already creates cells correctly
   - Ensure fold-indicator is preserved
   - Ensure cells are in correct order

### Phase 9: Testing and Edge Cases
**Goal**: Ensure all functionality works with new structure

1. **Test caret movement**:
   - Within single cell
   - Across cell boundaries
   - At start/end of row

2. **Test selection**:
   - Within single cell
   - Across multiple cells
   - Across indent and text cells

3. **Test text operations**:
   - Insert text
   - Delete text
   - Tab insertion
   - Backspace at cell boundaries

4. **Test content updates**:
   - `setContent()` with various cell configurations
   - Text extraction with mixed indent/text cells

## Implementation Order

1. **Phase 1** - Fix constants and setContent() (preserve rowContent structure)
2. **Phase 2** - Fix cell access methods (foundation)
3. **Phase 3** - Fix content access (core functionality)
4. **Phase 4** - Add Cell-level interaction methods (cell-centric operations)
5. **Phase 5** - Update caret/selection at Row level (delegates to Cell methods)
6. **Phase 6** - Update controller to use cell-centric model
7. **Phase 7** - Update helpers (supporting functions)
8. **Phase 8** - Verify text conversion (data integrity)
9. **Phase 9** - Testing (validation)

## Key Files to Modify

- `src/editor.ts`: Main conversion work
  - `getContentSpans()` method
  - `Row.cells` getter
  - `Row.activeCell` getter
  - `Cell` class methods
  - `createRowElement()` function
  - All caret/selection methods
  - Helper functions

- `src/controller.ts`: Controller updates
  - All keyboard handlers
  - Arrow key navigation (left/right between cells)
  - Functions using `caretOffset`, `getSelectionRange()`, `htmlContent`
  - Helper functions for finding previous/next editable cells

- `css/ambit.css`: Verify `.rowContent` styles are correct for containing cells

## Notes

- The `Cell` wrapper class should be used consistently where appropriate
- Tab character conversion (`\t` ↔ `VISIBLE_TAB`) must be preserved
- Caret/selection across cell boundaries is the most complex part
- Maintain backward compatibility with `SceneRowCells` structure
- **Important**: rowContent wrapper must be preserved - it contains all cells
- Row structure: `newEl` (div) → fold-indicator + rowContent → cells (indentation/editableCell)

## Architecture Direction (Apply to Remaining Phases)

- **Deprecation**: `Row.htmlContent` and `Row.visibleText` are deprecated for external use
- **Cell-level interactions**: User interactions will be at the Cell level, not Row level
- **Selection/Cursor**: Code handling selection/cursor should resolve to a single Cell that has the cursor, rather than working at the Row level
- **Implementation guidance**: As we implement Phase 4 (Caret/Selection) and Phase 5 (Helper Functions), consider moving selection/caret logic from Row methods to Cell methods where appropriate

