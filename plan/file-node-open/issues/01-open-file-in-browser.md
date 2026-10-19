# 01 — File node: Open button opens the file in the browser

**Type:** coding
**Status:** coded
**Blocked by:** None — can start immediately
**Trackers:** architecture, project spec, user manual

## Context

A person sees a File node in the Browser and presses Open. Open already opens a link. The file is on the Server, inside a named Workspace. The person is often on a phone and has no local copy.

## What to build

1. [x] **One Open command** — Open stays one command. A File node opens its file. A Workspace node or a Directory node does nothing. Every other node opens a link as it does today.
2. [x] **Server file in a new tab** — Open on a File node inside a named Workspace opens `/ambit/dav/{label}/{relative}` in a new tab. The label and each path segment are escaped. A dot in a Workspace name stays part of that name.
3. [x] **No local path** — The URL comes from the Graph. Open does not read a Desktop path.
4. [x] **Same login** — The new tab uses the Browser cookie. The person is not asked to sign in again.
5. [x] **Content type from the extension** — The Server sets Content-Type from the file extension and sends no `Content-Disposition: attachment`. Markdown is plain text. `.html` and `.htm` are plain text so a Workspace file cannot run script. `.svg` is `image/svg+xml`.
6. [x] **Nothing to open** — A File node that is not inside a named Workspace returns no URL. That includes a name with an extension and a name with no extension. Open does nothing. A missing or ignored file is a 404.

### Acceptance Criteria

1. [x] **Open dispatch** — `jumpTargetOp` sends `Special File` to `fileOpenUrl` and does not fall back to link search. `Special Workspace` and `Special Directory` do nothing, including a name such as `my.notes`. Every other kind keeps the link search.
2. [x] **File open URL** — `fileOpenUrl` uses the named Workspace from the node's kind (`enclosingWorkspaceName`) and the path under that label. It does not decide from a dot in the name.
3. [x] **Content type** — `contentTypeFor` follows the architecture table, including the `.html` / `.htm` plain-text row and the `.svg` row. `handleGet` passes that type to `Results.File` with no download name.
4. [x] **Tests** — Shared tests cover a dotted Workspace name, a ROOT File with an extension, and a ROOT File with no extension. Shared tests cover Open on a Workspace named `my.notes` and a Directory named `my.notes`: no file URL, and the open action is no-op. A File node inside that Workspace still opens. Server tests cover the content-type table and a cookie GET of a PDF.

## Amendments

1. **contentTypeFor is internal** — `contentTypeFor` is `internal`, not `private`, so the Server tests can call it.
2. **JSON content type** — `.json` is `application/json; charset=utf-8`.
3. **Node kind replaces the dot heuristic** — Only a File node looks at an extension. A Workspace, a Directory, and any other kind do not treat a dot in the name as meaningful. File open URL reads the named Workspace from the node's kind. It does not guess a root File from a dot in the name.
4. **String reference without a Graph** — A single path segment that contains `.` still maps to a root File on disk (`name.ext`). A directory reference keeps a dotted segment. The Graph path drops a leading segment only when that node is a File.
5. **Extracted tickets** — SVG script, native HTML display, serving ROOT files, download versus new tab, and file types the mobile browser cannot render are the later tickets named in See also.
6. **Workspace and Directory no-op** — Jump to Target on a Workspace node or a Directory node does nothing. It does not open a file URL and it does not search the node for a link. A dot in the name is not an extension. A File node inside that Workspace or Directory still opens.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, deleting things no longer true and checking off things newly achieved.

## See also

1. [02 — SVG served as image/svg+xml can run script](02-svg-served-as-image-can-run-script.md)
2. [03 — Native display of HTML](03-native-display-of-html.md)
3. [04 — Serve ROOT files outside a named Workspace](04-serve-root-files-outside-a-named-workspace.md)
4. [05 — Download versus new tab](05-download-versus-new-tab.md)
5. [06 — File types the browser cannot render on mobile](06-file-types-the-browser-cannot-render-on-mobile.md)
6. [File node Open architecture](plan/file-node-open/arch.md)
7. [File node Open](plan/file-node-open/spec.md)
8. [workspace file sync](doc/current/workspace-file-sync.md)
9. [Browser](doc/current/browser.md)
10. [Commands](doc/user/reference/commands.md)

## Time

- 2026-10-15 2h — file open URL, content type, Jump to Target dispatch, and tests (from chat)
- 2026-10-15 2h — kind-based Workspace path, tests, and ticket split (from chat)
- 2026-10-15 30m — Workspace and Directory Open no-op (from chat)
