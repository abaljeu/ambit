# Contract DocumentState implementation

## 1. Selected issue

1. **Issue** — [20 — Contract special-node state inside DocumentState](https://github.com/abaljeu/ambit/blob/cursor/contract-document-state-3faa/plan/core-refinement/issues/20-contract-special-node-state-inside-document-state.md)
2. **Project** — [core-refinement](https://github.com/abaljeu/ambit/blob/cursor/contract-document-state-3faa/plan/core-refinement/project.md)
3. **Base** — `origin/staging` at `77c34378` (`Migrate special-node state onto the DocumentState record`). That squash is the published [19 — Migrate special-node state onto the DocumentState record](https://github.com/abaljeu/ambit/blob/cursor/contract-document-state-3faa/plan/core-refinement/issues/19-migrate-special-node-state-onto-the-document-state-record.md) change from PR 246. PR 246 stays open and was not altered.
4. **Branch** — `cursor/contract-document-state-3faa` from `origin/staging`. Did not reuse `cursor/migrate-document-state-3faa`. Did not touch `plan/core-refinement/reports/first-open-ticket-ambiguity.md`.

## 2. Assumptions

1. **Final record** — `DocumentState` is exactly `{ parseState: ParseState; persistState: PersistState; noServerFile: bool }`.
2. **Removed storage** — Node standalone `parseState` and `persistState` fields, and the nested `documentState` field, leave.
3. **Wire enum stays** — `DocumentStateEnum` stays for `Op.SetDocumentState` and JSON/SQL until those callers migrate.
4. **Lockstep writes** — After the nested field is gone, Current is Parsed, Unparsed is Unparsed, and NoServerFile implies Unparsed.
5. **JSON siblings stay** — Encode still writes sibling `parseState` / `persistState` keys and a derived `documentState` enum string.
6. **No GUI test** — This is a Shared record contraction.

## 3. Binding architecture

1. **Home** — [core-refinement architecture](https://github.com/abaljeu/ambit/blob/cursor/contract-document-state-3faa/plan/core-refinement/arch.md) §3 step 1 **Contract**, §4 **Approach (Mikado)**, §9 story path 1 **Contract**.
2. **Other arch** — [parse-thread architecture](https://github.com/abaljeu/ambit/blob/cursor/contract-document-state-3faa/plan/parse-thread/arch.md) still uses `SetDocumentState` Unparsed to Current. That Op remains. [github-transport architecture](https://github.com/abaljeu/ambit/blob/cursor/contract-document-state-3faa/plan/github-transport/arch.md) does not bind this record shape.
3. **Contradiction** — None. The unchecked Private-axes line (`Op.SetDocumentState` is not the writer of the parsed axis) stays a later target. This beat is the storage contract.

## 4. Test plan

1. **Red** — Rewrite the seam test to read only the three final fields, including NoServerFile implying Unparsed.
2. **Green** — Contract the record and Node fields until that test passes.
3. **Callers** — Update production matches and tests that read nested `documentState` or standalone axes.
4. **Focused modules** — `SpecialNodeStateAxesTests`, related Shared modules, Persist/CoreLoop Server tests, Client compile gate.

## 5. Changes

1. **Record** — [Model.fs](https://github.com/abaljeu/ambit/blob/cursor/contract-document-state-3faa/src/Shared/Model.fs) `DocumentState` has `parseState`, `persistState`, `noServerFile`. `DocumentState.toLegacy` derives Current | Unparsed | NoServerFile.
2. **Node storage** — Node has no `parseState` or `persistState` field. Members of those names read the record so CRLF [FileAgent.fs](https://github.com/abaljeu/ambit/blob/cursor/contract-document-state-3faa/src/Server/Core/FileAgent.fs) did not need an edit.
3. **Writers** — `Node.withDocumentState` writes parseState and noServerFile together. `withParseState` clears noServerFile. `withPersistState` writes only persistState on the record.
4. **Collectors** — [PersistCollectors.fs](https://github.com/abaljeu/ambit/blob/cursor/contract-document-state-3faa/src/Server/Core/PersistCollectors.fs) blocks when `parseState` is Unparsed. The dual-axis Current+Unparsed open path is gone.
5. **Claims** — [graph.md](https://github.com/abaljeu/ambit/blob/cursor/contract-document-state-3faa/doc/current/graph.md) Document state, [parse-persist.md](https://github.com/abaljeu/ambit/blob/cursor/contract-document-state-3faa/doc/current/parse-persist.md) Persist thread block, [op.md](https://github.com/abaljeu/ambit/blob/cursor/contract-document-state-3faa/doc/current/op.md) explanation. Arch contract boxes are checked. User-manual pages did not change.

## 6. Red and green evidence

1. **Before** — `dotnet test tests/Shared.Tests --filter "FullyQualifiedName~special node stores parse persist and noServerFile only inside DocumentState"` failed: expected Unparsed, actual Parsed (`SetDocumentState` NoServerFile left parseState Parsed).
2. **After** — The same filter passed (1 test). Log: `/opt/cursor/artifacts/contract-document-state-seam-test.log`.

## 7. Focused checks

1. **Shared related** — `SpecialNodeStateAxesTests`, `HistoryTests`, `DirectoryReconcileTests`, `SerializationTests`, `WorkspaceUploadStructureTests`, `DocumentAssemblyTests`, `GraphProjectionTests`, `ImportDocumentTests`, `LazyLoadReconciliationTests`: 271 passed.
2. **Server related** — `PersistThreadTests`, `CoreLoopInMsgTests`, `DocumentOpPersistenceTests`: 16 passed. Removed `Current document with stale Unparsed axis still persists`.
3. **ViewModel** — `ViewModelRowStateTests` plus the seam module: 52 passed.
4. **Client compile gate** — `bash scripts/client.sh build` succeeded (Fable then esbuild).
5. **Not run** — Full `scripts/test.sh all`, per the worker contract focused-tests-only rule.

## 8. Review outcome

1. **Standards** — Mechanical scan vs HEAD: no in-scope findings. Hits on `plan/core-refinement/reports/first-open-ticket-ambiguity.md` belong to another worker and were not read or edited. `History.fs` stayed at 800 lines. `Node.parseState` / `Node.persistState` members are single-word public names on the type; they read the contracted record and exist so FileAgent did not take a CRLF whitespace hit.
2. **Spec** — Contract items 1–4 and obligations 1–2 hold. `DocumentStateEnum` remains for Op/JSON callers (amendment 1).
3. **Arch** — §3 step 1 **Contract** holds. No blocker. Private axes stays unchecked.
4. **Status** — Issue Status is `coded`. Not `done`.

## 9. Bookkeeping

1. **Ticket** — Status `coded`. Finish-up checked. Amendments written. Actual 3h.
2. **Project** — Stage stays `build`. Actual 15h15m. Issue list Status `coded`.
3. **User manual** — No change.

## 10. Commits, branch, PR state

1. **Branch** — `cursor/contract-document-state-3faa`
2. **Commit** — Written after this report.
3. **PR** — Draft toward `staging`. ManagePullRequest is not in this tool set. Body: `/opt/cursor/artifacts/contract-document-state-pr-body.md`. Parent opens or updates the draft.

## 11. Artifacts

1. **Seam log** — `/opt/cursor/artifacts/contract-document-state-seam-test.log`
2. **Handoff** — `/opt/cursor/artifacts/code-tour-author-handoff.md`
3. **PR body** — `/opt/cursor/artifacts/contract-document-state-pr-body.md`

## 12. Risks

1. **SetDocumentState writes parseState** — Directory reconcile and disk parse that emit Unparsed to Current now set Parsed immediately. `ParseFinished` is then a no-op on parseState.
2. **Persist block** — Unparsed always blocks persist. The migrate-period Current+Unparsed open path is gone.
3. **Wire** — JSON/SQL still encode the derived enum. Stored graphs keep loading.
4. **Members** — `node.parseState` still typechecks as a member. Callers can keep reading it. Storage is only the record.
