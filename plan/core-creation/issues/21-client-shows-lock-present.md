# 21 — Client shows live Actor (active chrome)

**Status:** coded
**Actual:** 4h
**Blocked by:** None — Server lifecycle Events and Browser Run (`?test` / `?ai`) are delivered. Credentialed Browser posts are `done` ([[20-client-presents-credential.md|20]]). Historical blockers [[02-core-actor-pool.md|02]] and Graph lock-present are superseded.

## Context

A person needs to see that an Actor is live for a Focus, get an immediate start result when Run admits the Actor, and get a stop/error result through Poll when the Actor finishes. The Graph lock-present field and span display are superseded by durable lifecycle Events ([[plan/llm-connector/issues/07-lock-run-agent-architecture.md|07]]). Issue title "lock-present" is historical; this ticket is **active Actor chrome and result conveyance**.

Server already emits `ActorStart` / `ActorStop` on the EventLog; Command and Poll carry them. Browser Command encode already posts `ActorStart` ([[35b-browser-run-hello.md|35b]], llm-connector vertical proof). Actor body failure still follows existing design (e.g. llm-connector [[plan/llm-connector/issues/09-agent-failure-preserves-children.md|09]]: no Focus wipe, no agent-body Error outline text). **Naming** provider errors and teaching the CloudAgents DLL to produce them is a **separate** ticket — this ticket only conveys whatever stop/result the Actor already emits.

Sequence (locked 2026-09-19):

1. Launch admits Actor → immediate Client start result (label per [[../arch.md|core-creation architecture]] **Actor live labels**); chrome on via `ActorStart` on the same Command response path (do not wait for a later Poll).
2. Actor body runs (DLL, etc.). Success / Failed / Cancelled use standard Actor stop handling.
3. `ActorStop` → leave pool → chrome off → stop/error **result message arrives at the Client via Poll** (and via Command response Events when present).

## What to build

1. [x] **Projection** — Client tracks live Focus ids from `EventBody.ActorStart` / `EventBody.ActorStop` applied through the same Event-apply path as Graph Changes for **Command response**, **Poll**, and **Load** tails. After `ActorStart` for a Focus, that Focus is live; after `ActorStop` (Succeeded, Failed, or Cancelled), it is not.
2. [x] **Chrome** — While a Focus is live, the Browser shows a clear active-Actor indicator on that Focus (row / outline). Indicator clears when the Focus leaves the live set.
3. [ ] **Start result** — When Command admits an Actor and the response includes `ActorStart`, Client `lastCmdResult` (or equivalent) shows **“Run: <Label> started.”** where Label follows architecture **Actor live labels** (TitleCase actor token from Command on the Focus→zoom owner path).
4. [ ] **Stop / error result** — When Poll (or Command response) applies `ActorStop`, Client shows a result message with the same Label rule as Start (architecture **Actor live labels**): success detail if useful; on `ActorFailed` / `ActorCancelled`, an Error (or clear status) from the stop. Conveyance only — do not invent provider error strings here.
5. [x] **Boot** — After `/state` (or equivalent boot), a still-live Actor still shows chrome. Prefer an existing Server surface if live Focus ids are already available; no Graph lock-present field and no new live-registry product Poll.
6. [x] **Non-goals** — No Graph lock-present field; no span lock; no separate History/audit UI; no Cancel UI ([[22-client-cancels-a-job.md|22]]); no CloudAgents DLL error naming / `setFake` Unauthorized catalog (separate ticket); no `?ai keyname options` registry.

## See also

[[../arch.md|core-creation architecture]] **Actor live labels**, [[22-client-cancels-a-job.md|22 — Client cancels a job]], [[17-cancel-a-job.md|17 — Cancel a job]] (Server `cancelByFocus` delivered on llm-connector [[plan/llm-connector/issues/10-cancel-by-focus.md|10]]), [[35b-browser-run-hello.md|35b]], [[plan/llm-connector/issues/13-vertical-proof-browser-ask.md|llm-connector 13]], [[plan/llm-connector/issues/09-agent-failure-preserves-children.md|09]]

## Comments

- 2026-09-20 — plan-or-doc-change: owning layer architecture **Actor live labels**; ticket consumes it. Failed review on hardcoded AI chip; Status stays `coded`.
- 2026-09-19 — Reconciled as active Actor chrome frontier after llm-connector vertical done. Cleared stale Blocked by (pool / credential). Status `defined`.
- 2026-09-19 — Expanded What to build: Command-response Event apply, start result “Run: AI started.”, Poll stop/error result conveyance, boot live chrome. DLL provider error naming stays out of this ticket.
- 2026-09-19 — Implemented live Focus projection on Poll / response Event apply and `actor-live` row chrome. Status `coded`.
- 2026-09-19 — Review fix: Command POST applies Events (`Run: AI started.`); `/state` seeds `liveFocusIds` from GetState lockPresent overlay (mailbox live table, not a Graph persist field); ActorStop Succeeded/Failed/Cancelled set lastCmdResult; `amb-actor-live`; `AppliedBrowserGraph`. Status `coded`.
- 2026-09-19 — Independent re-review Good; squash-landed on staging. Status `done`.

## Time

- 2026-09-19 2h — Client live Actor projection and Focus row chrome (from chat)
- 2026-09-19 1.5h — Command apply, boot live seed, lastCmdResult, module split (from chat)
- 2026-09-19 0.5h — Expanded 21 conveyance (Succeeded result + ticket text from staging)
