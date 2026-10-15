# File node Open architecture

Spec: [[spec.md]]
Updated: 2026-10-14
Sequence: tracer-cut

Sources: [spec.md](spec.md) Solution and 7 stories; Alan's 2026-10-14 locks (one Open command with type-based dispatch; assume Open works for all file types; no grilling). In code, the user-facing Open command is `JumpToTarget`, named "Jump to Target" ([src/Shared/CommandEntry.fs:266](src/Shared/CommandEntry.fs)). Checklist: `[x]` already true of the code; `[ ]` still to build.

## 1. Story paths

1. **Open a File node**
   1. [x] Person runs Open (`JumpToTarget`, keys `Alt+j` / `j`) on a focused node ([src/Client/Commands.fs:354](src/Client/Commands.fs))
   2. [ ] **Open dispatch** sees a node of kind `Special File` and asks **File open URL** for a URL
   3. [ ] **File open URL** builds `/ambit/dav/{label}/{relative}` from the node's Workspace path
   4. [x] The Browser opens the URL in a new tab through `openUrlInNewTab` ([src/Client/JsInterop.fs:487](src/Client/JsInterop.fs))
   5. [x] **Workspace WebDAV GET** admits the request and streams the file ([src/Server/WorkspaceWebDav.fs:371](src/Server/WorkspaceWebDav.fs))

2. **Open a link as today**
   1. [ ] **Open dispatch** sees any kind other than `Special File`
   2. [x] `OpenTarget.tryFindOpenableUriWithFirstChildFallback` finds the URL in the node text, then the first Child ([src/Client/Commands.fs:248](src/Client/Commands.fs))
   3. [x] The Browser opens the URL in a new tab

3. **No local path**
   1. [x] `NodeDesktopPath.pathForNodeId` gives the File node's `//label/relative` path from the Graph alone ([src/Shared/NodeDesktopPath.fs:94](src/Shared/NodeDesktopPath.fs))
   2. [x] `NodeDesktopPath.tryParseWorkspacePath` splits it into label and relative path ([src/Shared/NodeDesktopPath.fs:175](src/Shared/NodeDesktopPath.fs))
   3. [ ] **File open URL** uses only that pair; no Desktop mapping is consulted

4. **Same login**
   1. [x] Login sets the auth cookie as `HttpOnly`, `Secure`, `SameSite=Lax` ([src/Server/RouteAuthentication.fs:22](src/Server/RouteAuthentication.fs))
   2. [x] A same-origin top-level navigation from `window.open` carries that cookie
   3. [x] The DAV route admits by cookie: `IsAuthenticated` is `mailboxIsAuthenticated`, which reads `BrowserRequestCreds.tryCookieCaller` ([src/Server/RouteRegistration.fs:424](src/Server/RouteRegistration.fs), used at [src/Server/WorkspaceWebDav.fs:532](src/Server/WorkspaceWebDav.fs))

5. **Native display**
   1. [ ] **Workspace WebDAV GET** answers with a Content-Type chosen by **File content type** from the file extension
   2. [x] The answer carries no `Content-Disposition: attachment` (`Results.File` without a download name, [src/Server/WorkspaceWebDav.fs:380](src/Server/WorkspaceWebDav.fs))
   3. [x] The browser applies its native handling

6. **Markdown as text**
   1. [ ] **File content type** maps `.md` and `.markdown` to `text/plain; charset=utf-8`

7. **Nothing to open**
   1. [ ] **File open URL** returns `None` when the node has no Workspace path; **Open dispatch** then does nothing, the same as a node with no link
   2. [x] **Workspace WebDAV GET** answers 404 for a missing or ignored file and 400 for an invalid path, as values through `Result` ([src/Server/WorkspaceWebDav.fs:371](src/Server/WorkspaceWebDav.fs))

## 2. Shared segments and test seam

1. **Core path** — every File story crosses **Open dispatch**, then **File open URL**, then **Workspace WebDAV GET**.
2. **Test seam** — **File open URL** is a pure Shared function from Graph and NodeId to `string option`, testable in Shared tests without a browser or server. **File content type** is a pure function from file name to content type, testable in Server tests.

