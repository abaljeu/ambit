# Lock the Run Agent architecture

**Type:** grilling
**Status:** done
Actual: 2h30m

## Context

[[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] fixed the user-facing Run Agent contract. This record locks the architecture that lets the Browser, Core, Document, Run Agent Actor, and CloudAgents implement that contract without reversing dependency direction or creating a second lifecycle model. It supersedes span locks, lock-present outside History, and the assumption that an Actor has no durable terminal result.

## Locked architecture

### Typed interfaces and dependency direction

- [x] The Browser sends a typed launch request with exact unordered membership of included NodeIds, Zoom, Focus, Command, and the current event id. Zoom and Focus are included and reachable. Command may be outside the included set. The Browser names the Command Node; it does not select an ActorName. Dispatch is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]].
- [x] Core resolves ActorName per that dispatch, constructs the extract from its current authoritative Graph, and validates exact membership, reachability, and launch admission at the typed Core boundary.
- [x] Core has one API with different request member types and one universal response member type: `{ nodes; events; latestId }`. Either collection may be empty. Browser Poll, Change, and Command requests and in-process Actor Change requests have the same Core behavior. HTTP is only a Browser adapter.
- [x] The launch response communicates the public actor identity through ActorStarted. A refused request creates no durable Event.
- [x] Database persistence is a private Core detail. No fifth Core API surface, public database interface, or public event-store adapter is added. Only tests of private persistence functions may call those functions directly.
- [x] Document supplies two generic capabilities: mixed-format Graph-extract serialization, where each Node uses its owning codec and one NodeId is marked, and Reference-Paste-style subdocument injection. These capabilities are not Agent-specific.
- [x] The Run Agent Actor owns the system prompt and orchestrates Document serialization, CloudAgents completion, Document subdocument injection, and normal Core Change submission.
- [x] CloudAgents exposes the minimum vendor-neutral face: system prompt plus document plus cancellation produces Completed text, Failed safe error, or Cancelled. Provider selection stays inside CloudAgents composition. Core and Browser do not depend on CloudAgents.

### Authority, identity, and the Event model

- [x] Login creates a Browser public identity and secret credential. Command launch creates Actor public and secret identities. A caller presents its public Authority and secret credential; Core validates both. Accepted Event records save the Authority's readable name, such as Browser A, Browser B, Cursor, Zapier, or Amble.
- [x] A secret credential never persists. A public Actor identity remains durable after the live Actor is removed.
- [x] One global durable ordered Event sequence contains Change, Undo, Redo, ActorStarted, and ActorFinished. There is no second Revision or EventPosition counter.
- [x] Existing Change.id keeps its two meanings: a submitted Change uses it as the basis event id, and an accepted Change receives the next global event id. changeId remains the deduplication identity.
- [x] Undo and Redo continue to include the Change and scan the relevant Events. Merge uses the basis event id and ignores lifecycle Events. Poll advances by latest event id.
- [x] Event records currently support synchronization and lifecycle projection. They do not imply a separate History or audit UI application.

### Lifecycle ordering and ownership

- [x] One Core mailbox strictly orders launch, Change, cancel, terminal, and drop messages. TaskPool execution stays outside the mailbox.
- [x] Launch safely creates identities, registers the Actor, durably appends ActorStarted, and schedules the task so ActorStarted and the registry exist before Actor output can be admitted. A task start failure queues Failed.
- [x] Focus exclusivity is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. Different Focus values may run concurrently even when their extracts overlap.
- [x] Core admits Actor Changes while the Actor remains registered. The Actor should await accepted responses when it needs a newer basis event id or Graph, but it need not await. If it does not await, later Changes may retain ActorStarted's event id as basis and ordinary merge applies.
- [x] Succeeded, Failed, and Cancelled are mailbox terminal messages. Processing the first terminal message durably appends ActorFinished, synchronously removes the registry and revokes the secret, then requests asynchronous termination only when the task is still running. Core never waits for termination.
- [x] Strict mailbox order means Change-before-Cancel applies and Cancel-before-Change rejects. Cancel by Focus is terminal Cancelled. A late duplicate completion after cancellation is ignored.
- [x] Core derives truthful success only after earlier queued Change messages have run. A later rejected output never reverses earlier accepted output.

