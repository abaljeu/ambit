Viewable copy of the Cursor implement plan for issue 02.

---
name: independent-concurrent-changes-issue02
overview: Drop the global revision gate from FileAgent and DbAgent apply so unrelated concurrent Changes succeed when per-Op preconditions match; keep same-target CAS Reject and leave Shared History untouched.
todos:
  - id: red-concurrency-tests
    content: Rewrite the obsolete revision-only 400 test and add failing unrelated-attribute/structural and collision scenarios in StateEndpointTests; pause for review
    status: pending
  - id: remove-revision-gate
    content: Delete the change.id vs server revision reject branch in FileAgent.applyBatch and DbAgent.applyBatch; leave History/GraphMutate alone
    status: pending
  - id: verify-handoff
    content: Run focused StateEndpointTests on both backends; update issue 02 checkboxes and note relaxed-concurrency slice 1 verify/handoff; leave issue 01 envelope work untouched
    status: pending
isProject: false
---

# Issue 02 — Independent concurrent Changes succeed

## Goal and non-goals

Deliver [plan/event-sourced-ops/issues/02-independent-concurrent-changes-succeed.md](plan/event-sourced-ops/issues/02-independent-concurrent-changes-succeed.md) by implementing sibling slice 1 from [plan/relaxed-concurrency/spec.md](plan/relaxed-concurrency/spec.md) and [plan/relaxed-concurrency/design.md](plan/relaxed-concurrency/design.md). Two Actors may POST Changes that name a stale global base revision when Ops do not collide on per-Op preconditions; both succeed. Same-target attribute or same-parent Replace compare-and-swap may still Reject until later amendment tickets. Do not invent field or child-list merge. Do not change the Post confirmation-echo contract or `externalChanges` semantics owned by issue 01.

## Dependency on issue 01

Issue 02 is **not functionally blocked** by issue 01. Gate removal is independent of the shared success envelope. Soft conflict only: both touch [src/Server/FileAgent.fs](src/Server/FileAgent.fs), [src/Server/DbAgent.fs](src/Server/DbAgent.fs), and [tests/Server.Tests/StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs). Prefer landing or rebasing after issue 01’s envelope edits in those files, or surgically edit only the `applyBatch` revision-mismatch arms so envelope encoding stays untouched.

## Code change (subtractive)

In both agents, delete the revision-gate branch and fall through to `History.applyChange`:

- [src/Server/FileAgent.fs](src/Server/FileAgent.fs) — remove `| None when change.id <> s.revision.Value -> Error "Revision mismatch..."` inside `applyBatch` (today ~154–156).
- [src/Server/DbAgent.fs](src/Server/DbAgent.fs) — same branch inside `applyBatch` (today ~112–114).

Preserve unchanged: `changeId` dedup, fail-fast batch fold, unchanged-submission Reject, revision bump by one per successful apply, persistence/validation, stamp overlay. Do **not** edit [src/Shared/History.fs](src/Shared/History.fs) or [src/Shared/GraphMutate.fs](src/Shared/GraphMutate.fs) — per-Op CAS already enforces real collisions (`old text does not match`, `old span does not match`, and siblings).

## Tests (primary seam)

Use parameterized `POST /ambit/changes` coverage in [tests/Server.Tests/StateEndpointTests.fs](tests/Server.Tests/StateEndpointTests.fs) across file and database backends:

1. Rewrite ``POST with wrong base revision returns 400`` into a success case (stale `change.id`, valid unrelated SetText → 200, revision advances, text present).
2. Unrelated attribute concurrency — A commits SetText on Y; B submits SetText on X with stale revision → both texts present.
3. Attribute collision — B’s stale oldText on X after A changed X → 400 with op-level mismatch; A’s text kept.
4. Unrelated structural concurrency — Replace under different parents with stale revision → both succeed.
5. Same-parent structural collision — stale Replace span → 400; parent children match A.
6. Confirm existing changeId dedup still succeeds with stale revision (revision unchanged on resubmit).

Assert HTTP status, returned revision, acked `changeId`s, and GET graph state — not agent internals. Out of scope: ViewModelJoinOps Owner fabrication, client merge-sync, Shared-only concurrency suite.

## Verification and handoff

Run focused `StateEndpointTests` (both backends). After green: check issue 02 acceptance boxes; treat [plan/relaxed-concurrency/](plan/relaxed-concurrency/) slice 1 as delivered/verify-handoff rather than a second build. Leave issue 01 envelope behavior and docs alone. Do not commit unless requested.

## Explicit defaults

- Build here (gate still present); not a docs-only verify.
- Keep CAS Reject for colliding Ops (issues 03/05 later).
- Keep one global Server revision sequence for poll/load/catch-up.
- Auth and malformed requests remain Reject; no new error codes.
