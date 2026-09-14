# 33 — Credentialed Browser Change posts

**Status:** done
**Blocked by:** None — can start immediately. Point 0 ([[30-reshape-coreactorpool-synchronized-table.md]], [[31-one-coremsg-loop-parameterized-persist.md]], [[32-move-persist-agents-under-coremailbox.md]]) is done. Not blocked by [[29-prove-testactor-hello.md|Prove TestActor hello]].
Estimate: 2h
Actual: 5h45m

## 1. Context

A live Browser already posts Changes and presents the Login.html session cookie `gambol_auth` ([[20-client-presents-credential.md]], [[23-close-core-object-seam.md]], [[25-bind-changes-at-core-seam.md]]). Architecture locks credentialed Change posts on the CoreMailbox door with CoreMsg validating Authority and secret before PersistHandlers — Story path **Browser Change posts** and shared segment **Credentialed `PostChange` through CoreMsg** on [[plan/core-creation/arch.md|Core creation architecture]].

An earlier implement on this ticket partially moved admission into CoreMsg but also pushed credentials into FileAgent / DbAgent / DatabaseSetup and closed a boot-minted GUID into `browserChanges` / `addBoundCredentials`. That is not the target. Treat the current tree as partial/wrong; this ticket describes target behavior and remaining work. Acceptance below is unchecked until the target holds.

Architecture (Story paths, Module map, Seams): [[plan/core-creation/arch.md|Core creation architecture]]. This ticket holds acceptance for that Browser path; do not restate Module map Interface here.

## 2. What to build

Make Browser Change posts credentialed end-to-end with cookie-as-credential and request-carried secrets: Browser presents `gambol_auth` → HTTP Adapter encodes Change + credentials → CoreMailbox credentialed `postChange` → CoreMsg admits against CoreCredentials → PersistHandlers.postChange(changes only). Nothing past CoreMailbox needs credentials (not FileAgent, not DbAgent, not PersistHandlers). Follow arch Story path **Browser Change posts** and shared segment **Credentialed `PostChange` through CoreMsg**. Point at arch Module map for State / Interface / Uses.

### Credential identity

1. Login.html cookie `gambol_auth` is the Browser authorization to post. That value is `AuthToken.deriveToken` from Auth config; boot seeds CoreCredentials with the same value.
2. When the server issues that cookie, that value is what belongs in CoreCredentials (`credentials.add`).
3. All Browser API requests include creds; they arrive at CoreMailbox; mailbox validates before action; nothing further needs the credentials.
4. A missing cookie still refuses at mailbox / API. There is no closed-over server fallback.

### Restart / seed (chosen: option 2)

1. Seed at server boot: source is deterministic `AuthToken.deriveToken` from Auth config username/password; `credentials.add` that value. “validToken” is recomputed from the same Auth config (not a mystery GUID).
2. After server restart, the same derived token still admits if the Cookie header is sent. Credential identity does not need client cookie re-issue or re-login.
3. `DeployEpochSec` / `window.__BUILD_TS__` is the server-restart signal for the Fable client (reload / page epoch). The client writes `__BUILD_TS__` on that signal and on initial load. That write is not credential re-establish.
4. Desktop presents Cookie via LocalProxy (AuthStore / captured Set-Cookie / auth-disabled `deriveToken("", "")`), not via `DeployEpochSec`.
5. A Browser with a durable cookie already has the credential; silent web re-login is not required.
6. Rejected for web: option 1 — re-login-after-restart only if invisible via stored password.
7. Not needed: option 3 — persist the credential set (seed replaces it).

### What this increment avoids

Actor live-table admit (Story path **Browser Run hello** hop admit-before-`PostChange`), StartActor, TestActor hello, Outside Core lifecycle proof, new Browser chrome or controls, PersistHandlers widening (still changes-only after admit), Actor `PostChange` beyond the shared CoreMsg credential check that Browser posts also use, and any CoreActor / CoreActorPool deltas for this round.

### 1. Undo mistaken deltas

Reverse off-intent plumbing from the earlier implement so credentials stop at CoreMailbox.

1. [x] FileAgent — remove `CoreCredentials` from `create` / `createWithDependencies` and from `CoreMailboxBackend.start` call sites that only exist to thread credentials through File
2. [x] DbAgent — remove `CoreCredentials` from `create*` / `createLoaded` / `createWithLiveSave` and from `startWithPrelude` call sites that only exist to thread credentials through Db
3. [x] CoreMailbox — remove credentials param from `createFile` / `createDb` / `createDbWithDataDir`
4. [x] DatabaseSetup — remove credentials from `getOrCreateDbHost`
5. [x] CoreRuntime — reverse boot-minted random GUID in `addBoundCredentials`; reverse `browserChanges` closing over that GUID; seed `AuthToken.deriveToken(Auth config user/pass)` via `credentials.add` instead
6. [x] CoreActor / CoreActorPool — reverse any credentials-plumbing deltas from this ticket round (those modules were not in scope)

### 2. Browser Run

Browser-originated Change posts. Contracts on arch **Browser Run**.

