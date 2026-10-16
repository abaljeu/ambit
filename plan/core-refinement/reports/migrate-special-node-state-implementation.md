# Migrate special-node state implementation

## 1. Selected issue

1. **Issue** — [19 — Migrate special-node state onto the DocumentState record](plan/core-refinement/issues/19-migrate-special-node-state-onto-the-document-state-record.md)
2. **Path** — `plan/core-refinement/issues/19-migrate-special-node-state-onto-the-document-state-record.md`
3. **Status after this run** — `coded` (not `done`)
4. **Contradiction check** — none. The issue matches [core-refinement architecture](plan/core-refinement/arch.md) §3 step 1.2 **Migrate** and §9 story path 1.2 **Migrate**.

## 2. Behavior

1. **Record field** — `Node.documentState` has type `DocumentState`.
2. **Temporary record shape** — fields are `parseState`, `persistState`, `noServerFile`, and nested `documentState`.
3. **Legacy enum** — `DocumentStateEnum` still has Current, Unparsed, and NoServerFile. `noServerFile` is true only when the nested enum is NoServerFile.
4. **Dual-write** — `Node.Create`, `Node.withParseState`, `Node.withPersistState`, `GraphMutate.setParseState`, `GraphMutate.setPersistState`, `markOwningSpecialUnpersisted`, and persist stamps write the record and the standalone axes. `Op.SetDocumentState` still writes the nested enum only and leaves the parse axis.
5. **Contraction stays later** — standalone axes, nested `documentState`, `DocumentStateEnum`, and `noServerFile` remain.
6. **Wire format** — JSON and SQL still store the nested enum as `"current"` | `"unparsed"` | `"noServerFile"`.

## 3. Assumptions

1. **In-memory type, stable wire** — changing the Node field type does not change the stored enum string.
2. **Axis-write** — `SetDocumentState` is not the writer of the parsed axis.
3. **Test seam** — the ticket Store test sits on Graph/Node public writers. Binding-arch Core loop apply is the InMsg seam and was not the migrate seam.

## 4. Red evidence

1. **Command** — `dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~special node stores the four-field record"`
2. **Result** — compile fail `FS0039: The type 'DocumentStateEnum' does not define a field, constructor, or member named 'parseState'` (and the same for `persistState`, `noServerFile`, nested `documentState`).
3. **Log** — `/opt/cursor/artifacts/store-test-red.log`

## 5. Green evidence

1. **Store test** — the same filter: `Passed! Failed: 0, Passed: 1`.
2. **SpecialNodeStateAxesTests** — 14 passed.
3. **SerializationTests plus GraphProjectionTests plus SpecialNodeStateAxesTests** — 83 passed.
4. **DocumentPartitionTests** — 22 passed.
5. **Server.Tests compile** — `dotnet build tests/Server.Tests` succeeded.
6. **Client compile gate** — `bash scripts/client.sh build` succeeded (Fable then esbuild).
7. **Log** — `/opt/cursor/artifacts/store-test-green.log` and `/opt/cursor/artifacts/client-compile-gate.log`

## 6. Exact checks and results

1. **git status first step** — `scripts/gitstatus.sh` showed previous disposable branch `cursor/core-refinement-plan-fix-3faa` and untracked [first-open-ticket-ambiguity](plan/core-refinement/reports/first-open-ticket-ambiguity.md). That report was not read, edited, staged, or committed.
2. **origin/staging** — fetched. Tip `c83ba4b5 Correct Core Refinement document state plan (#245)` is an ancestor of `origin/staging`.
3. **Branch** — `cursor/migrate-document-state-3faa` from `origin/staging`.
4. **git diff --check** — passed.
5. **Focused tests** — listed in §5 **Green evidence**. Full suite not run (subagents.md focused-tests-only).
6. **Toolchain** — `dotnet` 10.0.402 present. Gate ran; not skipped.

## 7. Code-review outcome

