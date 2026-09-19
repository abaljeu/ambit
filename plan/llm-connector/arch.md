# llm-connector architecture

Spec: [[spec.md]]
Updated: 2026-09-19
Sequence: tracer-cut

Sources: [[issues/06-define-command-run-agent-redesign.md|06 — Define the revised Command + Run Agent seam]], [[issues/07-lock-run-agent-architecture.md|07 — Lock the Run Agent architecture]], [[plan/core-creation/arch.md|Core creation architecture]] (hello lifecycle already delivered). Checklist: `[x]` delivered; `[ ]` still to build for this Project.

## 1. Story paths

1. **TestActor hello (prerequisite — delivered)**
   1. [x] Browser Run Command text `?test hello`
   2. [x] HTTP Adapter typed launch + credentials
   3. [x] CoreMailbox / CoreMsg / CoreActorPool StartActor
   4. [x] TestActor body → Owned child `hello` via ordinary Change
   5. [x] ActorStarted / ActorFinished on EventLog; drop live row
   Home: [[plan/core-creation/arch.md]] Story paths **Browser Run hello** and **Outside Core lifecycle proof**.

2. **Agent ask from what I see**
   1. [x] Browser names Command Node; typed launch membership (Zoom, Focus, Command, included ids, event id)
   2. [x] Core validates Browser Authority (hello path)
   3. [ ] Resolve ActorName from Agent Command text (`?ai` + args); Focus exclusivity admit
   4. [ ] Register Run Agent Actor, append ActorStarted, schedule body
   5. [ ] First increment: ignore extract; pass Focus Node text to CloudAgents
   6. [ ] CloudAgents complete → Completed text | Failed | Cancelled; ActorFinished; drop live (no Graph write)
   7. [ ] Pack supplied extract as nested `<div>` / `<focus>` strings (not Md; not owning-codec)
   8. [ ] Parse complete reply in that format; replace every Focus Child (empty success clears all)
   9. [ ] Run Agent Actor submits ordinary Core Change; await optional for newer basis
   10. [ ] Succeeded → ActorFinished; drop live Actor row (observe via live Focus ids; secrets are not an observation surface)
   11. [ ] Browser Poll shows new Focus Children

3. **Agent failure preserves children**
   Rule: on failure the Actor framework does not cause Changes; the AI Actor does not erase data (future agentic extensions out of scope).
   1. [x] Framework Failed via TestActor / `?test` → ActorFinished; framework posts no Change; Focus Children unchanged; live Focus id gone from the pool (`liveFocusIds`)
   2. [x] Safe domain error only on the terminal (no raw provider payload in Graph Events)
   3. [ ] AI Actor erase-on-Failed (CloudAgents) deferred until [[issues/08-agent-ask-from-what-i-see.md|08 — Run Agent Actor calls CloudAgents]] and [[issues/12-replace-focus-children-from-reply.md|12 — Replace Focus Children from reply]]

4. **Cancel by Focus**
   1. [ ] Browser cancel by Focus NodeId
   2. [ ] Core mailbox orders Cancelled vs Change (Change-before-Cancel applies; Cancel-before-Change rejects)
   3. [ ] CloudAgents cancel; ActorFinished without Error or Change; drop live
   4. [ ] Focus Children preserved except earlier accepted Changes
   5. [ ] Browser live projection clears (no Graph lock-present field) — chrome on core-creation 21/22; first Agent vertical may prove cancel without UI

5. **Credentialed Change while Agent runs**
   1. [x] Browser Change posts through CoreMailbox
   2. [x] Ordinary merge / amendment; lifecycle Events ignored by merge
   3. [ ] Concurrent edit under Focus reconciles without Agent-specific stale rules

Shared segments (Agent paths 2–4):
1. [x] CoreMailbox one-loop launch / Change / terminal / drop
2. [x] CoreActorPool live registry + TaskPool outside mailbox
3. [x] EventLog ActorStarted / ActorFinished
4. [ ] Document nested-tag pack of the supplied extract + Reference-Paste replace; mixed-format tabled
5. [ ] CloudAgents vendor-neutral complete / fail / cancel
6. [ ] Run Agent Actor orchestration

