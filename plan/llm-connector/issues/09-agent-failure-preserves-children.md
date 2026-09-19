# 09 — Agent failure preserves children

**Status:** done
**Blocked by:** None
**Type:** task
Actual: 2h45m

## Context

Implements Story path **Agent failure preserves children** in [llm-connector architecture](../arch.md).

**Rule (this Project):** On failure, the Actor *framework* does not cause Changes. The AI Actor does not erase data. Future agentic extensions that might intentionally mutate on failure are out of scope and not defined here.

Framework half: seed Focus Children, Run a failing `?test` Command (non-hello text → ActorFailed), assert Children unchanged and no failure Change.

AI half: seed Focus Children, Run `?ai` with CloudAgents `setFake` that yields Failed (not Finished). Focus Children stay; no Focus wipe; no Error Graph text from the Agent body; no erase or response Change from the failed Ask. Observe drop via `liveFocusIds` (secrets are not an observation surface).

## What to build

### 1. Framework does not cause Changes

1. [x] Terminal Failed only — queue Failed → ActorFinished and drop the live Actor row; do not post any Change from the framework on this path. Observe drop via live Focus ids (`liveFocusIds`); secrets are not an observation surface.
2. [x] Safe error only — ActorFinished records a safe domain error; no raw provider payload in Graph Events.

### 2. Proof (TestActor / `?test`)

1. [x] Seed Focus Children — under Focus, create at least one Child before the failing Run.
2. [x] Fail with `?test` — Run Command text that selects TestActor and ends ActorFailed (e.g. `?test unknown`); fake CloudAgents not required.
3. [x] Preserve children — Focus Children match the pre-failure set.
4. [x] No failure Changes — EventLog / Graph show no erase or response Change from the failed run.
5. [x] Observe ActorFinished — safe failure terminal present; Focus id gone from `liveFocusIds` (not a secret check).

### 3. AI Actor does not erase data

1. [x] Seed Focus Children — under Focus, create Children before the failing Ask.
2. [x] Fail with `?ai` — CloudAgents `setFake` yields Failed (not Finished).
3. [x] Preserve children — Focus Children match the pre-failure set (ids and text).
4. [x] No Focus wipe — do not delete or replace Focus Children on Failed.
5. [x] No Error Graph text — do not write provider or failure prose from the Agent body into the outline.
6. [x] No failure Changes — EventLog / Graph show no erase or response Change from the failed Ask.
7. [x] Observe drop — Focus id gone from `liveFocusIds` (not a secret check).

## See also

[llm-connector architecture](../arch.md), [08 — Agent ask from what I see](08-agent-ask-from-what-i-see.md), [12 — Replace Focus Children from reply](12-replace-focus-children-from-reply.md) (cancelled; Amb replace landed on 08), [06 — Define the revised Command + Run Agent seam](06-define-command-run-agent-redesign.md), [07 — Lock the Run Agent architecture](07-lock-run-agent-architecture.md)

## Comments

- 2026-09-19 — Landed AI-Actor Failed preserve on staging after Good.
- 2026-09-19 — Alan: Actor framework shall not cause Changes on failure; AI Actor will not erase data (barring future agentic extensions not defined now).
- 2026-09-19 — Alan: use `?test` to implement the framework proof first; do not fold AI-Actor erase into this ticket (that waits on Agent ask / replace).
- 2026-09-19 — Alan: secrets are not observable; drop is proved via live Focus ids, not Credential/`isLive`.
- 2026-09-19 — Framework half done: `?test unknown` → ActorFailed, no Change, Focus Children preserved. Proof in [TestActorCommandErrorTests](../../../tests/Server.Tests/TestActorCommandErrorTests.fs). Independent review Approve with nits; secret-observation nit dismissed (not an observation surface).
- 2026-09-19 — Squash-landed on staging after review.
- 2026-09-19 — AI erase proof done: `?ai` + `setFake` Failed leaves Focus Children (ids and text), posts no erase/response Change, writes no Agent-body Error text, drops via `liveFocusIds`. [08 — Agent ask from what I see](08-agent-ask-from-what-i-see.md) landed; [12 — Replace Focus Children from reply](12-replace-focus-children-from-reply.md) cancelled (Amb replace on 08). Proof in [AgentFailurePreserveTests](../../../tests/Server.Tests/AgentFailurePreserveTests.fs). Status `coded`.

## Time

- 2026-09-19 1h30m — TestActor Focus-children preserve proof; framework Failed posts no Change (from chat)
- 2026-09-19 1h15m — CloudAgents `setFake` Failed yield; AI Actor Failed preserve proof (from chat)
