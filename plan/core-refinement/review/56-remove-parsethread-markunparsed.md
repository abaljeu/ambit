# 56 — Remove ParseThread markUnparsed

Ticket: [[../issues/56-remove-parsethread-markunparsed.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- 1. **Standards scan** — `python` is absent. `python3 scripts/standards-scan.py --diff origin/staging` exited 0. Printed sizes: `parseOne` 15 lines, `loop` 9 lines, `startInner` 14 lines, `start` 9 lines. Each is under the 40-line function limit in `.agents/rules/fsharp-source.md`. No GLOSSARY.md PHRASE line.
- 2. **Inner deps copy** — Judgement. Smell Duplicated Code. `InnerDeps` repeats `ParseThreadDeps` except `markUnparsed`.

```fsharp
type ParseThreadDeps =
    { dataDir: string
      consumer: unit -> NodeId option
      push: NodeId -> unit
      getGraph: unit -> Async<Result<Graph, string>>
      postOps: NodeId -> Op list -> Async<Result<unit, string>>
      markUnparsed: NodeId -> unit
      finishParse: NodeId -> unit }
```

```fsharp
type private InnerDeps =
    { dataDir: string
      consumer: unit -> NodeId option
      push: NodeId -> unit
      getGraph: unit -> Async<Result<Graph, string>>
      postOps: NodeId -> Op list -> Async<Result<unit, string>>
      finishParse: NodeId -> unit }
```

`ParseThreadDeps` is `src/Server/ParseThread.fs` lines 7–14. `InnerDeps` is lines 21–27.

- 3. **Start forwards** — Judgement. Smell Middle Man. `ParseThread.start` copies fields and calls `startInner`. `src/Server/ParseThread.fs` lines 163–170.

```fsharp
let start (deps: ParseThreadDeps) =
    startInner
        { dataDir = deps.dataDir
          consumer = deps.consumer
          push = deps.push
          getGraph = deps.getGraph
          postOps = deps.postOps
          finishParse = deps.finishParse }
```

## 2. Spec

- 1. **Field remains** — Partial. [56 — Remove ParseThread markUnparsed](../issues/56-remove-parsethread-markunparsed.md) says remove `ParseThread.markUnparsed`. Spec section 2 **Document-state Set ops** says `ParseThread` `markUnparsed` is removed. `src/Server/ParseThread.fs` line 13 still declares `markUnparsed: NodeId -> unit`. The `afterPost` call and the `bootCore` `Notice.NodeUnparsed` post are gone. `start` does not read the field.
- 2. **Named test** — Missing. Acceptance **Unparsed test** names `parse thread does not mark Unparsed`. No test has that name.

## 3. Arch

- 1. **Producer name remains** — Blocker. Arch **Remove markUnparsed**, **Unparsed producers**, and **No Unparsed producer** say `ParseThread` `markUnparsed` is removed. The field is still `src/Server/ParseThread.fs` line 13.

## 4. Tests

- 1. **Check script** — `scripts/check-test.sh 5481` is not executable. `bash scripts/check-test.sh 5481` printed `DONE` and `/tmp/ambit-full-suite-20261027-200721.log`.
- 2. **Server suite** — `scripts/agent-test.sh --full` ran Server.Tests and stopped on failure. Passed 701, failed 20, skipped 1 (`DocumentPersistenceTests.upload structure persistence preserves existing file mtime`). Total 722.
- 3. **Shared suite** — The full script did not reach Shared.Tests. `dotnet test tests/Shared.Tests -nodeReuse:false -p:AMBIT_FULL_SUITE=1` on this branch passed 1905, failed 0, skipped 3. Total 1908. Skipped: `WorkspaceUploadStructureTests.reuses existing owned path without duplicating`, `WorkspaceUploadStructureTests.planServerFilePresentOps transitions only matching absent files`, `WorkspaceLocalInventoryTests.listForPush without git still returns walk`.
- 4. **Staging comparison** — The same 20 names failed in a detached worktree at `origin/staging` (`2d8af951`). The branch was not changed. `db bootstrap duplicate returns stored Change and rejects no-op` fails on both with the same actual value: an extra `Notice.NodePersisting` after `Change` and `NodePersisted ROOT`.
- 5. **Named test** — `parse thread does not mark Unparsed` is absent. The ticket has no Outputs list.

Failed on this branch and on `origin/staging`:

- `DatabaseProjectionContractTests.db bootstrap duplicate returns stored Change and rejects no-op` — `tests/Server.Tests/DatabaseProjectionContractTests.fs` line 347
- `StateEndpointTests.New server uses snapshot + log replay` — `tests/Server.Tests/StateEndpointTests.fs` line 1467
- `StateEndpointTests.POST with stale base revision and valid SetText succeeds` — `tests/Server.Tests/StateEndpointTests.fs` line 756
- `StateEndpointTests.POST same-parent structural collision amends and succeeds` — `tests/Server.Tests/StateEndpointTests.fs` line 1085
- `StateEndpointTests.DB rows persist after agent cache reset` — `tests/Server.Tests/StateEndpointTests.fs` line 1224
- `StateEndpointTests.DB is present in db mode` — `tests/Server.Tests/StateEndpointTests.fs` line 1208
- `StateEndpointTests.POST changes twice bumps revision to 2` — `tests/Server.Tests/StateEndpointTests.fs` line 592
- `StateEndpointTests.DB restart keeps inverse Change in EventLog` — `tests/Server.Tests/StateEndpointTests.fs` line 1392
- `StateEndpointTests.POST same submissionId twice is idempotent` — `tests/Server.Tests/StateEndpointTests.fs` line 725
- `StateEndpointTests.DB rows survive second server startup without DB reset` — `tests/Server.Tests/StateEndpointTests.fs` line 1241
- `StateEndpointTests.DB restart replays log when projection is cleared` — `tests/Server.Tests/StateEndpointTests.fs` line 1321
- `StateEndpointTests.POST changes persists in GET state` — `tests/Server.Tests/StateEndpointTests.fs` line 685
- `StateEndpointTests.POST changes batch with bad second change keeps earlier items` — `tests/Server.Tests/StateEndpointTests.fs` line 650
- `StateEndpointTests.POST changes SetText changes child text and bumps revision` — `tests/Server.Tests/StateEndpointTests.fs` line 501
- `StateEndpointTests.DB restart preserves NodeIds` — `tests/Server.Tests/StateEndpointTests.fs` line 1294
- `StateEndpointTests.POST changes NewNode+Replace adds child to root` — `tests/Server.Tests/StateEndpointTests.fs` line 532
- `StateEndpointTests.DB restart keeps duplicate submissionId idempotent` — `tests/Server.Tests/StateEndpointTests.fs` line 1354
- `StateEndpointTests.POST duplicate submissionId with stale revision stays idempotent` — `tests/Server.Tests/StateEndpointTests.fs` line 1118
- `StateEndpointTests.POST unrelated structural edits with stale revision both succeed` — `tests/Server.Tests/StateEndpointTests.fs` line 1018
- `StateEndpointTests.POST unrelated attribute edits with stale revision both succeed` — `tests/Server.Tests/StateEndpointTests.fs` line 818

## 5. Miss

- 1. **Field remains** — Partial. `markUnparsed` is still on `ParseThreadDeps` at `src/Server/ParseThread.fs` line 13.
- 2. **Named test** — Missing. `parse thread does not mark Unparsed` does not exist.
- 3. **Producer name remains** — Blocker. Arch **Remove markUnparsed** is not met while line 13 remains.
- 4. **Full suite failures** — The 20 Server failures in section 4 also fail on `origin/staging`. They are not introduced by this diff.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- No findings.
