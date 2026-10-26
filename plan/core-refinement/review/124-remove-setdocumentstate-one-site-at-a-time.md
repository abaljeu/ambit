# 124 — Remove SetDocumentState one site at a time

Ticket: [[../issues/124-remove-setdocumentstate-one-site-at-a-time.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- **Hard** — `.agents/rules/writing-markdown.md` says to separate each block with one blank line. `plan/core-refinement/issues/124-remove-setdocumentstate-one-site-at-a-time.md` line 7 is a second blank line before `## 1. Type`.
- **Judgement** — Duplicated Code. `tests/Shared.Tests/ImportDocumentTests.fs` `applyChange` and `tests/Shared.Tests/SpecialNodeTestHelpers.fs` `stateAfterImportMark` both read `ImportText.tryImportFocus` and apply `Notice.NodeParsed` before the Change ops. `stateAfterImportMark` keeps the prior state on `ApplyResult.Invalid`. `applyChange` raises.
- **Judgement** — Primitive Obsession. `commandName` carries the focus id as text.

```
let private importMarkPrefix = "Import:"
let importCommand (focusId: NodeId) : string =
    importMarkPrefix + focusId.Value.ToString("N")
```

## 2. Spec

- **Wrong** — Section 3, sequence item 1 Expand. `Api.postEvents` queues `Notice.NodeParsed` before `CoreDoor.postEvents` accepts the import Change. `CoreMailboxApi.postNotice` only enqueues that Notice. A rejected Change still leaves the node Current. Expand posts that Notice when `Api.postEvents` runs the import Change. On the success path the enum is Current, which matches `Ev.documentStateFromNotice` for `Notice.NodeParsed`. A second post leaves Current.
- **Unnamed** — `ImportText.importCommand` and `ImportText.tryImportFocus` add a `commandName` form `Import:` plus the focus id. The ticket and the spec leave that interface unnamed. Section 4 maps a non-empty `commandName` to a logged command.

## 3. Arch

- **Blocker** — Section 9 Story paths, Graph change expand. `src/Server/Api.fs` `Api.importParsedNotices` runs before `CoreDoor.postEvents`. That path posts `Notice.NodeParsed` through `CoreMailboxApi.postNotice` when `Api.postEvents` runs the import Change.

## 4. Tests

- Pass 1 names `build import marks unparsed file current before tree operations` and `import logs NodeParsed beside SetDocumentState`. Both facts exist. The server fact checks Current, `Notice.NodeParsed`, the child list, and a second `Notice.NodeParsed`. The shared fact checks that the Change ops are the package ops plus the attach op.
- `scripts/check-test.sh 71341` printed `DONE` and `/tmp/ambit-full-suite-20261025-231104.log`. Server.Tests failed 1, passed 761, total 762. The failed fact is `Undo pushes the document root and the persist thread writes the undone graph`. `File.ReadAllText` reported the file in use. That fact is outside this pass. That script run stopped before Shared.Tests.
- A rerun of that one fact passed. Shared.Tests then passed 1942, skipped 1, total 1943.

## 5. Miss

- **Hard.** Remove the extra blank line before `## 1. Type` in the ticket.
- **Wrong.** Apply `Notice.NodeParsed` for an import Change on the graph that Change's ops see, and commit that Notice only when that Change is accepted. A rejected Change leaves the node Unparsed.
- **Blocker.** Same fix as the Wrong item. The site is Graph change expand in `Api.postEvents`.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- No findings.
