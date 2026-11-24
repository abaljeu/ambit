# Ambit child-list investigation (read-only), revised 2026-11-23 ~21:45 EST

Store: Azure Postgres gambol-pg / db gambol (src/Server/Database.fs `initSchema`: events, graph, nodes,
node_children) + server DataDir. Not reachable from this box. Run `event-log-queries.sql` (Q0–Q18, all SELECT
in a READ ONLY transaction). On-box evidence: server-committed git trees /workspace/ambit, /workspace/life.

## 1. Glyph
src/Shared/ViewModelChildrenIndicator.fs `rowChildrenIndicator` (drawn in src/Client/RowView.fs `nodeBullet`):
Unparsed -> slashed hollow; children not resident -> hollow; Parsed + [] -> FILLED; Parsed + children -> chevron.
Filled = Parsed with an empty list. d57b5be6 Unparsed nodes draw slashed, so they are not the filled nodes.

## 2. One path explains all three workspace kinds
Server reconcile against a missing or mismatched folder:
- src/Server/DocumentPersistPath.fs `discoverInDirectory` returns `Ok []` when the folder does NOT exist.
- src/Shared/dotnet/DirectoryReconcile.fs `planFromEntries`: every owned File/Directory child whose name is not
  in that listing is moved to TRASH (`ViewModelDeleteOps.planDeleteDroppedOwnedMany`); Normal outline children are kept.
- src/Server/ParseThread.fs `parseDirectory` posts it as a "Parse" change, then `finishParse` posts NodeParsed
  -> the Directory/Workspace is Parsed with no File/Directory children = filled circle. The subtrees are in TRASH with their lists.
- Trigger: any Load that reaches the node. `CoreMailboxApi.mbLoadNode` (Order.Load) -> `CoreMailboxFilesAxis.markUnparsedThenPush`
  marks Unparsed and re-parses, so even a Parsed node is re-reconciled. GithubTransportActor `runLoadBody`
  -> `runGitOrNeither` -> `runNeither` -> `finishLoadOn` -> `startParse` loads the WORKSPACE. Between d57b5be6 (Nov 11)
  and PR 632 (merged Nov 20, deploy unknown) every plain Load anywhere went through this. After PR 632 a Workspace Load still does.
| Workspace | Why the folder is missing or mismatched | Which nodes get hit |
|---|---|---|
| Unbased (graph-only) | DataDir/<name>/ never exists (`DocumentPartition.artifactDirectoryRelative` still yields `<name>/`); `gitRemoteArm` -> Neither | ALL owned File/Directory children of the workspace on the first Load that reached it. Normal children survive. Still live after PR 632. |
| Git (ambit) | Manual server merge a4e17e56 (Nov 21 13:36 EST, author "Ambit", not the actor) moved HEAD from 45397f77 (Oct 18) by 448 commits / 1285 paths (700 A, 31 D, 419 M, 138 R). The actor reconciles only `beforeHead..afterHead` of ITS OWN pull (`mergeLocked` / `changedAfterPull`), so this merge was never reconciled. 97 folders the graph knew are gone or renamed on disk (`ambit_dirs_gone_in_nov21_merge.txt`). | Only folders Loaded while their disk folder was missing (empty listing -> all File/Dir children trashed), and folders whose names differ only by case (Linux `Directory.Exists` is case-sensitive; `knownNamed` is not). Folders never Loaded, or whose folder still exists, keep their lists: the partial pattern. |
| Desktop (life) | Directory stubs from Upload TreeStructure/StubOnly mode (`WorkspaceSyncLimits.classify`) have no server folder yet; case collisions (`AGENTS` vs `agents`) | Stubs that were Loaded before bytes landed. Also, Upload stubs (commandName "Load", `WorkspaceUploadStructure.planStubOps`) start Parsed with no children (`ChangeAmendment.markCreatedUnparsed` marks only "Parse"). |

## 3. Client edits (unbased workspace)
- Stale or empty client list in a Replace cannot erase server children: `ChangeAmendment.tryAmendReplace` -> `ChildListMerge.acceptBoth`
  removes only anchor items missing from the posted list; an empty anchor removes nothing. A filled client view submitted as a Replace does not make the loss real.
- Real client removals (Delete, Move/drag, Paste over, Undo/Redo of an add) are possible and would carry authority Browser and a
  user command name. Q14 separates them from authority "Parse" reconcile removals; Q15 pairs each removal with the Load order before it.
- No boot, migration or Workspaces-list rebuild edits child lists: `initSchema` only DDL and kind fixes.
  Save Neither (`runSaveAfterPersist`) only persists and maybe commits; it doesn't reconcile. Its persist can create DataDir/<name>/.amb,
  and a later Load then reconciles against a folder holding only `.amb` -> same trash result.

