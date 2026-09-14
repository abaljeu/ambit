---
name: to-tickets
description: Propagate arch.md Sequence into implementation tickets (tracer-cut from Story paths, module-build from Module map, or expand-contract), each declaring blocking edges, published via PUBLISH.md.
disable-model-invocation: true
---

# To Tickets

Turn the Project's `arch.md` into **tickets**. Propagate its **Sequence** — do not re-choose or quiz. Draft the full predefined set for that Sequence. Each ticket declares the tickets that **block** it.

## Process

### 1. Gather from arch

Load the Project via [[.agents/skills/project-work/SKILL.md]]. Read `arch.md`. Take **Sequence**, Story path names, Module map names, Seams, and checklist state from there. Do not explore the codebase for ticket content — arch is the source.

Tickets cite Story paths and modules by name (wikilink to `arch.md`); they hold acceptance and section checklists, not a second Module map Interface. Vocabulary and Committed Decisions come from arch and its See-also targets.

Done: Sequence and the named set to ticket are in hand.

### 2. Propagate Sequence

Use `Sequence` from `arch.md`. Values: `tracer-cut` | `module-build` | `expand-contract`.

If Sequence is missing, stop and ask once; do not invent a mode. Otherwise proceed with the arch value — no quiz.

Done: one mode is fixed for this run.

### 3. Draft the full set

Draft every ticket the Sequence implies (all open Story paths, or all open Module map entries, or the expand–contract chain). When the user names a subset, ticket only that subset — Sequence still governs the kind. Number and name every ticket, section, and list item per [[.agents/rules/refer-by-name.md]]. Give each ticket its **blocking edges**. A ticket with no blockers can start immediately. Skip Story paths or modules the arch already marks fully `[x]` unless the user names them.

#### Tracer cut (`tracer-cut`)

One ticket per **Story path** (exemplar shape: [[plan/core-creation/issues/34-outside-core-lifecycle-proof.md|Outside Core lifecycle proof]]).

- A product slice cuts a narrow but COMPLETE path through every affected layer — vertical, NOT a horizontal slice of one layer
- An instruction-file slice is verifiable by repo search and published instruction behavior; schema, API, and UI are not required
- A completed slice is demoable or verifiable on its own
- Each slice is sized to fit in a single fresh context window
- Under What to build, name subsections by the modules crossed; cite modules by name. Point at `arch.md` for State / Interface / Uses; do not restate those bullets on the ticket. Acceptance checklists use numbered tasks (`1. [ ]`), not bare `- [ ]`

#### Module build (`module-build`)

One ticket per **Module map** entry (cohesive testable module/capability).

- One ticket is one capability that hangs together and can be proven with tests
- A finished ticket is verifiable once its blockers are done — later tickets are not required to know whether this one works
- Size each ticket to fit in a single fresh context window
- Split when two capabilities can be tested independently; merge when a split would leave an untestable fragment
- After testable functionality is covered, add tickets for user-visible functionality only when the arch's stories still need them and Sequence stays `module-build`

#### Expand–contract (`expand-contract`)

Use when a **wide mechanical change** — rename a column, retype a shared symbol — has blast radius across the codebase so a single edit breaks many call sites and no tracer cut can land green. This mode does not derive ticket bodies from Story paths or Module map the way the other two do; keep the expand / migrate / contract procedure below.

First **expand** — add the new form beside the old so nothing breaks. Then **migrate** call sites in batches sized by blast radius (per package, per directory), each batch its own ticket blocked by the expand, keeping CI green batch to batch because the old form still exists. Finally **contract** — delete the old form once no caller remains, in a ticket blocked by every migrate batch. When even the batches cannot stay green alone, keep the sequence but let them share an integration branch that all block a final integrate-and-verify ticket — green is promised only there.

Done: a numbered draft list exists for every item in the Sequence's set, each with title, blockers, and what it delivers.

### 4. Publish

Publish the full draft set per [[PUBLISH.md]] — no breakdown quiz. Set `Stage: slice` and `Updated:` on `project.md` per [[doc/agents/project-status.md]]. Name the frontier in the reply. Done: each ticket is one file, the frontier is named, and project Stage is `slice`.