1. [x] Browser Change submit — existing Change submit presents cookie / Authority and secret on the post (request-carried; not a server-closed GUID)
2. [x] No new Browser chrome — reuse existing Change UI; add no new control
3. [x] Restart / initial load epoch — client writes `window.__BUILD_TS__` from `DeployEpochSec` (restart signal and initial load); that is not cookie re-issue or credential re-establish

### 3. HTTP Adapter

Transport encoding. Contracts on arch **HTTP Adapter**.

1. [x] Encode Change + credentials — decode Browser Change; Change posts carry credentials from the request/cookie
2. [x] Pass into Core door — pass Authority and secret into CoreMailbox `postChange`
3. [x] Adapter stays decode and status — admission is not re-implemented in the Adapter

### 4. CoreMailbox

Public door. Contracts on arch **CoreMailbox**; Seam **CoreMailbox door**.

1. [x] Credentialed `postChange` — public `postChange` takes Change list plus Browser Authority and secret (same door Browser and Actor will share; this ticket exercises Browser only)
2. [x] Validation stops here — after CoreMsg admit, PersistHandlers and File/Db see changes only

### 5. CoreMsg / CoreMailboxBackend

Mailbox validation before persist. Contracts on arch **CoreMsg / CoreMailboxBackend**; Seam **Credentialed Change posts**; shared segment **Credentialed `PostChange` through CoreMsg**.

1. [x] `PostChange` carries credentials — every `PostChange` (Browser path here) requires Authority and secret on the message
2. [x] Validate before PersistHandlers — CoreMsg validates Browser credentials before calling PersistHandlers
3. [x] PersistHandlers stays changes-only — `postChange` after admit does not take credentials
4. [x] Live admits — a live Browser credential (cookie value seeded / added) is admitted and the Change reaches PersistHandlers
5. [x] Inactive refuses — a missing or inactive credential is the same auth refuse; PersistHandlers is not called for that refuse
6. [x] No Actor live-table admit — this ticket does not require CoreActorPool live-row admit (that stays with hello Story paths)

### 6. CoreRuntime seed

Boot seed for Browser credential. Contracts on arch **CoreRuntime**.

1. [x] Seed at boot — `credentials.add` of `AuthToken.deriveToken` from Auth config user/pass
2. [x] Cookie match — Login.html / auth routes issue the same derived token as `gambol_auth`; that value is what CoreCredentials holds

## 3. Out of scope

1. FileAgent / DbAgent / PersistHandlers credential parameters (undo if present; do not add)
2. CoreActor / CoreActorPool changes for this round (undo if present; do not add)
3. Persisting the credential set across restarts (option 3)
4. Invisible stored-password re-login after restart for web (option 1)
5. Actor live-table admit, StartActor, TestActor hello, Outside Core lifecycle proof, new Browser chrome

## 4. See also

[[plan/core-creation/arch.md|Core creation architecture]], [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]], [[20-client-presents-credential.md]], [[23-close-core-object-seam.md]], [[25-bind-changes-at-core-seam.md]], [[32-move-persist-agents-under-coremailbox.md]], [[29-prove-testactor-hello.md]], [[Implementation Planning and Record.md]], [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]], [[src/Server/AuthToken.fs]]

## 5. Comments

- 2026-09-13 — Filed via `/to-tickets` narrowed to Story path **Browser Change posts** only (tracer-cut). Paths 1–2 not ticketed here.
- 2026-09-13 — Partial implement: `PostChange` carries Authority + secret; CoreMsg admits before PersistHandlers; CoreMailbox credentialed door; Browser path stamped via `browserChanges` / `Authority "Browser"`. Seam tests in CredentialedChangePostsTests. Also wrongly threaded credentials into File/Db/DatabaseSetup and closed a boot GUID into `browserChanges` / `addBoundCredentials`.
- 2026-09-13 — Redesign locked: cookie `gambol_auth` is the credential; seed at boot with `AuthToken.deriveToken(Auth config)`; request-carried creds validate at CoreMailbox only; undo File/Db/CoreActor/CoreActorPool/GUID deltas. Status returned to `ready-for-agent`; acceptance unchecked until target holds. Arch Story path **Browser Change posts** still shows `[x]` — align [[plan/core-creation/arch.md|Core creation architecture]] when reconciling (prefer this ticket as source of truth for remaining work).
- 2026-09-13 — Re-implement against redesign: File/Db take MailboxStarter (no CoreCredentials); createFile/createDb take starter; boot seeds `deriveToken`; Change posts carry request cookie; login `credentials.add`; request path reseeds cookie into the set.
- 2026-09-14 — Spec-gap fix after review of `4008e8d`: state/poll/load use request cookie; missing cookie refuses (no closed-over fallback); client reacts to DeployEpochSec restart/initial-load; boot+login `credentials.add` only.

## Time

- 2026-09-13 2h30m — Credentialed Browser Change posts through CoreMsg (from chat; partial/wrong vs redesign)
- 2026-09-13 1h30m — Re-implement cookie-as-credential, boot seed, stop-at-mailbox undo (from chat)
- 2026-09-14 1h30m — Spec-gap fix: request-carried creds on state/poll/load, no closed-over fallback, client DeployEpochSec reseed, drop every-post add (from chat)
