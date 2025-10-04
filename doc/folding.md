# Folding Feature Implementation

## Overview
Ctrl+Period toggles folding of the current line. When a line is folded, any lines below it that are more indented (based on leading tab characters) become hidden until a line with equal or less indentation is encountered.

## Implementation Plan

### Scene (scene.ts)
- Add `fold: boolean` and `visible: boolean` properties to `Row` class
- Add method to calculate indentation level of a row (count leading tabs)
- Add method to identify which rows should be toggled when folding/unfolding
- Return change specification indicating which rows changed visibility

### Editor (editor.ts)
- Restructure row element: `<div><span class="fold-indicator"></span><span class="content" contenteditable="true">...</span></div>`
- Update content getters/setters to work only with content span
- Add methods to query/update fold indicator element
- Add `addAfter(referenceRow, rows[])` for bulk row insertion
- Add `deleteAfter(referenceRow, count)` for bulk row deletion
- Update caret/offset logic to work within content span only

### View (view.ts)
- Add `C-.` (Ctrl+Period) case to keyboard handler
- Implement `handleToggleFold()` function
- Query Scene for fold calculation
- Apply visibility changes to Editor (add/remove DOM elements)
- Update fold indicator display

### CSS (ambit.css)
- Style for fold indicator span (fixed width, inline-block)
- Icons/characters for folded vs foldable states

## Edge Cases (Deferred)
- **Editing folded content**: How do insert/delete operations behave when rows are hidden?
- **Navigation through folds**: Should ArrowUp/ArrowDown skip hidden rows automatically?
- **Folding nested structures**: Multiple levels of folding
- **Saving fold state**: Should fold state persist across sessions?
- **Search/replace in folded content**: Should operations affect hidden rows?
- **Copy/paste with folds**: What gets copied when rows are hidden?

## Technical Notes
- Model remains unchanged (no fold state)
- Scene tracks fold/visible state per row
- Hidden rows removed from DOM but remain in Scene
- Tabs in Scene are standard `\t`, VISIBLE_TAB `→` only in Editor display
- Indentation logic lives in Scene, not Editor
- Only rows BELOW current row can be hidden (current row never hidden by its own fold)

## Implementation Complete

### Bug Fixes (Sync Issues)

**Problem**: Editor changes weren't being synced to Scene, causing:
- Fold indicators not updating when indentation changed
- Fold operations using stale Scene data
- Tab insertions not reflected in Scene

**Solution**:
1. Created `editorInput()` handler that:
   - Syncs all editor content to scene (`syncAllRowsToScene()`)
   - Updates all fold indicators (`updateAllFoldIndicators()`)
   - Updates wikilinks
2. Updated all edit handlers to sync to scene:
   - `handleTab` - now syncs tab insertion and updates fold indicators
   - `handleEnter` - properly converts tabs and updates fold indicators
   - `handleBackspace` - syncs deletions and updates fold indicators
3. Changed event listener from `View.links` to `View.editorInput` for comprehensive sync

### Changes Made

#### Scene (scene.ts)
- Added `fold: boolean` and `visible: boolean` properties to `Row` class (default: false, true)
- Added `getIndentLevel()` method to count leading tabs
- Added `toggleFold(rowId)` method that:
  - Calculates which rows should be affected based on indentation
  - Toggles fold state on the target row
  - Updates visibility of child rows
  - Returns array of affected rows

#### Editor (editor.ts)
- Restructured row elements: `<div><span class="fold-indicator"></span><span class="content" contenteditable="true">...</span></div>`
- Updated `Row` class with:
  - `getContentSpan()` and `getFoldIndicatorSpan()` helper methods
  - Modified `content` getter/setter to work only with content span
  - Added `setFoldIndicator(indicator)` method
  - Updated caret positioning methods (`setCaretInRow`, `offsetAtX`, `focus`) to work within content span
- Updated `createRowElement()` to create structured row with both spans
- Updated `addBefore()` to set content in content span
- Updated `getCurrentParagraph()` and `getCurrentParagraphWithOffset()` to navigate DOM structure correctly
- Updated `setCaretInParagraph()` to work with content span
- Updated `getContent()` to extract text only from content spans
- Added bulk operations:
  - `addAfter(referenceRow, rows[])` - insert multiple rows efficiently
  - `deleteAfter(referenceRow, count)` - remove multiple rows efficiently

#### View (view.ts)
- Added `C-.` (Ctrl+Period) keyboard handler
- Implemented `handleToggleFold(currentRow)`:
  - Calls Scene to calculate fold changes
  - Updates fold indicator on current row
  - Removes/adds DOM rows based on fold state
  - Restores focus after operation
- Implemented `updateFoldIndicator(editorRow, sceneRow)`:
  - Checks if row has indented children
  - Sets indicator: `+` (folded), `-` (foldable), ` ` (no children)
- Updated `setEditorContent()` to:
  - Only render visible rows
  - Set fold indicators on initial load

#### CSS (ambit.css)
- Added `.fold-indicator` styles:
  - Fixed width (1.2em)
  - Centered text
  - Non-selectable
  - Monospace font
  - Pointer cursor
  - Gray color (#666)
- Added `.content` styles for proper white-space handling
- Updated focus styles to work with new structure using `:has()` selector

