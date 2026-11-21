# 124 — Remove SetDocumentState one site at a time scope

## Slice

[ ] [124 — Remove SetDocumentState one site at a time](plan/core-refinement/issues/124-remove-setdocumentstate-one-site-at-a-time.md) removes each producer of `Op.SetDocumentState` in one pass after an equivalent `Notice` already posts through the Mailbox.
[ ] The `Op` union case `SetDocumentState` stays until [45 — SetDocumentState leaves; NodeUnpersisted replaces it](plan/core-refinement/issues/45-remove-document-state-set-ops.md).
[ ] This ticket does not remove match arms that only classify, apply, invert, or serialize the op.
[ ] [122 — Document state moves by Notice](plan/core-refinement/issues/122-document-state-moves-by-notice.md) blocks every pass: the site must already post its equivalent `Notice` before the op is removed.
[ ] [45 — SetDocumentState leaves; NodeUnpersisted replaces it](plan/core-refinement/issues/45-remove-document-state-set-ops.md) is the later contract that deletes the union case; [124 — Remove SetDocumentState one site at a time](plan/core-refinement/issues/124-remove-setdocumentstate-one-site-at-a-time.md) does not delete the case.
[ ] Order: one producer group per pass, tests for that group move to the `Notice` in the same pass, the `Op` case remains.

## Signatures

[ ] `Op.SetDocumentState` in `src/Shared/History.fs`: `SetDocumentState of nodeId: NodeId * oldState: DocumentStateEnum * newState: DocumentStateEnum`.
[ ] `DocumentStateEnum` in `src/Shared/Model.fs`: `Current` | `Unparsed` | `PullingFile` (`Current` is the legacy name for Parsed on disk).
[ ] `Notice` document-state cases in `src/Shared/History.fs`: `NodeUploading`, `NodeReceived`, `MarkUnparsed`, `NodeParsed`, plus non-enum cases `NodeParsing`, `NodePersisting`, `NodePersisted`, `PersistFailed`.
[ ] `Ev.documentStateFromNotice` in `src/Shared/History.fs` maps `NodeUploading` → `PullingFile`, `NodeReceived` / `MarkUnparsed` → `Unparsed`, `NodeParsed` → `Current`.
[ ] `DocumentOpImpact.noticesBeside` in `src/Shared/DocumentOpImpact.fs`: `Op list` → `Notice list` beside a Change.
[ ] `CoreMailboxApi.postNotice` in `src/Server/Core/CoreMailboxApi.fs`: `MailboxHost` → `Notice` → unit.
[ ] `Api.inferredNodeEvents` in `src/Server/Api.fs` calls `DocumentOpImpact.noticesBeside` on `EventBody.Change` ops after `CoreDoor.postEvents`.
[ ] `FileAgent.postGraphOnlyNotices` in `src/Server/Core/FileAgent.fs` calls `DocumentOpImpact.noticesBeside` on graph-only Changes when `persistFinished` is bound.
[ ] `LazyLoadReconciliationServer.postParsedFromPulling` in `src/Server/LazyLoadReconciliationServer.fs` posts `Notice.NodeParsed` for each `PullingFile` → `Current` op in a posted reconcile batch (`noticesBeside` has no case for that pair).
[ ] `LazyLoadReconciliationServer.reconcileReceivedFiles` in `src/Server/LazyLoadReconciliationServer.fs` posts `Notice.NodeReceived` per received file beside the git-pull op list.

## Match sites

### Producers (construct `Op.SetDocumentState`)

