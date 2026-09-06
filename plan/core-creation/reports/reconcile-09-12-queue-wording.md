# Reconcile 09–12 queue wording

Date: 2026-09-06

Purpose: Find statements that contradict the locked apply-mailbox design (delete-actor FIFO after that Actor’s Posts; registry / number / lock last until delete-actor applies). List each contradiction and whether it was fixed.

Canonical: [[../issues/09-define-core-command-launch-contract.md]], [[../issues/10-define-actor-cancellation-and-output-admission.md]], [[../issues/11-define-actor-finish-and-failure-behavior.md]], [[../issues/12-define-actor-pool-shutdown-behavior.md]]. Map: [[../map.md]].

## Fixes (9)

1. [[../issues/09-define-core-command-launch-contract.md]] ## Answer — said query fails after the task ends because the number is gone. Fixed: query works while the Actor is registered; number is gone after delete-actor applies.
2. [[grill-issue-09-launch-contract.md]] Locked contract — same “after the task ends” rule with no delete-actor. Fixed: one-line later amendment (issue 11). Historical Q11/Q12 answers unchanged.
3. [[../issues/10-define-actor-cancellation-and-output-admission.md]] ## Answer — still retained a lock-present flag on the job. Fixed: lock-present is on the Node (11). Comments Amend updated.
4. [[grill-issue-10-cancellation.md]] Locked contract — job lock-present flag; two refuse kinds; open may-change to map Unauthorized to 401. Fixed: Node lock; one auth family; system error stays on 12.
5. [[grill-issue-10-cancellation.md]] session header, Design tree, Round 7 — still taught the 401 / Unauthorized split as current. Fixed: they merge (2026-09-06). Historical Q15 answer unchanged.
6. [[grill-issue-11-finish.md]] Weakest assumption — cited 09 as if the current Answer still dropped the number at task end. Fixed: “at grill start” plus later 09 match.
7. [[../issues/12-define-actor-pool-shutdown-behavior.md]] Comments Q6 / Amend — “dropped outage items stay gone” still read as current. Fixed: Amend says that was the old Q2B world. ## Answer already kept siblings.
8. [[grill-issue-12-shutdown.md]] Q6 Answer — same dropped-items line with no later note. Fixed: one-line later amendment. Historical A/B text unchanged. Locked contract already applied siblings.
9. [[grill-issue-12-facts.md]] From issue 10 — Core Error Unauthorized only; Core retains a lock-present job flag. Fixed: one auth family; lock-present on the Node.

## Already agreed (no edit)

- [[../map.md]] Decisions so far for 09–12 already used delete-actor, one mailbox rule, one auth refuse, SQL omit lock, host-stop drain.
- [[../issues/11-define-actor-finish-and-failure-behavior.md]] ## Answer already used delete-actor FIFO and Node lock.
- [[../issues/12-define-actor-pool-shutdown-behavior.md]] ## Answer already applied siblings and host-stop delete-actor.
- [[../project.md]] summary lines were true. Stage not changed.

## Leftover (not changed)

- Issue 09 Comments Q10–Q12 still say “after the task ends” / “number is gone” (historical grill log).
- Issue 10 Comments Q7 still say lock-present flag on the job; Q15 still names two refuse words (Amend covers).
- Issue 11 Comments Q2/Q3/Q5 still say lock-clear (later named delete-actor).
- Issue 12 Comments Q2/Q13 still record drop-siblings (Amend covers).
- [[grill-issue-12-facts.md]] §1 still snapshots issue 12 as Status open (grill-start fact, not a contract rule).
- [[grill-issue-12-shutdown.md]] Weakest assumption and Alan’s open still say “disable the mailbox” (opening fork, not Locked contract).
- Cookie facts in [[grill-issue-10-cancellation.md]] still say today’s Adapter maps Core Error to HTTP 400 (code fact, not the locked refuse family).
