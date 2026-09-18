# Code review — 12 Contract leftover Change and Revision

Independent review. Not approval. Ticket [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md) stays **Status:** `coded`.

Range: `origin/staging...HEAD` (three-dot). Tip `b5ed59ce`. Base `9630cf23`. Non-empty. 79 files. One commit: `Contract leftover Change and Revision (12)`. [EventBody.Change](src/Shared/History.fs) of Op list is kept. [plan/core-creation/arch.md](plan/core-creation/arch.md) is not in the range.

Mechanical scan (`python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`): FILE [DatabaseProjection.fs](src/Server/DatabaseProjection.fs) 565→569; [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) 578→592; [DatabaseProjectionContractTests.fs](tests/Server.Tests/DatabaseProjectionContractTests.fs) 619→625; [DbAgentTests.fs](tests/Server.Tests/DbAgentTests.fs) 582→597; [LazyLoadReconciliationServerTests.fs](tests/Server.Tests/LazyLoadReconciliationServerTests.fs) 810→841; [StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs) 1252→1315; [TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs) 454→470; [DeleteOpsTests.fs](tests/Shared.Tests/DeleteOpsTests.fs) 611→621; [ImportDocumentTests.fs](tests/Shared.Tests/ImportDocumentTests.fs) 1311→1333; [LazyLoadReconciliationTests.fs](tests/Shared.Tests/LazyLoadReconciliationTests.fs) 773→777; [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs) 632→666; [ViewModelMoveOpsTests.fs](tests/Shared.Tests/ViewModelMoveOpsTests.fs) 524→528; [ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) 2981→2983; [WorkspaceUploadStructureTests.fs](tests/Shared.Tests/WorkspaceUploadStructureTests.fs) 429→431. LONG: [IgnoredDestinationValidationTests.fs](tests/Server.Tests/IgnoredDestinationValidationTests.fs) L156 (146); [LazyLoadReconciliationServerTests.fs](tests/Server.Tests/LazyLoadReconciliationServerTests.fs) L734 (159), L760 (163), L796 (159); [StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs) L967 (112), L1253 (224); [ClientHistoryTests.fs](tests/Shared.Tests/ClientHistoryTests.fs) L103 (132), L198 (104); [DeleteOpsTests.fs](tests/Shared.Tests/DeleteOpsTests.fs) L64 (162), L93 (162), L115 (162), L136 (162), L179 (162), L276 (162), L306 (162), L334 (162), L403 (157), L447 (161), L469 (162), L575 (162), L612 (162); [HistoryTests.fs](tests/Shared.Tests/HistoryTests.fs) L1022 (109); [ImportDocumentTests.fs](tests/Shared.Tests/ImportDocumentTests.fs) L941 (158), L1038 (158), L1217 (146), L1275 (150), L1326 (150); [WorkspaceOpsTests.fs](tests/Shared.Tests/WorkspaceOpsTests.fs) L347 (165), L362 (165). No changed binding over 40 lines.

Alan locks applied: DELETE leftover record `type Change`, `module Change`, `Ev.ofChange` / `Ev.asChange`, unused [EventId.fs](src/Shared/EventId.fs), `type Revision`, `ofRevision` / `toRevision`. KEEP `EventBody.Change of Op list` and names that mean that body or HTTP types. Do not edit [plan/core-creation/arch.md](plan/core-creation/arch.md). EventId: only [EventLog.fs](src/Shared/EventLog.fs) uses `next`; carry EventId in-process; peel only at wire/SQL. Change→Ev rename locals/APIs unless they mean EventBody.Change. Labeled links `[label](path)`. No GitHub PR for this report.

## Standards

Range `origin/staging...HEAD` is not empty (79 files). Tip `b5ed59ce`. Base `origin/staging` `9630cf23`. Mechanical-scan lines are documented-standard hits.

**Hard documented violations**

