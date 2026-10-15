# 01 — File node: Open button opens the file in the browser

**Type:** feature
**Status:** open
**Blocked by:** None

## Core design point: one Open command

Open is the same user command that already opens links. It is one command, and what it does depends on the node's type:

- If the node is a link node, or a node with link text, Open opens the link, exactly as it does today.
- If the node is a file node, Open opens the file, following the browser behaviors described below.

## Request

For a file node, the Open button opens the file that exists on the server, using whatever the browser does natively. No local path is involved.

## Target platform: iPhone Safari

How each file type behaves on iPhone Safari:

- **PDF** displays inline in the tab, using the browser's PDF viewer.
- **Images** display inline in the tab.
- **Text files** display inline.
- **Markdown files** display as raw source. Safari has no markdown renderer, so hash symbols and other markup show as written.
- **Other types**, such as Office documents, may make the browser show a download sheet instead of rendering the file. This is a known limitation and an open question, not a solved case.

## Desktop browsers

For file nodes, the same Open action should also work in desktop browsers. There, the behavior follows each browser's native handling.

## Open questions

Alan chose to assume Open works for every file type for now, so questions 1 and 2 are deferred to later work.

1. Does the choice between downloading and opening in a new tab matter beyond iPhone Safari?
2. How should file types the browser can't render be handled on mobile?
3. **Answered in [arch.md](../arch.md):** reuse the existing WebDAV GET, which already admits the Browser cookie, and give it an extension-based Content-Type. How does the browser fetch the file's bytes? Either open a new server endpoint for this, or reuse the existing WebDAV endpoint at `/ambit/dav/{label}/…` ([[doc/current/workspace-file-sync.md]]). Reusing WebDAV needs a check that a plain browser GET works there with the browser's existing login, and that it sends a content type that lets the browser render the file inline.