Narrowest shared test seam:
1. [ ] Run Agent Actor with fake CloudAgents, Focus text only, no Graph write
2. [ ] Later: same Actor with packed extract through ordinary Core Change
3. [x] Public Core lifecycle with TestActor (already proven)

## 2. Module map

1. **Browser Run / Cancel**
   File: Client Run and Poll surfaces (existing Browser Command path).
   1. State
      1. [x] Session cookie / credential
      2. [ ] Live Actor projection by Focus from Poll Events — owned by core-creation 21/22, not first Agent vertical
   2. Interface
      1. [x] Typed launch: included NodeIds, Zoom, Focus, Command, event id
      2. [ ] Cancel by Focus NodeId — owned by core-creation 22; first Agent vertical may omit Browser cancel chrome
      3. [x] Poll consume Ev tail
   3. Uses
      1. [x] HTTP Adapter
      2. [x] Local Graph / Sync

2. **HTTP Adapter**
   File: [[src/Server/Api.fs]] (and related route registration).
   1. State
      1. [x] None beyond request decode
   2. Interface
      1. [x] Decode launch / Change / Poll; encode universal `{ nodes; events; latestId }`
      2. [ ] Decode cancel-by-Focus when exposed
   3. Uses
      1. [x] CoreMailbox doors only

3. **CoreMailbox / CoreMsg / CoreActorPool**
   Files: [[src/Server/Core/CoreMailbox.fs]], [[src/Server/Core/CoreMailboxBackend.fs]], [[src/Server/Core/CoreActorPool.fs]].
   1. State
      1. [x] One mailbox; live registry table; EventLog tip
      2. [x] Focus exclusivity for live Actors
   2. Interface
      1. [x] StartActor / postEvents / ActorStop; Authority validation
      2. [ ] Cancelled terminal by Focus
      3. [x] ActorName resolve from Command text (`test` hello path)
      4. [ ] ActorName resolve for Agent Command form
   3. Uses
      1. [x] PersistHandlers / EventLog
      2. [x] Registered Actor definitions (composition)

4. **TestActor**
   File: Server TestActor (outside Core; composition-registered).
   1. State
      1. [x] None durable
   2. Interface
      1. [x] Interpret `?test hello` → post hello Change
   3. Uses
      1. [x] CoreMailbox postEvents

5. **Run Agent Actor**
   File: new Server Actor module (outside Core; composition-registered). Not a revival of cancelled Create/Md paste Actor.
   1. State
      1. [ ] Job memory: CloudAgents agentId / runId for poll and cancel
   2. Interface
      1. [ ] First increment: Focus text → CloudAgents complete → terminal; no Graph write
      2. [ ] Later: Document pack → CloudAgents complete → Document inject → Core Change
      3. [ ] On cancel token: request CloudAgents cancel and stop
      4. [ ] Never turn pack or provider errors into a second Agent call; never write raw provider text as Graph Error
   3. Uses
      1. [ ] Document nested-tag pack / Reference-Paste inject (after first increment)
      2. [ ] CloudAgents
      3. [ ] CoreMailbox postEvents

6. **Document (pack + Reference Paste)**
   Files: Shared / Server Document codec surfaces (existing Reference Paste facts: [[reports/reference-paste-and-change-post-facts.md]]).
   1. State
      1. [x] Owning codecs per document (unused by the temporary pack)
   2. Interface
      1. [ ] Write and parse the supplied extract as nested `<div>` / `<focus>` strings (not a file; not Owner-bounded). Existing Md artifact write does not change.
      2. [ ] Tabled: serialize Graph extract to mixed-format document with owning codecs and Focus marked
      3. [ ] Plan Ops replacing every current Focus Child (empty success removes all)
   3. Uses
      1. [x] Op / Ev Change construction helpers