File size ([fsharp-source.md](.agents/rules/fsharp-source.md): 400 lines; do not grow a file already over 400). Already-over-400 files grew: [DatabaseProjection.fs](src/Server/DatabaseProjection.fs) 565→569; [CoreMailboxDoorTests.fs](tests/Server.Tests/CoreMailboxDoorTests.fs) 578→592; [DatabaseProjectionContractTests.fs](tests/Server.Tests/DatabaseProjectionContractTests.fs) 619→625; [DbAgentTests.fs](tests/Server.Tests/DbAgentTests.fs) 582→597; [LazyLoadReconciliationServerTests.fs](tests/Server.Tests/LazyLoadReconciliationServerTests.fs) 810→841; [StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs) 1252→1315; [TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs) 454→470; [DeleteOpsTests.fs](tests/Shared.Tests/DeleteOpsTests.fs) 611→621; [ImportDocumentTests.fs](tests/Shared.Tests/ImportDocumentTests.fs) 1311→1333; [LazyLoadReconciliationTests.fs](tests/Shared.Tests/LazyLoadReconciliationTests.fs) 773→777; [SyncLogicTests.fs](tests/Shared.Tests/SyncLogicTests.fs) 632→666; [ViewModelMoveOpsTests.fs](tests/Shared.Tests/ViewModelMoveOpsTests.fs) 524→528; [ViewModelTests.fs](tests/Shared.Tests/ViewModelTests.fs) 2981→2983; [WorkspaceUploadStructureTests.fs](tests/Shared.Tests/WorkspaceUploadStructureTests.fs) 429→431.

Long lines (same rule, 100 chars; added hunks only): [IgnoredDestinationValidationTests.fs](tests/Server.Tests/IgnoredDestinationValidationTests.fs) L156 (146); [LazyLoadReconciliationServerTests.fs](tests/Server.Tests/LazyLoadReconciliationServerTests.fs) L734 (159), L760 (163), L796 (159); [StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs) L967 (112), L1253 (224); [ClientHistoryTests.fs](tests/Shared.Tests/ClientHistoryTests.fs) L103 (132), L198 (104); [DeleteOpsTests.fs](tests/Shared.Tests/DeleteOpsTests.fs) L64 (162), L93 (162), L115 (162), L136 (162), L179 (162), L276 (162), L306 (162), L334 (162), L403 (157), L447 (161), L469 (162), L575 (162), L612 (162); [HistoryTests.fs](tests/Shared.Tests/HistoryTests.fs) L1022 (109); [ImportDocumentTests.fs](tests/Shared.Tests/ImportDocumentTests.fs) L941 (158), L1038 (158), L1217 (146), L1275 (150), L1326 (150); [WorkspaceOpsTests.fs](tests/Shared.Tests/WorkspaceOpsTests.fs) L347 (165), L362 (165). Typical added Ev literal: `let change = { id = EventId.fromJson 0; submissionId = System.Guid.NewGuid(); authority = Authority "Browser"; commandName = ""; body = EventBody.Change ops }`. No changed binding over 40 lines. Surgical under-100-line preference is not a fail ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)).

EventId serial ([core-api.md](.agents/rules/core-api.md) EventId serial; Alan lock: only EventLog uses `EventId.next`; carry EventId in-process; peel only at wire/SQL). Production `EventId.next` is only [EventLog.fs](src/Shared/EventLog.fs). Production `fromJson` / `toJson` stay on serialize / wire / file peel ([History.fs](src/Shared/History.fs), [EventJson.fs](src/Shared/EventJson.fs), [ApiResponseSerialization.fs](src/Shared/ApiResponseSerialization.fs), [Bookkeeping.fs](src/Server/Bookkeeping.fs), [Database.fs](src/Server/Database.fs), [BootCacheStore.fs](src/Client/BootCacheStore.fs), [Api.fs](src/Server/Api.fs)). In-process test fixtures mint with `EventId.fromJson 0` (not serialize/wire/SQL). Same files as the long Ev literals. [TestBackend.fs](tests/Server.Tests/TestBackend.fs) `wireEvent` already uses `EventId.zero`.

Change→Ev names ([fsharp-source.md](.agents/rules/fsharp-source.md) rename locals to `event`/`events`; Alan lock: KEEP names that mean EventBody.Change or HTTP types): production `events` rename in [DatabaseProjection.fs](src/Server/DatabaseProjection.fs) and [CoreCredentials.fs](src/Server/Core/CoreCredentials.fs) matches. `buildImportChange` / `mintChange` mean EventBody.Change — KEEP.