[ ] **1 — markDocumentCurrentBeforeParse** — `src/Shared/ImportText.fs`, `ImportText.markDocumentCurrentBeforeParse`, line 47: `Unparsed` → `Current`.
[ ] **2 — planParseFile markCurrent** — `src/Shared/dotnet/ImportDocument.fs`, `ImportDocument.planParseFile`, lines 130–133: `Unparsed` → `Current` when the File is Unparsed before parse ops.
[ ] **3 — markCurrent directory reconcile** — `src/Shared/dotnet/DirectoryReconcile.fs`, `DirectoryReconcile.markCurrent`, line 226: `Unparsed` → `Current` on Directory or Workspace.
[ ] **4 — markUnparsed upload stubs** — `src/Shared/WorkspaceUploadStructure.fs`, `WorkspaceUploadStructure.markUnparsed`, lines 143–145: `Current` → `Unparsed` or `PullingFile` → `Unparsed`.
[ ] **5 — markPullingFile upload stubs** — `src/Shared/WorkspaceUploadStructure.fs`, `WorkspaceUploadStructure.markPullingFile`, lines 151–153: `Current` → `PullingFile` or `Unparsed` → `PullingFile`.
[ ] **6 — markParentCurrent upload stubs** — `src/Shared/WorkspaceUploadStructure.fs`, `WorkspaceUploadStructure.markParentCurrent`, line 160: `Unparsed` → `Current` on Directory or Workspace parent.
[ ] **7 — planServerFilePresentOps** — `src/Shared/WorkspaceUploadStructure.fs`, `WorkspaceUploadStructure.planServerFilePresentOps`, line 319: `PullingFile` → `Unparsed` on an existing File stub.
[ ] **8 — markUnparsed lazy-load stubs** — `src/Shared/dotnet/LazyLoadReconciliationApply.fs`, `LazyLoadReconciliationApply.markUnparsed`, lines 23–25: same transitions as **4 — markUnparsed upload stubs**.
[ ] **9 — markParentCurrent lazy-load stubs** — `src/Shared/dotnet/LazyLoadReconciliationApply.fs`, `LazyLoadReconciliationApply.markParentCurrent`, line 32: same as **6 — markParentCurrent upload stubs**.
[ ] **10 — parseDirInfoIfPresent markCurrent** — `src/Shared/dotnet/LazyLoadReconciliationApply.fs`, `LazyLoadReconciliationApply.parseDirInfoIfPresent`, lines 145–150: `Unparsed` → `Current` or `PullingFile` → `Current` before directory artifact parse ops.
[ ] **11 — unparsedStateOp git directory marks** — `src/Shared/dotnet/LazyLoadReconciliation.fs`, `LazyLoadReconciliation.unparsedStateOp`, lines 370–373: current enum → `Unparsed` on containing Directory or Workspace; consumed from `LazyLoadReconciliation.containingDirectoryMarkOps`.

### Orchestrators (no direct constructor; compose producer helpers)

[ ] **12 — markNewStubsUnparsed** — `src/Shared/WorkspaceUploadStructure.fs`, `WorkspaceUploadStructure.markNewStubsUnparsed`: calls **4**, **5**, **6** after `planStubOps` creates stubs.
[ ] **13 — markAddedDocumentsUnparsed** — `src/Shared/dotnet/LazyLoadReconciliationApply.fs`, `LazyLoadReconciliationApply.markAddedDocumentsUnparsed`: calls **8** and **9** on added reconcile stubs.
[ ] **14 — planStubOps** — `src/Shared/WorkspaceUploadStructure.fs`, `WorkspaceUploadStructure.planStubOps`: appends **12** state ops to create ops.
[ ] **15 — planReceivedFiles** — `src/Shared/dotnet/LazyLoadReconciliationReport.fs`, `LazyLoadReconciliationReport.planReceivedFiles`: appends **11** directory marks and file unparsed ops from `renamedFileUnparsedOps` (**8**).

### Classify, apply, invert, serialize (out of scope for removal in this ticket)

[ ] **16 — Op.apply** — `src/Shared/History.fs`, `Op.apply` / `applyAllowed`, line 248: applies `SetDocumentState` to the graph.
[ ] **17 — Op.invert** — `src/Shared/History.fs`, `Op.invert`, lines 332–333: builds swapped `SetDocumentState` for undo inversion.
[ ] **18 — Op.undo** — `src/Shared/History.fs`, `undoAllowed`, line 372: applies inverted document state on undo.
[ ] **19 — DocumentOpImpact** — `src/Shared/DocumentOpImpact.fs`: `documentStateTransition`, `noticesBeside`, `opAnchor`, `touchedNodeIds` match the op.
[ ] **20 — Serialization** — `src/Shared/Serialization.fs`, encode line 358 and decode line 412: JSON for the op.
[ ] **21 — ResidentProjection** — `src/Shared/ResidentProjection.fs`, `applyOpAllowing`, line 20: delegates apply when the node is resident.
[ ] **22 — DatabaseProjection** — `src/Server/DatabaseProjection.fs`, line 95: SQL projection match on the op.
[ ] **23 — DocumentColdParse** — `src/Shared/documents/DocumentColdParse.fs`, line 294: classifies root `SetDocumentState` during cold parse planning.
[ ] **24 — LazyLoadReconciliationServer classify** — `src/Server/LazyLoadReconciliationServer.fs`, `parsedFromPulling` lines 273–275 and `receivedFileIds` / `planMarksUnparsed` line 382: read ops without constructing them.

