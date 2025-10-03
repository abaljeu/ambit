# Ambit

A minimalist wiki-like document editor with wikilink support.

## What it does

- Edit plain text documents (`.amb` files) stored on the server
- Create links between documents using `[[document name]]` syntax
- Each document is a collection of editable lines
- Documents are cached in memory and synced to the server on save

## Key features

- **Wikilinks**: Type `[[page name]]` to create navigable links
- **Auto-save**: Ctrl+S saves to server
- **Document cache**: Previously loaded documents stay in memory
- **Line-based editing**: Each line is an independent editable element
- **Keyboard Shortcuts**: Custom shortcuts handled in view.ts (editorKeyDown)


