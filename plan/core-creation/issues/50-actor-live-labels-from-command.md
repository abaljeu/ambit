# 50 — Actor live result labels from Command node

**Status:** defined
**Blocked by:** None — [21 — Client shows live Actor](21-client-shows-lock-present.md) and [22 — Client cancels a job](22-client-cancels-a-job.md) are `done`.
**Type:** bug
Estimate: 1h 30m

## Context

[21 — Client shows live Actor](21-client-shows-lock-present.md) asked for start wording that may vary by Actor (e.g. TestActor) but landed with hardcoded **AI** in [`ActorLive.lastCmdResult`](../../../src/Shared/ActorLive.fs). [14 — Provider-named AI errors](../../llm-connector/issues/14-provider-named-ai-errors.md) then scrubbed Ask → AI on the same path. Bug: Run on `?test hello` shows **“AI: …”** / **“AI: Actor succeeded.”** instead of Test-appropriate labels.

Lifecycle Events keep empty `commandName` and do not carry an Actor display name. The Client already has Graph + zoom; resolve the Command node from Focus, then label from that node’s text.

## Locked: find Command id

From the lifecycle Focus id, scan **up** the owner path toward the current **zoom root** (inclusive of Focus and zoom root). The Command node is the first node whose text’s **first character is `?`** (same rule as [`CommandRequest.isCommandText`](../../../src/Shared/CommandRequest.fs)).

- On `ActorStart`, Focus is `start.focusId`; zoom bound is `start.zoomId` (or the Client’s current zoom root when applying).
- On `ActorStop`, Focus is the stop Focus id; zoom bound is the Client’s current zoom root.
- If no `?` node is found on that path, use a generic label (not AI).

Actor select for wording: [`CommandRequest.actorNameFromText`](../../../src/Shared/CommandRequest.fs) on that Command text (`?test …` → `test`, `?ai …` → `ai`). Display labels: **Test** / **AI** / capitalize other names; unknown → **Actor**.

## What to build

### 1. Resolve + label

1. [ ] Shared helper: given `graph`, `focusId`, `zoomRoot`, return the Command `NodeId option` by scanning owner-parents upward until zoom root for `text` starting with `?`.
2. [ ] `ActorLive.lastCmdResult` takes Graph (and zoom root, or equivalent) so Start / Succeeded / Failed / Cancelled labels come from that Command, not a hardcoded AI string.

### 2. Conveyance

1. [ ] Start: `Detail (Some "Run", "<Label> started.")` — e.g. `?test` → **“Run: Test started.”**; `?ai` → **“Run: AI started.”**
2. [ ] Stop success / fail / cancel: command chip is that Label (Test / AI / …), not always AI.
3. [ ] Shared tests: `?test` and `?ai` fixtures; optional Focus-as-child-of-Command case for the scan (Focus under Command still finds the `?` ancestor).

### 3. Non-goals

1. Stamping Actor name onto Event `commandName` or extending `ActorStart` / `ActorStop` wire shape.
2. Changing chrome CSS / cancel control.
3. Provider error string content (14 stays).
4. Stream / incremental Focus writes (llm-connector 17/18).

## See also

[21 — Client shows live Actor](21-client-shows-lock-present.md), [14 — Provider-named AI errors](../../llm-connector/issues/14-provider-named-ai-errors.md), [CommandRequest](../../../src/Shared/CommandRequest.fs)

## Comments

- 2026-09-20 — Filed from live bug: `?test hello` labeled AI. No failed ticket; 21 done with hardcoded AI. Command id = scan up from Focus to zoom root for `text` first char `?`. Status `defined`.

## Time

- 2026-09-20 — Ticket (from chat)