### Callers of producer orchestrators

[ ] **25 — buildImportChange** — `src/Shared/ImportText.fs`, `ImportText.buildImportChange` / `buildDirectoryMergeChange`: includes **1**.
[ ] **26 — Api.postEvents** — `src/Server/Api.fs`: runs Changes that carry upload, import, and arrival ops; posts `inferredNodeEvents`.
[ ] **27 — WorkspaceCloudUpload** — `src/Shared/dotnet/WorkspaceCloudUpload.fs`: calls **14** and **7**.
[ ] **28 — UpdateWorkspaceSync** — `src/Client/UpdateWorkspaceSync.fs`: calls **14** and **7** on the client graph.
[ ] **29 — ParseThread** — `src/Server/ParseThread.fs`: runs **2** and **3** via `DocumentPersistWrite.planParseFile` / `DirectoryReconcile.planDirectoryReconcile`; `finishParse` posts `Notice.NodeParsed`.
[ ] **30 — DocumentPersistWrite.planParseFile** — `src/Server/DocumentPersistWrite.fs`: delegates to **2**.
[ ] **31 — LazyLoadReconciliationServer postReconcileReport** — `src/Server/LazyLoadReconciliationServer.fs`: graph-only post plus `beside` callback and **postParsedFromPulling** for **10** `PullingFile` → `Current`.

## Notice beside each producer transition

[ ] **1 — markDocumentCurrentBeforeParse** (`Unparsed` → `Current`): `Notice.NodeParsed` via `DocumentOpImpact.noticesBeside` when the import Change posts through `Api.postEvents` or graph-only `FileAgent.postGraphOnlyNotices` and the same op list has no upload-or-arrival pair.
[ ] **2 — planParseFile markCurrent** (`Unparsed` → `Current`): `Notice.NodeParsed` via `ParseThread.finishParse` / `CoreMailboxApi.postNotice` after parse ops post (not from `noticesBeside` on the parse op list alone).
[ ] **3 — markCurrent directory reconcile** (`Unparsed` → `Current`): `Notice.NodeParsed` via `ParseThread.finishParse` after directory reconcile posts.
[ ] **4 — markUnparsed upload stubs** (`Current` or `PullingFile` → `Unparsed`): `Notice.MarkUnparsed` when old state is not `PullingFile`; `Notice.NodeReceived` when old state is `PullingFile` (`noticesBeside`).
[ ] **5 — markPullingFile upload stubs** (→ `PullingFile`): `Notice.NodeUploading` (`noticesBeside`).
[ ] **6 — markParentCurrent upload stubs** (`Unparsed` → `Current`): `Notice.NodeParsed` (`noticesBeside`) only when the same Change has no `PullingFile` or `Unparsed` target in any `SetDocumentState` in that list.
[ ] **7 — planServerFilePresentOps** (`PullingFile` → `Unparsed`): `Notice.NodeReceived` (`noticesBeside` via `Api.postEvents` or `FileAgent.postGraphOnlyNotices`).
[ ] **8 — markUnparsed lazy-load stubs**: same Notices as **4 — markUnparsed upload stubs** when the reconcile batch posts graph-only through `FileAgent.postGraphOnlyNotices`.
[ ] **9 — markParentCurrent lazy-load stubs**: same rule as **6** for `noticesBeside`; often bundled with **8** in **13 — markAddedDocumentsUnparsed**.
[ ] **10 — parseDirInfoIfPresent markCurrent** (`Unparsed` → `Current`): `Notice.NodeParsed` via `noticesBeside` when posted alone; `PullingFile` → `Current` uses `LazyLoadReconciliationServer.postParsedFromPulling` explicit `Notice.NodeParsed`.
[ ] **11 — unparsedStateOp** (→ `Unparsed` on container): `Notice.MarkUnparsed` when old state is not `PullingFile`; git-pull file marks use **8** with `Notice.NodeReceived` beside `reconcileReceivedFiles` explicit posts.

## Blocked producer groups (no equivalent Mailbox Notice yet for every op in the batch)

[ ] **6 — markParentCurrent upload stubs** in **12 — markNewStubsUnparsed** / **14 — planStubOps**: same Change includes **5 — markPullingFile** ops, so `DocumentOpImpact.noticesBeside` sets `uploadOrArrival` and omits `Notice.NodeParsed` for the parent `Unparsed` → `Current` op.
[ ] **9 — markParentCurrent lazy-load stubs** in **13 — markAddedDocumentsUnparsed**: same batch as **8 — markUnparsed lazy-load stubs** / **5 — markPullingFile**-equivalent stub marks, so `Notice.NodeParsed` for the parent is not inferred beside that op list until an explicit post is added.

