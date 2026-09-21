# 22 — Client cancels a job

**Status:** done
**Actual:** 2h30m
**Blocked by:** None — [21 — Client shows live Actor](21-client-shows-lock-present.md) Status `done`. Server cancel path delivered ([10 — Cancel by Focus](../../llm-connector/issues/10-cancel-by-focus.md)). [17 — Cancel a job](17-cancel-a-job.md) Status catch-up separate.

## Context

A person in the Browser stops a running job on a Focus that shows live Actor chrome ([21 — Client shows live Actor](21-client-shows-lock-present.md)). Cancel is not Undo. Server already cancels by Focus NodeId (llm-connector [10 — Cancel by Focus](../../llm-connector/issues/10-cancel-by-focus.md)).

Sequence after [21 — Client shows live Actor](21-client-shows-lock-present.md): live chrome visible → user cancels → Server Cancel → `ActorStop` Cancelled → chrome off → Cancelled (or Error) result message via Poll conveyance from 21.

## What to build

1. [x] **Cancel control** — While a Focus is live (chrome from [21 — Client shows live Actor](21-client-shows-lock-present.md)), the Browser offers cancel for that Focus.
2. [x] **Send cancel** — Client sends cancel by Focus NodeId (existing Server door).
3. [x] **After cancel** — Later Actor output is refused; earlier merged Changes stay; chrome clears and stop result shows through [21 — Client shows live Actor](21-client-shows-lock-present.md) Poll conveyance (`ActorStop` Cancelled).
4. [x] **Non-goals** — No Undo; no new Server cancel semantics; no DLL error catalog; no start-result / projection work (owned by [21 — Client shows live Actor](21-client-shows-lock-present.md)).

## See also

[21 — Client shows live Actor](21-client-shows-lock-present.md), [10 — Define Actor cancellation and output admission](10-define-actor-cancellation-and-output-admission.md), [10 — Cancel by Focus](../../llm-connector/issues/10-cancel-by-focus.md)

## Comments

- 2026-09-19 — Independent review Good; squash-landed on staging. Status `done`.
- 2026-09-19 — Server cancelByFocus landed on llm-connector [10 — Cancel by Focus](../../llm-connector/issues/10-cancel-by-focus.md). This ticket remains Client cancel chrome; blocked by [21 — Client shows live Actor](21-client-shows-lock-present.md).
- 2026-09-19 — Expanded: cancel control on live chrome, Poll/chrome/result conveyance reuse from 21. Status `defined`.
- 2026-09-19 — Implemented live-row Cancel control, palette/key Cancel while focused Focus is live, POST `/ambit/cancel` by Focus NodeId through [CoreMailbox.cancelByFocus](../../../src/Server/Core/CoreMailbox.fs). Chrome and Cancelled result reuse 21 Poll conveyance. Status `coded`.

## Time

- 2026-09-19 2h30m — Browser cancel control, HTTP cancel door, Client send, focused tests (from chat)
