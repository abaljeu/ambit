# 135 — Op readers leave

## 1. Type

coding

## 2. Blockers

[ ] 125, 126, 127, 128, 129, 130, 131, 132, and 133. Each producer posts its own Notice before the code that reads that Notice from the op leaves.

## 3. Problem

Ticket 122 covered most producers only through the op. The code that reads it is:

- `DocumentOpImpact.noticesBeside`, which works out the Notice from `Op.SetDocumentState` after the Change is accepted
- its callers `Api.inferredNodeEvents`, `FileAgent.postGraphOnlyNotices`, and `DatabaseProjection.noticesBeside` (called from `DbAgent.commitPostChange`)
- `LazyLoadReconciliationServer.parsedFromPulling` and `postParsedFromPulling`

Removing a producer's op therefore removed its Notice.

## 4. Shared rule (125–134)

Each producer names its covering Notice and returns it next to its ops, in apply order, using the existing `EventBody` list: the lead Notices, then `EventBody.Change ops`, then the trail Notices. No new type is added.

- **Lead.** A `Notice.NodeParsed` that unlocks content ops in the same plan goes before the Change. The ops then run on a Current root, which is the job the leading op did against `History.blockedForApply` and `ChangeAmendment.applyForGraphOnly`.
- **Trail.** Every other Notice goes after the Change, in planner order. A new stub does not exist before its `NewSpecialNode`, and a container's `MarkUnparsed` has to land after `markParentCurrent`.
- **Posting.** Server producers post through `CoreMailboxApi.postNotice` and their usual Change door, waiting for each reply. Client producers post through [134](134-client-door-forwards-state-notices.md). Planner folds apply each body with `Ev.apply`.

Why this placement: the mailbox is FIFO, `Ev.apply` already applies a Notice, and every op in these producers either leads its node's ops or is appended after the plan. Rounds 2 and 3 of 124 already work this way.

Accepted tradeoff: a lead Notice is its own mailbox step. If the Change after it is rejected, the root stays Current or Parsed with the old content.

A second post of the same Notice is idempotent (124 §5), so 125–134 land in any order.

## 5. Change

Delete:

- `DocumentOpImpact.noticesBeside` and `documentStateTransition`
- `Api.inferredNodeEvents`
- `FileAgent.postGraphOnlyNotices`
- `DatabaseProjection.noticesBeside` and its call in `DbAgent`
- `LazyLoadReconciliationServer.parsedFromPulling` and `postParsedFromPulling`

Source: `DocumentOpImpact.fs`, `Api.fs`, `FileAgent.fs`, `DatabaseProjection.fs`, `DbAgent.fs`, `LazyLoadReconciliationServer.fs`.

## 6. Tests

[ ] `NodeEventExpandTests`: the remaining `… beside SetDocumentState` facts that post the op directly

## 7. Done

[ ] A repo search under `src` finds no `noticesBeside`, `inferredNodeEvents`, `postGraphOnlyNotices`, or `parsedFromPulling`.
[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
