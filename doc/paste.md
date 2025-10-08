# Pasting

## Current State
- No explicit paste handling - only generic `input` event handler
- Row-based architecture: each line is a `RowData` containing HTML content in a span
- HTML preservation: content stored as innerHTML with tab conversion (`\t` ↔ `→`)

## Required Components for Successful Paste

### 1. Paste Event Interception
```typescript
// In events.ts - add paste event listener
lm.editor.addEventListener('paste', handlePaste);
```

### 2. Paste Handler Function
The paste handler needs to:
- **Intercept default paste behavior** (`preventDefault()`)
- **Extract clipboard data** (both plain text and HTML)
- **Determine paste location** (current cursor position in current row)
- **Split content by newlines** to create multiple RowData objects
- **Insert new rows** at the correct position
- **Update the scene** with new RowData objects

### 3. Content Processing Pipeline

#### A. Clipboard Data Extraction
```typescript
function handlePaste(e: ClipboardEvent) {
    const clipboardData = e.clipboardData;
    const htmlData = clipboardData?.getData('text/html');
    const textData = clipboardData?.getData('text/plain');
    
    // Prefer HTML if available, fallback to plain text
    const content = htmlData || textData;
}
```

#### B. Line Division Logic
```typescript
function divideContentIntoLines(content: string): string[] {
    // Split by newlines, preserving HTML structure
    return content.split('\n');
}
```

#### C. RowData Creation and Insertion
```typescript
function insertPastedContent(currentRow: Editor.Row, lines: string[]): void {
    const scene = Scene.data;
    const currentRowIndex = scene.findIndexByLineId(currentRow.id);
    
    // Process each line
    for (let i = 0; i < lines.length; i++) {
        const lineContent = lines[i];
        
        if (i === 0) {
            // First line: split current row and insert at cursor position
            const htmlOffset = currentRow.getHtmlOffset();
            const beforeContent = currentRow.content.substring(0, htmlOffset);
            const afterContent = currentRow.content.substring(htmlOffset);
            
            // Update current row with content before cursor + first pasted line
            const newContent = beforeContent + lineContent;
            scene.updateRowData(currentRow.id, newContent);
            
            // Insert remaining lines after current row
            if (lines.length > 1) {
                const remainingLines = lines.slice(1);
                for (const remainingLine of remainingLines) {
                    scene.insertBefore(currentRow.id, remainingLine);
                }
            }
        }
    }
}
```

### 4. Integration Points

#### A. HTML Tag Fixing
- Use `HtmlUtil.fixTags()` to ensure proper HTML structure
- Apply to each line before inserting into RowData

#### B. Tab Conversion
- Convert `\t` to `→` for display in editor
- Convert `→` back to `\t` for storage in scene

#### C. Scene Synchronization
- Update `Scene.data` with new RowData objects
- Trigger `editorInput()` to sync editor display
- Update fold indicators if needed

### 5. Edge Cases

#### A. Pasting at End of Row
- If cursor at end of row, append first line to current row
- Insert remaining lines as new rows after current row

#### B. Pasting at Beginning of Row
- If cursor at start of row, prepend first line to current row
- Insert remaining lines as new rows before current row

#### C. Pasting in Middle of Row
- Split current row at cursor position
- Insert first pasted line after split
- Insert remaining lines as new rows

#### D. Empty Lines
- Handle empty lines in pasted content
- Create RowData with empty content string

### 6. Implementation Steps

1. **Add paste event listener** in `events.ts`
2. **Create `handlePaste` function** in `controller.ts`
3. **Implement content division logic** for line splitting
4. **Integrate with existing row insertion methods** (`Scene.data.insertBefore`)
5. **Add HTML processing** using `HtmlUtil.fixTags()`
6. **Test with various content types** (plain text, HTML, mixed)

## Plain Text Pasting
- Extract `text/plain` from clipboard
- Split by newlines
- Insert each line as new RowData
- Apply tab conversion for display

## HTML Pasting
- Extract `text/html` from clipboard
- Split by newlines while preserving HTML structure
- Use `HtmlUtil.fixTags()` to ensure valid HTML
- Insert each line as new RowData with HTML content
