# 125 — Import posts NodeParsed, and the preview leaves

## 1. Type

coding

## 2. Blockers

[ ] [134 — The client door forwards state Notices](134-client-door-forwards-state-notices.md)

## 3. Problem

This is round 1 of [124 — Remove SetDocumentState one site at a time](124-remove-setdocumentstate-one-site-at-a-time.md). Staging has no `Op.SetDocumentState` in `ImportText`. Instead, the `Import:` command-name preview applies `NodeParsed` ahead of the import ops: `importCommand`, `tryImportFocus`, `stateForImportOps`, and `commitImportApply`. That preview is wired through `Api.importParsedNotices`, `FileAgent`, `DbAgent`, `BootCache`, `EventLog`, and `SyncLogic`.

## 4. Change

- `buildImportChange` and `buildDirectoryMergeChange` emit `Notice.NodeParsed root` as a **lead** Notice for an Unparsed document root. Placement is the rule in [135 — Op readers leave](135-op-readers-leave.md).
- The preview leaves: `importCommand`, `tryImportFocus`, `stateForImportOps`, `commitImportApply`, `Api.importParsedNotices`, and their calls.
- There is no op line to delete.

Source: `ImportText.fs`, `Api.fs`, `FileAgent.fs`, `DbAgent.fs`, `BootCache.fs`, `EventLog.fs`, `SyncLogic.fs`.

## 5. Tests

[ ] `ImportTextTests`
[ ] `ImportDocumentTests` (takes the Change from the import list)
[ ] `SpecialNodeTestHelpers`
[ ] `NodeEventExpandTests` `import logs NodeParsed beside SetDocumentState`

## 6. Open question (Alan)

On staging, `ImportText.buildImportChange` and `buildDirectoryMergeChange` have no `src` caller. Only tests call them. Is pass 1 live code?

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
