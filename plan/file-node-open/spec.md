# File node Open

Sources: [project.md](project.md), [01 — File node: Open button opens the file in the browser](issues/01-open-file-in-browser.md), and Alan's 2026-10-14 direction to assume Open works for every file type and expand behaviors later.

## 1. Problem Statement

1. **File nodes do not open** — A person sees a File node in the Browser and presses Open. Today Open only finds a URL in the node's text or its first Child's text, so a File node usually does nothing.
2. **The file is on the Server, not on the device** — The person, often on an iPhone, has no local copy and no local path. The only copy they can reach is the one the Server holds in its Workspace folder.
3. **Two commands would be one too many** — The person already knows Open for links. A second command just for files would split one intent across two controls.

## 2. Solution

1. **One Open command** — Open stays one command. It dispatches on the node: a File node opens its file, a Workspace node or a Directory node does nothing, and every other node opens a link exactly as it does today.
2. **The Server's copy, opened natively** — On a File node, Open opens the Server's copy of the file in a new browser tab. The browser then does whatever it natively does with that file type. No local path is involved.
3. **Same login** — The tab uses the login the Browser already has. The person is not asked to sign in again.
4. **Assume it works** — This effort assumes the browser shows every file type usefully. Per-type and per-platform behavior is expanded in later work.

## 3. User Stories

1. [x] **Open a File node** — As a person, I want Open on a File node to open that file in a new tab, so that I can read the file without leaving Ambit for another app. A Workspace whose name contains `.` is still a Workspace.
2. [x] **Open a link as today** — As a person, I want Open on a link node, or a node with link text, to open the link exactly as it does today, so that the File case does not change what I already rely on. A Workspace node or a Directory node does nothing, including when the name contains `.`.
3. [x] **No local path** — As a person on a phone, I want Open to use the Server's copy, so that I can open a file I have never downloaded.
4. [x] **Same login** — As a signed-in person, I want the opened tab to use my existing login, so that I am not asked to sign in again or shown an error page.
5. **Native display** — As a person, I want the browser to receive the file with its real content type and no forced download, so that the browser can show a PDF, image, or text file inline when it is able to.
   1. [x] **PDF, image, text, and JSON** — Those types use a real content type, and the response has no forced download.
   2. [ ] **HTML content type** — `.html` and `.htm` use a real HTML content type, and a Workspace file still cannot run script. [03 — Native display of HTML](issues/03-native-display-of-html.md)
6. [x] **Markdown as text** — As a person, I want a Markdown file to open as plain text, so that I can read its source even though the browser has no Markdown renderer.
7. [x] **Nothing to open** — As a person, I want Open on a File node the Server cannot serve to fail quietly as a missing page, not to break the Browser, so that a stale or ignored file does not interrupt my work. A File node outside a named Workspace does nothing, whether or not the name contains `.`.

## 4. Out of Scope

1. **Per-type handling**
   1. [x] **No special handling in this effort** — This effort does not add special handling for types the browser cannot render, such as Office documents on a phone.
   2. [ ] **Mobile types that do not render** — How those types behave on mobile is later work. [06 — File types the browser cannot render on mobile](issues/06-file-types-the-browser-cannot-render-on-mobile.md)
2. **Download versus new tab**
   1. [x] **New tab** — This effort always opens a new tab.
   2. [ ] **Choice** — A choice between downloading and opening is later work. [05 — Download versus new tab](issues/05-download-versus-new-tab.md)
3. [x] **Rendering** — This effort does not render Markdown or any other format inside Ambit.
4. [x] **Directories and Workspaces** — This effort opens File nodes in named Workspaces only. Open on a Directory node or a Workspace node does nothing. It does not open a file URL and it does not open a link. A dot in that name is not an extension. A File node inside that Workspace or Directory still opens.
5. **Root Workspace files**
   1. [x] **Open does nothing** — Open does nothing for a File node outside a named Workspace. The name may contain an extension or no extension.
   2. [ ] **Serving those files** — Serving a File node outside a named Workspace is later work. [04 — Serve ROOT files outside a named Workspace](issues/04-serve-root-files-outside-a-named-workspace.md)
6. [x] **Desktop local paths** — This effort does not open a mapped local copy through the Desktop app.
7. [ ] **SVG script** — `.svg` is served as `image/svg+xml`, and that response can run script on the Ambit origin. [02 — SVG served as image/svg+xml can run script](issues/02-svg-served-as-image-can-run-script.md)
