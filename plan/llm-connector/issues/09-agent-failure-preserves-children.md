# 09 — Agent failure preserves children

**Status:** done
**Blocked by:** None — framework proof uses TestActor / Command `?test` (Story path **TestActor hello** already delivered). Section **AI Actor does not erase data** stays out of this ticket until [[08-agent-ask-from-what-i-see.md|08 — Run Agent Actor calls CloudAgents]] and [[12-replace-focus-children-from-reply.md|12 — Replace Focus Children from reply]] land.
**Type:** task
Actual: 1h30m

## Context

Implements Story path **Agent failure preserves children** in [[arch.md|llm-connector architecture]], **framework** half first.

**Rule (this Project):** On failure, the Actor *framework* does not cause Changes. The AI Actor does not erase data. Future agentic extensions that might intentionally mutate on failure are out of scope and not defined here.

This ticket proves the framework rule with TestActor: seed Focus Children, Run a failing `?test` Command (non-hello text → ActorFailed), assert Children unchanged and no failure Change. It does **not** implement AI Actor erase-on-failure behavior; that waits on the Agent ask path.

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

### 3. Out of scope here (after Agent ask)

Leave for a follow-on once [[08-agent-ask-from-what-i-see.md|08 — Run Agent Actor calls CloudAgents]] and [[12-replace-focus-children-from-reply.md|12 — Replace Focus Children from reply]] exist:

1. AI Actor does not erase data on CloudAgents Failed — no Focus wipe, no Error Graph text from the Agent body.
2. Fake CloudAgents Failed outcome as the Agent-specific proof.

## See also

[[arch.md|llm-connector architecture]], [[08-agent-ask-from-what-i-see.md|08 — Run Agent Actor calls CloudAgents]], [[12-replace-focus-children-from-reply.md|12 — Replace Focus Children from reply]], [[06-define-command-run-agent-redesign.md|06]], [[07-lock-run-agent-architecture.md|07]]

## Comments

- 2026-09-19 — Alan: Actor framework shall not cause Changes on failure; AI Actor will not erase data (barring future agentic extensions not defined now).
- 2026-09-19 — Alan: use `?test` to implement the framework proof first; do not fold AI-Actor erase into this ticket (that waits on Agent ask / replace).
- 2026-09-19 — Alan: secrets are not observable; drop is proved via live Focus ids, not Credential/`isLive`.
- 2026-09-19 — Framework half done: `?test unknown` → ActorFailed, no Change, Focus Children preserved. Proof in [TestActorCommandErrorTests](../../../tests/Server.Tests/TestActorCommandErrorTests.fs). Independent review Approve with nits; secret-observation nit dismissed (not an observation surface). Section **AI Actor does not erase data** remains open.
- 2026-09-19 — Squash-landed on staging after review.

## Time

- 2026-09-19 1h30m — TestActor Focus-children preserve proof; framework Failed posts no Change (from chat)
