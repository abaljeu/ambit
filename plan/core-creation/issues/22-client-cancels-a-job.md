# 22 — Client cancels a job

**Status:** defined
**Blocked by:** [[21-client-shows-lock-present.md|21 Client shows live Actor (active chrome)]] — Server cancel path delivered ([[plan/llm-connector/issues/10-cancel-by-focus.md|10]]); [[17-cancel-a-job.md|17]] Status catch-up separate.

## Context

A person in the Browser stops a running job on a Focus that shows live Actor chrome ([[21-client-shows-lock-present.md|21]]). Cancel is not Undo. Server already cancels by Focus NodeId (llm-connector [[plan/llm-connector/issues/10-cancel-by-focus.md|10]]).

Sequence after [[21-client-shows-lock-present.md|21]]: live chrome visible → user cancels → Server Cancel → `ActorStop` Cancelled → chrome off → Cancelled (or Error) result message via Poll conveyance from 21.

## What to build

1. [ ] **Cancel control** — While a Focus is live (chrome from [[21-client-shows-lock-present.md|21]]), the Browser offers cancel for that Focus.
2. [ ] **Send cancel** — Client sends cancel by Focus NodeId (existing Server door).
3. [ ] **After cancel** — Later Actor output is refused; earlier merged Changes stay; chrome clears and stop result shows through [[21-client-shows-lock-present.md|21]] Poll conveyance (`ActorStop` Cancelled).
4. [ ] **Non-goals** — No Undo; no new Server cancel semantics; no DLL error catalog; no start-result / projection work (owned by [[21-client-shows-lock-present.md|21]]).

## See also

[[21-client-shows-lock-present.md|21 — Client shows live Actor (active chrome)]], [[10-define-actor-cancellation-and-output-admission.md]], [[plan/llm-connector/issues/10-cancel-by-focus.md|10 — Cancel by Focus]]

## Comments

- 2026-09-19 — Server cancelByFocus landed on llm-connector [[plan/llm-connector/issues/10-cancel-by-focus.md|10]]. This ticket remains Client cancel chrome; blocked by [[21-client-shows-lock-present.md|21]].
- 2026-09-19 — Expanded: cancel control on live chrome, Poll/chrome/result conveyance reuse from 21. Status `defined`.
