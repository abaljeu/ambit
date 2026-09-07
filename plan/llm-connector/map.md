# llm-connector

Labels: wayfinder:map

## Destination

Run Agent with a message and included context. The reply is Owned children of the focus Node. The call is a long-running Actor: launch, answers arrive while the person works, cancel stops a slow job.

## Notes

- Enables [[plan/roadmap/epics/agent-chat-managed-context.md]] Chapter **Ask from what I see**.
- This Project owns pack, LLM call, and write-back. [[plan/expression-language/issues/33-recognize-ask-run-statement.md]] only recognizes `?` as a Run statement.
- Spoken name is Run Agent. Spelling on Focus is `?` plus a message, then Run. Glossary: [[CONTEXT.md]] Run Agent, Included context, Agent. Do not say Agent for the Actor.
- Long-running Actor: [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]], [[plan/core-creation/issues/02-core-actor-pool.md]]. ESO background: [[plan/event-sourced-ops/details/actors-and-jobs.md]].

## Decisions so far

- Same Run command. Third statement `?` plus a message. Spoken name: Run Agent.
- Reply is Owned children of Focus. Actor converts reply Md→graph and adds those children.
- Included context is SiteMap under Zoom, honoring Fold. First Run Agent uses Zoom as `rootnode`. Later calls may pass another root. Nodelist is SiteMap visible Nodes below `rootnode`.
- First Agent is **Cursor Cloud Agents API** (thin client), primary repo **`life`**. Cursor Dashboard API key + life URL bundled with the Actor. Not Grok Bot agents; not required to be OpenAI-compatible. Details: [[reports/first-agent-cursor-cloud-agents.md]], [[issues/02-which-llm-and-credentials.md]]
- Extra launch UI and secondary logins stay later. Executing Run Agent launches the Actor: Browser async POST → Server launch → Actor. Not a blocking Run.
- `POST /ambit/actors` Create only. JSON `{ actor, nodelist, focusnode, rootnode, revision }`. Cookie from [[plan/core-creation/issues/20-client-presents-credential.md]]. No token field in JSON. Returns `PublicNumber` (Browser may ignore until cancel).
- ActorName selects which Actor. LLM message is the `?` remainder on Focus Header, not a LaunchRequest field.
- This Project changes extract/`LaunchRequest` to nodelist + focus + root. Do not revise [[plan/core-creation/issues/09-define-core-command-launch-contract.md]]. Extract graph is that subgraph (include `rootnode` as Md document root). Actor Md-writes it (not to a file). Codec is Md for now.
- Lock is about Changes: only Focus Changes, so lock Focus only. Extract may be larger.
- First usable Run Agent has no cancel. Actor must return by itself. Drop-on-complete waits on [[plan/core-creation/issues/18-finish-and-drop.md]]. Cancel remains on the destination as later work.

## Not yet specified

- Implement thin Cloud Agents Actor per [[reports/first-agent-cursor-cloud-agents.md]]. [[plan/core-creation/issues/18-finish-and-drop.md]] is done. Cancel is later, not first usable Run Agent. Stream is later.

## Out of scope

- Follow-up turns, LLM-authored Changes, Graph/file queries, CLI/MCP — later Chapters on the Epic.
- An Epic Project folder.