## 4. Other git-side paths checked
| Path | Parsed+[]? | Partial because |
|---|---|---|
| Save `focusCanBeParsed`/`persistFocus` on File or Directory focus | Yes: flips Unparsed->Parsed without parsing; File persist writes empty content | Only the Save focus (issue 07). Persist never deletes disk files. |
| git pull reconcile (`LazyLoadReconciliationReport.planReceivedFiles`) | No for files (stubs and Modified marked Unparsed). A Modified `.amb` is applied as the directory's list (`parseDirInfoIfPresent`) then NodeParsed, so a stale `.amb` shortens the list. | Only paths in that pull's diff; per-path failures (kind conflict, "cannot modify an unparsed document") are skipped and only logged. |
| Chunked post (`GraphOnlyChangeChunks.split`, 80 ops; `GraphOnlyChangePost.postChunks`) | Partial apply: earlier chunks stay if a later one fails; ParseFailed for parsedDirIds, no rollback | Large pulls or reconciles stop partway |
| Boot rebuild | No: graph loads from DB rows; no disk reparse | — |
| ParseThread `alreadyParsed`, `requeueOnUnparsed=false` | Keeps filled nodes filled until an explicit Load | — |

## 5. Server commits Nov 19–23 (server_commits_nov19_23_filtered.txt)
| Commit | Time (EST) | Loss |
|---|---|---|
| ambit 3a1a0aa8 gambol: git Save | 2026-11-21 16:37 | `plan/command-line-actor/issues/07-…md` -> 0 bytes (52 lines) |
| life e31beb60 "edits" (manual, Ambit) | 2026-11-20 10:05 | root `.amb` lost `prompts` (and its self-entry); 18 root + 10 contacts ids re-minted |
| ambit a4e17e56 / life 62ec2d0 merges | 11-21 13:36 / 11-20 09:57 | none vs upstream parent (took upstream cleanly) |
| (earlier) life fe87092 daily autosave | 2026-10-16 | `AGENTS/README.md` deleted (case collision) |
No other emptied files and no other `.amb` lost entries.

## 6. Root cause (revised)
Main (Run ranked 2nd, see sections 8 and 9): Load -> reconcile of a Workspace/Directory against a disk folder that is missing (unbased workspace, folders moved by the
unreconciled manual merge, Upload stubs with no folder) or differs in case. The empty listing moves all File/Directory children to TRASH
and marks the parent Parsed -> filled. Secondary: Save on an Unparsed focus (issue 07); Upload stubs created Parsed (life root).

## 7. Recovery plan (nothing executed)
| # | Step | Risk | Go-ahead |
|---|---|---|---|
| 0 | No Load/Save/Upload on the affected workspaces, especially the unbased one (each Load repeats the trash move) | none | Alan |
| 1 | Run Q3, Q14, Q15, Q16, Q17, Q18 to list removal events and where each subtree went | none | Alan runs |
| 2 | Move the TRASH subtrees back to their parents in original order: one Replace per parent restoring `oldChildren` from the Q14 event, plus a TRASH Replace removing them. Ids, children and Refs are preserved. Do this as one reviewed change, not via Undo. | medium | Alan + cloud-agent tool |
| 2b | If Q18 shows Run removals: those children are orphaned, not in TRASH. Re-attach them by restoring that event's Replace `oldChildren` (the nodes are still in `nodes`, and Q7 lists them). Remove the AI output only after review | medium | Alan |
| 3 | Git workspace: before any Load, run a full reconcile of the manual merge (or reset the tracked head so the actor diffs 45397f77..HEAD) so renamed folders move instead of being emptied | medium | Alan |
| 4 | Issue 07: restore file from b3b612d5 to staging and the server tree, then Load | low | Alan |
| 5 | life: restore `AGENTS/README.md` (fe87092^), re-link `prompts`, decide on re-minted root nodes (old ids in TRASH/orphaned, Q4) | low–med | Alan |
| 6 | Code tickets: `discoverInDirectory` must return Error for a missing folder, and reconcile must skip graph-only (unbased) workspaces; Save must parse or refuse Unparsed focus; Upload Directory stubs start Unparsed; case-sensitive name match; manual merges need a reconcile hook; chunked parse posts need all-or-nothing or resume | — | Alan |

## 8. Run / command-line actor (Alan's theory)
**The command-line actor does not exist yet.** It is only a plan: b3b612d5, PR #633, Nov 21 12:40 EST, docs and tickets only.
`CommandRequest.tryStart` accepts only `?test` and `?ai`. Any other `?name` gets "unknown actor" and changes nothing.
A `>` line is the old Amble Run, `AmbleRun.legacyRun`, which is a search that adds Refs.

