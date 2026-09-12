# Check issue 06 completed details

Date: 2026-09-11
Issue: [[../issues/06-define-command-run-agent-redesign.md]]

Issue 06 is a done grilling record. Every Answer checkbox is a locked contract detail, not an implementation task. All sixteen boxes are `[x]`.

## Invocation and request envelope

- Command Node text is the dispatch; `?test hello` launches TestActor hello; `?ai ...` will launch the Agent Actor later; role, Kind, and CSS class do not select the Actor; Command Nodes do not create a separate provider instruction channel — `[x]`. This ticket already locked that wording. The 29 locator is removed from the bullet. `?ai ...` payload remaining later does not leave the lock incomplete.
- One Run request carries one Graph subgraph with one root, Zoom, and exactly one Focus Node — `[x]`. Locked on this ticket.
- The Actor serializes that Graph as model context; the model does not maintain Graph structure or ownership — `[x]`. Locked on this ticket.

## Mixed-format Agent protocol

- Each Node encodes through the codec of its owning document; one mixed-format document — `[x]`. Locked on this ticket.
- The document codec formats Command Nodes as prompt-like content and does not extract a separate instruction — `[x]`. Locked on this ticket.
- CloudAgents request is system prompt plus mixed-format extract; transport-only Focus wrapper; return text for insertion below Focus — `[x]`. Locked on this ticket. Sentinel spelling remains an implementation detail and does not leave the lock incomplete.
- CloudAgents stays standalone; Cursor is an ordinary adapter; provider selection is outside this issue — `[x]`. Locked on this ticket.

## Response and apply

- On success, replace every current Child under Focus from the complete response; no Node outside Focus is directly changed — `[x]`. Locked on this ticket.
- First attempt the structural parse; if it does not conform, discard it atomically and parse the complete response as a plain-text indentation outline — `[x]`. Locked on this ticket.
- Completion emits ordinary Core Changes; this issue adds no stale check or last-writer policy — `[x]`. Locked on this ticket.

## Admission, failure, and cancellation

- At most one live Actor per Focus NodeId; different Focus values may run concurrently — `[x]`. Locked on this ticket.
- Provider or Agent failure preserves Focus Children and emits no response or failure Change — `[x]`. Locked on this ticket. [[../issues/07-lock-run-agent-architecture.md]] records the ActorFinished failure path.
- User cancellation preserves Focus Children, emits no Error and no Change, and clears running state through ActorFinished and drop — `[x]`. Locked on this ticket.

## Scope

- Durability and format persistence are already implemented and outside this issue — `[x]`. Standing product fact.
- Core merge behavior is already implemented and outside this issue — `[x]`. Standing product fact.
- The cancelled issue 05 Create payload, Focus-only lock, Md paste-replace implementation, and vertical proof do not return — `[x]`. Locked withdrawal on this ticket.

## Gap left unspecified

Alan asked how `?test hello` specifies that test is the Actor and hello is the command. Issue 06 does not lock that split. The locked fact is only that the Command Node's text is the dispatch, and that those two full strings launch those Actors. This report does not add a parse or grammar to 06.
