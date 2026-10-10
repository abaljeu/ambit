---
name: to-tickets
description: Propagate arch.md Sequence into implementation tickets (tracer-cut from Story paths, module-build from Module map, or expand-contract), each declaring blocking edges and Binding arch, published via PUBLISH.md.
disable-model-invocation: true
---

# To Tickets

Turn the Project's `arch.md` into **tickets**. Propagate its **Sequence** — do not re-choose or quiz. Draft and publish the full predefined set for that Sequence immediately; the written files are the review surface. Each ticket declares the tickets that **block** it and names **Binding arch** and **Trackers**.

## Process

### 1. Gather from arch

Load the Project via [[.agents/skills/project-work/SKILL.md]]. Read `arch.md`. Take **Sequence**, Story path names, Module map names, Seams, and checklist state from there. Do not explore the codebase for ticket content — arch is the source.

Done: Sequence and the named set to ticket are in hand.

### 2. Propagate Sequence

Use `Sequence` from `arch.md`. Values: `tracer-cut` | `module-build` | `expand-contract`.

If Sequence is missing, stop and ask once; do not invent a mode. Otherwise proceed with the arch value — no quiz.

Done: one mode is fixed for this run.

### 3. Name the set

Name every ticket the Sequence implies (all open Story paths, or all open Module map entries, or the expand–contract chain). When the user names a subset, ticket only that subset — Sequence still governs the kind. Number and name every ticket per [[.agents/rules/refer-by-name.md]]. Give each ticket its **blocking edges** and a one-line goal. A ticket with no blockers can start immediately. Skip Story paths or modules the arch already marks fully `[x]` unless the user names them.

#### Tracer cut (`tracer-cut`)

One ticket per **Story path** (exemplar shape: [[plan/core-creation/issues/34-outside-core-lifecycle-proof.md|Outside Core lifecycle proof]]).

- A product slice cuts a narrow but COMPLETE path through every affected layer — vertical, NOT a horizontal slice of one layer
- An instruction-file slice is verifiable by repo search and published instruction behavior; schema, API, and UI are not required
- A completed slice is demoable or verifiable on its own
- Each slice is sized to fit in a single fresh context window

#### Module build (`module-build`)

One ticket per **Module map** entry (cohesive testable module/capability).

- One ticket is one capability that hangs together and can be proven with tests
- A finished ticket is verifiable once its blockers are done — later tickets are not required to know whether this one works
- Size each ticket to fit in a single fresh context window
- Split when two capabilities can be tested independently; merge when a split would leave an untestable fragment
- After testable functionality is covered, add tickets for user-visible functionality only when the arch's stories still need them and Sequence stays `module-build`

#### Expand–contract (`expand-contract`)

Use when a **wide mechanical change** ( rename a column, retype a shared symbol ) has blast radius across the codebase so a single edit breaks many call sites and no tracer cut can land green. This mode does not derive ticket bodies from Story paths or Module map the way the other two do; keep the expand / migrate / contract procedure below.

First **expand** ( add the new form beside the old so nothing breaks. Then **migrate** call sites in batches sized by blast radius (per package, per directory), each batch its own ticket blocked by the expand, keeping CI green batch to batch because the old form still exists. Finally **contract** — delete the old form once no caller remains, in a ticket blocked by every migrate batch. When even the batches cannot stay green alone, keep the sequence but let them share an integration branch that all block a final integrate-and-verify ticket ) green is promised only there.

Done: a draft list exists for every item in the Sequence's set, each with a title, a one-line goal, and blockers.

### 4. Dispatch pieces

For each named ticket, dispatch one subagent per piece file: [piece-goal](piece-goal.md), [piece-arch](piece-arch.md), [piece-obligations](piece-obligations.md), and [piece-trackers](piece-trackers.md). Run a piece when every name on its Depends line has returned. Pieces with `Depends: none` run separately, each in a fresh context. Give a subagent that file and only its Inputs: the ticket's one-line goal, the Project `spec.md` and `arch.md` paths, and earlier outputs only for names on Depends. The subagent returns the output section. It does not write a report.

Done: every named ticket has one returned output for each piece file.

### 5. Assemble

Write one file per ticket per [[PUBLISH.md]]. Place each returned output in the section that piece names. Under What to build, place the goal opening, then the architecture subsections, then the obligation checklist.

Done: each ticket is one file in that shape, Trackers is set, Finish-up is present, and the ticket text holds each piece's decisions.

### 6. Publish

The assembled files are the published set. Do not ask for approval or run a breakdown quiz first. Set `Stage: slice` and `Updated:` on `project.md` per [[doc/agents/project-status.md]]. Name the frontier in the reply. Done: the frontier is named, and project Stage is `slice`.
