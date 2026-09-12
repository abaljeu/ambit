# Reference Paste and Change POST facts

Date: 2026-09-11. Place: `dev` after [[scripts/gitstatus.sh]]. Recon only for Phase 1b Run Agent grill (response write-back same as Reference Paste; Actor Change POST unified with Browser `/ambit/changes`). No product, issue, project, CONTEXT, or prior-report edits.

Terms: Browser, Core, CoreChanges, Actor, Change, Focus, DocumentColdParse, ChildListWire. Forced write-back shape from [[../issues/06-define-command-run-agent-redesign.md]]: delete every Child under Focus; create new Children from the full response.

## 1. Reference Paste end-to-end (external cold paste = subdocument inject)

This is the path that creates new nodes from text (not link-paste). Link-paste reuses existing NodeIds and emits only attach ops; it is a sibling branch, not the subdocument inject used as the write-back reference.

1. Browser event: [[src/Client/Controller.fs]] `onPaste` reads `text/plain` (or HTML strip), optional node-id clipboard format via `getPasteNodeIds`, dispatches `ApplyOp` with `pasteNodesOp`.
2. Client planner: [[src/Client/UpdatePaste.fs]] `pasteNodes` → Selecting `pasteNodesSelecting` or Editing `pasteNodesEditing`. External text calls `planColdPaste` → [[src/Shared/documents/DocumentColdParse.fs]] `planPasteOps`.
3. Subdocument representation (no separate Subdocument type): `planPasteOps` builds a disposable stub File graph (`PasteRelativePath` = `__paste__.txt`), cold-plans via Plain codec (`planApplyCold`), then `peelDocumentRootOps` returns `(topLevelIds, nestedOps)`. Nested ops are ordinary `Op` values (`NewNode` / `Replace` / …) for the peeled subtree. Top-level ids are the roots to attach under the live parent.
4. Attach: [[src/Client/UpdateHelpers.fs]] `childrenForPaste` picks `Ownership.Owner` vs `Ownership.Ref` from existing owners. Selecting uses [[src/Shared/ChildListWire.fs]] `edit` (replace selection span). Editing multiline uses first-line `SetText` plus `insertAt` for remaining tops. Ops order: `pasteOps @ [attachOp]`.
5. Change construction: `newChange` sets `id = model.revision.Value`, `changeId = Guid.NewGuid()`, `ops = …` ([[src/Shared/History.fs]] `Change`).
6. Local apply + queue: `applyAndPost "Paste"` → [[src/Shared/SyncLogic.fs]] `applyLocalChange` (ResidentProjection; records ClientHistory) → [[src/Client/UpdateHelpers.fs]] SyncPlanner enqueue; may emit `SubmitPendingBatch` when the queue was idle.
7. Wire: [[src/Client/App.fs]] `runSubmitPendingBatch` → [[src/Shared/SyncBatch.fs]] `toWireBatch` (rewrites `Change.id` into a contiguous chain from `baseRev`) → POST `/{currentFile}/changes` (Browser path; when pathname is `ambit`, URL is `/ambit/changes`).
8. HTTP: [[src/Server/RouteRegistration.fs]] sole Change write route `MapPost("/ambit/changes", …)` (no singular `/ambit/change`). Auth required; `bindClientHint`; body string → [[src/Server/Api.fs]] `postChange` with `changesBound` = [[src/Server/Core/CoreRuntime.fs]] `browserChanges ()` (Browser credential bound through `bindChanges`).
9. Core merge: `Api.postChange` decodes `ChangeBatch` then `handle.postChange batch.changes`. File/Db agent: empty batch refused; dedup by `changeId`; [[ChangeAmendment]] apply; revision increments; Unchanged rejected; optional disk-effect validation; persist log; returns [[src/Server/Core/CoreChanges.fs]] `CoreChangesAccepted` (`revision`, confirmed `changes`, `externalChanges`, `message`, `isReady`).
10. HTTP response: `ChangeSuccessResponse` ([[src/Shared/ApiResponses.fs]]): revision, build epochs, apiVersion, isReady, externalChanges, changes, message, bootstrapHash (Api sets None). Errors → BadRequest/Unauthorized (auth refuse).
11. Browser ack: decode → `SubmitResponse` → [[src/Client/Update.fs]] reconcile via SyncLogic ack / external-ack; blocked sync states ignore the ack.

