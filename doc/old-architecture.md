# Architecture

## Frontend Modules

- **src/ambit.ts**: Application entry point, document loading/saving via fetch API
- **src/model.ts**: `Doc` and `Line` classes, document cache
- **src/scene.ts**: multi-doc composition (`Scene.RowData`, `Scene.Data`)
- **src/editor.ts**: DOM wrappers (`Editor.Row`, `Editor.Rows`) under `id='editor'`
- **src/controller.ts**: Controller orchestration between `Scene` and `Editor`, wikilink parsing, keyboard handlers
- **src/elements.ts**: DOM element references (editor, save button, etc.)
- **src/events.ts**: Event listener setup

### Key Implementation Details

**Model (`model.ts`):**
- `Model.Doc`: Immutable document with path, original text, and line array
- `Model.Line`: Individual line with unique ID and content
- `Model.findDoc()`, `Model.addOrUpdateDoc()`: Document cache operations
- Private `documents` array accessible only through Model functions

**Editor (`editor.ts`):**
- The view class; its state is the DOM.
- `Editor.Row`: Wraps a single `contentEditable` line element; portrays one `Scene.RowData`
- handles row-level html subelements.
- `Editor.Rows`: Manages the collection of DOM rows and DOM operations under `id='editor'`

**Scene (`scene.ts`):**
- The view model class.
- `Scene.Data`: Takes select info to/from the Model, and transforms it to what the editor will display.
- `Scene.RowData`: Represents one row of the editor, interpreting one line of the model `Model.Doc` and a `Model.Line`.

**Controller (`controller.ts`):**
- Orchestrates between `Scene` (what to show) and `Editor` (how it is shown in the DOM)
- Keyboard shortcuts: Enter (new line), Arrow Up/Down (navigate), Ctrl+S (save)
- Helper functions: `getCurrentParagraph()`, `setCursorInParagraph()`
- `getEditorContent()`: Extracts content from editor DOM
- `setEditorContent()`: Populates editor DOM from `Scene.Rows`/`Model.Doc.lines`

## Backend Structure
- **php/ambit.php**: Main PHP entry point
- **php/auth.php**: Authentication logic
- **php/core.php**: Core PHP utilities
- **php/loadsave.php**: Document load/save API endpoint
- **php/config.php**: Configuration (not in git, use config.example.php as template)
