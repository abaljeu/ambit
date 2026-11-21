# Code review — text file Open

- Range: `git diff HEAD` plus [FileText.fs](src/Shared/FileText.fs), [WorkspaceFileMedia.fs](src/Server/WorkspaceFileMedia.fs), and [FileTextTests.fs](tests/Shared.Tests/FileTextTests.fs)
- Spec: [05 — Download versus new tab](plan/file-node-open/issues/05-download-versus-new-tab.md)
- Arch: [File node Open architecture](plan/file-node-open/arch.md)

## 1. Standards

1. No findings.
2. The mechanical scan printed no lines.
3. `FileText.Sample` carries `[<RequireQualifiedAccess>]` so the single-word public name has a qualifier.

## 2. Spec

1. **Unnamed** — `FileText.Sample`. The spec states the text decision and the 8192-byte rule. It does not name this record. [File node Open architecture](plan/file-node-open/arch.md) section Text sample names it.
2. **Unnamed** — `FileText.Kind`. The spec states text or binary. It does not name this union. The same architecture section names it.

## 3. Arch

1. No findings.
2. The binary-sample test includes `.md` and `.gitignore`, matching the architecture test row.

## 4. Totals

1. Standards: 0 findings. The scan was empty.
2. Spec: 2 findings. Worst: the spec does not name `FileText.Sample` or `FileText.Kind`.
3. Arch: 0 findings. The touched rules hold.
