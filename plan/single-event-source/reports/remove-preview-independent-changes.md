# Remove preview; independent Changes

Product decision (2026-09-16): Changes are independent. A transport list is not all-or-nothing. Option B from cluster-1, plus delete the batch contract that made preview exist.

## 1. Files changed

1. [[src/Server/Core/CoreMailbox.fs]] — removed `previewTransportBatch` and its calls from `postChange` and `postEvents`. Each item still goes through `mergePostLoop` → one `PostEvent`.
2. [[src/Server/Core/CoreEventDispatch.fs]] — removed `previewEvents`. Nothing else called it. Admit stays in `postEvent` before persist.
3. [[tests/Server.Tests/StateEndpointTests.fs]] — one rewritten fact (section 2).

Persist is unchanged: [[src/Server/Core/FileAgent.fs]] and [[src/Server/Core/DbAgent.fs]] still use ChangeAmendment, Unchanged reject, and submissionId dedup. No new preview. No batch atomicity reimplemented.

Deleted leftover [[tmp/gitstatus-cluster1.sh]].

## 2. Tests rewritten

Search covered Server.Tests, CoreMailbox, CoreEventDispatch, and comments (`all-or-nothing`, `before any commit`, `leaves state unchanged`, `previewEvents`). One test demanded a multi-Change / multi-Ev post as all-or-nothing:

1. **POST changes batch with bad second change keeps earlier items** (was `… leaves state unchanged`) in [[tests/Server.Tests/StateEndpointTests.fs]] — HTTP `POST /ambit/events` with a valid first Change and an Invalid second. Old fact: `400` and revision 0, missing child, empty Poll. New fact: `400` because the second item fails; first Change is applied (revision 1, child present); Poll from 0 contains that first submissionId. Poll `externalChanges` is true when the tail is not empty ([[src/Server/Api.fs]] `getPoll`).

Single-item reject facts were kept:

1. **POST unchanged submission is rejected** — Unchanged reject of one Change.
2. **Actor PostChange without live row is refused before persist** — admit before persist.

[[tests/Server.Tests/FileAgentFailureTests.fs]] **trailing duplicate keeps stamps on last new Change** still posts `[second; first]` to PersistHandlers. That is leftover persist-list apply, not the transport preview. Not rewritten.

## 3. Commands and outcomes

1. `scripts/gitstatus.sh` — workplace `dev`; dirty tree was `test-server.txt` only. No commit. No push.
2. Focused Server.Tests after the Poll assertion fix: 16 passed, 0 failed. Filter: four StateEndpoint amend/stale facts × File+Db, Actor handle wrap, Actor PostChange without live row, rewritten bad-second × File+Db, **POST changes batch with two changes bumps revision to 2** × File+Db, **POST Change and inverse Changes return complete confirmations in request order** × File+Db (three-item success list).
3. Scratch [[tmp/remove-preview-tests.sh]] deleted after the green run.

No Client compile gate: Shared and Client were not edited.

## 4. Not chased

1. Cluster 3 — DbAgent timeout / sweep-begin / DB-away error wrapping.
2. Cluster 4 — HttpResponseLogTests "change json" vs "event json" and log `00000000` prefix.
3. Broader leftover-Change deletion from [[plan/single-event-source/arch.md]] Sequence.
4. [[doc/api.md]] still says a multi-Change HTTP batch is all-or-nothing (`400` leaves state unchanged). This pass is code+tests.
5. FileAgent / DbAgent `applyBatch` still folds a PersistHandlers Change list as all-or-nothing inside one persist call. Transport no longer sends that list through preview.

## 5. Architecture note

[[plan/single-event-source/arch.md]] does not describe preview-as-all-or-nothing. Not rewritten. [[plan/core-creation/arch.md]] was not edited.
