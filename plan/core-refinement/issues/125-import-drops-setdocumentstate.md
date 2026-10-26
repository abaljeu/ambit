# 125 — Import drops its SetDocumentState

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

This is round 1 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). Importing into an Unparsed document root moves that root `Unparsed → Current` before the import ops, so `History.blockedForApply` lets them through. Since #407 the op is back: `ImportText.markDocumentCurrentBeforeParse` puts `Op.SetDocumentState(focusId, Unparsed, Current)` first in `buildImportChange` and `buildDirectoryMergeChange`, and `DocumentOpImpact.noticesBeside` posts `NodeParsed` from it.

## 4. Change

- Delete `markDocumentCurrentBeforeParse` and its use in `buildImportChange` and `buildDirectoryMergeChange`.
- The import Change carries only the paste ops and the `Replace` on the root.

Backend inference: an Edit Change whose `Replace` attaches owned children to an Unparsed document root infers a lead `NodeParsed root`. The rule goes in `DocumentOpImpact.inferNotices` and is applied by the 135 hook in `ChangeAmendment.applyPosted`.

Hard: those ops are the same as an ordinary edit under an Unparsed root, which `blockedForApply` exists to refuse. The ops alone do not say "import", and since #407 its command name is empty.

Source: `ImportText.fs`, `DocumentOpImpact.fs`.

**Resolution (Alan, 2026-10-26):** the inference above is moot. The removed code made no edit changes, so there is nothing to infer from. The deletion was done by [#415 — 125: delete dead desktop ImportText parse-before-upload path](https://github.com/abaljeu/ambit/pull/415) and [#416 — 125: delete unreachable client parse effect that posted to removed /ambit/file/parse](https://github.com/abaljeu/ambit/pull/416).

## 5. Tests

[ ] `ImportTextTests` `build import marks unparsed file current before tree operations`
[ ] `ImportDocumentTests`
[ ] `SpecialNodeTestHelpers`
[ ] `NodeEventExpandTests` `import logs NodeParsed beside SetDocumentState`

## 6. Open question (Alan)

On staging, `ImportText.buildImportChange` and `buildDirectoryMergeChange` have no `src` caller. Only tests call them. Is round 1 live code?

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
