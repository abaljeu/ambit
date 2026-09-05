# Scope vs commitment

**Scope** is local to one effort — a Project spec, Wayfinder map, ticket, or slice. It names what *this work* will not do *now*.

**Commitment** is product-wide — a choice that binds Gambol beyond the current effort. Record it only through an authorized channel (below).

A direction declined for a task is **scope**, not a product exclusion. "Do not build a bus for this slice" is scope. "Gambol excludes plugin buses" is commitment and needs a human-recorded source.

## Authorized commitment channels

A product-wide fact belongs only where the human has placed it:

| Channel | What it may commit |
| --- | --- |
| [[doc/Decisions/]] | A **Committed Decision** — costly to reverse, surprising without context, chosen between genuine alternatives ([[doc/Decisions/README.md]]) |
| [[doc/current/]], [[doc/arch.md]], [[doc/spec.md]] | Implemented or agreed system behavior, after promotion per [[.cursor/skills/maintain-doc-currency/SKILL.md]] |
| [[CONTEXT.md]] | Ubiquitous language — terms and meanings, not exclusions or architecture |

Everything else — `plan/` specs and maps, [[doc/roadmap/]], reports, tickets, agent chat — is **non-authoritative for product commitments**. Treat material there as scope, history, or draft unless promoted.

## Scope wording

Scope statements name the effort they belong to:

- "Out of scope for [[plan/selective-client-loading/spec.md]]"
- "This spec does not include …"
- "Deferred; not decided for the product"

When one effort's scope touches another, say which effort owns the work — as in [[plan/client-start-time/reports/cache-first-boot-via-poll.md]] (IndexedDB caches are out of selective-loading scope because fast reboot owns them, not because they are forbidden).

## Commitment wording

Product commitments state the decision and point at the record:

- "Committed Decision: …" with a link to `doc/Decisions/NNNN-….md`
- "Current behavior: …" with a link to the authoritative `doc/` baseline

## Surmise

**Surmise** is a constraint inferred from code absence, old roadmap text, another project's Out of scope section, or chat history. Surmise is not commitment.

Before writing a product-wide exclusion or "the system …" rule, locate the human-authored source. No source → phrase as scoped deferral or ask. Never aggregate several project scope lines into one product exclusion.

Phrases that signal unjustified promotion (rewrite or downgrade to scope):

- "The system explicitly excludes …"
- "Gambol will never …"
- "Architectural decision: no …" without a Committed Decision file

## When synthesizing or promoting

Applies to `/to-spec`, doc promotion, architecture wikis, and any summary of "what Gambol is":

1. Classify each exclusion as **scope** or **commitment**.
2. Every commitment cites its authorized channel; scope cites the named effort.
3. Unclassified exclusions stay out of `doc/` and out of Committed Decisions until the human confirms.

Done when every product-wide rule in the output traces to an authorized channel, and every deferral names the effort it belongs to.
