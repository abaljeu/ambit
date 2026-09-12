# Agent redesign and Core lifecycle — Implementation planning and record

This is the controlling planning and record document for the reviewed and rewound Actor work. Do not restore discarded implementations or add wrap patches. Phase 2 lists the eight Core lifecycle program pieces, not a serial implement order. Phase 2b is the implementation sequence for the remaining spec. The current implement cut is built in 2b.

## Phase 1 — Agent design locked

[[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] is `done`. It locks Command-text dispatch, one Zoom-rooted extract with exactly one Focus, mixed-format CloudAgents protocol, Focus-child replacement on success, one live Actor per Focus NodeId, preserve-children on failure and cancel, and ordinary Core merge for concurrent reconciliation. Command role, Kind, and CSS class do not select the Actor. See that issue for the contract and scope exclusions.

## Phase 1b — Lock the Run Agent architecture — complete

[[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] is `done`. It locks typed Browser/Core/Actor/Document/CloudAgents boundaries, exact launch membership, Core-owned extraction, Authority identities, one global Event sequence, universal Core responses, one-mailbox lifecycle ordering, durable terminal Events, restart reconciliation, ordinary Change amendment, and named test seams. Parser mechanics, persistence mechanics, provider selection, file-state operations, and implementation bodies remain deferred.

## Phase 2 — The Spec

The eight numbered items are pieces of the Core lifecycle program. They are not the implementation sequence.

1. **Rebuild the Actor pool baseline** — Registry and admission live on the one Core mailbox; TaskPool runs and terminates Actors without waiting. [[plan/core-creation/issues/02-core-actor-pool.md]]
2. **Establish Authority admission** — Validate public Authority and secret together on each request; persist only the readable Authority name. [[plan/core-creation/issues/14-server-tracks-credentials.md]]
3. **Rebuild launch and Focus registration** — Register public identity, secret, termination handle, and Focus NodeId in mailbox state; refuse only a second live Actor for that Focus. [[plan/core-creation/issues/15-launch-actor-and-hold-span.md]]
4. **Preserve live Actor query** — Query the registered Actor by public identity while it is live. [[plan/core-creation/issues/16-track-running-job.md]]
5. **Establish terminal, failure, and drop behavior** — The first terminal message appends exactly one ActorFinished (safe error when Failed), then drops the live registry and secret and terminates without waiting. [[plan/core-creation/issues/18-finish-and-drop.md]]
6. **Rebuild cancellation by Focus NodeId** — Preserve earlier accepted Changes, reject later output, and do not Undo. [[plan/core-creation/issues/17-cancel-a-job.md]]
7. **Prove the Actor system independently** — Prove the rebuilt lifecycle with TestActor cases and no Agent transport. [[plan/core-creation/issues/27-prove-core-actor-lifecycle-with-testactor.md]]
8. **Complete host-stop lifecycle later** — Refuse new Posts, drain through terminal Events and drop, and stop at idle or the host timeout. [[plan/core-creation/issues/28-drain-actor-lifecycle-on-host-stop.md]]

## Phase 2b — Sequence of Spec Implementation

Phase 2b is the implementation sequence for the remaining spec, not only the eight Core lifecycle pieces. The sequence selects pieces from that spec to implement visible increments. Advanced parts wait.

Point 0 = Core-shape preamble: [[30-reshape-coreactorpool-synchronized-table.md]] (done, own commit) then [[31-one-coremsg-loop-parameterized-persist.md]], then [[29-prove-testactor-hello.md]] as first new behavior. Current implement cut: [[31-one-coremsg-loop-parameterized-persist.md]]. 30 strips CoreActorPool mailbox queue; synchronized table + thread pool. 31 unifies FileAgent and DbAgent MailboxProcessor loops; persist is a parameter for persist cases only. After 31 is agent-done, 29 remains the first user-visible increment. After 29 is agent-done, mark delivered facts and identify the next increment. Do not invent later increment tickets now. That cut does not include the later vendor-neutral Agent seam or the locked vertical proof.

- Vendor-neutral Agent seam — After the Core lifecycle program is executable, create the smallest implementation issue for the standalone CloudAgents project and the contracts in [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] and [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Keep provider selection and provider-specific behavior behind the vendor-neutral CloudAgents API. Do not restore the old Md-only payload or Cursor-specific domain behavior.
- Locked vertical proof — Define and run the vertical proof only after the Core lifecycle and [[plan/llm-connector/issues/06-define-command-run-agent-redesign.md]] are executable. The cancelled Create/Focus/Md paste-replace path is not that proof.

## Record

The former executable three-set sequence was withdrawn by [[plan/llm-connector/reports/agent-redesign-locked-2026-09.md]]. Its CloudAgents and Create-to-reply steps no longer direct implementation.

2026-09-11 — Alan locked Command-text dispatch. See Phase 1.
2026-09-12 — Reviews of the TestActor hello increment failed. Code was stashed. Issue checkboxes and implementation logs from 2026-09-11 were cleared. The current implement cut remains [[29-prove-testactor-hello.md]].
2026-09-12 — Issue [[30-reshape-coreactorpool-synchronized-table.md]] created as prefactor: strip CoreActorPool mailbox-queue design; synchronized table mutators + thread pool. Issue 29 blocked by 30. Current implement order: 30, then 29.
2026-09-12 — Issue [[31-one-coremsg-loop-parameterized-persist.md]] created as second Point 0 increment: one CoreMsg loop, File/Db persist injected for persist cases only. Point 0 = 30 (done, own commit) then 31, then 29 as first new behavior. Current implement cut: 31.
