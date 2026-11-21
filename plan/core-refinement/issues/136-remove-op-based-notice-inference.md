# 136 — Remove op-based Notice inference

**Status:** `defined`

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

Every producer posts its own document-state Notices. The backend still derives Notices by reading `Op.SetDocumentState` out of posted Changes:

- `DocumentOpImpact.noticesBeside`
- `Api.inferredNodeEvents`, called from `Api.postEvents`
- `FileAgent.postGraphOnlyNotices`, called from `processPostEvents`
- `DatabaseProjection.noticesBeside`, called from `DbAgent` after a graph-only commit

## 4. Change

- Delete `DocumentOpImpact.noticesBeside` and its private helper `documentStateTransition`.
- Delete `Api.inferredNodeEvents` and its use in `Api.postEvents`.
- Delete `FileAgent.postGraphOnlyNotices` and its call.
- Delete `DatabaseProjection.noticesBeside` and its call in `DbAgent`.
- No Notice is derived from ops anywhere.

Source: `DocumentOpImpact.fs`, `Api.fs`, `FileAgent.fs`, `DatabaseProjection.fs`, `DbAgent.fs`.

## 5. Tests

[ ] A test that only checked a Notice inferred from a hand-built `Op.SetDocumentState` is deleted.
[ ] A test that needs a Notice gets it from the producer that posts it.

## 6. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
