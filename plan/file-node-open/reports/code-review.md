# Code review — File node Open

- Range: `git diff origin/staging...HEAD`
- Spec: [File node Open](plan/file-node-open/spec.md)
- Arch: [File node Open architecture](plan/file-node-open/arch.md)

## 1. Standards

1. **Hard — Bare question numbers** — [refer-by-name.md](.agents/rules/refer-by-name.md): "Never identify anything only by number. Always include a name." [01 — File node: Open button opens the file in the browser](plan/file-node-open/issues/01-open-file-in-browser.md) named Download versus new tab and File types the browser cannot render on mobile only by number.
2. **Hard — Bare table row** — [refer-by-name.md](.agents/rules/refer-by-name.md): "Never identify anything only by number. Always include a name." [01 — File node Open implementation](plan/file-node-open/reports/01-implement-code-review.md) named the `.html` / `.htm` plain-text row only by a table number.
3. **Hard — Issue-relative arch link** — [markdown-writing.md](.agents/rules/markdown-writing.md): "Same-directory files use the file name; other local files use a path relative to the project root." [01 — File node: Open button opens the file in the browser](plan/file-node-open/issues/01-open-file-in-browser.md) line 38 links `[arch.md](../arch.md)`. Line 43 of that issue already uses `plan/file-node-open/arch.md`.
4. **Note — Scan sizes** — Measured bindings are under the 40-line function limit in [fsharp-source.md](.agents/rules/fsharp-source.md): `openFileNode` 7, `openLinkTarget` 15, `jumpTargetOp` 11, `contentTypeFor` 20, `davFileUrl` 10, `fileOpenUrl` 12. No added source line is over 100 characters. [WorkspaceWebDav.fs](src/Server/WorkspaceWebDav.fs) is 773 lines, under 800, and this change did not push it over the limit. Not findings.
5. **Note — Public names** — Met. `fileOpenUrl` at [NodeDesktopPath.fs](src/Shared/NodeDesktopPath.fs) line 206 is public, more than one word, and the only definition. The module has `[<RequireQualifiedAccess>]`. `contentTypeFor` at [WorkspaceWebDav.fs](src/Server/WorkspaceWebDav.fs) line 371 is `internal` and more than one word. No new single-word public name.
6. **Note — Surgical changes** — Met. [core-agent-behavior.md](.agents/rules/core-agent-behavior.md) Surgical Changes. The diff is the Open dispatch in [Commands.fs](src/Client/Commands.fs) lines 234–265, the content-type table and the `Results.File` call, the DAV URL helpers, their tests, and this feature's plan and current-doc lines.
7. **Note — Errors as values** — Met. [fsharp-source.md](.agents/rules/fsharp-source.md): "Don't throw Exceptions. Use Error types." `fileOpenUrl` returns `string option`. `contentTypeFor` returns a string for every extension. `jumpTargetOp` lines 262–265 return `model, []` when the node is missing. The new production bindings do not `raise` or `failwith`.
8. **Note — Product name in current docs** — Met. Added lines 39–41 of [workspace file sync](doc/current/workspace-file-sync.md) keep Ambit wording.

## 2. Spec

