# 131 — Reconcile parent drops its SetDocumentState

Ticket: [131 — Reconcile parent drops its SetDocumentState](../issues/131-reconcile-parent-drops-setdocumentstate.md)

Spec: [Document-state events](../spec.md)

Arch: [core-refinement architecture](../arch.md)

Range: `2eaf83559fd15d798c0b5192e8e105dde2de3449` — Post NodeParsed for a reconciled parent from its owning container.

## 1. Standards

- 1. **Parsed-parent post** — Judgement. Smell Duplicated Code. `LazyLoadReconciliationServer.postParsedFromPulling` and `LazyLoadReconciliationServer.postParsedParents` both iterate ids and call `CoreMailboxApi.postNotice` with `Notice.NodeParsed`.

## 2. Spec

- 1. **Handler side effect** — Extra. `LazyLoadReconciliationServer.postReconcileReport` posts `Notice.NodeParsed` for `parsedParentIds`. `CoreMailboxLoop.applyNotice` sends that case to `CoreMailboxFilesAxis.applyNodeParsed`, which calls `pushParsedUnpersisted` and `noteParsed`. [6 — Answer (Alan)](../issues/131-reconcile-parent-drops-setdocumentstate.md) says those two calls are wrong for a parent that goes Unparsed to Current without a parse, and tables the fix. [8 — Round](../issues/131-reconcile-parent-drops-setdocumentstate.md) leaves both functions unchanged. A parent whose persist state is Persisted skips the push and the diff request. An Unpersisted parent takes both.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/check-test.sh 81570` printed `DONE`. The log stopped in `tests/Server.Tests` at `SessionTests.fs` FS0764, field `GitToken` on `Authentication`. See [6 — Found elsewhere](#6-found-elsewhere).

## 5. Miss

- No findings.

## 6. Found elsewhere

The following issues were found beyond the ticket scope. The fixes are suggested.

- 1. **Stale reference assembly** — Problem. `src/Server/obj/Debug/net10.0/ref/Gambol.Server.dll` still named `GitToken` after commit `98accaf815e74b063e2cd4c71c0b99cfc1105bcf` — Delete leftover git token auth — dropped that field from `Authentication` in `src/Server/AmbitApp.fs`. The built `Gambol.Server.dll` matches that record. The full suite stopped before any fact ran. Fix: rebuild `src/Server/Gambol.Server.fsproj` so the reference assembly is rewritten. That rebuild dropped the field. `git load reconcile posts NodeParsed for an Unparsed parent` and `NodeParsed sets legacy DocumentState to Current` then passed.
- 2. **Directory Parsed producer** — Problem. [core-refinement architecture](../arch.md) section Directory Parse done says `ParseThread.finishParse` posts `Notice.NodeParsed` for a Directory Node. This commit posts that Notice from `LazyLoadReconciliationServer.reconcileReceivedFiles`. Fix: name that producer on the git-pull finish when this round is accepted.
