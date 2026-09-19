# 13 — Vertical proof: Browser Ask from what I see

**Status:** done
**Blocked by:** None — [[08-agent-ask-from-what-i-see.md|08]], [[09-agent-failure-preserves-children.md|09]], [[10-cancel-by-focus.md|10]], and [[11-simple-extract-format.md|11]] are `done`.
**Type:** task
Actual: 1h

## Context

Arch locked **Vertical proof timing**: define the full Browser → Run Agent → Focus-children proof after the first CloudAgents / Run Agent implement tickets are `defined`. Those tickets are now `done`. This Project’s first Agent vertical is **Graph + Poll only** — no live-Actor chrome ([[plan/core-creation/issues/21-client-shows-lock-present.md|21]], [[plan/core-creation/issues/22-client-cancels-a-job.md|22]]).

Client Command already builds `ActorStart` the same way for any `?name` path (`CommandRequest.oneNodeStart` + `SubmitCommand` POST `/command`), including `?test hello`. Launch payload generation is not new work. This ticket proves the Ask path end-to-end the way a Browser session would: Run `?ai`, complete through CloudAgents (`setFake` for a stable proof; live key optional), Poll until Focus Children and ActorFinished match success.

## What to build

A person (or Browser-shaped harness) Runs Command text `?ai` under Zoom/Focus with Included `graphIds`. After completion, Poll / Graph shows new Focus Children from the Agent reply; ActorStarted then ActorFinished appear; the live Focus id is gone from `liveFocusIds`. No Browser live-Actor UI required.

### 1. Proof path

1. [x] Launch — Browser or harness submits `ActorStart` for `?ai` (same Client encode as `?test`); Server starts the Run Agent Actor.
2. [x] Complete — CloudAgents finishes via `setFake` (deterministic Finished text); live Cursor optional extra.
3. [x] Poll Focus Children — after success, Poll / Graph shows the new Focus Children under Focus.
4. [x] Lifecycle — ActorStarted and ActorFinished on the EventLog; Focus id gone from `liveFocusIds` (secrets are not an observation surface).

### 2. Non-goals

- Live-Actor chrome (core-creation 21/22).
- New Client `ActorStart` encode (already shared with `?test`).
- Mixed-format owning-codec pack; nested-tag format.
- Cancel or Failed paths (covered by 09 / 10).

## See also

[[arch.md|llm-connector architecture]], [[map.md]], [[08-agent-ask-from-what-i-see.md|08 — Agent ask from what I see]], [[11-simple-extract-format.md|11 — Pack extract with Amb (supplied-fragment walk)]], [[09-agent-failure-preserves-children.md|09]], [[10-cancel-by-focus.md|10]], [[06-define-command-run-agent-redesign.md|06]], [[07-lock-run-agent-architecture.md|07]]

## Comments

- 2026-09-19 — Landed on staging after independent review Good (Poll Focus Children from Agent replace, not seed; map Implementation).
- 2026-09-19 — Filed after implement slice 08–11 done. Alan: Client call shape already matches `?test hello`; vertical proof is launch → complete → Poll Focus Children, not a new encode.

## Time

- 2026-09-19 1h — Browser-shaped `?ai` launch via `CommandRequest.oneNodeStart`, `setFake` Finished, Poll Focus Children, ActorStarted then ActorFinished, `liveFocusIds` drop (from chat)
