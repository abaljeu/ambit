---
name: to-feature-tickets
description: Break a plan, spec, or conversation into tickets of cohesive testable functionality, each declaring its blocking edges, published as one file per ticket under plan/.
disable-model-invocation: true
---

# To Feature Tickets

Break a plan, spec, or conversation into **tickets**. Each ticket is one cohesive piece of functionality that you can test on its own. Each ticket names the tickets that **block** it.

## Process

### 1. Gather context

Work from whatever is already in the conversation context. If the user passes a reference (a spec path, an issue number or URL), fetch it and read its full body and comments.

### 2. Explore the codebase (optional)

If you have not already explored the codebase, do so. Ticket titles and descriptions should use the project's domain glossary vocabulary, and respect ADRs in the area you're touching.

### 3. Draft feature tickets

Break the work into tickets of **cohesive testable functionality**.

- One ticket is one capability that hangs together and can be proven with tests
- A finished ticket is verifiable once its blockers are done — later tickets are not required to know whether this one works
- Size each ticket to fit in a single fresh context window
- Split when two capabilities can be tested independently; merge when a split would leave an untestable fragment
- After testable functionality is covered, add tickets for user-visible functionality.

Give each ticket its **blocking edges** — the other tickets that must complete before it can start. A ticket with no blockers can start immediately.

### 4. Quiz the user

Present the proposed breakdown as a numbered list. For each ticket, show:

- **Title**: short descriptive name
- **Blocked by**: which other tickets (if any) must complete first
- **What it delivers**: the functionality this ticket makes work, and how you would know it works

Ask the user:

- Does the granularity feel right? (too coarse / too fine)
- Are the blocking edges correct — does each ticket only depend on tickets that genuinely gate it?
- Should any tickets be merged or split further?

Iterate until the user approves the breakdown.

### 5. Publish the tickets

Publish the approved tickets.

Write one file per ticket under `plan/<feature-slug>/issues/<NN>-<slug>.md`, numbered from `01` in dependency order (blockers first). Each file's "Blocked by" lists the numbers/titles it depends on. Use the per-ticket file template below — one ticket per file. Include one or two pointers to defining specs from which the ticket was derived. Not just `spec.md` but more trace back to decision files.

Do not close or modify any parent issue.

<local-ticket-template>

# <NN> — <Ticket title>

**Status:** ready-for-agent
**Blocked by:** the numbers/titles of the tickets that gate this one, or "None — can start immediately".

## Context
Explain the scenario where the newly built feature will be applied. Write in the style of [[.agents/skills/wait-what/SKILL.md]].

## What to build
The functionality this ticket makes work — not a layer-by-layer implementation list. Write in the style of [[.agents/skills/wait-what/SKILL.md]].

- [ ] Acceptance criterion 1
- [ ] Acceptance criterion 2

## Comments
Add this when there is a comment.

## See also
One or two wikilinks to the defining spec and decision files this ticket was derived from (not only the parent `spec.md`).

</local-ticket-template>

Avoid code snippets — they go stale fast. Exception: if a prototype produced a snippet that encodes a decision more precisely than prose can (state machine, reducer, schema, type shape), inline it and note briefly that it came from a prototype. Trim to the decision-rich parts.
