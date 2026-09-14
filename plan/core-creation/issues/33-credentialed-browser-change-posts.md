# 33 — Credentialed Browser Change posts

**Status:** done
**Blocked by:** None — can start immediately. Point 0 ([[30-reshape-coreactorpool-synchronized-table.md]], [[31-one-coremsg-loop-parameterized-persist.md]], [[32-move-persist-agents-under-coremailbox.md]]) is done. Not blocked by [[29-prove-testactor-hello.md|Prove TestActor hello]].
Estimate: 2h
Actual: 2h30m

## 1. Context

A live Browser already posts Changes and presents a session credential ([[20-client-presents-credential.md]], [[23-close-core-object-seam.md]], [[25-bind-changes-at-core-seam.md]]). Admission today sits outside the mailbox (`CoreAuth.bind` / `browserChanges` before enqueue). `PostChange` on CoreMsg carries only the Change list and hands straight to PersistHandlers. Architecture locks credentialed Change posts on the CoreMailbox door with CoreMsg validating Authority and secret before PersistHandlers — Story path **Browser Change posts** and shared segment **Credentialed `PostChange` through CoreMsg** on [[plan/core-creation/arch.md|Core creation architecture]].

Architecture (Story paths, Module map, Seams): [[plan/core-creation/arch.md|Core creation architecture]]. This ticket holds acceptance for that Browser path; do not restate Module map Interface here.

## 2. What to build

Make Browser Change posts credentialed end-to-end: Browser Change submit supplies Authority and secret → HTTP Adapter encodes Change + credentials → CoreMailbox credentialed `postChange` → CoreMsg validates Browser credentials before PersistHandlers. A live Browser credential is admitted; a missing or inactive credential is the same auth refuse, and refuse happens before persist. Follow arch Story path **Browser Change posts** and shared segment **Credentialed `PostChange` through CoreMsg**. Point at arch Module map for State / Interface / Uses.

### What this increment avoids

Actor live-table admit (Story path **Browser Run hello** hop admit-before-`PostChange`), StartActor, TestActor hello, Outside Core lifecycle proof, new Browser chrome or controls, PersistHandlers widening, and Actor `PostChange` beyond the shared CoreMsg credential check that Browser posts also use.

### 1. Browser Run

Browser-originated Change posts. Contracts on arch **Browser Run**.

1. [x] Browser Change submit — existing Change submit supplies Authority and secret on the post
2. [x] No new Browser chrome — reuse existing Change UI; add no new control

### 2. HTTP Adapter

Transport encoding. Contracts on arch **HTTP Adapter**.

1. [x] Encode Change + credentials — decode Browser Change; Change posts carry credentials
2. [x] Pass into Core door — pass Authority and secret into CoreMailbox `postChange`
3. [x] Adapter stays decode and status — admission is not re-implemented in the Adapter

### 3. CoreMailbox

Public door. Contracts on arch **CoreMailbox**; Seam **CoreMailbox door**.

1. [x] Credentialed `postChange` — public `postChange` takes Change list plus Browser Authority and secret (same door Browser and Actor will share; this ticket exercises Browser only)

### 4. CoreMsg / CoreMailboxBackend

Mailbox validation before persist. Contracts on arch **CoreMsg / CoreMailboxBackend**; Seam **Credentialed Change posts**; shared segment **Credentialed `PostChange` through CoreMsg**.

1. [x] `PostChange` carries credentials — every `PostChange` (Browser path here) requires Authority and secret on the message
2. [x] Validate before PersistHandlers — CoreMsg validates Browser credentials before calling PersistHandlers
3. [x] Live admits — a live Browser credential is admitted and the Change reaches PersistHandlers
4. [x] Inactive refuses — a missing or inactive credential is the same auth refuse; PersistHandlers is not called for that refuse
5. [x] No Actor live-table admit — this ticket does not require CoreActorPool live-row admit (that stays with hello Story paths)

## 3. See also

[[plan/core-creation/arch.md|Core creation architecture]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]], [[20-client-presents-credential.md]], [[23-close-core-object-seam.md]], [[25-bind-changes-at-core-seam.md]], [[32-move-persist-agents-under-coremailbox.md]], [[29-prove-testactor-hello.md]], [[Implementation Planning and Record.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]

## 4. Comments

- 2026-09-13 — Filed via `/to-tickets` narrowed to Story path **Browser Change posts** only (tracer-cut). Paths 1–2 not ticketed here.
- 2026-09-13 — Implemented: `PostChange` carries Authority + secret; CoreMsg admits before PersistHandlers; CoreMailbox credentialed door; Browser path stamps via `browserChanges` / `Authority "Browser"`. Seam tests in CredentialedChangePostsTests.

## Time

- 2026-09-13 2h30m — Credentialed Browser Change posts through CoreMsg (from chat)
