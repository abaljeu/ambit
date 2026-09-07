# First Agent: Cursor Cloud Agents (thin)

Date: 2026-09-07

Locked for implementers (Cursor / cloud coding agents). Supersedes the earlier “Grok Bot / Get API key” reading of [[../issues/02-which-llm-and-credentials.md]] for the **first** outbound Agent.

## Target

- **First Agent** = Cursor **Cloud Agents API** (https://cursor.com/docs/cloud-agent/api/endpoints), not Grok Bot desktop agents, not a raw xAI/OpenAI chat key for v1.
- **Primary repo** = `life` (markdown / plan), not a traditional app codebase. Ambit outline is the UI; Cloud Agent edits `life`.
- **Billing** = Cursor plan / Cloud Agents usage (Dashboard API key). Cursor surplus does **not** transfer to `api.x.ai`. A plain LLM Actor may come later for cheap Q&A.

## Ambit seam (unchanged)

- Spoken: Run Agent. Focus: `?` + message, then Run.
- Browser: async `POST /ambit/actors` with `{ actor, nodelist, focusnode, rootnode, revision }` + session cookie.
- Actor: Md-write pack → call Agent → reply Md→graph → `postChange` Owned children of Focus.
- Core finish/drop: [[plan/core-creation/issues/18-finish-and-drop.md]] (landed). First usable Run Agent needs no cancel.

## Minimal Cloud client (v1)

Do **not** implement the full Cloud Agents surface (pools, MCP inject, artifacts, worker claim, etc.).

1. `POST /v1/agents` with `prompt.text` built from `?` message + Md pack (and a short brief for “what’s next” / “work on that”).
2. For work on `life`: `repos: [{ url: <life git url>, startingRef: … }]`. Omit `repos` only if a no-repo ask is enough.
3. Auth: Cursor Dashboard **API key** as process/env secret bundled with the Actor at register time. Core never sees it.
4. Poll `GET /v1/agents/{id}/runs/{runId}` until terminal; use `result` (and PR URL from `git` if present). **No streaming in v1.**
5. Post that text into the outline as Owned children of Focus.

## Launch data vs composition

**From Ambit launch (enough for the prompt):** actor name, nodelist, focus, root, revision, cookie; message from Focus Header; pack = subgraph Md.

**Bundled with the Actor (not Graph fields):** Cursor API key; default `life` repo URL (+ optional startingRef); optional default `model.id`.

**Job memory only:** Cloud `agentId` / `runId` for poll (and later cancel).

## Cancel (later, not first usable)

- Core: mailbox cancel (issues 10/17) + [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]].
- Actor on cancel: `POST /v1/agents/{id}/runs/{runId}/cancel`, then stop polling/reading.
- Do not treat “kill thread / abort HTTP” as the protocol; remote cancel is authoritative.

## Stream (later)

SSE `…/runs/{runId}/stream` exists; defer until partial outline updates are wanted.

## OpenAI-compatible

Not a requirement. Was only a shopping note for plain LLM hosts. First Agent is Cloud Agents HTTP as documented by Cursor.

## Related

- Map: [[../map.md]]
- Credentials grill (amended): [[../issues/02-which-llm-and-credentials.md]]
- Seam: [[../issues/03-seam-after-ask-recognition.md]]
- Spine: [[../issues/04-how-much-eso-before-first-ask.md]]
