# 54 — Parse and import stop the Parsed Set — Import orientation

Branch: `dev` (`scripts/gitstatus.sh`).

## Import now

[ImportText](src/Shared/ImportText.fs) builds a desktop import package and the Change that applies it. `ImportText.buildPackage` turns paste text into a `DesktopImportPackage` through `Paste`. [LocalProxy](src/Desktop/LocalProxy.fs) `handleImport` calls that for a directory, sets `isDirectory`, and returns the package as JSON. A file path calls `ImportDocument.buildFilePackage`. `ImportText.buildImportChange` builds one `EventBody.Change`: paste ops, then `ChildListWire.replace`. `ImportText.buildDirectoryMergeChange` keeps top-level entries whose names are new, then `ChildListWire.append`. Both prepend `ImportText.markDocumentCurrentBeforeParse`. That function matches an Unparsed document root (`DocumentPartition.isDocumentRootNode`, `Node.documentStateEnum`). On `dev` HEAD it emits `Op.SetDocumentState(focusId, Unparsed, Current)`. The working copy returns an empty list, and sets `commandName` to `Import:` plus the focus id. `ImportText.tryImportFocus` parses that name. No file under `src` calls the two Change builders or `tryImportFocus`. [GLOSSARY](GLOSSARY.md) says Parsed for that state.

## What the issue asks for Import

[54 — Parse and import stop the Parsed Set](plan/core-refinement/issues/54-parse-and-import-stop-the-parsed-set.md) is `defined`, type coding, blocked by 44 — Apply document state from Node events.

- `ImportText.markDocumentCurrentBeforeParse` stops `Op.SetDocumentState(focusId, Unparsed, Current)`.
- `ImportText.buildImportChange` and `ImportText.buildDirectoryMergeChange` do not run on the parse thread.
- Route that import through the parse thread.
- `ParseThread.finishParse` posts `Notice.NodeParsed` for the focus node.
- The Parsed mark is `ParseThread.finishParse`. The handler sets Parsed.
- File parse and directory reconcile are the other two producers in the same issue.

## Simplest Import-first slice

### Change

- In `ImportText.markDocumentCurrentBeforeParse`, the Unparsed document-root arm returns no op. The working copy already does this.
- The two Change builders already prepend that list, so the Change keeps the paste ops and the child attach.

### Leave

- Parse-thread routing of the two Change builders.
- `ParseThread.finishParse` and `Notice.NodeParsed`.
- File parse and directory reconcile.
- Package build, paste ops, and child attach.
- The working-copy `Import:` command name. Pairing can drop it or keep it as a later hook.

### Follow-on

- This slice removes Import's writer of the Parsed mark. A later slice routes the two builders through the parse thread so `ParseThread.finishParse` posts `Notice.NodeParsed`.

## Unread on purpose

- `ParseThread`, History, `FileAgent`, `CoreMailbox`, `DirectoryReconcile`
- 119 — Add notice NodeUnpersisted, and the sibling notice issues
- Tests, including ImportText tests
- `plan/core-refinement/arch.md`, `plan/core-refinement/spec.md`, `plan/core-refinement/project.md`
- `ImportDocument` past the `buildFilePackage` call in `handleImport`
- `plan/core-refinement/reports/54-parse-and-import-stop-the-parsed-set-retro.md`
