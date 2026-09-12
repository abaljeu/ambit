# Lock Run Agent architecture — fact inventory

Date: 2026-09-11. Place: `dev` after `scripts/gitstatus.sh`. Recon only for Phase 1b ([[../issues/06-define-command-run-agent-redesign.md]] contract; gate in [[../../core-creation/issues/Implementation Planning and Record.md]] lines 19–27). Issue 07 and product code were not edited. Terms: [[CONTEXT.md]] Browser, Core, Core API Command, Actor, Agent, Run Agent, Focus, Zoom, SiteMap, Change, CloudAgents.

## 1. Current seams (concrete types and dependency direction)

### Browser (Client)

- Run today is local Expression work, not Agent launch: [[src/Client/Commands.fs]] `execRunOp` → [[src/Client/UpdateAmbleRun.fs]] `runAmbleOp` / `applyRunPlan` → [[src/Shared/AmbleRun.fs]] `shouldExec` / `runPlanOnNode` → [[src/Shared/ExprRun.fs]] `isRunStatement` / `run`. Posts via `applyAndPost` to `POST /ambit/changes`.
- Focus identity: [[src/Shared/ViewModelSelection.fs]] `tryFocusedNodeId` (Browser selection). SiteMap occurrence ancestry: [[src/Shared/ViewModelOccurrence.fs]] / [[src/Shared/ViewModelRowState.fs]] `ancestorMatch` on `parentInstanceId`. No `command` CSS-class resolver exists (`CssClass.contains` is unrelated).
- No `/ambit/actors` route remains ([[src/Server/RouteRegistration.fs]]). Poll/Change status uses [[src/Server/Core/CoreChanges.fs]] `CoreChangesAccepted.message` on the Sync path ([[src/Client/App.fs]], [[src/Client/Update.fs]]).
- Dependency today: Client → Shared (AmbleRun/ExprRun/ViewModel) → Server Changes HTTP. Client does not depend on Core Command or CloudAgents.

### Core Command / Actor

- Pool face: [[src/Server/Core/CoreActorPool.fs]] `LaunchRequest` (`ActorName`, `Revision`, `span: NodeRange`), `ActorFn = Graph -> Credential -> CoreChanges -> Async<unit>`, `PublicNumber`, `launch` / `query` / `lockedIds` / `withLocks`. Held on [[src/Server/Core/CoreRuntime.fs]] `command`.
- Extract/admit today: [[src/Shared/GraphSpan.fs]] `spanIds` / `extract`; refuse on span NodeId intersect (`CoreAdmissionError.Overlap`). Registry job retains span, not Focus. No cancel field; `ActorFn` has no `CancellationToken`; `register`/`launch` callers are tests only ([[plan/llm-connector/reports/actor-spine-facts.md]]).
- Auth/write: [[src/Server/Core/CoreCredentials.fs]] `Credential` / `CoreAuth.bindHandle`; Actor Posts through bound `CoreChanges.postChange`.
- Dependency: Server Core → Shared GraphSpan/Graph; Actor definition is intended outside Core ([[plan/core-creation/issues/02-core-actor-pool.md]]). Current pool still uses a second mailbox; Phase 2 rebuild targets one Changes apply mailbox + TaskPool ([[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]], issue 02).

### Document

- Codec dispatch: [[src/Shared/documents/DocumentFormat.fs]] `DocumentCodec` / `classifyCodec`; face [[src/Shared/documents/DocumentHandler.fs]] `parse` / `readCold` / `readWarm` / `write` over artifact `documentRootId`. Amb/Md/Plain/CStyle handlers exist. No mixed-format Zoom extract, no Focus transport wrapper, no Agent-specific parse fallback.
- Nearby text helpers (not Agent pack): [[src/Shared/Paste.fs]] `serializeSubtree` (SiteMap/Fold), [[src/Shared/ExportText.fs]] `serializeOwnedChildren`, [[src/Shared/documents/DocumentColdParse.fs]] `planPasteOps`.
- Dependency: Shared Documents are Graph/text libraries; Server persist ([[src/Server/DocumentPersistence.fs]]) consumes them. No CloudAgents dependency.

