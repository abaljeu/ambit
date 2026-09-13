---
name: to-tickets
description: Break a plan, spec, or conversation into implementation tickets under one Sequence mode (tracer-cut, module-build, or expand-contract), each declaring blocking edges, published via PUBLISH.md.
disable-model-invocation: true
---

# To Tickets

Break a plan, spec, or conversation into **tickets**. Pick one **Sequence** mode for the Project (usually from `arch.md`), then draft in that mode. Each ticket declares the tickets that **block** it.

## Process

### 1. Gather context

Work from whatever is already in the conversation context. If the user passes a reference (a spec path, an issue number or URL) as an argument, fetch it and read its full body and comments.

When the Project has `arch.md`, read it. Use its Module map names and its **Sequence** field. Tickets cite Story paths and modules by name (wikilink to `arch.md`); they hold acceptance and section checklists, not a second Module map Interface. Done: you have the source material and, when present, arch module names and Sequence.

### 2. Choose Sequence

Read `Sequence` from `arch.md` when present. Values: `tracer-cut` | `module-build` | `expand-contract`.

If Sequence is missing, ask the user once which mode to use, then proceed with that answer for this run. Do not re-ask after they answer.

Done: one mode is selected for this Project run.

### 3. Explore the codebase (optional)

If you have not already explored the codebase, do so to understand the current state of the code. Ticket titles and descriptions should use the project's domain glossary vocabulary, and respect Committed Decisions in the area you're touching.

Look for opportunities to prefactor the code to make the implementation easier. "Make the change easy, then make the easy change."

Done: vocabulary and Committed Decisions for the area are in hand (or exploration was skipped because context already covers them).

### 4. Draft tickets

Draft in the selected mode only. Number and name every ticket, section, and list item per [[.agents/rules/refer-by-name.md]]. Give each ticket its **blocking edges** — the other tickets that must complete before it can start. A ticket with no blockers can start immediately.

#### Tracer cut (`tracer-cut`)

Each ticket is a **visible end-user increment** across modules (exemplar: [[plan/core-creation/issues/29-prove-testactor-hello.md|Prove TestActor hello]]).

- A product slice cuts a narrow but COMPLETE path through every affected layer (schema, API, UI, tests) — vertical, NOT a horizontal slice of one layer
- An instruction-file slice (skills, rules, agent docs) is verifiable by repo search and published instruction behavior; schema, API, and UI are not required
- A completed slice is demoable or verifiable on its own
- Each slice is sized to fit in a single fresh context window
- Any prefactoring should be done first
- Under What to build, name subsections by the modules crossed; cite modules by name per [[.agents/rules/refer-by-name.md]]. Point at `arch.md` for State / Interface / Uses; do not restate those bullets on the ticket. Acceptance checklists use numbered tasks (`1. [ ]`), not bare `- [ ]`

#### Module build (`module-build`)

Each ticket is one **cohesive testable module/capability** (casual module), not one-to-one with end-user features.

- One ticket is one capability that hangs together and can be proven with tests
- A finished ticket is verifiable once its blockers are done — later tickets are not required to know whether this one works
- Size each ticket to fit in a single fresh context window
- Split when two capabilities can be tested independently; merge when a split would leave an untestable fragment
- After testable functionality is covered, add tickets for user-visible functionality

#### Expand–contract (`expand-contract`)

Use when a **wide mechanical change** — rename a column, retype a shared symbol — has blast radius across the codebase so a single edit breaks many call sites and no tracer cut can land green.

Sequence as expand–contract: First **expand** — add the new form beside the old so nothing breaks. Then **migrate** call sites in batches sized by blast radius (per package, per directory), each batch its own ticket blocked by the expand, keeping CI green batch to batch because the old form still exists. Finally **contract** — delete the old form once no caller remains, in a ticket blocked by every migrate batch. When even the batches cannot stay green alone, keep the sequence but let them share an integration branch that all block a final integrate-and-verify ticket — green is promised only there.

Done: a numbered draft list exists in the selected mode, each ticket with title, blockers, and what it delivers.

### 5. Quiz the user

Present the proposed breakdown as a numbered list of named tickets. For each ticket, show:

1. **Title**: short descriptive name (number rides inside the name)
2. **Blocked by**: which other tickets (if any) must complete first — cite each by number and name
3. **What it delivers**: end-to-end behaviour (tracer-cut), testable capability (module-build), or expand/migrate/contract step (expand-contract)

Ask the user:

1. Does the granularity feel right? (too coarse / too fine)
2. Are the blocking edges correct — does each ticket only depend on tickets that genuinely gate it?
3. Should any tickets be merged or split further?

Iterate until the user approves the breakdown. Done: user approved the breakdown.

### 6. Publish

When the user approves the breakdown, publish the tickets per [[PUBLISH.md]]. Set `Stage: slice` and `Updated:` on `project.md` per [[doc/agents/project-status.md]]. Done: each approved ticket is one file, the frontier is named, and project Stage is `slice`.
