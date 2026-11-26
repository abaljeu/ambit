# Fix children wipe

Stage: investigation — waiting on a logged occurrence
Summary: In the live store, Nodes lost their owned child lists in three kinds of Workspace (graph-only, git-backed, desktop-mapped). Only some Nodes were hit. The UI showed each one as a filled circle: Parsed with an empty child list. [[evidence/investigation-report.md]] ranks the candidate causes; [[evidence/event-log-queries.sql]] (Q0–Q18) confirms which one actually fired.
Updated: 2026-11-25
Started: 2026-11-23

## Goal

Find why Nodes lost their owned children, recover what was lost, then fix the code path that caused it. Recovery comes before any code fix: the investigation report section 7 is the candidate recovery plan, and nothing in that plan has run yet. Follow-on, Alan, 2026-11-25: recovery has now been agreed unnecessary.

## Affected nodes

A filled circle is a Directory, File, or Workspace Node that is Parsed with an empty owned-child list (`src/Shared/ViewModelChildrenIndicator.fs` `rowChildrenIndicator`, drawn by `src/Client/RowView.fs` `nodeBullet`). Only some Nodes in each Workspace were hit; the rest kept their lists.

| Workspace kind | Which Nodes were hit | Why (candidate) |
|---|---|---|
| Graph-only (unbased) | Every owned File/Directory child of the Workspace, on the first Load that reached it | The Workspace has no `DataDir/<name>/` folder on disk at all |
| Git-backed (ambit) | Only folders Loaded after the unreconciled Nov 21 manual merge moved or deleted their disk folder, or whose name differs only by case | `GithubTransportActor` reconciles only its own pull range; a manual server merge is never reconciled |
| Desktop-mapped (life) | Upload stubs Loaded before their bytes landed; the life root (Upload stub created Parsed) | A Directory Upload stub has no server folder yet, or a case collision (`AGENTS` vs `agents`) |

The investigation report section 2 is the full table with code references. Section 5 lists every confirmed-emptied file and the commit that emptied it.

## Findings (2026-11-24)

Ticket 01's event-log queries ran against production Postgres, read-only. The newest event is 27784 at 2026-11-22 11:57 EST, when the user stopped using the app; as of that event LAPS (`161b30c1-73e6-4909-b477-e32060e223ad`) had 14 children and the ambit workspace (`56df2956-22d0-4434-95f6-16dc1d486328`) had 31. No event ever emptied LAPS's child list; every change was a 1–2 child user edit, and there are no orphans. [[evidence/results/results.txt]] holds the result sets.

On 2026-11-23, when the user first connected, LAPS's descendants and the ambit workspace showed no children (filled-circle bullets, Parsed with empty lists) before the user did anything. On 2026-11-24 the same Nodes showed their children again, with no event between those times that touched either list. The data was intact on the server throughout; the client, or the initial feed, was missing it. This is a display/load bug, not data loss, and it is [06 — Children missing on the first connect after idle](issues/06-children-missing-on-connect.md).

The only confirmed real loss is issue 07, emptied by a Save on an Unparsed Node (events 27360→27362→27364, 2026-11-21 16:37 EST). That is ticket 04's cause. The causes behind tickets 03 and 05 were not seen in the event log; they stand as hardening, not as proven causes of an actual loss.

## Candidate causes, ranked

Ranking and evidence are the investigation report section 9. Confirming or revising this ranking is ticket 01, below. The Findings above revise the "Confirmed by" column for ranks 1, 3, and 4.

| Rank | Cause | Code | Confirmed by |
|---|---|---|---|
| 1 | Load → `DirectoryReconcile` against a missing or wrong-case folder | `src/Server/DocumentPersistPath.fs` `discoverInDirectory` can return an empty listing for a folder that does not exist; `src/Shared/dotnet/DirectoryReconcile.fs` `planFromEntries` then trashes every owned child not in that listing | Not seen in the event log (Findings above); ticket 03 is hardening, not a proven cause |
| 2 | Run on the Node itself | `?ai`/Amble Run replacing the focus Node's children | Q18 |
| 3 | Save on an Unparsed Node | `GithubTransportActor.focusCanBeParsed`; emptied [07 — Document the command-line actor](../command-line-actor/issues/07-document-the-command-line-actor.md) at `3a1a0aa8` | Confirmed — the only real loss found in the log (Findings above) |
| 4 | Desktop Upload stubs created Parsed | `WorkspaceUploadStructure.planStubOps` | Not seen in the event log (Findings above); ticket 05 is hardening, not a proven cause |
| 5 | Run hitting a Node other than its focus | — | No code path found; see the investigation report section 8 |

## Issues

| # | Name | Status |
|---|---|---|
| 01 | [Run the event-log queries](issues/01-run-event-log-queries.md) | ready-for-human |
| 02 | [Recover the lost children](issues/02-recover-children.md) | not needed |
| 03 | [Guard DirectoryReconcile against a missing folder](issues/03-reconcile-missing-folder-guard.md) | pending confirmation from 01 |
| 04 | [Guard Save on an Unparsed Node](issues/04-save-unparsed-guard.md) | pending confirmation from 01 |
| 05 | [Desktop Upload Directory stubs start Unparsed](issues/05-upload-stubs-unparsed.md) | pending confirmation from 01 |
| 06 | [Children missing on the first connect after idle](issues/06-children-missing-on-connect.md) | pending |
| 07 | [Persistent server error log](issues/07-persistent-error-log.md) | ready-for-human |
| 08 | [Startup error hides children on first connect](issues/08-startup-error-hides-children-on-first-connect.md) | waiting on 07 — Persistent server error log and a repeated, logged occurrence |
| 09 | [Error log remaining uses](issues/09-error-log-remaining-uses.md) | defined |

## Decisions

1. **Its own Project** — Alan, 2026-11-24: this bug gets its own fix Project, separate from any feature Project already in flight.
2. **Recovery first** — Recovery comes before any code fix. Ticket 02 is not blocked on 03–05, and 03–05 do not start before recovery is agreed. Follow-on, Alan, 2026-11-25: recovery has now been agreed unnecessary.
3. **No Load, Save, or Upload on the affected Workspaces** — Every Load repeats the trash move on an unbased Workspace. No Load, Save, or Upload runs on ambit, life, or any other affected Workspace until recovery is agreed. Follow-on, Alan, 2026-11-25: recovery has now been agreed unnecessary. The hold is lifted (Decision 7).
4. **Confirmation is the query results, not this report** — The ranking above is a candidate, read from the code and the commits. The cause is confirmed only once Alan runs the queries against the Azure Postgres store (read-only) and the results are pasted into `evidence/results/`.
5. **Database intact; 06 — Children missing on the first connect after idle is the display/load fix** — Alan, 2026-11-25: the database was intact; a startup process error masked the issues. The database-repair dependency is dropped; 06 — Children missing on the first connect after idle is the display/load fix.
6. **Recovery is agreed unnecessary** — 2026-11-25 Alan: recovery is agreed unnecessary. The data was intact; the missing children came from a startup error, not data loss.
7. **Load, Save, and Upload hold lifted** — Alan, 2026-11-25: the Load, Save, and Upload hold on ambit, life, and the other affected Workspaces is lifted.

## See also

[[evidence/investigation-report.md]], [[evidence/event-log-queries.sql]]