### CloudAgents

- Product source was rewound (git `9c1ce44`); `src/CloudAgents` and `src/CloudAgents.Console` hold build artifacts only — no `.fs` public API in tree. Older Cursor-adapter path is gone with that rewind ([[plan/llm-connector/reports/first-agent-cursor-cloud-agents.md]] notes redo after Core rebuild).
- Intended direction (plans, not code): standalone vendor-neutral library; Cursor is an ordinary adapter; Core and Browser must not own provider protocol ([[../issues/06-define-command-run-agent-redesign.md]], [[../map.md]]).

### Reconciliation already owned

- Concurrent edit: [[src/Shared/ChangeAmendment.fs]] (and Core apply). Issue 06 adds no stale check or overlap policy beyond Focus admission.

## 2. Forced by Phase 1 vs still open

### Forced (issue 06 + Implementation Planning Phase 1)

- Dispatch is [[../issues/06-define-command-run-agent-redesign.md]]. Command text `?test hello` launches TestActor hello.
- One Run payload: Zoom-rooted Graph extract with exactly one Focus; Actor serializes model context; model does not own Graph structure.
- Each Node encodes through its owning document codec → one mixed-format document; transport-only Focus mark; CloudAgents call = system prompt + that document.
- Success: delete every Child under Focus; create new Children from the complete response; structural parse first, else whole-response plain-text indentation outline (atomic discard of failed structural parse).
- Admission: at most one live Actor per Focus `NodeId`; other extract overlap allowed. Cancel by Focus `NodeId`; `PublicNumber` remains query identity.
- Provider failure: preserve Focus Children; no failure Change / no Graph error text; existing Poll/Change Error path carries the message. User cancel: preserve Children; no Error; no Change; normal drop.
- Existing Core Change merge/amendment owns reconciliation. CloudAgents stays vendor-neutral; Cursor is ordinary adapter. Durability, format persistence, Core merge policy, and provider selection stay outside this gate.

### Forced by Core Phase 2 plans (constraint on architecture, not Agent behavior)

- One apply mailbox; TaskPool runner; registry holds public number, credential, terminate handle, Focus `NodeId` ([[02-core-actor-pool.md]], [[18-finish-and-drop.md]], [[17-cancel-a-job.md]], Decision 0004).
- Actor definitions and Agent transport stay outside Core; [[27-prove-core-actor-lifecycle-with-testactor.md]] proves lifecycle without Agent.
- Issue 09 span-shaped launch arguments and span-overlap refuse are superseded for Run Agent by Focus-keyed admission ([[../map.md]], core-creation map note on issue 09).

### Genuinely open for interactive Phase 1b

- Where typed encode (Graph+Focus → mixed document) and decode (response text → Focus-Children Changes) live: Shared pure modules vs DocumentHandler faces vs Browser-built opaque text.
- Exact Core Command launch input after span supersession (Zoom root + Focus + Revision + ActorName, and whether Core extracts or receives a Browser-built NodeId set).
- CloudAgents public interface shape (minimum system prompt + document → text Result, plus error/cancel hooks) and which module may reference it (Actor yes; Core/Browser no).
- How Browser Run/`?` invocation composes to Command launch and cancel without choosing HTTP details yet.
- How Document codecs are accessed for subdocument serialization when the extract crosses owning files (codec lookup by path vs injected handler map).
- Exact Focus sentinel spelling/escaping (explicitly deferred by issue 06) — architecture can name a transport-wrapper seam without locking spelling.

Withdrawn framings must not drive ownership: soft lock / working-set / Included-context-as-pack in [[agent-redesign-locked-2026-09.md]] and older pack reports are superseded by issue 06 Zoom-rooted Graph extract.

## 3. Plausible typed-interface compositions

### A. Actor-orchestrated; Shared owns pack/parse (recommended baseline)

Interfaces (sketch): Browser → Core Command `Launch { actor; revision; zoom; focus }`; Core → `ActorFn` with extracted Graph + Focus + Credential + bound Changes (+ CT after Phase 2); Shared `RunAgentPack.encode : Graph * Focus -> MixedDocument`; Shared `RunAgentApply.plan : Graph * Focus * responseText -> Change list`; CloudAgents `complete : { systemPrompt; document } -> Async<Result<string,_>>`.