1. **Range** — `git diff HEAD` then commit `2aef6114` versus `origin/staging`.
2. **Standards scan** — `python3 .agents/skills/code-review/scripts/standards-scan.py`. Printed BARE_ID lines on the other worker's untracked ambiguity report; those are outside this change. Measured new functions: `fromParts` 10 lines, `documentStateEnum` 3 lines, `withPersistState` 6 lines.
3. **Standards**
   1. **Hard** — No findings in this change's files.
   2. **Judgement — Shotgun Surgery** — many readers and tests changed because the Node field type changed. Writes go through `Node` helpers. Required by the migrate.
4. **Spec**
   1. **No findings** — Record field, Temporary record shape, Legacy enum, Dual-write, Contraction stays later, Workspace graph, and Store test are present.
5. **Arch**
   1. **No findings** — [core-refinement architecture](plan/core-refinement/arch.md) §3 step 1.2 **Migrate** holds. [parse-thread architecture](plan/parse-thread/arch.md) still uses `SetDocumentState` with the enum. [browser-residency architecture](plan/browser-residency/arch.md) Unparsed indicator reads the nested enum.
6. **Totals**
   1. **Standards** — 1 finding. Worst: Shotgun Surgery judgement.
   2. **Spec** — 0 findings.
   3. **Arch** — 0 findings. No contradiction, so Status is `coded`.

## 8. Bookkeeping

1. **Ticket Status** — `coded`.
2. **Amendments** — none changed.
3. **Trackers** — [Workspace graph](doc/current/graph.md) Document state updated. User-manual pages unchanged: Unparsed observation is the same enum.
4. **Architecture checkboxes** — §3 step 1.2 **Migrate** and §9 story path 1.2 **Migrate** checked. Contract left unchecked.
5. **Project** — [core-refinement](plan/core-refinement/project.md) Stage `build`. Actual `12h15m`. Ticket Actual `2h`.
6. **Jev** — `jev_decide` failed with `Jev API error 400: Unsupported model`. Labels unset.

## 9. Commits

1. **Implementation** — `2aef6114` `Migrate special-node state onto the DocumentState record`

## 10. Branch

1. **Name** — `cursor/migrate-document-state-3faa`
2. **Base** — `origin/staging` at `c83ba4b5`
3. **Remote** — `origin/cursor/migrate-document-state-3faa`
4. **Open PR URL** — `https://github.com/abaljeu/ambit/pull/new/cursor/migrate-document-state-3faa`

## 11. Pull request

1. **ManagePullRequest** — not available in this run's tools.
2. **gh pr create** — failed: `GraphQL: Resource not accessible by integration (createPullRequest)`.
3. **Parent next** — open a draft PR toward `staging` from `cursor/migrate-document-state-3faa`. Suggested body is Summary (diff sketch), Evidence (red compile / green store test), Merge Danger (two-way, Shared Node field), Binding arch (`plan/core-refinement/arch.md`, `plan/parse-thread/arch.md`, `plan/browser-residency/arch.md`), Facet table (Jev unset).

## 12. Artifact paths

1. **Red log** — `/opt/cursor/artifacts/store-test-red.log`
2. **Green log** — `/opt/cursor/artifacts/store-test-green.log`
3. **Client compile** — `/opt/cursor/artifacts/client-compile-gate.log`
4. **Handoff** — `/opt/cursor/artifacts/code-tour-author-handoff.md`

## 13. Residual risks

1. **SQL reload** — projection still stores the enum string; reload derives parse and defaults persist to Persisted, then rebuilds the record.
2. **Nested enum versus parse axis** — they can still diverge because `SetDocumentState` does not write parse. That is the expand-contract state until [20 — Contract special-node state inside DocumentState](plan/core-refinement/issues/20-contract-special-node-state-inside-document-state.md).
3. **No draft PR** — this integration cannot create the GitHub PR. The branch is pushed.
4. **Other-worker file** — untracked [first-open-ticket-ambiguity](plan/core-refinement/reports/first-open-ticket-ambiguity.md) remains unstaged.
