# 125 — Import drops its SetDocumentState

## 1. Type

coding

## 2. Blockers

[ ] [135 — Backend infers document-state Notices from ops](135-backend-infers-document-state-notices-from-ops.md)

## 3. Problem

This is round 1 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). Importing into an Unparsed document root moves that root `Unparsed → Current` before the import ops, so `History.blockedForApply` lets them through. On staging there is no op. The `Import:` command-name preview does the job instead: `ImportText.importCommand`, `tryImportFocus`, `stateForImportOps`, `commitImportApply`, and `Api.importParsedNotices`, wired through `FileAgent`, `DbAgent`, `BootCache`, `EventLog`, and `SyncLogic`.

After a rollback to `47a85e1d^`: the preview is gone (8c7a1b3c), and the op is back as `ImportText.markDocumentCurrentBeforeParse`'s `Op.SetDocumentState(focusId, Unparsed, Current)`.

## 4. Change

- Delete the op line in `markDocumentCurrentBeforeParse`, or on staging delete the preview functions and their calls.
- The import Change carries only the paste ops and the `Replace` on the root.

Backend inference: an Edit Change whose `Replace` attaches owned children to an Unparsed document root infers a lead `NodeParsed root`. The rule goes in `DocumentOpImpact.inferNotices` and is applied by the 135 hook in `ChangeAmendment.applyPosted`.

Hard: those ops are the same as an ordinary edit under an Unparsed root, which `blockedForApply` exists to refuse. The ops alone do not say "import". Only the `Import:` command name did that.

Source: `ImportText.fs`, `DocumentOpImpact.fs`, plus on staging `Api.fs`, `FileAgent.fs`, `DbAgent.fs`, `BootCache.fs`, `EventLog.fs`, `SyncLogic.fs`.

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
