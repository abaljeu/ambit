# WYSIWYG Markdown Implementation

## Design Decision
**Approach**: WYSIWYG Markdown - visual in DOM, markdown syntax in storage
- **DOM/Editor**: Displays formatted text using HTML (`<b>`, `<i>`, `<u>`, etc.)
- **Scene/Model**: Stores markdown syntax (`**bold**`, `*italic*`, etc.)
- **Analogous to tabs**: `→` in editor, `\t` in scene

## Architecture

### Bidirectional Conversion
Similar to tab handling, need conversion functions:
- `markdownToHtml()` - When loading Scene → Editor
- `htmlToMarkdown()` - When syncing Editor → Scene

### Supported Markdown Syntax (Initial)
- `**bold**` ↔ `<b>bold</b>`
- `*italic*` ↔ `<i>italic</i>`
- ~~`__underline__` ↔ `<u>underline</u>`~~ (not standard markdown)
- `[[wikilink]]` - already handled, keep as-is

### Editor Changes Required

#### Content Extraction/Setting
- `Row.content` getter must convert HTML → Markdown
- `Row.setContent()` must convert Markdown → HTML
- `syncAllRowsToScene()` must extract markdown, not textContent
- `getContent()` must aggregate markdown

#### Input Handling
- Trap Ctrl+B, Ctrl+I formatting commands
- Apply HTML formatting to selection in DOM
- Immediately sync to Scene as markdown
- `editorInput()` already syncs, but needs markdown conversion

#### Caret/Selection Management
- Caret offset calculations more complex with HTML nodes
- `offsetAtX()` needs to traverse HTML structure
- Selection handling for Ctrl+B must work across text nodes

### Scene Changes Required

#### RowData Storage
- No changes needed - stores plain text with markdown syntax
- Indentation calculation must ignore markdown syntax
  - `**\t\ttext**` has 2 tabs, not affected by `**`

#### Fold Calculations
- `getIndentLevel()` must skip markdown when counting tabs
- Or ensure markdown never appears before leading tabs

### Controller Changes Required

#### Keyboard Handlers
- Add cases for `C-b`, `C-i` (and others)
- Get current selection
- Wrap selection with appropriate HTML tags
- Sync to scene with markdown conversion
- Prevent default browser behavior

#### Content Sync
- `syncAllRowsToScene()` - convert HTML → Markdown
- `setEditorContent()` - convert Markdown → HTML

### Technical Challenges

#### Parsing Complexity
- Need robust markdown parser for bidirectional conversion
- Handle nested formatting: `***bold italic***`
- Handle partial formatting: `**bold on` [line break] `two lines**`
- Edge case: `**` typed but not yet closed

#### DOM Complexity
- contentEditable with HTML creates text node fragmentation
- Selection across formatted boundaries
- Cursor position when inside `<b>` tags
- Backspace/delete across format boundaries

#### Markdown Edge Cases
- `**` intended as literal asterisks, not formatting
- Escaping: `\*\*not bold\*\*`
- Markdown inside markdown: `` `**code**` ``
- Partial patterns during typing: user types `**`, should it format immediately?

#### Performance
- HTML ↔ Markdown conversion on every keystroke
- More expensive than simple tab replacement
- May need to optimize/cache

## Implementation Plan

### Phase 1: Foundation
- [ ] Implement markdown ↔ HTML conversion functions
- [ ] Update `Row.content` getter to extract markdown
- [ ] Update `Row.setContent()` to render markdown as HTML
- [ ] Test with bold only (`**text**`)

### Phase 2: Input Handling
- [ ] Trap Ctrl+B to apply formatting
- [ ] Update `syncAllRowsToScene()` for markdown
- [ ] Test round-trip: type → save → reload

### Phase 3: Additional Formatting
- [ ] Add italic support (`*text*`)
- [ ] Consider underline (non-standard markdown)
- [ ] Test nested formatting

### Phase 4: Edge Cases
- [ ] Handle line breaks inside formatted text
- [ ] Cursor positioning within formatted text
- [ ] Backspace/delete across format boundaries
- [ ] Partial pattern handling

### Phase 5: Polish
- [ ] Optimize conversion performance
- [ ] Add escape sequences if needed
- [ ] Document markdown syntax for users

## Library vs Custom Implementation

### Option 1: Use Existing Libraries
**Markdown → HTML:**
- `marked` - popular, mature, but includes block-level parsing we don't need
- `markdown-it` - modular, can disable block rules
- `micromark` - small, spec-compliant

**HTML → Markdown:**
- `turndown` - specifically designed for HTML to markdown conversion
- Can configure rules for which tags to convert

**Pros:**
- Battle-tested, handles edge cases
- Maintained by community
- Spec-compliant (CommonMark)

**Cons:**
- Bundle size (we only need inline formatting)
- May include features we don't want (block quotes, lists, etc.)
- Need to configure/strip block-level processing
- Two separate libraries for bidirectional conversion
- May conflict with our custom `[[wikilink]]` syntax

### Option 2: Custom Implementation
Write minimal converters for our specific needs:
- `**bold**` ↔ `<b>`
- `*italic*` ↔ `<i>`
- `[[wikilink]]` (already custom)
- Tabs `\t` ↔ `→` (already custom)

**Pros:**
- Minimal code (maybe 50-100 lines)
- No bundle size impact
- Full control over edge cases
- Matches existing tab conversion pattern
- Can integrate seamlessly with wikilink handling
- Easy to debug and maintain

**Cons:**
- Need to handle edge cases ourselves
- Risk of bugs in parsing
- No community testing

### Recommendation: **Custom Implementation**

**Rationale:**
1. **Scope is minimal** - Only inline bold/italic, not full markdown
2. **Existing pattern** - Already doing custom conversion for tabs and wikilinks
3. **Bundle size** - Keep client code lean
4. **Control** - Can define exact behavior for edge cases
5. **Integration** - Works naturally with existing architecture

**Implementation approach:**
```typescript
// Markdown → HTML (when loading to editor)
function markdownToHtml(text: string): string {
    return text
        .replace(/\*\*(.*?)\*\*/g, '<b>$1</b>')  // **bold**
        .replace(/\*(.*?)\*/g, '<i>$1</i>');     // *italic*
}

// HTML → Markdown (when syncing to scene)
function htmlToMarkdown(html: HTMLElement): string {
    let text = '';
    for (const node of html.childNodes) {
        if (node.nodeType === Node.TEXT_NODE) {
            text += node.textContent;
        } else if (node.nodeName === 'B') {
            text += `**${node.textContent}**`;
        } else if (node.nodeName === 'I') {
            text += `*${node.textContent}*`;
        }
        // ... handle other cases
    }
    return text;
}
```

**If scope grows:** Can always switch to library later if we add links, code blocks, etc.

## Open Questions
- [ ] Should formatting be allowed inside indented/folded sections?
- [ ] Should fold indicators recognize markdown-prefixed lines?
- [ ] How to handle `[[wikilinks]]` with formatting: `**[[link]]**`?
- [ ] Support links: `[text](url)` or just wikilinks?
- [ ] Support code: `` `inline` `` or ```` ```block``` ````?
- [ ] Support headers: `# H1`, `## H2`? (These are single lines with special meaning)