Run end to end (src/Client/Commands.fs `execRunOp`, the target is always `focusedNodeId committed.graph sel`):
| Branch | Chosen by | Op on target | Old children go to |
|---|---|---|---|
| Query (`=` in focus text) | `CommandRequest.tryQueryStart`: focus must contain `=` | server query actor, Replace on focus | orphaned |
| Amble `=` scan-stop / `name = expr` / `>` line | `isAmbleScanStop` + `AmbleRun.shouldExec`. This requires the FOCUS itself to be a statement or a `>` line | `execAmbleRunOp`: `deleteChildSpan` (all focus children, "Delete") then `runAmbleOp` -> `ExprRun.replaceChildren` / `AmbleRun.replaceAllChildrenOp` Replace(focus, …) | TRASH (delete step) |
| `?ai` / `?test` actor | `actorNodeOnOwnerPath`: the first `?`/`=` owner from focus up to zoom, inclusive. **The focus can be any node under the `?ai` line.** | `ActorOrderFromStart` focusId = `start.focusId`. RunAgentActor `postAdd`/`nextChildren`: the first add gives Replace(focus, current, [edge]). TextReady `FocusChildrenReplace.plan`: an empty reply gives Replace(focus, old, []) | orphaned, with no TRASH op |

Wrong-target check:
- I found no wrong-target bug in the code.
- The target is the start focus captured at the keypress. The start also records the definition node, `commandId`, separately.
- `FocusXmlStream` parents are only the focus or newly minted ids, so the AI output cannot address an existing node.
- `AmbleEval` RefSpec nodes come from the same graph, so they always become Refs. Nothing is re-created or stolen.
- The "wrong node" effect is possible **by design**:
  - Pressing Run with the cursor on a DESCENDANT of a `?ai` line runs the AI with that descendant as focus.
  - That erases the descendant's children, and an empty or whitespace reply leaves it empty (filled glyph).
- A trap for anyone reading the event log: an `actorOrder` event's JSON `focusId` is actually `actorNodeId` (`EventJson.actorOrderFields`), so it names the definition node, not the target.

Git log for the Run files (Commands.fs, CommandRequest.fs, AmbleRun.fs, ExprRun.fs, FocusXmlStream.fs, RunAgentActor.fs, ActorOrderFromStart.fs, SearchActor.fs), Nov 15–21:
- **No commits in that window.** The most recent changes are all Nov 13 or earlier.
- None of the changes below altered how the target is chosen. The real choice of target was made in #573 (Nov 10).

| Commit | PR | Date (EST) | What changed |
|---|---|---|---|
| 80cad19c | #619 | 11-13 16:48 | ActorStopped notice (plumbing) |
| 9c9e1aac, 6c9ada45 | #618, #616 | 11-13 | server search actor |
| 4273ef22 | #617 | 11-13 15:11 | ActorResult (plumbing) |
| 9e1ec14b | #609 | 11-13 12:41 | Commands.fs: Save routing, not Run |
| 264a02cb | #608 | 11-13 10:26 | limits the AI extract |
| 8a64e2bb | #605 | 11-12 20:41 | reject unknown Run commands before the queue |
| f8b26535 | #579 | 11-10 15:53 | query command start |
| a66cdded, b19181a7 | #621, #628 | 11-15 | remove CoreMsg.ActorStop (server core plumbing, no target logic) |

How Run would look in the DB (Q18):
- An authority-Actor or Exec/Delete removal.
- `target_is_start_focus` = true.
- The removed children are **orphaned** (Actor) or in TRASH with a "Delete" right before an Exec Replace (Amble).
- A start event exists, and its `run_definition_text` starts with `?ai`.

How the reconcile path looks instead (Q14/Q15):
- Authority/command "Parse".
- A `load` order just before it.
- File/Directory children moved to TRASH, with Normal children kept.

## 9. Ranking (pending Q14–Q18)
| Rank | Path | Explains unbased / git / desktop | Explains "only some" | Explains filled glyph | Evidence so far |
|---|---|---|---|---|---|
| 1 | Load -> reconcile against a missing or case-mismatched folder (section 2) | yes / yes / yes | yes: only Loaded folders that are missing on disk | yes (NodeParsed after an empty listing) | 97 folders gone in the unreconciled merge; unbased has no DataDir folder |
| 2 | Run on the affected node itself (`?ai` under a `?ai` line, or an Amble statement) | only if Run was started on each one | yes, if Run was started on each one | yes (Run leaves it Parsed with `[]` or new children) | none yet. Needs Q18 rows. Workspace/Directory nodes are rarely under a `?ai` line |
| 3 | Save on an Unparsed focus | issue 07 only | — | yes | 3a1a0aa8 emptied issue 07 |
| 4 | Upload stubs created Parsed | desktop only | stub nodes | yes | life root |
| 5 | Chunked partial apply | git | yes | partly | none |
| 6 | Run hitting a node other than its focus (a true wrong-target bug) | — | — | — | no such code path found |
Run moves to rank 1 only if Q18 returns rows on the affected nodes with `target_is_start_focus` = true and orphaned children.
