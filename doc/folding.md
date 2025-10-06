# Folding has been implemented
Ctrl+. to toggle a fold.
Tab/Shift+Tab when cursor is in the indent area maintains the fold structure of the indented item and its children.

## Known Issues / TODOs

### Navigation
- [x] ArrowUp/ArrowDown should skip hidden (folded) rows
- [ ] Consider what happens when you navigate to a hidden row via search or other means

### Editing Folded Content
- [ ] When content is folded and user pastes/inserts at end of folded line, should it unfold?
- [ ] Should Ctrl+Shift+ArrowUp/Down (swap operations) work across folds?
- [ ] What happens if you try to swap a folded section?
- [ ] Enter on a folded parent line
- [ ] Backspace at beginning of folded line
- [ ] Backspace from below the fold into the fold
- [ ] Indenting a line below the fold.

### Fold State Persistence
- [x] Currently fold state is lost on save/reload.  This is by design.

### Visual Feedback
- [ ] Consider highlighting or showing count of hidden lines when folded (e.g., "+5" instead of just "+")
- [ ] Hover tooltips showing first line of folded content?

### Nested Folding
- [x] Test behavior with deeply nested folds (3+ levels)
- [ ] Ensure fold indicators update correctly when parent/child fold states change

### Edge Cases
- [ ] Copy/paste behavior - should hidden rows be included in clipboard?
- [ ] Search/replace - should it search inside folded content?
- [ ] Undo/redo with fold operations
- [ ] What happens if you fold everything? Can you still navigate?

### Performance
- [ ] `updateAllFoldIndicators()` runs on every input event - may be slow with large documents.  Should only run inside inputs that affect fold state.
- [ ] Consider throttling or incremental updates

### Code Quality
- [x] Consider consolidating tab conversion logic (scattered across multiple functions)
- [ ] Scene sync happens frequently - ensure no race conditions