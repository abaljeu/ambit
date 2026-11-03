# 04 — Use a placeholder

Ticket: [[04-use-placeholder-ticket.md]]
Spec: [[plan/client-never-fails/spec.md]]
Arch: [[plan/client-never-fails/arch.md]]

## 1. Standards

- **Scan.** `python scripts/standards-scan.py --diff origin/staging` printed no line. That range is `origin/staging...HEAD`, and this ticket is uncommitted. `python scripts/standards-scan.py --diff HEAD` printed no line. That range is the worktree diff.
- **Two edit matches.** **Judgement.** Duplicated Code. `startEditOp` and `startEditAtPos` in `src/Client/UpdateOps.fs` each match `Map.tryFind targetId model.graph.nodes` and return `model, []` on `None`. Source: [[plan/context/SMELLS.md]]. The hunk is `| None -> model, []` at lines 55 and 69.
- **Two absent-Header graphs.** **Judgement.** Duplicated Code. `placeholderChildUnderRoot` in `tests/Shared.Tests/ViewModelRowStateTests.fs` and ``compose wants a placeholder id with no child list after parent expand`` in `tests/Shared.Tests/WantTests.fs` each unload an id and then `fromExisting` with `Map.remove` of that id. Source: [[plan/context/SMELLS.md]]. The hunk is `fromExisting unloaded (Map.remove placeholderId unloaded.nodes)`.
- **parentInst.** **Judgement.** Mysterious Name. In that Want fact, `parentInst` is `findInstanceId placeholderId siteMap0`, the placeholder row. Source: [[plan/context/SMELLS.md]]. The hunk is `let parentInst = findInstanceId placeholderId siteMap0`.

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

- **Suite.** `scripts/check-test.sh 97953` printed `DONE` and `/tmp/ambit-full-suite-20261031-152257.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 728, skipped 1, failed 0. `tests/Shared.Tests` passed 1914, skipped 3, failed 0.
- **Selection fact.** ``isEntrySelected is true for a placeholder SiteEntry with no Header`` builds a child whose id is absent from `graph.nodes`, places that SiteEntry in a `singleSelection` range, and asserts `isEntrySelected` is true.
- **Expand fact.** ``expandEntry leaves a placeholder row expanded`` calls `expandEntry` on a collapsed row whose id has no child-list key and asserts that row's `expanded` is true.
- **Want fact.** ``compose wants a placeholder id with no child list after parent expand`` asserts `Want.compose` returns that id. `expandEntry` runs on the placeholder instance. `buildSiteMapFrom` already sets the zoom root `expanded` to true, so that id is already in the Included walk before that call.
- **Edit fact.** ``startEditInstanceAtPos leaves Selecting when Header is absent`` removes the Header, calls `startEditInstanceAtPos`, and asserts the mode is `Selecting`.
- **Delete fact.** ``classifyDeleteForChildSpan moves a placeholder child with no Header to TRASH`` classifies that child as `MoveToTrash`, applies the ops, and finds the id under TRASH with the Header still absent. `scripts/agent-test.sh --filter "FullyQualifiedName~DeleteOpsTests" shared` passed 22.

## 5. Miss

- No findings.

## 6. Found elsewhere

The Found elsewhere section takes the PR body's Found elsewhere list. That list is empty.

- No findings.