## Existing tests

[ ] **`build import marks unparsed file current before tree operations`** — `tests/Shared.Tests/ImportTextTests.fs`: asserts **1** `SetDocumentState(focusId, Unparsed, Current)` is the first op in `buildImportChange`.
[ ] **`import logs NodeParsed beside SetDocumentState`** — `tests/Server.Tests/NodeEventExpandTests.fs`: import Change posts `Notice.NodeParsed` beside **1**.
[ ] **`planParseFile blank input marks Unparsed file Current`** — `tests/Shared.Tests/ImportDocumentTests.fs`: **2** emits `Unparsed` → `Current`.
[ ] **`planParseFile unparsed marks Current`** — `tests/Shared.Tests/ImportDocumentTests.fs`: **2** on Unparsed File.
[ ] **`planParseFile Unparsed with prior children warms and keeps line ids`** — `tests/Shared.Tests/ImportDocumentTests.fs`: **2** in op list.
[ ] **`planParseFile logs NodeParsed beside SetDocumentState`** — `tests/Server.Tests/NodeEventExpandTests.fs`: **2** path with `ParseThread.finishParse` posting `Notice.NodeParsed`.
[ ] **`planDirectoryReconcile logs NodeParsed beside SetDocumentState`** — `tests/Server.Tests/NodeEventExpandTests.fs`: **3** via parse thread on Directory.
[ ] **`markPullingFile sets PullingFile`** — `tests/Shared.Tests/WorkspaceUploadStructureTests.fs`: **5** on File stub; parent **6** `Unparsed` → `Current`.
[ ] **`new file stubs start PullingFile; directory parent stays Current`** — `tests/Shared.Tests/WorkspaceUploadStructureTests.fs`: **14** / **12** combined **4**, **5**, **6**.
[ ] **`upload posts NodeUploading beside SetDocumentState`** — `tests/Server.Tests/NodeEventExpandTests.fs`: **5** and manual **5** transitions beside `Notice.NodeUploading`.
[ ] **`upload emits NodeUploading`** — `tests/Server.Tests/NodeEventExpandTests.fs`: **14** upload Change applies PullingFile via op and `Notice.NodeUploading`.
[ ] **`incoming file posts NodeReceived beside SetDocumentState`** — `tests/Server.Tests/NodeEventExpandTests.fs`: **7** beside `Notice.NodeReceived`.
[ ] **`incoming file emits NodeReceived`** — `tests/Server.Tests/NodeEventExpandTests.fs`: **7** persist and parse effects via `Notice.NodeReceived`.
[ ] **`git changed file posts NodeReceived beside SetDocumentState`** — `tests/Server.Tests/NodeEventExpandTests.fs`: git pull ops plus explicit `Notice.NodeReceived` in **31**.
[ ] **`reconcile emits NodeReceived`** — `tests/Server.Tests/NodeEventExpandTests.fs`: received-file reconcile without asserting the Set op.
[ ] **`received file add marks the containing directory Unparsed`** — `tests/Shared.Tests/LazyLoadReconciliationTests.fs`: **11** directory mark ops.
[ ] **`git pull add marks the containing directory Unparsed`** — `tests/Server.Tests/LazyLoadReconciliationServerTests.fs`: **15** / **11** on server pull.
[ ] **`SetDocumentState writes parseState and pullingFile together`** — `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`: direct op apply for enum pairs (stays until **45** removes the case).
[ ] **`SetDocumentState includes the containing parent and filters unwritable root`** — `tests/Shared.Tests/DocumentOpImpactTests.fs`: **5** `Current` → `PullingFile` impact set (not a Notice test).
[ ] **`Op.SetDocumentState round-trip`** — `tests/Shared.Tests/SerializationTests.fs`: codec for the op case (stays in **124**).
[ ] **`NodeUploading applies PullingFile`** — `tests/Server.Tests/CoreLoopNoticeTests.fs`: Notice write parity for **5**.
[ ] **`NodeReceived applies Unparsed`** — `tests/Server.Tests/CoreLoopNoticeTests.fs`: Notice write parity for **7** / **4** arrival leg.
[ ] **`MarkUnparsed sets Unparsed and leaves PersistState`** — `tests/Server.Tests/CoreLoopNoticeTests.fs`: Notice write parity for **4** / **11**.
[ ] **`NodeParsed pushes a persist request`** — `tests/Server.Tests/CoreLoopNoticeTests.fs`: Notice write parity for **1** / **2** / **3** / **10** Parsed leg.

