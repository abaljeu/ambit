# 09 — Agent failure preserves children

**Status:** defined
**Blocked by:** None — can start immediately. Framework proof uses TestActor / Command `?test` (Story path **TestActor hello** already delivered). Section **AI Actor does not erase data** stays out of this ticket until [[08-agent-ask-from-what-i-see.md|08 — Run Agent Actor calls CloudAgents]] and [[12-replace-focus-children-from-reply.md|12 — Replace Focus Children from reply]] land.
**Type:** task

## Context

Implements Story path **Agent failure preserves children** in [[arch.md|llm-connector architecture]], **framework** half first.

**Rule (this Project):** On failure, the Actor *framework* does not cause Changes. The AI Actor does not erase data. Future agentic extensions that might intentionally mutate on failure are out of scope and not defined here.

This ticket proves the framework rule with TestActor: seed Focus Children, Run a failing `?test` Command (non-hello text → ActorFailed), assert Children unchanged and no failure Change. It does **not** implement AI Actor erase-on-failure behavior; that waits on the Agent ask path.

## What to build

### 1. Framework does not cause Changes

1. [ ] Terminal Failed only — queue Failed → ActorFinished and drop live row/secret; do not post any Change from the framework on this path.
2. [ ] Safe error only — ActorFinished records a safe domain error; no raw provider payload in Graph Events.

### 2. Proof (TestActor / `?test`)

1. [ ] Seed Focus Children — under Focus, create at least one Child before the failing Run.
2. [ ] Fail with `?test` — Run Command text that selects TestActor and ends ActorFailed (e.g. `?test unknown`); fake CloudAgents not required.
3. [ ] Preserve children — Focus Children match the pre-failure set.
4. [ ] No failure Changes — EventLog / Graph show no erase or response Change from the failed run.
5. [ ] Observe ActorFinished — safe failure terminal present; live row gone.

### 3. Out of scope here (after Agent ask)

Leave for a follow-on once [[08-agent-ask-from-what-i-see.md|08 — Run Agent Actor calls CloudAgents]] and [[12-replace-focus-children-from-reply.md|12 — Replace Focus Children from reply]] exist:

1. AI Actor does not erase data on CloudAgents Failed — no Focus wipe, no Error Graph text from the Agent body.
2. Fake CloudAgents Failed outcome as the Agent-specific proof.

## See also

[[arch.md|llm-connector architecture]], [[08-agent-ask-from-what-i-see.md|08 — Run Agent Actor calls CloudAgents]], [[12-replace-focus-children-from-reply.md|12 — Replace Focus Children from reply]], [[06-define-command-run-agent-redesign.md|06]], [[07-lock-run-agent-architecture.md|07]]

## Comments

- 2026-09-19 — Alan: Actor framework shall not cause Changes on failure; AI Actor will not erase data (barring future agentic extensions not defined now).
- 2026-09-19 — Alan: use `?test` to implement the framework proof first; do not fold AI-Actor erase into this ticket (that waits on Agent ask / replace).
