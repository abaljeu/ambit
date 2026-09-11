# Agent redesign and Core lifecycle — Implementation planning and record

This is the controlling planning and record document for the reviewed and rewound Actor work. Do not restore discarded implementations or add wrap patches. Phase 2 lists the eight Core lifecycle program pieces, not a serial implement order. Phase 2b is the implementation sequence for the remaining spec. The current implement cut is built in 2b.

## Phase 1 — Agent design locked

[[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] is `done`. Its contract replaces the withdrawn working-set and transport-selection framing:

- The Command Node's text is the dispatch. `?test echo` launches TestActor echo. `?ai ...` will launch the Agent Actor; payload details later. Command role, Kind, and CSS class do not select the Actor.
- Run sends one Zoom-rooted Graph extract containing exactly one Focus. Each Node encodes through its owning document codec; a transport-only wrapper marks Focus.
- The vendor-neutral CloudAgents call receives a system prompt plus that mixed-format document. Command Nodes are not extracted into a separate provider instruction.
- Successful returned text replaces every Child under Focus. A failed structural parse retries the complete response through the plain-text indentation outline parser.
- At most one live Actor targets a Focus NodeId. Different Focus NodeIds may run concurrently regardless of extract overlap. Cancel uses Focus NodeId; PublicNumber remains query identity.
- Provider failure and user cancellation preserve Focus Children. The locked lifecycle records safe failure and cancellation as ActorFinished Events. Neither writes error text into the Graph.
- Existing Core Change merge and amendment own concurrent reconciliation.

Durability, format persistence, Core merge behavior, provider selection, and provider-specific behavior are outside this gate. The standalone CloudAgents project keeps its vendor-neutral API; Cursor is an ordinary adapter.

## Phase 1b — Lock the Run Agent architecture — complete

[[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] is `done`. It locks typed Browser/Core/Actor/Document/CloudAgents boundaries, exact launch membership, Core-owned extraction, Authority identities, one global Event sequence, universal Core responses, one-mailbox lifecycle ordering, durable terminal Events, restart reconciliation, ordinary Change amendment, and named test seams. Parser mechanics, persistence mechanics, provider selection, file-state operations, and implementation bodies remain deferred.

## Phase 2 — The Spec

The eight numbered items are pieces of the Core lifecycle program. They are not the implementation sequence.

1. **Rebuild the Actor pool baseline** — Use one Core mailbox for launch, Change, cancel, terminal, and drop ordering; keep registry and admission state there; return the universal `{ nodes; events; latestId }` response; and run or terminate Actors outside the mailbox through TaskPool without waiting. [[plan/core-creation/issues/02-core-actor-pool.md]]
2. **Establish Authority admission** — Keep public Authority identity and secret credential together while live, validate both on each request, persist only the readable Authority name, and never persist the secret. [[plan/core-creation/issues/14-server-tracks-credentials.md]]
3. **Rebuild launch and Focus registration** — Resolve ActorName from Command Node text, retain the public Actor identity, secret credential, termination handle, and Focus NodeId in mailbox state, and refuse only a second live Actor for the same Focus. [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]]
4. **Preserve live Actor query** — Query the registered Actor by public identity while lifecycle Events remain the durable result after drop. [[plan/core-creation/issues/16-track-running-job.md]]
5. **Establish terminal, failure, and drop behavior** — Order terminal messages after earlier Changes, append exactly one ActorFinished with a safe error when Failed, remove registry state and revoke the secret synchronously, then asynchronously terminate only a still-running task without waiting. [[plan/core-creation/issues/18-finish-and-drop.md]]
6. **Rebuild cancellation by Focus NodeId** — Order Cancelled with Changes, preserve earlier accepted Changes, reject later output through normal credential admission, and do not Undo. [[plan/core-creation/issues/17-cancel-a-job.md]]
7. **Prove the Actor system independently** — Use TestActor cases to prove launch, durable lifecycle Events, ordered Changes, terminal cleanup, restart reconciliation, and eventual drop without an Agent transport. [[plan/core-creation/issues/27-prove-core-actor-lifecycle-with-testactor.md]]
8. **Complete host-stop lifecycle later** — After the current redo, refuse new Posts, drain the mailbox through terminal Events and drop, cancel Actors, and stop at idle or the host timeout. [[plan/core-creation/issues/28-drain-actor-lifecycle-on-host-stop.md]]

## Phase 2b — Sequence of Spec Implementation

Phase 2b is the implementation sequence for the remaining spec, not only the eight Core lifecycle pieces. The sequence selects pieces from that spec to implement visible increments. Advanced parts wait.

Current implement cut: [[29-prove-testactor-echo.md]]. After that increment is agent-done, mark delivered facts and identify the next increment. Do not invent later increment tickets now. [[29-prove-testactor-echo.md]] does not include the later vendor-neutral Agent seam or the locked vertical proof.

Later sequence items wait. Do not create tickets for them now.

- Vendor-neutral Agent seam — After the Core lifecycle program is executable, create the smallest implementation issue for the standalone CloudAgents project and the contracts in [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] and [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Keep provider selection and provider-specific behavior behind the vendor-neutral CloudAgents API. Do not restore the old Md-only payload or Cursor-specific domain behavior.
- Locked vertical proof — Define and run the vertical proof only after the Core lifecycle and [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] are executable. The cancelled Create/Focus/Md paste-replace path is not that proof.

## Record

The former executable three-set sequence was withdrawn by [[plan/llm-connector/reports/agent-redesign-locked-2026-09.md]]. Its CloudAgents and Create-to-reply steps no longer direct implementation.

2026-09-11 — Alan: the Command Node's text is the Actor dispatch (`?test echo` / `?ai ...`). This replaces Command-role, Kind, and CSS selection. `?ai` payload details stay deferred. First increment launch signal: [[29-prove-testactor-echo.md]].