## Copy-from

[ ] **`DocumentOpImpact.noticesBeside`** — `src/Shared/DocumentOpImpact.fs`, `DocumentOpImpact.noticesBeside`: infers `NodeUploading`, `NodeReceived`, `MarkUnparsed`, and `NodeParsed` beside a Change op list.
[ ] **`Ev.documentStateFromNotice`** — `src/Shared/History.fs`, `Ev.documentStateFromNotice`: enum each document-state Notice writes (used by `ResidentProjection.applyDocumentNotice` and Core apply).
[ ] **`Api.inferredNodeEvents`** — `src/Server/Api.fs`, `Api.inferredNodeEvents`: posts `noticesBeside` after `postEvents`.
[ ] **`FileAgent.postGraphOnlyNotices`** — `src/Server/Core/FileAgent.fs`, `FileAgent.postGraphOnlyNotices`: same beside helper after graph-only Changes.
[ ] **`LazyLoadReconciliationServer.postParsedFromPulling`** — `src/Server/LazyLoadReconciliationServer.fs`: explicit `Notice.NodeParsed` for `PullingFile` → `Current` when `noticesBeside` is silent.
[ ] **`LazyLoadReconciliationServer.reconcileReceivedFiles` beside callback** — `src/Server/LazyLoadReconciliationServer.fs`: explicit `Notice.NodeReceived` per received file id.
[ ] **`ParseThread.finishParse`** — `src/Server/ParseThread.fs`: posts `Notice.NodeParsed` after **2** and **3** parse work (production and tests).

## write-ticket

### Producer groups (one pass each)

[ ] **Pass A — markDocumentCurrentBeforeParse** — function **1**; ops `Unparsed` → `Current`; replace with existing `Notice.NodeParsed` beside import Change; tests **`build import marks unparsed file current before tree operations`**, **`import logs NodeParsed beside SetDocumentState`**.
[ ] **Pass B — planParseFile markCurrent** — function **2**; ops `Unparsed` → `Current`; replace with `ParseThread.finishParse` `Notice.NodeParsed` (align with [54 — Parse and import stop the Parsed Set](plan/core-refinement/issues/54-parse-and-import-stop-the-parsed-set.md)); tests **`planParseFile blank input marks Unparsed file Current`**, **`planParseFile unparsed marks Current`**, **`planParseFile Unparsed with prior children warms and keeps line ids`**, **`planParseFile logs NodeParsed beside SetDocumentState`**.
[ ] **Pass C — markCurrent directory reconcile** — function **3**; ops `Unparsed` → `Current`; replace with `ParseThread.finishParse` `Notice.NodeParsed`; tests **`planDirectoryReconcile logs NodeParsed beside SetDocumentState`** and Directory reconcile tests that expect **3** in op lists.
[ ] **Pass D — planServerFilePresentOps** — function **7**; ops `PullingFile` → `Unparsed`; replace with `Notice.NodeReceived`; tests **`incoming file posts NodeReceived beside SetDocumentState`**, **`incoming file emits NodeReceived`**.
[ ] **Pass E — markPullingFile upload stubs** — function **5**; ops → `PullingFile`; replace with `Notice.NodeUploading`; tests **`markPullingFile sets PullingFile`**, **`upload posts NodeUploading beside SetDocumentState`**, **`upload emits NodeUploading`**.
[ ] **Pass F — markUnparsed upload stubs** — function **4**; ops → `Unparsed`; replace with `Notice.MarkUnparsed` or `Notice.NodeReceived`; tests that assert **4** in **`WorkspaceUploadStructureTests.fs`** and **`SpecialNodeStateAxesTests.fs`** where upload marks unparsed.
[ ] **Pass G — markParentCurrent upload stubs** — function **6**; blocked until **6 — markParentCurrent upload stubs** batch posts explicit `Notice.NodeParsed` for each parent; then remove **6** only; tests **`new file stubs start PullingFile; directory parent stays Current`**, **`reused Unparsed directory with owned child becomes Current`**.
[ ] **Pass H — markUnparsed lazy-load stubs** — function **8**; same Notices as **Pass F**; tests **`LazyLoadReconciliationTests.fs`** / server pull tests that apply **8** via reconcile plans.
[ ] **Pass I — markParentCurrent lazy-load stubs** — function **9**; blocked like **Pass G**; then tests on added stub parent promotion in **`LazyLoadReconciliationTests.fs`**.
[ ] **Pass J — parseDirInfoIfPresent markCurrent** — function **10**; `Unparsed` → `Current` via `Notice.NodeParsed`; `PullingFile` → `Current` via **postParsedFromPulling**; tests **`LazyLoadReconciliationTests.fs`** Added-path directory parse cases.
[ ] **Pass K — unparsedStateOp / containingDirectoryMarkOps** — function **11**; ops → `Unparsed` on containers; replace with `Notice.MarkUnparsed`; tests **`received file add marks the containing directory Unparsed`**, **`git pull add marks the containing directory Unparsed`**, related **`LazyLoadReconciliationTests.fs`** / **`GithubTransportLoadPullTests.fs`** cases.

