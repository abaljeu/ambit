# 21 — Client shows live Actor (active chrome)

**Status:** defined
**Blocked by:** None — Server lifecycle Events and Browser Run (`?test` / `?ai`) are delivered. Credentialed Browser posts are `done` ([[20-client-presents-credential.md|20]]). Historical blockers [[02-core-actor-pool.md|02]] and Graph lock-present are superseded.

## Context

A person needs to see that an Actor is live for a Focus. The Graph lock-present field and span display are superseded by durable lifecycle Events ([[plan/llm-connector/issues/07-lock-run-agent-architecture.md|07]]). Issue title "lock-present" is historical; this ticket is **active Actor chrome**.

Server already emits `ActorStart` / `ActorStop` on the EventLog; Poll carries them. Browser Command encode already posts `ActorStart` ([[35b-browser-run-hello.md|35b]], llm-connector vertical proof). This ticket is Client projection and visible chrome only — no new Server door, no Graph field, no second History.

## What to build

1. [ ] **Projection** — Client tracks live Focus ids from `EventBody.ActorStart` / `EventBody.ActorStop` applied through normal Poll / response Event apply (same path as Graph Changes). After `ActorStart` for a Focus, that Focus is live; after `ActorStop` for that Focus, it is not.
2. [ ] **Chrome** — While a Focus is live, the Browser shows a clear active-Actor indicator on that Focus (row / outline). Indicator clears when the Focus leaves the live set.
3. [ ] **Non-goals** — No Graph lock-present field; no span lock; no separate History/audit UI; no Cancel UI ([[22-client-cancels-a-job.md|22]]); no live registry Poll beyond Events already on the wire.

## See also

[[22-client-cancels-a-job.md|22 — Client cancels a job]], [[17-cancel-a-job.md|17 — Cancel a job]] (Server `cancelByFocus` delivered on llm-connector [[plan/llm-connector/issues/10-cancel-by-focus.md|10]]), [[35b-browser-run-hello.md|35b]], [[plan/llm-connector/issues/13-vertical-proof-browser-ask.md|llm-connector 13]]

## Comments

- 2026-09-19 — Reconciled as active Actor chrome frontier after llm-connector vertical done. Cleared stale Blocked by (pool / credential). Status `defined`.
