# File node Open architecture

Spec: [[spec.md]]
Updated: 2026-10-20
Sequence: tracer-cut

Sources: [spec.md](spec.md) Solution and 7 stories; Alan's 2026-10-14 locks (one Open command with type-based dispatch; assume Open works for all file types; no grilling). In code, the user-facing Open command is `JumpToTarget`, named "Jump to Target" ([src/Shared/CommandEntry.fs:266](src/Shared/CommandEntry.fs)). Checklist: `[x]` already true of the code; `[ ]` still to build.

## 1. Story paths

1. **Open a File node**
   1. [x] Person runs Open (`JumpToTarget`, keys `Alt+j` / `j`) on a focused node ([src/Client/Commands.fs:367](src/Client/Commands.fs))
   2. [x] **Open dispatch** sees a node of kind `Special File` and asks **File open URL** for a URL
   3. [x] **File open URL** builds `/ambit/dav/{label}/{relative}` from the node's Workspace path
   4. [x] The Browser opens the URL in a new tab through `openUrlInNewTab` ([src/Client/JsInterop.fs:487](src/Client/JsInterop.fs))
   5. [x] **Workspace WebDAV GET** admits the request and streams the file ([src/Server/WorkspaceWebDav.fs:391](src/Server/WorkspaceWebDav.fs))

2. **Open a link as today**
   1. [x] **Open dispatch** sees a kind other than `Special File`, `Special Workspace`, and `Special Directory`
   2. [x] `OpenTarget.tryFindOpenableUriWithFirstChildFallback` finds the URL in the node text, then the first Child ([src/Client/Commands.fs:250](src/Client/Commands.fs))
   3. [x] The Browser opens the URL in a new tab

3. **Workspace and Directory do nothing**
   1. [x] **Open dispatch** on `Special Workspace` or `Special Directory` returns no effects
   2. [x] That dispatch does not call **File open URL** and does not call link search
   3. [x] A dot in the Workspace or Directory name is not an extension
   4. [x] A File node inside that Workspace or Directory still opens

4. **No local path**
   1. [x] `NodeDesktopPath.pathForNodeId` gives the File node's path from the Graph alone ([src/Shared/NodeDesktopPath.fs:94](src/Shared/NodeDesktopPath.fs))
   2. [x] `NodeDesktopPath.enclosingWorkspaceName` reads the named Workspace from the node's kind ([src/Shared/NodeDesktopPath.fs:253](src/Shared/NodeDesktopPath.fs)). A dot in a Workspace name is not an extension. Only a File node looks at an extension.
   3. [x] `relativeUnderLabel` takes the path under that label ([src/Shared/NodeDesktopPath.fs:213](src/Shared/NodeDesktopPath.fs))
   4. [x] **File open URL** uses only that pair; no Desktop mapping is consulted

5. **Same login**
   1. [x] Login sets the auth cookie as `HttpOnly`, `Secure`, `SameSite=Lax` ([src/Server/RouteAuthentication.fs:22](src/Server/RouteAuthentication.fs))
   2. [x] A same-origin top-level navigation from `window.open` carries that cookie
   3. [x] The DAV route admits by cookie: `IsAuthenticated` is `mailboxIsAuthenticated`, which reads `BrowserRequestCreds.tryCookieCaller` ([src/Server/RouteRegistration.fs:424](src/Server/RouteRegistration.fs), used at [src/Server/WorkspaceWebDav.fs:554](src/Server/WorkspaceWebDav.fs))

6. **Native display**
   1. [x] **Workspace WebDAV GET** answers with a Content-Type from **File content type**. PDF and images use the extension. HTML and SVG use the extension. Every other file uses **Text sample**.
   2. [x] A text file is `text/plain; charset=utf-8` with `Content-Disposition: inline` ([src/Server/WorkspaceWebDav.fs:380](src/Server/WorkspaceWebDav.fs))
   3. [x] The answer carries no `Content-Disposition: attachment`
   4. [x] The browser shows a text file in the tab. A binary file still downloads.

7. **Markdown as text**
   1. [x] A Markdown file whose sample is text is `text/plain; charset=utf-8` (table in Module map, File content type)

8. **Nothing to open**
   1. [x] **File open URL** returns `None` when the File node is not inside a named Workspace, including a name with no `.`; **Open dispatch** then does nothing, the same as a node with no link
   2. [x] **Workspace WebDAV GET** answers 404 for a missing or ignored file and 400 for an invalid path, as values through `Result` ([src/Server/WorkspaceWebDav.fs:391](src/Server/WorkspaceWebDav.fs))

