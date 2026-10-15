# File node Open

Sources: [project.md](project.md), [01 — File node: Open button opens the file in the browser](issues/01-open-file-in-browser.md), and Alan's 2026-10-14 direction to assume Open works for every file type and expand behaviors later.

## 1. Problem Statement

1. **File nodes do not open** — A person sees a File node in the Browser and presses Open. Today Open only finds a URL in the node's text or its first Child's text, so a File node usually does nothing.
2. **The file is on the Server, not on the device** — The person, often on an iPhone, has no local copy and no local path. The only copy they can reach is the one the Server holds in its Workspace folder.
3. **Two commands would be one too many** — The person already knows Open for links. A second command just for files would split one intent across two controls.

## 2. Solution

1. **One Open command** — Open stays one command. It dispatches on the node: a File node opens its file, and every other node opens a link exactly as it does today.
2. **The Server's copy, opened natively** — On a File node, Open opens the Server's copy of the file in a new browser tab. The browser then does whatever it natively does with that file type. No local path is involved.
3. **Same login** — The tab uses the login the Browser already has. The person is not asked to sign in again.
4. **Assume it works** — This effort assumes the browser shows every file type usefully. Per-type and per-platform behavior is expanded in later work.

## 3. User Stories

1. **Open a File node** — As a person, I want Open on a File node to open that file in a new tab, so that I can read the file without leaving Ambit for another app.
2. **Open a link as today** — As a person, I want Open on a link node, or a node with link text, to open the link exactly as it does today, so that the File case does not change what I already rely on.
3. **No local path** — As a person on a phone, I want Open to use the Server's copy, so that I can open a file I have never downloaded.
4. **Same login** — As a signed-in person, I want the opened tab to use my existing login, so that I am not asked to sign in again or shown an error page.
5. **Native display** — As a person, I want the browser to receive the file with its real content type and no forced download, so that the browser can show a PDF, image, or text file inline when it is able to.
6. **Markdown as text** — As a person, I want a Markdown file to open as plain text, so that I can read its source even though the browser has no Markdown renderer.
7. **Nothing to open** — As a person, I want Open on a File node the Server cannot serve to fail quietly as a missing page, not to break the Browser, so that a stale or ignored file does not interrupt my work.

## 4. Out of Scope

1. **Per-type handling** — This effort does not add special handling for types the browser cannot render, such as Office documents on a phone. Alan chose to assume Open works and expand later.
2. **Download versus new tab** — This effort always opens a new tab. A choice between downloading and opening is later work.
3. **Rendering** — This effort does not render Markdown or any other format inside Ambit.
4. **Directories and Workspaces** — This effort opens File nodes in named Workspaces only. Open on a Directory node or a Workspace node keeps today's link behavior.
5. **Root Workspace files** — This effort does not open File nodes directly under ROOT, outside any named Workspace. Alan settled this as later work.
6. **Desktop local paths** — This effort does not open a mapped local copy through the Desktop app.