## Sequences

### Success

1. Browser names the Command Node and sends the typed launch request.
2. Core validates the Browser Authority, resolves ActorName per [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]], constructs the authoritative extract, registers the Actor, appends ActorStarted, and schedules the Run Agent Actor.
3. The Run Agent Actor asks Document to serialize the mixed-format extract with Focus marked, then asks CloudAgents to complete it.
4. Completed text receives a structural codec parse. If that complete parse fails, Document retries the complete text as Plain. Partial structural output is never retained.
5. Document plans Reference-Paste-style replacement of every current Focus Child. Empty successful completion is valid and therefore removes all Focus Children.
6. The Run Agent Actor submits a normal Core Change, not a Parse-only Graph-only Change. Core applies ordinary merge and amendment, and returns `{ nodes; events; latestId }`.
7. The Actor queues Succeeded. After earlier queued Changes run, Core appends ActorFinished and drops the live Actor.

### Failure

CloudAgents Failed queues Failed with a safe domain error. Core logs raw provider details but does not persist them. Core appends ActorFinished with the safe error, removes the live Actor, and preserves Focus Children unless an earlier Change was already accepted.

### Cancellation

Browser cancels by Focus. Core orders Cancelled with Change messages, appends ActorFinished without Error or Change, revokes the secret, removes the live Actor, and requests non-blocking task termination. Change-before-Cancel applies; Cancel-before-Change rejects. Cancellation preserves Focus Children except for earlier accepted Changes.

### Recovery

On restart, Core reconciles every unmatched ActorStarted with ActorFinished Interrupted. A secret is not recovered, and the Actor does not resume. Host stop uses the same ordered terminal and drop model; it does not invent a non-event lock cleanup path.

### Reconciliation

Ordinary Core Change amendment owns concurrent reconciliation. Run Agent adds no stale-output rule, span-overlap rule, last-writer rule, or special undo of accepted output.

## Named test seams

- [x] Browser Command Node naming and typed launch-request construction.
- [x] The public Core request and universal-response lifecycle with TestActor, as planned by [[plan/core-creation/issues/27-prove-core-actor-lifecycle-with-testactor.md]].
- [x] Document mixed-format serialization and Reference-Paste-style replacement.
- [x] Run Agent orchestration with fake CloudAgents through ordinary Core Change.
- [x] Vendor adapters through vendor contract tests.
- [x] The full vertical proof remains the later locked-vertical-proof item in [[plan/core-creation/issues/Implementation Planning and Record.md]].

## Deferred implementation details

Parser and helper mechanics, sentinel spelling and escaping, exact persistence implementation, provider-selection specifics, file-state and import-current operations, lower-level helper and type shapes, and implementation bodies remain deferred. The standing Reference Paste facts are in [[plan/llm-connector/reports/reference-paste-and-change-post-facts.md]].

## Comments

- 2026-09-11 — Interactive grilling locked the architecture and authorized reconciliation of directly contradicted plans. No new Committed Decision was requested.
- 2026-09-11 — This record supersedes contradictory launch-span, non-event lock, and no-terminal-result assumptions in the Core lifecycle plans. Existing Graph Change, Undo, Redo, merge, amendment, and Reference Paste protocols remain standing.
- 2026-09-11 — Dispatch is [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]]. That ticket owns command text `?test hello`.
- 2026-09-11 — Marked locked architecture and named-seam bullets complete. Sequences stay numbered prose.

## Time

- 2026-09-11 2h10m — grill and lock typed boundaries, Event authority, lifecycle ordering, outcome sequences, recovery, reconciliation, and test seams (from chat)
- 2026-09-11 5m — record Command-text Actor dispatch (from chat)
- 2026-09-11 5m — drop TestActor command example; point dispatch at 06 (from chat)
- 2026-09-11 10m — mark locked architecture bullets complete; drop the 29 command-text deferral (from chat)