## 2. Shared segments and test seam

1. **Core path** — every File story crosses **Open dispatch**, then **File open URL**, then **Workspace WebDAV GET**.
2. **Test seam** — **File open URL** is a pure Shared function from Graph and NodeId to `string option`, testable in Shared tests without a browser or server. `nodeOpenAction` is a pure Shared function from node kind to open-file, open-link, or no-op. **Text sample** is a pure Shared function from a byte sample to text or binary, testable in Shared tests. **File content type** is a pure function from a file name and that sample to a content type, testable in Server tests.

## 3. Module map

1. **Open dispatch** — [src/Client/Commands.fs](src/Client/Commands.fs) `jumpTargetOp`
   1. State
      1. [x] None; reads the focused node from the ViewModel.
   2. Interface
      1. [x] `jumpTargetOp : VM -> VM * Effect list` dispatches on `nodeOpenAction`: `Special File` uses **File open URL**; `Special Workspace` and `Special Directory` do nothing; every other kind uses the existing `OpenTarget` link search.
      2. [x] A File node with no URL does nothing; it does not fall back to link search.
      3. [x] A Workspace or Directory does nothing even when its text contains a link, and a dot in its name is not an extension.
   3. Uses
      1. [x] `nodeOpenAction`
      2. [x] **File open URL**
      3. [x] `OpenTarget` ([src/Shared/OpenTarget.fs](src/Shared/OpenTarget.fs))
      4. [x] `openUrlInNewTab`

2. **File open URL** — [src/Shared/NodeDesktopPath.fs](src/Shared/NodeDesktopPath.fs) `fileOpenUrl` (subject home: [[doc/current/browser.md]])
   1. State
      1. [x] None; pure.
   2. Interface
      1. [x] `fileOpenUrl : Graph -> NodeId -> string option` returns `/ambit/dav/{label}/{relative}` for a `Special File` node inside a named Workspace, with the label and each path segment escaped, else `None`.
   3. Uses
      1. [x] `enclosingWorkspaceName`, `pathForNodeId`, and `relativeUnderLabel`

3. **Workspace WebDAV GET** — [src/Server/WorkspaceWebDav.fs](src/Server/WorkspaceWebDav.fs) `handleGet` (subject home: [[doc/current/workspace-file-sync.md]])
   1. State
      1. [x] Files under `DataDir/{label}/`.
   2. Interface
      1. [x] `GET /ambit/dav/{label}/{*path}` admits by the Browser cookie.
      2. [x] The response Content-Type comes from **File content type** through `openFileResult` in `handleGet` (line 380).
      3. [x] A text file sends `Content-Disposition: inline`. No response sends `Content-Disposition: attachment`.
   3. Uses
      1. [x] **File content type**

4. **File content type** — [src/Server/WorkspaceFileMedia.fs](src/Server/WorkspaceFileMedia.fs) `internal` function `contentTypeFor` (subject home: [[doc/current/workspace-file-sync.md]])
   1. State
      1. [x] None; pure. The extension table is fixed in code, not configuration.
   2. Interface
      1. [x] `contentTypeFor : fileName: string -> sample: FileText.Sample -> string` reads the extension with `Path.GetExtension`, compares it case-insensitively, and returns a Content-Type. It never fails.
      2. [x] PDF and image extensions keep their media types. The sample is ignored.
         1. `.pdf` → `application/pdf`
         2. `.png` → `image/png`
         3. `.jpg`, `.jpeg` → `image/jpeg`
         4. `.gif` → `image/gif`
         5. `.webp` → `image/webp`
         6. `.svg` → `image/svg+xml`
         7. `.heic` → `image/heic`
      3. [x] `.html` and `.htm` → `text/plain; charset=utf-8`, so a Workspace file cannot run script on the origin. The sample is ignored.
      4. [x] Every other extension, and a name with no extension, uses **Text sample**.
         1. Text → `text/plain; charset=utf-8`
         2. Binary → `application/octet-stream`
   3. Uses
      1. [x] `System.IO.Path.GetExtension`
      2. [x] **Text sample**
   4. Call site
      1. [x] `handleGet` calls `WorkspaceFileMedia.openFileResult` ([src/Server/WorkspaceWebDav.fs:380](src/Server/WorkspaceWebDav.fs)). That reads the first 8192 bytes, picks the Content-Type, and serves the file. A text file sets `Content-Disposition: inline`. PROPFIND, PUT, and MKCOL are unchanged.
   5. Tests
      1. [x] Server tests check one kept extension per entry, including uppercase extensions such as `.PDF`. A text sample and a binary sample do not change PDF, image, HTML, or SVG.
      2. [x] Server tests check that a text sample is `text/plain; charset=utf-8` for a source extension, `.json`, `.txt`, `.md`, `.gitignore`, and a name with no extension.
      3. [x] Server tests check that a binary sample is `application/octet-stream` for those names.
      4. [x] A Server route test sends a cookie-bearing `GET` of a text file and checks status 200, Content-Type `text/plain; charset=utf-8`, and `Content-Disposition: inline`.
      5. [x] A Server route test sends a cookie-bearing `GET /ambit/dav/{label}/doc.pdf` and checks status 200, Content-Type `application/pdf`, and no `Content-Disposition: attachment`.

