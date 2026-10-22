# Wayfinder map — structure

Structure for the shared map and its decision tickets. Chart and Work procedure: [[SKILL.md]]. Tracker fields and file layout: [[doc/agents/issue-tracker.md]].

## The Map

The map is the canonical artifact. Its tickets are children of the map.

The map is an **index**, not a store. It lists the decisions made and points at the tickets that hold their detail; a decision lives in exactly one place ( its ticket ) so the map never restates it, only gists it and links.

### The map body

The whole map at low resolution, loaded once per session. Live tickets are **not** listed — the frontier finds them.

```markdown
## 1. Destination

<what reaching the end of this map looks like — the spec, decision, or change this effort is finding its way to. One or two lines; every session orients to it before choosing a ticket.>

## 2. Notes

<domain; skills every session should consult; standing preferences for this effort>

## 3. Decisions so far

<!-- the index — one numbered line per resolved ticket: enough to judge relevance, then zoom the link for the detail the ticket holds -->

1. [<NN> ( <resolved ticket title>](link) ) <one-line gist of the answer>

## 4. Not yet specified

<!-- see "Fog of war": in-scope fog you can't ticket yet; graduates as the frontier advances; numbered list when items exist -->

## 5. Out of scope

<!-- see "Out of scope": work ruled beyond the destination; never graduates; numbered list when items exist -->
```

### Tickets

Each ticket is a child of the map. Its body is the question, sized to one 100K token agent session:

```markdown
## 1. Question

<the decision or investigation this ticket resolves>
```

Choose a **Type** ( `research`, `prototype`, `grilling`, or `task` (see [Ticket Types](#ticket-types)). Type is a Type axis. Status is a different field, on the tracker. Implementation tickets use `coding` or `bug-fixing` ) see [[doc/agents/issue-tracker.md]] Ticket Type; those are not Wayfinder decision tickets.

The answer is recorded on resolution (see [[SKILL.md]] Work through the map). Assets created while resolving a ticket are linked from the issue, not pasted in.

## Ticket Types

Every ticket is either **HITL** ( human in the loop, worked _with_ a human who speaks for themselves ) or **AFK**, driven by the agent alone. A HITL ticket only resolves through that live exchange; the agent never stands in for the human's side of it (a grilling agent that answers its own questions has broken this).

- **Research** (AFK): Reading documentation, third-party APIs, or local resources like knowledge bases to surface a fact a decision waits on. Resolved by a `/research` **subagent**. Use when knowledge outside the current working directory is required.
- **Prototype** (HITL): Raise the fidelity of the discussion by making a cheap, rough, concrete artifact to react to — an outline, a rough take, a stub, or UI/logic code via the /prototype skill. Links the prototype as an asset. Use when "how should it look" or "how should it behave" is the key question.
- **Grilling** (HITL): Conversation. The default case. Always invoke the /grilling and /domain-modeling skills.
- **Task** (HITL or AFK): Manual work that must happen before a _decision_ can be made ( nothing to decide, prototype, or research, but the discussion is blocked until it's done. Signing up for a service so its API can be judged, provisioning access, moving data so its shape can be seen. This is the one type that _does_ rather than decides ) and it earns its place by unblocking a decision, not by delivering the destination. The agent drives it alone where it can (AFK); otherwise it hands the human a precise checklist (HITL). Resolved when the work is done; the answer records what was done and any resulting facts (credentials location, new URLs, row counts) later tickets depend on.

**Not Wayfinder types — coding / bug-fixing:** feature build/prove → `**Type:** coding`; defect fix → `**Type:** bug-fixing` per [[doc/agents/issue-tracker.md]]. Do not put those on the map as decision tickets; hand off when the way is clear.

## Fog of war

The map is _deliberately_ incomplete: don't chart what you can't yet see. Beyond the live tickets lies the **fog of war** ( the dim view of decisions and investigations you can tell are coming but can't yet pin down, because they hang on questions still open. Resolving a ticket clears the fog ahead of it, graduating whatever's now specifiable into fresh tickets ) one at a time, until the way to the destination is clear and no tickets remain.

The map's **Not yet specified** section is where that dim view is written down: the suspected question, the area to revisit later. It's the undiscovered frontier _toward_ the destination — everything here is in scope, just not sharp enough to ticket. Write as loosely or as fully as the view allows; it doubles as a signpost for collaborators reading where the effort is headed.

**Fog or ticket?** The test is whether you can state the question precisely now — _not_ whether you can answer it now.

- **Ticket when** the question is already sharp — even if it's blocked and you can't act on it yet.
- **Not yet specified when** you can't yet phrase it that sharply. Don't pre-slice the fog into ticket-sized pieces: it's coarser than a ticket, and one patch may graduate into several tickets, or none, once the frontier reaches it.

**Not yet specified** excludes what's already decided (Decisions so far), what's already a live ticket, and what's out of scope (the next section).

## Out of scope

Fog only ever gathers _toward_ the destination. The destination fixes the scope, so work beyond it is **out of scope** — it isn't fog, and it doesn't belong in **Not yet specified**. It gets its own **Out of scope** section on the map: work you've consciously ruled out of _this_ effort. Scope, not sharpness, lands it here.

Out-of-scope work never graduates ( the frontier stops at the destination ) so it returns only if the destination is redrawn, and then as a fresh effort, not a resumption.

Ruling something out of scope is a scoping act, not a step on the route. When a ticket that already exists turns out to sit past the destination ( mis-scoped in while charting, or exposed by a resolution — take it off the frontier per the tracker and leave one line in the **Out of scope** section: the gist plus why it's out of scope, linking the ticket. It stays out of **Decisions so far**, which records the route actually walked ) a scope boundary isn't a step on it.
