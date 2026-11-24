# Fix children wipe

Stage: investigation — evidence pending
Summary: In the live store, Nodes lost their owned child lists in three kinds of Workspace (graph-only, git-backed, desktop-mapped). Only some Nodes were hit. The UI showed each one as a filled circle: Parsed with an empty child list. [[evidence/investigation-report.md]] ranks the candidate causes; [[evidence/event-log-queries.sql]] (Q0–Q18) confirms which one actually fired.
Updated: 2026-11-24
Started: 2026-11-23

## Goal

Find why Nodes lost their owned children, recover what was lost, then fix the code path that caused it. Recovery comes before any code fix: the investigation report section 7 is the candidate recovery plan, and nothing in that plan has run yet.

## Affected nodes

A filled circle is a Directory, File, or Workspace Node that is Parsed with an empty owned-child list (`src/Shared/ViewModelChildrenIndicator.fs` `rowChildrenIndicator`, drawn by `src/Client/RowView.fs` `nodeBullet`). Only some Nodes in each Workspace were hit; the rest kept their lists.

| Workspace kind | Which Nodes were hit | Why (candidate) |
|---|---|---|
| Graph-only (unbased) | Every owned File/Directory child of the Workspace, on the first Load that reached it | The Workspace has no `DataDir/<name>/` folder on disk at all |
| Git-backed (ambit) | Only folders Loaded after the unreconciled Nov 21 manual merge moved or deleted their disk folder, or whose name differs only by case | `GithubTransportActor` reconciles only its own pull range; a manual server merge is never reconciled |
| Desktop-mapped (life) | Upload stubs Loaded before their bytes landed; the life root (Upload stub created Parsed) | A Directory Upload stub has no server folder yet, or a case collision (`AGENTS` vs `agents`) |

The investigation report section 2 is the full table with code references. Section 5 lists every confirmed-emptied file and the commit that emptied it.

## Candidate causes, ranked

Ranking and evidence are the investigation report section 9. Confirming or revising this ranking is ticket 01, below.

| Rank | Cause | Code | Confirmed by |
|---|---|---|---|
| 1 | Load → `DirectoryReconcile` against a missing or wrong-case folder | `src/Server/DocumentPersistPath.fs` `discoverInDirectory` can return an empty listing for a folder that does not exist; `src/Shared/dotnet/DirectoryReconcile.fs` `planFromEntries` then trashes every owned child not in that listing | Q14–Q17 |
| 2 | Run on the Node itself | `?ai`/Amble Run replacing the focus Node's children | Q18 |
| 3 | Save on an Unparsed Node | `GithubTransportActor.focusCanBeParsed`; emptied [07 — Document the command-line actor](../command-line-actor/issues/07-document-the-command-line-actor.md) at `3a1a0aa8` | Q11 (already observed) |
| 4 | Desktop Upload stubs created Parsed | `WorkspaceUploadStructure.planStubOps` | Q5, Q10 (already observed on the life root) |
| 5 | Run hitting a Node other than its focus | — | No code path found; see the investigation report section 8 |

## Issues

| # | Name | Status |
|---|---|---|
| 01 | [Run the event-log queries](issues/01-run-event-log-queries.md) | ready-for-human |
| 02 | [Recover the lost children](issues/02-recover-children.md) | blocked by 01 |
| 03 | [Guard DirectoryReconcile against a missing folder](issues/03-reconcile-missing-folder-guard.md) | pending confirmation from 01 |
| 04 | [Guard Save on an Unparsed Node](issues/04-save-unparsed-guard.md) | pending confirmation from 01 |
| 05 | [Desktop Upload Directory stubs start Unparsed](issues/05-upload-stubs-unparsed.md) | pending confirmation from 01 |

## Decisions

1. **Its own Project** — Alan, 2026-11-24: this bug gets its own fix Project, separate from any feature Project already in flight.
2. **Recovery first** — Recovery comes before any code fix. Ticket 02 is not blocked on 03–05, and 03–05 do not start before recovery is agreed.
3. **No Load, Save, or Upload on the affected Workspaces** — Every Load repeats the trash move on an unbased Workspace. No Load, Save, or Upload runs on ambit, life, or any other affected Workspace until recovery is agreed.
4. **Confirmation is the query results, not this report** — The ranking above is a candidate, read from the code and the commits. The cause is confirmed only once Alan runs the queries against the Azure Postgres store (read-only) and the results are pasted into `evidence/results/`.

## See also

[[evidence/investigation-report.md]], [[evidence/event-log-queries.sql]]