5. **Text sample** — [src/Shared/FileText.fs](src/Shared/FileText.fs) `classifySample`
   1. State
      1. [x] None; pure.
   2. Interface
      1. [x] `classifySample : FileText.Sample -> FileText.Kind` decides from the bytes in the sample. `truncated` is true when the file is longer than 8192 bytes.
      2. [x] A sample with a NUL byte is binary.
      3. [x] A sample that is valid UTF-8 is text. An incomplete UTF-8 sequence at the end is text only when `truncated` is true.
      4. [x] Any other sample is binary. A NUL or invalid UTF-8 past the first 8192 bytes does not change the decision.
   3. Uses
      1. [x] None.
   4. Tests
      1. [x] Shared tests cover an empty sample, ASCII, UTF-8, a NUL byte, invalid UTF-8, a truncated tail, and an incomplete sequence at the end of the file.

## 4. Seams

1. **Command to URL** — Interface on **File open URL**; Shared tests cross it.
2. **Browser to Server** — the existing DAV GET route; Interface on **Workspace WebDAV GET**. Server tests cross it with a cookie-bearing GET and check status, Content-Type, and Content-Disposition.
3. **Desktop sync** — the Desktop download client reads GET bodies as bytes and ignores Content-Type ([src/Shared/dotnet/WorkspaceDavClient.fs:290](src/Shared/dotnet/WorkspaceDavClient.fs)), so the Content-Type change does not affect it.

## 5. Alternative considered

1. **New browser GET endpoint** — A dedicated route such as `GET /ambit/file/{label}/{*path}` with its own content-type logic. It would duplicate path resolution, the `.git` guard, the ignore check, and cookie admission that the DAV route already has ([src/Server/WorkspaceWebDav.fs:62](src/Server/WorkspaceWebDav.fs)). The only gap in the DAV route is the fixed Content-Type, a one-line change that is harmless to the Desktop client. Reusing DAV won.
2. **Opaque token URL** — `/ambit/dav-resource/{token}` already accepts GET ([src/Server/WorkspaceWebDav.fs:750](src/Server/WorkspaceWebDav.fs)). It hides the path but the opened tab would show a meaningless name, and it still needs the same login. The readable path route won.

## 6. Out of scope; later work

1. **Root Workspace files**
   1. [x] Open on a File node that is not inside a named Workspace returns no URL, so Open does nothing. The name may contain an extension or no extension. The decision uses the node's kind, not a dot in the name.
   2. [ ] Serving a File node outside a named Workspace is later work. [04 — Serve ROOT files outside a named Workspace](issues/04-serve-root-files-outside-a-named-workspace.md)
2. [ ] **SVG script** — `.svg` is `image/svg+xml` in the table above. A same-origin SVG document can run script. [02 — SVG served as image/svg+xml can run script](issues/02-svg-served-as-image-can-run-script.md)
3. [ ] **HTML native display** — The `.html` / `.htm` row is plain text so a Workspace file cannot run script. Native HTML display is later work. [03 — Native display of HTML](issues/03-native-display-of-html.md)
4. **Download versus new tab**
   1. [x] Open always uses a new tab. A text file shows in that tab and is never downloaded.
   2. [ ] The choice for a binary type stays open. [05 — Download versus new tab](plan/file-node-open/issues/05-download-versus-new-tab.md)
5. [ ] **File types the browser cannot render on mobile** — This effort adds no per-type handling. [06 — File types the browser cannot render on mobile](issues/06-file-types-the-browser-cannot-render-on-mobile.md)

## 7. Unsettled

1. [ ] **Binary download versus new tab** — A text file is settled. The choice for a binary type stays open. [05 — Download versus new tab](plan/file-node-open/issues/05-download-versus-new-tab.md)
2. [ ] **File types the browser cannot render on mobile** — [06 — File types the browser cannot render on mobile](issues/06-file-types-the-browser-cannot-render-on-mobile.md)
