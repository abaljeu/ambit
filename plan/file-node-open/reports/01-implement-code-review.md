# Code review — File node Open implementation

- Range: `git diff HEAD`
- Spec: [01 — File node: Open button opens the file in the browser](plan/file-node-open/issues/01-open-file-in-browser.md)
- Arch: [File node Open architecture](plan/file-node-open/arch.md)

## 1. Standards

1. **Hard** — [markdown-writing.md](.agents/rules/markdown-writing.md): "Same-directory files use the file name; other local files use a path relative to the project root." Finish-up links in [01 — File node: Open button opens the file in the browser](plan/file-node-open/issues/01-open-file-in-browser.md) used paths relative to the issue file. Corrected to `plan/file-node-open/arch.md` and `doc/current/workspace-file-sync.md`.
2. **Judgement — Mysterious Name** — [NodeDesktopPathTests.fs](tests/Shared.Tests/NodeDesktopPathTests.fs). One test name said None while the body also asserted a URL. Corrected: the URL assert is ``fileOpenUrl builds dav url for home docs readme``.
3. **Judgement — Duplicated Code** — [WorkspaceWebDavTests.fs](tests/Server.Tests/WorkspaceWebDavTests.fs). The unknown-extension case repeated the uppercase pair that `assertContentType` already checks. Corrected: that case calls `assertContentType`.
4. Scan note: measured bindings are 1–20 lines. That is under the 40-line function limit in [fsharp-source.md](.agents/rules/fsharp-source.md). Not a finding. The scanner labels `contentTypeFor` as `internal`.

## 2. Spec

1. **Wrong** — Native display. `contentTypeFor` sends `.html` and `.htm` as `text/plain; charset=utf-8`. The spec story asks for the real content type. Left in place because [File node Open architecture](plan/file-node-open/arch.md) table row 11 requires plain text so a Workspace HTML file cannot run script on the Ambit origin.
2. **Wrong** — Open a File node. `fileOpenUrl` returns `None` when `tryParseWorkspacePath` treats a dotted first segment as a root file, so a Workspace named `my.notes` does not open. Left in place because the architecture says **File open URL** uses `tryParseWorkspacePath`.

## 3. Arch

1. No findings.
2. Allowed seam: `contentTypeFor` is `internal` so the Server test project can call it. The architecture requires those Server tests. F# `private` is invisible to that project. The heading still says private.

## 4. Totals

- Standards: 3 findings, all corrected in this change. Worst: Finish-up links were not project-root paths.
- Spec: 2 findings. Worst: HTML is plain text, which the architecture table requires.
- Arch: 0 findings. Worst: none.
