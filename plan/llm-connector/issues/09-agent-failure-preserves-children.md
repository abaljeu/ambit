# 09 — Agent failure preserves children

**Status:** defined
**Blocked by:** [[08-agent-ask-from-what-i-see.md|08 — Agent ask from what I see]]
**Type:** task

## Context

Implements Story path **Agent failure preserves children** in [[arch.md|llm-connector architecture]].

**Rule (this Project):** On failure, the Actor *framework* does not cause Changes. The AI Actor does not erase data. Future agentic extensions that might intentionally mutate on failure are out of scope and not defined here.

## What to build

Force a CloudAgents Failed outcome (fake preferred). Focus Children are unchanged. The framework path to ActorFinished / drop posts no Change (no Focus-child replacement, no Error text in the Graph). ActorFinished carries a safe domain error only; raw provider details stay in logs.

Earlier accepted Changes from a prior successful path are a different case — this ticket is about failure before a successful response Change.

### 1. Framework does not cause Changes

1. [ ] Terminal Failed only — queue Failed → ActorFinished and drop live row/secret; do not post any Change from the framework on this path.
2. [ ] Safe error only — ActorFinished records a safe domain error; no raw provider payload in Graph Events.

### 2. AI Actor does not erase data

1. [ ] No Focus wipe — do not delete or replace Focus Children on Failed.
2. [ ] No Error Graph text — do not write provider or failure prose into the outline.

### 3. Proof

1. [ ] Preserve children — Focus Children match the pre-failure set.
2. [ ] No failure Changes — EventLog / Graph show no response or erase Change from the failed run.
3. [ ] Observe ActorFinished — safe error present; live row gone.

## See also

[[arch.md|llm-connector architecture]], [[08-agent-ask-from-what-i-see.md|08 — Agent ask from what I see]], [[06-define-command-run-agent-redesign.md|06]], [[07-lock-run-agent-architecture.md|07]]

## Comments

- 2026-09-19 — Alan: Actor framework shall not cause Changes on failure; AI Actor will not erase data (barring future agentic extensions not defined now).
