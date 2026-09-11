# 06 — Define the revised Command + Run Agent seam

**Type:** grilling
**Status:** done
Blocked by:
Actual: 1h5m

## Context

The locked September redesign withdrew [[plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md]] and returned this Project to `chart`. This issue defines the replacement contract. It does not implement the Actor, Core lifecycle, or a provider adapter.

## Answer

### Invocation and request envelope

- The Command Node's text is the dispatch. `?test echo` launches TestActor echo. `?ai ...` will launch the Agent Actor; payload details later. Command role, Kind, and CSS class do not select which Actor to run. Command Nodes do not create a separate provider instruction channel.
- One Run request carries one Graph subgraph with one root, Zoom, and exactly one Focus Node inside it.
- The Actor serializes that Graph as model context. The model does not maintain Graph structure or ownership.

### Mixed-format Agent protocol

- Each Node encodes through the codec of its owning document. Subdocument serialization must therefore have access to that codec. The result is one mixed-format document, and the Agent is trusted to understand the formatting.
- The document codec formats Command Nodes as prompt-like content, but it does not extract Command Nodes into a separate instruction.
- The vendor-neutral CloudAgents request contains a system prompt plus the mixed-format document extract. A format-neutral, transport-only wrapper such as `<prompt>...</prompt>` marks Focus. The system prompt asks the Agent to return text for insertion below Focus. Exact sentinel spelling and escaping are implementation details.
- CloudAgents remains the standalone vendor-neutral project and API. Cursor is an ordinary adapter with no special domain behavior. Provider selection is outside this issue.

### Response and apply

- On success, Focus is the stable replacement boundary. Delete every current Child under Focus and create new Children from the complete returned response. There is no response marker, response NodeId metadata, or retained generated-Node set. No Node outside Focus is directly changed.
- First attempt the structural parse used for insertion below Focus. If the complete response does not conform, discard that parse atomically and parse the complete response as a plain-text indentation outline. Never keep a partial structural parse plus residue.
- Completion emits ordinary Core Changes. Existing Core Change merge and amendment own concurrent-edit reconciliation. This issue adds no stale check, hard edit refusal, last-writer policy, or structural overlap check.

### Admission, failure, and cancellation

- At most one live Actor may target a Focus NodeId. Reject a second launch for the same Focus. Actors with different Focus NodeIds may always run concurrently, even when their Zoom extracts overlap or nest.
- Provider or Agent failure preserves all Focus Children and emits no response or failure Change. [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] records safe failure in ActorFinished; raw provider details remain logs. Never turn the Error into Graph text or another Agent call.
- User cancellation preserves all Focus Children, emits no Error and no Change, and clears running state through terminal ActorFinished and drop. The Browser cancels by Focus NodeId. Public actor identity remains durable.

### Scope

- Durability and format persistence are already implemented and are outside this issue.
- Core merge behavior is already implemented and is outside this issue.
- The cancelled [[plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md]] Create payload, Focus-only lock, Md paste-replace implementation, and vertical proof do not return.

## See also

[[plan/llm-connector/reports/agent-redesign-locked-2026-09.md]], [[plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md]], [[plan/core-creation/issues/Implementation Planning and Record.md]]

## Comments

- 2026-09-11 — Locked the revised Agent seam and summarized it in [[../map.md]].
- 2026-09-11 — [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] added durable ActorStarted and ActorFinished lifecycle Events without changing the Graph response behavior locked here.
- 2026-09-11 — Command Node text is the Actor dispatch (`?test echo` / `?ai ...`). This replaces Command-role, Kind, and CSS selection.

## Time

- 2026-09-11 10m — reconcile locked redesign and stub the first charting gate (from chat)
- 2026-09-11 50m — grill and lock the revised Run envelope, Agent protocol, response, overlap, failure, and cancellation contracts (from chat)
- 2026-09-11 5m — record Command-text Actor dispatch (from chat)