## 3. Module map

1. **Open dispatch** — [src/Client/Commands.fs](src/Client/Commands.fs) `jumpTargetOp`
   1. State
      1. [x] None; reads the focused node from the ViewModel.
   2. Interface
      1. [ ] `jumpTargetOp : VM -> VM * Effect list` dispatches on the focused node's kind: `Special File` uses **File open URL**; every other kind uses the existing `OpenTarget` link search.
      2. [ ] A File node with no URL does nothing; it does not fall back to link search.
   3. Uses
      1. [ ] **File open URL**
      2. [x] `OpenTarget` ([src/Shared/OpenTarget.fs](src/Shared/OpenTarget.fs))
      3. [x] `openUrlInNewTab`

2. **File open URL** — [src/Shared/NodeDesktopPath.fs](src/Shared/NodeDesktopPath.fs) `fileOpenUrl` (subject home: [[doc/current/workspace-file-sync.md]])
   1. State
      1. [ ] None; pure.
   2. Interface
      1. [ ] `fileOpenUrl : Graph -> NodeId -> string option` returns `/ambit/dav/{label}/{relative}` for a `Special File` node inside a named Workspace, with the label and each path segment escaped, else `None`.
   3. Uses
      1. [x] `pathForNodeId` and `tryParseWorkspacePath`

3. **Workspace WebDAV GET** — [src/Server/WorkspaceWebDav.fs](src/Server/WorkspaceWebDav.fs) `handleGet` (subject home: [[doc/current/workspace-file-sync.md]])
   1. State
      1. [x] Files under `DataDir/{label}/`.
   2. Interface
      1. [x] `GET /ambit/dav/{label}/{*path}` admits by the Browser cookie.
      2. [ ] The response Content-Type comes from **File content type**, replacing today's fixed `application/octet-stream`.
      3. [x] No `Content-Disposition: attachment`.
   3. Uses
      1. [ ] **File content type**

4. **File content type** — [src/Server/WorkspaceWebDav.fs](src/Server/WorkspaceWebDav.fs) private `contentTypeFor`
   1. State
      1. [ ] None; pure.
   2. Interface
      1. [ ] `contentTypeFor : string -> string` uses ASP.NET Core `FileExtensionContentTypeProvider`, maps `.md` and `.markdown` to `text/plain; charset=utf-8`, and returns `application/octet-stream` for unknown extensions.

## 4. Seams

1. **Command to URL** — Interface on **File open URL**; Shared tests cross it.
2. **Browser to Server** — the existing DAV GET route; Interface on **Workspace WebDAV GET**. Server tests cross it with a cookie-bearing GET and check status and Content-Type.
3. **Desktop sync** — the Desktop download client reads GET bodies as bytes and ignores Content-Type ([src/Shared/dotnet/WorkspaceDavClient.fs:290](src/Shared/dotnet/WorkspaceDavClient.fs)), so the Content-Type change does not affect it.

## 5. Alternative considered

1. **New browser GET endpoint** — A dedicated route such as `GET /ambit/file/{label}/{*path}` with its own content-type logic. It would duplicate path resolution, the `.git` guard, the ignore check, and cookie admission that the DAV route already has ([src/Server/WorkspaceWebDav.fs:62](src/Server/WorkspaceWebDav.fs)). The only gap in the DAV route is the fixed Content-Type, a one-line change that is harmless to the Desktop client. Reusing DAV won.
2. **Opaque token URL** — `/ambit/dav-resource/{token}` already accepts GET ([src/Server/WorkspaceWebDav.fs:728](src/Server/WorkspaceWebDav.fs)). It hides the path but the opened tab would show a meaningless name, and it still needs the same login. The readable path route won.

## 6. Unsettled

1. **Root Workspace files** — `tryParseWorkspacePath` returns `None` for a file directly under ROOT, so such a File node has no URL in this design. Whether ROOT files need a DAV mount is left for later.
