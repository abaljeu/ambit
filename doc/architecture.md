# Architecture

- **Frontend**: TypeScript compiled to ES6 modules
- **Backend**: PHP for authentication and file storage
- **Storage**: Plain text `.amb` files in `php/doc/`
- **URL format**: `ambit.php?doc=filename.amb`

## Design Principles

TypeScript code is modular. Each file defines a namespace/module as its primary export.

Code is based on strongly typed objects with clear ownership:
- Objects have public members for access
- Only the owning object or module should modify internal state
- The Model namespace encapsulates all data operations

**Dependency structure:**
- View depends on Model (never the reverse)
- Network operations (Get/Post) reference Model only, not View
- Events flow: User → View → Model → Network

**Development:**
- TypeScript compiler runs in watch mode (`tsc --watch`)
- Changes auto-compile to `/dist` with source maps
- Import paths use `.js` extension (ES6 module requirement)

## Frontend Modules

- **src/ambit.ts**: Application entry point, document loading/saving via fetch API
- **src/model.ts**: `Model` namespace containing `Doc` and `Line` classes, document cache
- **src/view.ts**: View layer with line-based editor, wikilink parsing, keyboard handlers
- **src/elements.ts**: DOM element references (editor, save button, etc.)
- **src/events.ts**: Event listener setup

### Key Implementation Details

**Model (`model.ts`):**
- `Model.Doc`: Immutable document with path, original text, and line array
- `Model.Line`: Individual line with unique ID and content
- `Model.findDoc()`, `Model.addOrUpdateDoc()`: Document cache operations
- Private `documents` array accessible only through Model functions

**View (`view.ts`):**
- Editor uses `<div>` elements (configurable via `LineElement` constant)
- Each line is a separate `contentEditable` div
- Keyboard shortcuts: Enter (new line), Arrow Up/Down (navigate), Ctrl+S (save)
- Helper functions: `getCurrentParagraph()`, `setCursorInParagraph()`
- `getEditorContent()`: Extracts content from div elements
- `setEditorContent()`: Populates editor from `Model.Doc.lines`

## Backend Structure
- **php/ambit.php**: Main PHP entry point
- **php/auth.php**: Authentication logic
- **php/core.php**: Core PHP utilities
- **php/loadsave.php**: Document load/save API endpoint
- **php/config.php**: Configuration (not in git, use config.example.php as template)