Markdown / planning: no consecutive-blank or bare-id hits ([markdown-writing.md](.agents/rules/markdown-writing.md), [refer-by-name.md](.agents/rules/refer-by-name.md)). Ticket and [project.md](plan/single-event-source/project.md) use `[label](path)`. [arch.md](plan/single-event-source/arch.md) keeps allowed `[[path]]` bare refs. Stage stays `build`; ticket Status `coded`. [plan/core-creation/arch.md](plan/core-creation/arch.md) is untouched.

**Judgement smells (not hard)**

Mysterious Name: after `type Revision` delete, [Bookkeeping.fs](src/Server/Bookkeeping.fs) still has `readRevision : EventId` and `writeRevision (rev: int)`; [SavePrep.fs](src/Server/SavePrep.fs) still has `getFileRevision: unit -> Async<EventId>`. Change→Ev lock does not force this rename.

Duplicated Code: [SpecialNodeTestHelpers.fs](tests/Shared.Tests/SpecialNodeTestHelpers.fs) adds `changeEvent` / `changeEventZero` / `eventOps` / `applyChange`; [TestBackend.fs](tests/Server.Tests/TestBackend.fs) copies the same four; call sites still inline the long Ev record instead of `changeEventZero`.

Shotgun Surgery across 79 files is the contract delete of leftover Change/Revision; suppress (ticket wins).

## Spec

Range `origin/staging...HEAD` is non-empty (79 files, tip `b5ed59ce`). Spec is [12 — Contract leftover Change and Revision](plan/single-event-source/issues/12-contract-leftover-change-and-revision.md). I did not run CI.

**(a) Missing or partial**

None against What to build.

- “Delete leftover Change record, `module Change` apply wrapping, `Ev.ofChange` / `Ev.asChange`, `eventFromChange`.” Gone from [History.fs](src/Shared/History.fs). No leftover `{ id; submissionId; ops }` record, `module Change`, `type Change`, `Ev.ofChange`, `Ev.asChange`, or `eventFromChange` in `src/` or `tests/`.
- “Delete unused [EventId.fs](src/Shared/EventId.fs).” Present on `origin/staging`, deleted on HEAD; no compile item in [Gambol.Shared.fsproj](src/Shared/Gambol.Shared.fsproj).
- “Delete `type Revision` and `EventId.ofRevision` / `toRevision`.” `type Revision` removed from [Model.fs](src/Shared/Model.fs); converters removed from [History.fs](src/Shared/History.fs); leftover Change/Revision codecs removed from [Serialization.fs](src/Shared/Serialization.fs).

Live `EventId` stays in [History.fs](src/Shared/History.fs). Leftover *names* (`readRevision`, `getFileRevision`, test `decodeRevision`, HTTP `postChange` helpers) are not `type Revision` or leftover Change. KEEP covers EventBody.Change / HTTP names.

**(b) Scope creep**

No edit of the forbidden file.

- “Do not edit [plan/core-creation/arch.md](plan/core-creation/arch.md) on this ticket.” Not in the range.
- [single-event-source arch.md](plan/single-event-source/arch.md) only checks Contract 1–3 leftover boxes for this ticket.
- Src/test rewrites (PersistHandlers wrappers, ImportText Ev mint, fixture helpers in [SpecialNodeTestHelpers.fs](tests/Shared.Tests/SpecialNodeTestHelpers.fs), drop leftover [OpListApplyTests.fs](tests/Shared.Tests/OpListApplyTests.fs) cases) are required so “No caller should remain.”

**(c) Looks implemented, looks wrong**

None.

- “KEEP EventBody.Change of Op list”: still `| Change of ops: Op list` in [History.fs](src/Shared/History.fs); EventJson `"change"` body kept.

## Summary

Standards: 44 hard findings (14 FILE growth, 29 LONG lines, EventId.fromJson 0 in added Ev fixtures), 3 judgement smells (Mysterious Name, Duplicated Code, Shotgun Surgery); worst is FILE growth of already-over-400 files, led by [StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs) 1252→1315 and L1253 (224).

Spec: 0 findings (deletes complete; EventBody.Change kept; no [core-creation arch](plan/core-creation/arch.md) edit) plus CI not run; no worst Spec issue.
