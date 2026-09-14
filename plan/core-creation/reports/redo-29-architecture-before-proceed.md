# Redo 29 — architecture before proceed

Hand this file to an agent. Scope: [[plan/core-creation/issues/29-prove-testactor-hello.md|Prove TestActor hello]] on [[plan/core-creation/project.md|Core creation]]. Do not implement hello code in the session that only produces arch. Do not migrate other Projects or specs ([[plan/planning-skills/project.md|Planning skills]] — no broad migration).

## 1. Inputs (keep; do not throw away)

Treat these as settled inputs for the redo:

1. Wayfinder map — [[plan/core-creation/map.md]]
2. Ticket — [[plan/core-creation/issues/29-prove-testactor-hello.md|Prove TestActor hello]] (sections 1ff not implemented; Point 0 done)
3. Related tickets — [[02-core-actor-pool.md]], [[14-server-tracks-credentials.md]], [[15-launch-actor-and-hold-span.md]], [[18-finish-and-drop.md]], [[27-prove-core-actor-lifecycle-with-testactor.md]], [[30-reshape-coreactorpool-synchronized-table.md]], [[31-one-coremsg-loop-parameterized-persist.md]], [[32-move-persist-agents-under-coremailbox.md]]
4. Implementation Planning and Record — [[plan/core-creation/issues/Implementation Planning and Record.md]]
5. Partial implement log — [[plan/core-creation/reports/implement-issue-29-testactor-hello.md]] (historical; foundation shape has moved under Point 0)

Done: you can name the Project, the ticket, open sections, and prior map decisions.

## 2. Produce arch.md (HITL) before more hello implement

1. Run [[.agents/skills/to-arch/SKILL.md|to-arch]] on Core creation. Work with the user (HITL).
2. Feature under design: the hello slice / one-mailbox Actor program locked by [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]]. Use the story/path method: numbered User Stories → Story paths across modules → Module map from shared segments → Seams → Sequence → Alternative considered → Unsettled (record only).
3. Cite modules by name ([[.agents/rules/refer-by-name.md]]). Prefer existing seams (`CoreMsg`, CoreMailbox, CoreActorPool, TestActor, Browser Run).
4. Sequence is likely `tracer-cut` (29’s shape is the tracer-cut exemplar in [[.agents/skills/to-tickets/SKILL.md]]). Confirm with the user once.
5. Write [[plan/core-creation/arch.md]]. Set `Stage: arch` and `Updated:` on [[plan/core-creation/project.md]] per [[doc/agents/project-status.md]].

Done: `arch.md` exists; user can critique Story paths, Module map, Seams, Sequence, Alternative considered, and Unsettled; Stage is `arch`.

## 3. After critique-approved arch — reconcile, then build

1. Do **not** re-ticket from scratch unless arch demands new cuts. Reconcile [[29-prove-testactor-hello.md|Prove TestActor hello]] sections (and related open tickets if needed) to the Module map names.
2. Continue [[.agents/skills/implement/SKILL.md|implement]] against remaining open sections (2 Register TestActor → 6 Prove from outside). Consume `arch.md` Module map, Seams, and Sequence.
3. Number and name every section and list item you write; refer by name, not bare numbers.

Done: tickets/sections cite arch modules by name; remaining open hello sections are in progress or done under `/implement`.

## 4. Out of scope for this redo

1. Broad migration of other Projects’ specs or tickets into the chart → spec → arch → slice → build chain.
2. Rewinding or discarding Wayfinder map Decisions so far.
3. Implementing remaining hello sections before arch is critique-approved.
4. Opening Wayfinder map tickets for Unsettled paths (record under Unsettled only; resolve in Stage `arch`).