[ ] Full producer removal touches **6** source files: `ImportText.fs`, `ImportDocument.fs`, `DirectoryReconcile.fs`, `WorkspaceUploadStructure.fs`, `LazyLoadReconciliationApply.fs`, `LazyLoadReconciliation.fs`.
[ ] **13** src files still match `SetDocumentState` for apply, invert, serialize, and classify when the union case remains; **124 — Remove SetDocumentState one site at a time** removes only producers in the **6** planner files above.
[ ] Each pass stays inside one producer function (or one private helper **1**–**11**); **12**–**15** shrink automatically when their callees are gone.

### Source files this slice would change (all passes)

[ ] `src/Shared/ImportText.fs`
[ ] `src/Shared/dotnet/ImportDocument.fs`
[ ] `src/Shared/dotnet/DirectoryReconcile.fs`
[ ] `src/Shared/WorkspaceUploadStructure.fs`
[ ] `src/Shared/dotnet/LazyLoadReconciliationApply.fs`
[ ] `src/Shared/dotnet/LazyLoadReconciliation.fs`
[ ] `src/Server/LazyLoadReconciliationServer.fs` when a pass removes **10** `PullingFile` → `Current` ops and relies only on **postParsedFromPulling** (already present).

### Tests this slice would add or change (by pass)

[ ] **Pass A** — `tests/Shared.Tests/ImportTextTests.fs`, `tests/Server.Tests/NodeEventExpandTests.fs` (`import logs NodeParsed beside SetDocumentState`).
[ ] **Pass B** — `tests/Shared.Tests/ImportDocumentTests.fs` (named `planParseFile` tests above), `tests/Server.Tests/NodeEventExpandTests.fs` (`planParseFile logs NodeParsed beside SetDocumentState`), possibly `tests/Server.Tests/ParseThreadLoadTests.fs` if op lists are asserted.
[ ] **Pass C** — `tests/Server.Tests/NodeEventExpandTests.fs` (`planDirectoryReconcile logs NodeParsed beside SetDocumentState`), `tests/Shared.Tests/DirectoryReconcileTests.fs` where **3** is expected in ops.
[ ] **Pass D** — `tests/Server.Tests/NodeEventExpandTests.fs` (incoming file pair), `tests/Shared.Tests/WorkspaceUploadStructureTests.fs` if **7** is asserted directly.
[ ] **Pass E** — `tests/Shared.Tests/WorkspaceUploadStructureTests.fs`, `tests/Server.Tests/NodeEventExpandTests.fs` (upload pair).
[ ] **Pass F** — `tests/Shared.Tests/SpecialNodeStateAxesTests.fs`, `tests/Shared.Tests/WorkspaceUploadStructureTests.fs`.
[ ] **Pass G** — `tests/Shared.Tests/WorkspaceUploadStructureTests.fs` (parent Current cases); add or extend Notice assertions before removing **6**.
[ ] **Pass H–K** — `tests/Shared.Tests/LazyLoadReconciliationTests.fs`, `tests/Server.Tests/LazyLoadReconciliationServerTests.fs`, `tests/Server.Tests/GithubTransportLoadPullTests.fs`, `tests/Server.Tests/NodeEventExpandTests.fs` (git received cases).

[ ] `--filter` examples per pass: `build import marks unparsed file current before tree operations`; `import logs NodeParsed beside SetDocumentState`; `planParseFile unparsed marks Current`; `incoming file posts NodeReceived beside SetDocumentState`; `upload posts NodeUploading beside SetDocumentState`; `received file add marks the containing directory Unparsed`.