- Depth: Shared pack/parse deep and pure; Actor thin orchestration; Core deep on lifecycle only; CloudAgents deep on vendor HTTP behind a small text face.
- Locality: encode/parse bugs fix once in Shared; lifecycle bugs stay in Core; provider bugs stay in CloudAgents adapters.
- Dependency direction: Client ↛ CloudAgents; Core ↛ CloudAgents; Actor → Shared + CloudAgents; Document codecs used as libraries by Shared pack (Documents ↛ CloudAgents).
- Testability: unit-test pack/parse; TestActor without CloudAgents; fake CloudAgents for Actor tests; vertical proof later (Phase 4).

### B. Document-owned agent encode/decode faces

Extend `DocumentHandler` (or sibling) with agent write/parse over Zoom extracts; Actor only calls Document + CloudAgents + `postChange`.

- Depth risk: current `DocumentHandler` is artifact-rooted (`documentRootId`); mixed multi-file extract forces a wide face or many caller-visible codec rules — shallower module, weaker locality.
- Dependency: Actor → Documents + CloudAgents; Shared Run Agent surface may disappear into Documents, coupling persist codecs to Agent protocol.
- Testability: still unit-testable, but tests drag DocumentFormat classification and path ownership into every Agent case.

### C. Browser-built opaque pack text; Actor is transport + Shared apply only

Browser resolves Command, builds mixed document (needs codec access in Client/Fable), posts text blob + Focus; Actor calls CloudAgents and Shared apply.

- Conflicts with issue 06 “Actor serializes that Graph as model context.”
- Dependency: Client gains Documents/agent encode; Server cannot re-derive pack from Graph alone for the same bytes; Core lifecycle tests cannot see encode.
- Testability of pack moves to Client/Fable; Core/Agent seams get weaker Shared coverage.

## 4. Best first interactive question

**Who owns Graph+Focus → mixed-document encode and response text → Focus-Children Changes: Shared pure modules behind the Actor (composition A), DocumentFormat faces (B), or Browser-built opaque text (C)?**

Recommendation: **A (Shared pack/parse; Actor orchestrates; CloudAgents stays text-in/text-out).**

Consequences if A:

- Phase 1b can name three small interfaces before Core issue 02: Command launch inputs (Zoom+Focus), Shared pack/parse, CloudAgents complete.
- Document codecs stay artifact persist modules; Agent reuse is a library call, not a new Document product face.
- Core rebuild and TestActor stay provider-neutral; Agent work waits for Phase 3 without blocking Phase 2.
- Rejects C’s conflict with “Actor serializes,” and avoids B’s pressure to widen DocumentHandler for multi-root mixed extracts.

## 5. Proposed decision sequence (no implementation)

1. Encode/parse ownership (question above) — locks module depth and dependency arrows.
2. Core Command launch typed inputs after span supersession (Zoom root, Focus, Revision, ActorName; Core extract responsibility) — locks admission key alignment with Focus.
3. CloudAgents minimal typed face (system prompt + document → text Result; cancel/error as transport Results, not Graph writes) — locks Actor→CloudAgents only.
4. Success sequence ownership: Browser resolve Command → launch → Core extract/admit → Actor encode → CloudAgents → Shared plan Changes → `postChange` → delete-actor/drop.
5. Failure sequence: transport/Agent error → no Focus Children Change → Error on existing Poll/Change message path → same drop path.
6. Cancellation sequence: Browser cancel by Focus → Core CT + credential drop → no Error/Change → shared drop; already-enqueued Posts still apply (Decision 0004).
7. Reconciliation: affirm ChangeAmendment/Core merge only; no new Run Agent conflict policy.
8. Test seams only: Core TestActor (issue 27); Shared pack/parse unit tests; CloudAgents fake/adapter tests; defer vertical proof to Phase 4.

Stop when those eight locks are recorded on issue 07; do not draft Actor bodies, HTTP routes, or Cursor adapter steps in this gate.