1. **Wrong — Native display for HTML** — [File node Open](plan/file-node-open/spec.md) story Native display asks for the file's real content type. `contentTypeFor` returns `text/plain; charset=utf-8` for `.html` and `.htm` ([WorkspaceWebDav.fs](src/Server/WorkspaceWebDav.fs) lines 387–388). [File node Open architecture](plan/file-node-open/arch.md) requires that row, and `handleGet` uses it (lines 400–402).
2. **Partial — Open a File node in a dotted workspace** — Story Open a File node asks Open on a File node to open that file. `Filename.create` accepts `.` ([Filename.fs](src/Shared/Filename.fs) lines 32–40). `isRootFileSegment` treats a first segment that contains `.` as a root file ([NodeDesktopPath.fs](src/Shared/NodeDesktopPath.fs) lines 118–119). `tryParseWorkspacePath` then returns `None` (lines 201–202) and `fileOpenUrl` returns `None` (lines 213–215). A file in a workspace named `my.notes` does nothing. The architecture Uses line says to call `tryParseWorkspacePath`. The Interface line still says a file inside a named workspace gets `/ambit/dav/{label}/{relative}`.
3. **Partial — Root file with no dot** — Out of scope item Root Workspace files says this effort does not open a File node directly under ROOT. A ROOT file named `name.ext` returns `None` ([NodeDesktopPathTests.fs](tests/Shared.Tests/NodeDesktopPathTests.fs) lines 288–297). A ROOT file whose name has no `.` fails `isRootFileSegment`, so `tryParseWorkspacePath` returns `Some (name, "")` ([NodeDesktopPath.fs](src/Shared/NodeDesktopPath.fs) line 203) and `openFileNode` opens `/ambit/dav/{name}` ([Commands.fs](src/Client/Commands.fs) lines 235–238).
4. **Note — No new data type** — The diff adds no global type. `fileOpenUrl` returns `string option`. No **Unnamed** item.

## 3. Arch

1. No findings.
2. **Note — Dotted workspace label** — [File node Open architecture](plan/file-node-open/arch.md) §3 File open URL Uses holds: `fileOpenUrl` calls `pathForNodeId` and `tryParseWorkspacePath`. That module's Interface does not hold when the workspace label contains `.` ([NodeDesktopPath.fs](src/Shared/NodeDesktopPath.fs) lines 118, 201, 213). A label without `.` still gets the escaped URL (lines 175–186).
3. **Note — Extensionless ROOT file** — §6 Out of scope holds for a ROOT file whose name contains `.` (`//name.ext` → `None`). An extensionless File directly under ROOT gets `Some "/ambit/dav/{name}"` and `openFileNode` calls `openUrlInNewTab` ([Commands.fs](src/Client/Commands.fs) line 237).
4. **Note — SVG script** — §3 File content type row `.svg` → `image/svg+xml` holds at [WorkspaceWebDav.fs](src/Server/WorkspaceWebDav.fs) line 379. The "cannot run script" rationale is only on the `.html` / `.htm` row (lines 387–388). A top-level `image/svg+xml` response can run script on the Ambit origin. The code follows the written table.

## 4. Totals

- Standards: 3 findings. Worst: the Open questions link still uses `../arch.md`.
- Spec: 3 findings. Worst: a File node in a workspace whose label contains `.` does not open.
- Arch: 0 findings. Worst: none.

## 5. Severity — Must-fix, Should-fix, and Nit

1. **Must-fix — Dotted workspace label** — Open does nothing for a legal workspace name such as `my.notes`. See Spec item Partial — Open a File node in a dotted workspace, and Arch note Dotted workspace label. `enclosingWorkspaceName` already returns `None` for ROOT and the label for a named workspace ([NodeDesktopPath.fs](src/Shared/NodeDesktopPath.fs) lines 218–233).
2. **Should-fix — Extensionless ROOT file** — Open navigates for a ROOT file whose name has no `.`. See Spec item Partial — Root file with no dot, and Arch note Extensionless ROOT file. The tested ROOT name `name.ext` does nothing.
3. **Should-fix — SVG on this origin** — `.svg` is `image/svg+xml` while `.html` is plain text so a workspace file cannot run script. See Arch note SVG script. A same-origin SVG document can run script.
4. **Nit — Bare question numbers** — See Standards item Hard — Bare question numbers. [01 — File node: Open button opens the file in the browser](plan/file-node-open/issues/01-open-file-in-browser.md) line 34.
5. **Nit — Bare table row** — See Standards item Hard — Bare table row. [01 — File node Open implementation](plan/file-node-open/reports/01-implement-code-review.md) line 16.
6. **Nit — Issue-relative arch link** — See Standards item Hard — Issue-relative arch link. Line 38 of that issue.
7. **Nit — HTML content type** — Spec story Native display and the architecture table disagree. The code follows the table. See Spec item Wrong — Native display for HTML. This is not a code defect against the architecture.

## 6. Facet verdicts

