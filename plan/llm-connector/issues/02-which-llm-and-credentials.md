# Which LLM and where credentials live

Type: grilling
Status: done
Blocked by:
Actual: 55m

## Question

Which Agent does the first agent-invoking Actor call, and where do credentials live? Decide provider and secret placement. Do not implement the connector in this ticket. The user action that launches the Actor is postponed.

## Answer

The first agent-invoking Actor calls Grok Bot. Get API key is for outbound send. Cursor Cloud Agent is not the first target. The user action that launches the Actor is postponed. Pipeline: that action launches the Actor; the Agent receives context; the Actor receives the response; the Actor posts to the Graph (`postChange`). Operate on a connected channel is log in, fetch, and send — not receive-only posts.

First context is the launch subgraph. Included context (SiteMap under Zoom, honoring Fold) waits on the postponed UI action. Pack shape is [[01-how-the-pack-is-encoded.md]]. Named may-change: Included context instead of subgraph.

The Grok Bot API key is a Server process secret for the current one-user Server (Alan). At rest it is an environment variable or host secret, not DataDir. The Actor is responsible for the key. Core and other Server code do not check or inject it. Composition may register a bundle of the Actor function plus that constant data. Missing key is the Actor's failure when it sends, not a Core launch refuse. The key is not in the Graph, not in the Browser cookie, and not a Core Graph-post `Credential`. **FIXME before publishing:** a second user must provide their own key; a shared process secret must not ship as the multi-user design. Secondary logins (Agent or Zapier-like access to other services) were not planned; they may be planned later.

## Comments

- 2026-09-02: Filed unclaimed from WORK.md. Map: [[../map.md]].
- 2026-09-06: Grilling started from chat. Launch is not committed to `?` ([[03-seam-after-ask-recognition.md]], [[plan/expression-language/issues/33-recognize-ask-run-statement.md]]).
- Fact: Gambol has no Grok Bot or Cloud Agent connector code. Ultra only means those products exist as targets.
- Glossary: [[CONTEXT.md]] **Agent**, not Grok as the kind name. Core `Credential` is a Graph-post sender, not an LLM API key.
- Secret placement: two kinds. (1) Grok Bot API key — Ambit Actor authenticates to the Agent. (2) Secondary logins — the Agent (or a Zapier-like connector) logs into other web services. (2) was not planned; it may be planned later. It is not this ticket's first lock.
- Q1: The user does some action (postponed). That launches an agent-invoking Actor. The Agent receives context. The Actor receives the response. The Actor posts to the Graph (`postChange`).
- Q2: First Agent is Grok Bot. The product has Get API key. That key is for outbound send from the Actor (and the connected-channel pattern is log in, fetch, and send — not receive-only posts onto the Graph). Cursor Cloud Agent is not the first target. The agent-chat read of [[plan/roadmap/epics/operate-connected-channels.md]] Notes as inbound posts was a misread of operate.
- Q3: First call sends the launch subgraph. Included context (SiteMap under Zoom, honoring Fold) waits on the postponed UI action. Pack shape is [[01-how-the-pack-is-encoded.md]]. Named may-change: Included context instead of subgraph — Alan could lean either way.
- Q4: The Grok Bot API key is a Server process secret for the current one-user Server (Alan). The Actor reads it. Not in the Graph, not in the Browser cookie, not a Core Graph-post `Credential`. **FIXME before publishing:** a second user must provide their own key; a shared process secret must not ship as the multi-user design.
- Q5: At rest the key is an environment variable or host secret. Not DataDir ([[plan/daily-git-save/project.md]] commits App DataDir).
- Q6: The Actor is responsible for its key. Core and other Server code do not check or inject it. If needed, composition registers a bundle: the Actor function plus its constant data (the key). Missing key is the Actor's failure when it sends, not a Core launch refuse.
- Q7: Lock. Answer written.
- 2026-09-06: Later grill ([[01-how-the-pack-is-encoded.md]], [[03-seam-after-ask-recognition.md]]): executing Run Agent (`?` then Run) is the launch. Postponed "launch action" is extra UI (credentials picker, secondary logins), not the Run path. The pack is Included context, superseding this ticket's subgraph-first default for Run Agent.

## Time

- 2026-09-06 10m — started grill; recorded Ultra targets and no in-repo connector (from chat)
- 2026-09-06 10m — Q1: postponed user action launches Actor; Agent gets context; Actor posts reply (from chat)
- 2026-09-06 10m — Q2: Grok Bot API key, outbound send; connected channel is fetch and send (from chat)
- 2026-09-06 5m — Q4 split: Grok Bot API key vs later secondary/Zapier logins (from chat)
- 2026-09-06 5m — Q3 subgraph default (may-change Included context); Q4 Server process secret (from chat)
- 2026-09-06 5m — Q4 FIXME: process secret is one-user only; second user must supply a key before publishing (from chat)
- 2026-09-06 5m — Q5 env not DataDir; Q6 Actor owns key via function+constants bundle (from chat)
- 2026-09-06 5m — Q7 lock; wrote Answer (from chat)
