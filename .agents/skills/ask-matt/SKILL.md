---
name: ask-matt
description: Ask which skill or flow fits your situation. A router over the skills in this repo.
disable-model-invocation: true
---

# Ask Matt

You do not remember every skill, so ask. This file is the human advisor. Default grill is `/grill-me`. The job catalog lives in [[.agents/rules/gambol.md]] (primary Agent instruction). Both routers remain. Do not copy that catalog here.

A **flow** is a path through the skills. Most paths run along **idea to ship**. On-ramps merge onto it. Gambol adapters apply on every path. Do not skip them. Names and sequence are in that catalog.

## Idea to ship

Start with `/grill-me`. If a question needs a runnable answer (state, business logic, a UI you have to see), detour through `/prototype`, bridged by `/handoff` in both directions. Capture the prototype on `dev`, then `ready` / `master`.

Multi-session build: `/to-spec` (spec.md on the Project, not a ticket), then `/to-tickets` or `/to-feature-tickets`, each ticket declaring its **blocking edges**. Work blockers-first. `/implement` per ticket, `/clear`ing context between each one. Same-session build: `/implement` in this window.

Either way, `/implement` is the entry. It drives `/tdd` at pre-agreed seams, then `/code-review` (Standards + Spec) before commit. Reach for `/tdd` alone when you want one behaviour test-first. Reach for `/code-review` alone to review a diff.

Keep grill, spec, and tickets in **one unbroken context window**. Each `/implement` starts fresh from the ticket. The limit is the [smart zone](https://www.aihero.dev/ai-coding-dictionary/smart-zone) (~150k tokens). If a session approaches it before tickets, `/compact` at the nearest phase boundary.

## On-ramps

**Bugs and requests that arrive raw** → `/qa`. It files valid tracker issues. `/implement` later picks them up. Do not run `/qa` on tickets a ticket skill already produced.

**Something is broken** → `/diagnosing-bugs`. For the hard ones: it refuses to theorise until it has a tight feedback loop, then fixes with a regression test. Its post-mortem hands off to `/improve-codebase-architecture` when there is no good seam.

**A huge effort, too big for one session** → `/wayfinder`. It charts a shared map of **decision tickets** and resolves them one at a time until the way is clear. Then it hands off: merge onto idea to ship at `/to-spec`, then a ticket skill and `/implement`. Do not loop the map straight into `/implement` unless the effort is small.

**A refactor to chart** → `/request-refactor-plan`. Like Wayfinder, it creates and charts a Feature-set Project. It is not a third spec or ticket skill in the idea-to-ship line.

## Vocabulary and health

`/improve-codebase-architecture` surfaces **deepening opportunities**. Picking one generates an idea you take into `/grill-me`. `/codebase-design` is the bench for the chosen shape. `/domain-modeling` sharpens domain language and records a hard-to-reverse choice as a Committed Decision. Reach for these when the **words** or the **shape** are the problem; or let the skills above pull them in.

## Gambol adapters

These apply on every path. Do not skip them. The names live in [[.agents/rules/gambol.md]]. Git work stays on `dev`, then `ready` / `master`.

## Standalone

`/grill-me` is the default grill. `/grill-with-docs` leaves a paper trail in [[CONTEXT.md]]; it is not the default invoke. `/grilling` is the primitive: rounds, the frontier, facts are the agent's job and decisions are yours. `/grill-me` and `/grill-with-docs` are named ways in; `/wayfinder` and `/improve-codebase-architecture` run it internally. Reach for `/grilling` directly only when you want the interview with no wrapper.

Research feeds `/grill-me` or `/to-spec`. It does not replace them. `/wait-what` works after a message did not land; `/grill-me` is the upfront cure. `/wizard` is for steps only a human can take. If the agent can do the step, it should.

## Phase boundaries

A **phase** is a chunk of work inside a session — the grilling, the implementation, the QA. At the **boundary** between two of them you have five options:

- **Continue** — stay put. Costs nothing, loses nothing.
- **`/clear`** — empty the window, when nothing here matters to what is next.
- **`/handoff`** — write a portable markdown file. Narrow: only for a **new harness**, a **new directory**, a **colleague**, or forking a side task **mid-phase**.
- **Subagent** — send a tightly-scoped task to its own window and get a report back.
- **`/compact`** — compress this context and seed a fresh session with it. The **default**, at the bottom of the tree rather than the first reach.

Read [[PHASE-BOUNDARIES.md]] for the ordered tree. Make the decision **at** a boundary; mid-phase, continue or split the rest into subagents.
