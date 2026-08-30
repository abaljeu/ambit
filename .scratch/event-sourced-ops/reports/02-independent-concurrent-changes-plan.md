# Report — Issue 02 independent concurrent Changes plan

**Date:** 2026-08-22
**Branch:** `w/event-sourced-ops`
**Issue:** [[../issues/02-independent-concurrent-changes-succeed.md]]
**Plan artifact:** `C:/Users/Windows 8/.cursor/plans/independent-concurrent-changes_issue02.plan.md` (same frontmatter shape as the sibling issue-01 plan). CreatePlan was unavailable in this subagent session; the file is the CreatePlan-equivalent deliverable.

## Findings

- The global revision gate still lives in both server apply paths: `FileAgent.applyBatch` and `DbAgent.applyBatch` reject when `change.id <> s.revision.Value` with `"Revision mismatch..."`. That is the only server acceptance predicate that refuses unrelated concurrent work.
- Shared `History.applyChange` / `GraphMutate` already enforce per-Op compare-and-swap. No Shared edits are required for this ticket.
- Existing integration test ``POST with wrong base revision returns 400`` in [[../../../tests/Server.Tests/StateEndpointTests.fs]] locks the obsolete gate behavior and must become a success scenario.
- Sibling delivery docs ([relaxed-concurrency/spec.md](../../relaxed-concurrency/spec.md), [design.md](../../relaxed-concurrency/design.md)) already specify the subtractive change, test seam, and out-of-scope items. Event-sourced-ops relation docs say slice 1 **stands**; later recoverable merge/amend is issues 03+.
- Gate is still present in the working tree, so this ticket is a **build** of slice 1 (with verify/handoff of the relaxed-concurrency project), not docs-only.

## Relation to issue 01

| Concern | Verdict |
| --- | --- |
| Functional block | **None.** Issue 02 can start without the shared envelope. |
| Soft conflict | Both touch `FileAgent.fs`, `DbAgent.fs`, and `StateEndpointTests.fs`. Issue 01 already expands confirmation encoding (`externalChanges = false`); issue 02 must only remove the revision-mismatch arms and add concurrency tests. |
| Contract | Issue 02 keeps Post confirmation-echo success. Do not set `externalChanges = true` or change ack apply semantics here. |

Recommended sequencing: rebase or land after issue 01’s agent/test edits if they collide, but do not wait for envelope semantics to unblock gate removal.

## Assumptions (defaults chosen; no AskQuestion)

1. Implement gate removal here now; treat relaxed-concurrency slice 1 as the delivery source of truth and close it via verify/handoff when tests pass.
2. Same-target attribute CAS and same-parent Replace CAS still Reject with today’s op-level errors until issues 03/05.
3. One global Server revision sequence remains; `change.id` stays on the wire as informational base revision, not an acceptance predicate.
4. No client planner, wire-format, or merge-model work in this ticket.
5. `ViewModelJoinOps.removeCurrentOp` Owner fabrication stays out of scope (existing WORK pending item).

## CreatePlan result

- **Title:** Issue 02 — Independent concurrent Changes succeed
- **Name/slug:** `independent-concurrent-changes-issue02`
- **Path:** `C:/Users/Windows 8/.cursor/plans/independent-concurrent-changes_issue02.plan.md`
- **Todos:** (1) red concurrency tests, (2) remove revision gate in both agents, (3) verify + handoff

## Recommended board mutations (for root; not applied)

- **move** — when implementation starts: Pending → Active for [[../issues/02-independent-concurrent-changes-succeed.md]] (parent: [[../project.md]]).
- **none** for block — do not Block on issue 01.
- Optional later **remove** / handoff note for [[../../relaxed-concurrency/spec.md]] once slice 1 is verified green (root decides; leave Pending until then).

## Stage note for root

Parent [[../project.md]] is already `Stage: active`. No stage change required for planning. Prefer report-only; leave Stage alone.

## Parent summary payload

- **Plan title/path:** Issue 02 — Independent concurrent Changes succeed → `.cursor/plans/independent-concurrent-changes_issue02.plan.md` (user plans dir).
- **Blocked on 01:** no (soft file-touch conflict only).
- **Board mutations:** `move` (to Active at implement start); `none` for block.