7. **CloudAgents**
   Files: [[src/CloudAgents/Gambol.CloudAgents.fsproj]], [[src/CloudAgents/AgentRunner.fs]], [[src/CloudAgents/PublicTypes.fs]]; Cursor adapter under Internal/.
   1. State
      1. [x] RunnerConfig (API key); no Ambit/Core references
   2. Interface
      1. [x] Existing public API: start / poll / cancel / waitUntilComplete (keep; do not reshape for Ambit)
      2. [ ] Ambit Run Agent Actor fits system prompt + document + cancel into that API and maps statuses to Completed | Failed | Cancelled outcomes
      3. [x] Provider selection and Cursor protocol stay inside composition / Internal
   3. Uses
      1. [x] Cursor HTTP only behind Internal adapter

8. **EventLog / Authority**
   Files: Shared Ev / EventLog; Core persist.
   1. State
      1. [x] One global Event sequence including Change and lifecycle
   2. Interface
      1. [x] Append ActorStarted / ActorFinished; Poll by latestId
      2. [x] Readable Authority on Events; secrets never persist
   3. Uses
      1. [x] PersistHandlers File/Db

## 3. Seams

1. [x] **Browser ↔ HTTP Adapter** — typed launch / Change / Poll encoding
2. [x] **HTTP Adapter ↔ CoreMailbox** — sole Core door
3. [x] **CoreActorPool ↔ Actor definitions** — register TestActor / Run Agent Actor at composition
4. [ ] **Run Agent Actor ↔ Document** — nested-tag pack and Reference-Paste inject (generic, not Agent-only APIs)
5. [ ] **Run Agent Actor ↔ CloudAgents** — vendor-neutral complete / fail / cancel
6. [ ] **CloudAgents ↔ Cursor adapter** — Internal only; vendor contract tests
7. [x] **CoreMsg admit ↔ postEvents** — Actor Changes while registered
8. [ ] **Browser cancel ↔ Core Cancelled** — Focus NodeId terminal

## 4. Alternative considered

1. **Fat Create vertical (issue 05 / PR #4)** — POST `/ambit/actors` with Md paste-replace under Focus-only lock. Rejected: missed Focus replace semantics, pack-error→Agent calls, and proof gaps; closed unmerged. Do not restore.
2. **Core owns CloudAgents** — Would reverse dependency direction. Rejected: CloudAgents stays standalone; Core and Browser do not reference it.
3. **Span soft-lock as Graph field** — Superseded by durable ActorStarted / ActorFinished and Focus exclusivity in [[issues/07-lock-run-agent-architecture.md|07]].

## 5. Locked during arch (2026-09-19)

1. **Agent Command spelling** — `?ai` plus optional args. That text invokes the Run Agent Actor. Args are ignored for now (reserved).
2. **Vertical proof timing** — Define the full Browser → Run Agent → Focus-children proof after the first CloudAgents / Run Agent implement tickets are `defined` (not now; not inside the first end-to-end ticket alone).
3. **Focus mark spelling** — First pack marks Focus with `<focus>` (simple nested-tag format). Mixed-format sentinel spelling stays tabled with owning-codec serialize.
4. **First Actor increment ignores the extract** — `?ai` launches the Run Agent Actor and passes Focus Node text into CloudAgents. No pack, no Focus-child Change.
5. **Stronger serialization tabled** — Owning-codec mixed-format and Md extract write are not the first pack. Temporary format is nested `<div>` / `<focus>` strings of the supplied extract.
6. **CloudAgents DLL interface** — Keep the existing public CloudAgents API (`start` / `poll` / `cancel` and related types). Do not reshape it for Ambit. The Run Agent Actor (Ambit side) fits system prompt + document + cancellation into that form. Cursor stays Internal; Core never references CloudAgents.
7. **Failure: no framework Changes, no erase** — On Failed, the Actor framework does not post Changes; the AI Actor does not erase Focus Children. Future agentic extensions that might mutate on failure are out of scope.
8. **Live Actor chrome** — Browser “Actor live for Focus” UI belongs on [[plan/core-creation/issues/21-client-shows-lock-present.md|21 — Client shows lock-present]] and [[plan/core-creation/issues/22-client-cancels-a-job.md|22 — Client cancels a job]]. This Project’s first Agent vertical proves Graph + Poll only (no live-Actor chrome).

## 6. Unsettled

None — arch grill closed 2026-09-19.