1. **no_local_path — met** — `fileOpenUrl` builds `/ambit/dav/...` from the graph `//` path ([NodeDesktopPath.fs](src/Shared/NodeDesktopPath.fs) lines 206–214). `openFileNode` opens that URL ([Commands.fs](src/Client/Commands.fs) lines 235–238). No desktop map and no local path are read.
2. **same_login — met** — `openUrlInNewTab` is same-origin `window.open` to `_blank` ([JsInterop.fs](src/Client/JsInterop.fs) lines 487–488). The auth cookie is HttpOnly, Secure, and SameSite=Lax ([RouteAuthentication.fs](src/Server/RouteAuthentication.fs) lines 24–27). `registerPersistenceAndRoutes` installs `mailboxIsAuthenticated` before `WorkspaceWebDav.registerRoutes` ([RouteRegistration.fs](src/Server/RouteRegistration.fs) lines 424–431 and 491). DAV `dispatch` returns Unauthorized when that check fails ([WorkspaceWebDav.fs](src/Server/WorkspaceWebDav.fs) lines 554–555 and 758–763). The check reads `tryCookieCaller` ([BrowserRequestCreds.fs](src/Server/BrowserRequestCreds.fs) lines 28–29). The PDF route test sends that cookie and expects 200 ([WorkspaceWebDavTests.fs](tests/Server.Tests/WorkspaceWebDavTests.fs) lines 207–209).
3. **native_display — partial** — `Results.File` gets `contentTypeFor` and no download name ([WorkspaceWebDav.fs](src/Server/WorkspaceWebDav.fs) lines 400–402). PDF, images, text, and json use a real type (lines 373–386). `.html` and `.htm` do not (lines 387–388).
4. **markdown_as_text — met** — `.md` and `.markdown` map to `text/plain; charset=utf-8` ([WorkspaceWebDav.fs](src/Server/WorkspaceWebDav.fs) lines 384–385) on that `Results.File` call (lines 400–402). There is no `Content-Disposition: attachment`.
5. **nothing_to_open — partial** — No URL returns `model, []` with no exception ([Commands.fs](src/Client/Commands.fs) line 239). A ROOT file named `name.ext` is `None` ([NodeDesktopPathTests.fs](tests/Shared.Tests/NodeDesktopPathTests.fs) lines 288–297). A ROOT file with no `.` opens a tab (Spec item Partial — Root file with no dot). A stale or ignored file that does have a URL is `Results.NotFound()` ([WorkspaceWebDav.fs](src/Server/WorkspaceWebDav.fs) lines 393–397).
6. **link_unchanged — met** — Any kind other than `Special File` calls `openLinkTarget` ([Commands.fs](src/Client/Commands.fs) line 264), which is the previous link search and new-tab open (lines 241–254). A missing node now returns `model, []` (line 265) instead of indexing a missing node.
7. **file_node_new_tab — met** — `Special File` calls `openFileNode` ([Commands.fs](src/Client/Commands.fs) line 263), which calls `openUrlInNewTab` (line 237, [JsInterop.fs](src/Client/JsInterop.fs) line 487).
8. **spec_binding_conflicts — partial** — Spec story Native display and architecture table row `.html` / `.htm` disagree. The code follows the architecture ([WorkspaceWebDav.fs](src/Server/WorkspaceWebDav.fs) lines 387–388). Spec story Open a File node and architecture §3 File open URL Interface disagree with `tryParseWorkspacePath` for a dotted workspace label. The code follows that parser. ROOT doing nothing for `name.ext` (spec out of scope Root Workspace files, architecture §6 Out of scope) and a 404 for a stale or ignored file (spec story Nothing to open, architecture §1 Nothing to open, [WorkspaceWebDav.fs](src/Server/WorkspaceWebDav.fs) lines 393–397) agree.
9. **refer_by_name — partial** — [spec.md](plan/file-node-open/spec.md), [arch.md](plan/file-node-open/arch.md), and [project.md](plan/file-node-open/project.md) number and name their items. Two nits remain: Standards items Hard — Bare question numbers, and Hard — Bare table row.
10. **public_names — met** — See Standards note Public names.
11. **surgical_changes — met** — See Standards note Surgical changes.
