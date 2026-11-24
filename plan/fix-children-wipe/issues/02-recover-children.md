# 02 — Recover the lost children

**Status**: blocked by 01
Blockers: [01 — Run the event-log queries](01-run-event-log-queries.md) — recovery targets each parent by the removal event those results name.

## Scope

Re-attach each parent's old children from TRASH or from orphaned Nodes, using the removal events ticket 01's results confirm (Q14, Q17, Q18). Restore [07 — Document the command-line actor](../../command-line-actor/issues/07-document-the-command-line-actor.md) from commit `b3b612d5` before any Load touches that file's Workspace. Restore `AGENTS/README.md` on the life Workspace from commit `fe87092^`. Re-link `prompts` under the life root. [[evidence/investigation-report.md]] section 7 is the candidate plan; this ticket turns it into one reviewed change per parent, not an Undo.

## Done

[ ] Every parent the Q14/Q17 results name has one Replace restoring its `oldChildren` from the removal event, plus a TRASH Replace removing those same ids from TRASH.
[ ] Every child Q18 shows as orphaned by a Run removal is re-attached by restoring that event's `oldChildren`, after a review of the AI output already standing in its place.
[ ] `07-document-the-command-line-actor.md` holds its `b3b612d5` content again, on the server tree and on staging.
[ ] `AGENTS/README.md` is restored from `fe87092^`.
[ ] `prompts` is re-linked under the life root, and each re-minted root id from the Nov 20 merge is resolved by name (kept, merged, or trashed), not silently dropped.

## Stop

[ ] Stop before any Load, Save, or Upload on ambit, life, or any other affected Workspace while recovery is in progress; `project.md` Decision 3 covers every one of them.
[ ] Stop if a restore target is not named by ticket 01's results; ask rather than guess which event to restore from.
[ ] Stop if a recovery Replace would also move a Node a person added after the loss; separate that case out for Alan instead of folding it into the restore.
