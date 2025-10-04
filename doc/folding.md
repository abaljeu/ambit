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

