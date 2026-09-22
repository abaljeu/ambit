# Cloud Agents continue-conversation

Date: 2026-09-21

Write-once report. Opinion of one agent from this repo only (no Cursor docs fetch).

## Answer

No. This repo does not reuse a Cursor Cloud Agent across Asks, and it does not implement a continue-conversation or follow-up HTTP call. Each live Ask is a new `POST https://api.cursor.com/v1/agents`. In-repo Cursor surface is create, poll one run, cancel that run, and a deferred same-run SSE stream. Product **Follow-up turns** / [Talk again](plan/roadmap/epics/chapters/talk-again.md) is later Epic work, not a warm-agent skip of VM boot.

## 1. What we do today

[AgentRunner.start](src/CloudAgents/AgentRunner.fs) always creates a new pair. Live path: [CursorAdapter.startAgent](src/CloudAgents/Internal/CursorAdapter.fs) → [CursorHttp.createAgent](src/CloudAgents/Internal/CursorHttp.fs) `POST /v1/agents` with `prompt.text`, optional `name`, `model`, `repos`. Fake path: new Guids for `agentId` and `runId`. There is no public continue, follow-up, or reuse function.

[RunAgentActor](src/Server/RunAgentActor.fs) `complete` calls `AgentRunner.start` on every Ask, then `pollUntilDone` with the returned ids. The Actor does not keep `agentId` after that Ask. Core job memory in [arch](plan/llm-connector/arch.md) is **Job memory: CloudAgents agentId / runId for poll and cancel** — one job, not a process pool.

[CloudAgents README](src/CloudAgents/README.md) public face: `start` / `poll` / `cancel` / `waitUntilComplete`. [CloudAgents.Console](src/CloudAgents.Console/Program.fs) starts once, prints ids, waits, then exits. [17 — CloudAgents Console stream](plan/llm-connector/issues/17-cloudagents-console-stream.md) adds same-run SSE `GET /v1/agents/{id}/runs/{runId}/stream`; that is not a second Ask.

HTTP verbs in [CursorHttp](src/CloudAgents/Internal/CursorHttp.fs): `POST /v1/agents`, `GET /v1/agents/{agentId}/runs/{runId}`, `POST /v1/agents/{agentId}/runs/{runId}/cancel`. No `POST` onto an existing agent id.

## 2. What start returns, and discard after wait

`AgentRunner.start` returns `Result<string * string, AgentError>` = `(agentId, runId)`. [CursorAdapter](src/CloudAgents/Internal/CursorAdapter.fs) maps `response.agent.id` and `response.run.id`. Create JSON also parses `agent.latestRunId` in [CursorTypes.CursorAgent](src/CloudAgents/Internal/CursorTypes.fs); after map, that field is unused.

[RunAgentActor.complete](src/Server/RunAgentActor.fs) binds `Ok(agentId, runId)` only for `pollUntilDone`. Outcomes are `TextReady`, `CompleteFailed`, or `CompleteCancelled`. Ids do not go to Graph, EventLog, or a module cache. `waitUntilComplete` returns `AgentResult` (`Text` + `Git`), not ids. After wait, the pair is gone.

## 3. In-repo mentions (follow-up, conversation, reuse, warm, extra runs)

| Source | What it says |
| --- | --- |
| [First Agent: Cursor Cloud Agents](first-agent-cursor-cloud-agents.md) | v1: `POST /v1/agents`, poll one run, later cancel. Do not implement the full Cloud Agents surface (pools, MCP inject, artifacts, worker claim). **Job memory only** for poll and later cancel. Stream later: SSE on that run. |
| [CloudAgents README](src/CloudAgents/README.md) | Create then wait. No continue. |
| [spec](plan/llm-connector/spec.md) Out of Scope 2 | **Follow-up turns** — Multi-turn chat, Talk again, LLM-authored free Changes — later Epic Chapters. |
| [map](plan/llm-connector/map.md) | Stream tickets are not follow-up turns. Out of scope: Follow-up turns. |
| [18 — AI Actor stream](plan/llm-connector/issues/18-ai-actor-stream.md) Non-goals | Follow-up turns / multi-Ask chat (Epic later). |
| [Talk again](plan/roadmap/epics/chapters/talk-again.md) | Product: follow-up turns live in the Graph, not a one-shot `?`. No Cursor HTTP. |
| [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md) | Fit `start` / `poll` / `cancel`. Never turn errors into a second Agent call. |
| [CursorAdapter.mapStatus](src/CloudAgents/Internal/CursorAdapter.fs) | Run status `EXPIRED` → `Failed "expired"`. |

This repo does not name a continue-conversation endpoint, a warm agent, or a create-run-on-existing-agent URL. `latestRunId` on create is the only hint that one agent can have more than one run. That is a parse field, not a second-run client.

Plan “follow-up” on tickets 14–20 means later work on a done Project, not Cursor conversation reuse.

## 4. What would have to change to reuse an agent

Opinion: skip-boot needs a Cursor call this library does not have. Confirm that call outside this report (Dashboard docs). `latestRunId` only suggests extra runs are possible.

If that call exists:

1. **[CursorHttp](src/CloudAgents/Internal/CursorHttp.fs)** — add the follow-up HTTP (likely `POST` with a new prompt on an existing agent id). Keep create for a cold start.
2. **[AgentRunner](src/CloudAgents/AgentRunner.fs)** — new public function besides `start` (arch lock: keep `start` / `poll` / `cancel` / `waitUntilComplete`; do not reshape those). Fake path must accept reuse, not always mint Guids.
3. **[RunAgentActor](src/Server/RunAgentActor.fs)** — keep `agentId` after `complete` (today job-local). Next Ask: follow-up if a usable id exists, else `start`. Decide key: Focus NodeId vs process vs `AiKeys` name. Core already admits at most one **live** Actor per Focus; after ActorFinished that job memory is gone, so reuse is new state, not the current job pair.
4. **Pack** — each Ask still sends full system prompt plus full Zoom extract. Warm VM does not by itself make a delta conversation. Product Talk again is Graph-resident turns, a different design.

## 5. Caveats

1. **Expired agents.** Adapter already maps `EXPIRED`. A stored id can die between Asks. Need a cold-start fallback.
2. **Cancel.** Cancel is per run (`…/runs/{runId}/cancel`). This repo does not say whether cancel also kills the agent VM. Do not assume a cancelled run leaves a warm agent.
3. **Focus-scoped vs process-scoped.** Focus exclusivity is for one live Actor. Process-wide reuse would share one Cloud Agent across Focus nodes and keys; Focus-scoped reuse would keep one id per Focus after ActorFinished. Neither exists. A process pool is closer to the omitted “pools” surface in [First Agent: Cursor Cloud Agents](first-agent-cursor-cloud-agents.md).
4. **ModelHint.** `grok-4-6` on create does not reuse an agent. It still `POST /v1/agents`.
5. **Stream vs boot.** [17 — CloudAgents Console stream](plan/llm-connector/issues/17-cloudagents-console-stream.md) can show tokens earlier on **this** run. It does not skip boot on the **next** Ask.

## Related

[First Agent: Cursor Cloud Agents](first-agent-cursor-cloud-agents.md), [08 — Agent ask from what I see](plan/llm-connector/issues/08-agent-ask-from-what-i-see.md), [17 — CloudAgents Console stream](plan/llm-connector/issues/17-cloudagents-console-stream.md), [18 — AI Actor stream](plan/llm-connector/issues/18-ai-actor-stream.md), [Talk again](plan/roadmap/epics/chapters/talk-again.md)