Related attach shape (not the paste event path): [[src/Shared/ImportText.fs]] `buildImportChange` packages paste ops then `ChildListWire.replace` of **all** children under a focus id — closer to issue 06 Focus replace-all than Selecting span `edit`.

## 2. Typed Core Changes face (Browser HTTP vs Actor)

| Face | Entry | Credential | Call |
| --- | --- | --- | --- |
| Browser | `POST /ambit/changes` → Api JSON decode → `CoreChanges.postChange` | Lifetime Browser credential via `browserChanges` | Same `CoreChanges` record |
| Actor | In-process `ActorFn` third arg | Job credential via `bindChanges` (tests: `CoreAuth.bindHandle`) | Same `postChange` / `postGraphOnlyChange` |

[[src/Server/Core/CoreChanges.fs]] is already the unified typed interface: `postChange: Change list -> Async<Result<CoreChangesAccepted, string>>` (and `postGraphOnlyChange`). [[src/Server/Core/CoreActorPool.fs]] `ActorFn = Graph -> Credential -> CoreChanges -> Async<unit>`. No Actor HTTP Change route. Unifying Actor with Browser means both post `Change list` through that handle; Browser alone owns JSON/HTTP mapping in Api + RouteRegistration.

## 3. Reuse unchanged vs generalize vs still decide

### Reuse unchanged

- `CoreChanges.postChange` / `CoreChangesAccepted` as the Actor write face (no second merge pipeline).
- Server merge/dedup/amendment/revision rules behind that handle.
- `Change` shape: basis `id` (revision), `changeId` (dedup), `ops`.
- `ChildListWire` full-list Replace helpers; `childrenForPaste` ownership rule when attaching created ids.
- Cold peel pattern: stub root → cold plan → peel top-level ids + nested ops ([[DocumentColdParse.planPasteOps]]).
- Browser route name stays plural `/ambit/changes` for Browser only.

### Conceptually generalizes (same idea, new caller)

- Text → `(topLevelIds * Op list)` via cold Plain peel (or structural codec then Plain fallback per issue 06) then attach under Focus.
- Focus replace-all attach (`ImportText.buildImportChange` / `ChildListWire.replace`) matches “delete every Child under Focus; create new Children” better than Selecting span `edit`.
- Actor posts the resulting `Change list` on its bound `CoreChanges` (same as Browser after decode).

### Phase 1b must still decide (facts do not settle)

- Exact planner module and ownership for response text → Focus-Children ops (reuse `planPasteOps` as-is vs Shared Run-Agent planner that tries structural parse then Plain; who calls codecs).
- Attach op: replace-all under Focus vs other ChildListWire form; whether mark-document-current ops from ImportText apply.
- Who builds `Change.id` / `changeId` on the Actor (launch revision vs live `getRevision`; Guid site).
- `postChange` vs `postGraphOnlyChange` for write-back.
- How Actor treats `CoreChangesAccepted` / Error (no Browser SyncBatch/SubmitResponse path); Browser still learns via Poll/Change message path per issue 06 failure rules.
- Whether any Shared helper is extracted for both Browser Paste and Actor write-back, or Actor only calls existing DocumentColdParse + ChildListWire.

## 4. Source index

| Concern | Primary paths |
| --- | --- |
| Paste event | [[src/Client/Controller.fs]], [[src/Client/UpdatePaste.fs]] |
| Cold subdocument peel | [[src/Shared/documents/DocumentColdParse.fs]] |
| Child attach wire | [[src/Shared/ChildListWire.fs]], [[src/Client/UpdateHelpers.fs]] `childrenForPaste` |
| Replace-all import | [[src/Shared/ImportText.fs]] `buildImportChange` |
| Local apply / wire batch | [[src/Shared/SyncLogic.fs]], [[src/Shared/SyncBatch.fs]], [[src/Client/App.fs]] |
| HTTP route + adapter | [[src/Server/RouteRegistration.fs]], [[src/Server/Api.fs]] |
| Typed Core face | [[src/Server/Core/CoreChanges.fs]], [[src/Server/Core/CoreRuntime.fs]], [[src/Server/Core/CoreActorPool.fs]], [[src/Server/Core/CoreCredentials.fs]] |
| Ack UI | [[src/Client/Update.fs]], [[src/Shared/ApiResponses.fs]] |
