---
name: wayfinder
description: Plan a huge chunk of work ( more than one agent session can hold ) as a shared map of decision tickets on your issue tracker, and resolve them one at a time until the way to the destination is clear.
disable-model-invocation: true
---

A loose idea has arrived ( too big for one agent session, and wrapped in fog: the way from here to the **destination** isn't visible yet. Wayfinding is about finding that way, not charging at the destination. This skill charts the way as a **shared map** on the repo's issue tracker, then works its **decision tickets** — questions whose resolution is a decision, not slices of a build to execute ) one at a time until the route is clear.

The destination varies per effort, and naming it is the first act of charting ( it shapes every ticket. It might be a spec to hand off and iterate on, a decision to lock before planning starts, or a change made in place like a data-structure migration. The map is domain-agnostic ) engineering work, course content, whatever fits the shape.

For claim, Type, Status, frontier, and file layout, follow [[doc/agents/issue-tracker.md]]. Commits follow [[.agents/skills/git-protocol/SKILL.md]]. Map body, Ticket Types, Fog, and Out of scope: [[MAP.md]].

## Plan, don't do

Wayfinder is **planning** by default: each ticket resolves a decision, and the map is done when the way is clear ( nothing left to decide before someone goes and does the thing. The pull to just do the work is usually the signal you've reached the edge of the map and it's time to hand off. An effort can override this in its **Notes** — carrying execution into the map itself ) but absent that, produce decisions, not deliverables.

Delegate research, but not grilling.  The user cannot easily review questions posed by the subagent.

## Refer by name

Follow [[.agents/rules/refer-by-name.md]]: number and name every ticket, map section, and list item. Decisions-so-far is one of those surfaces.

## Invocation

Two modes. Either way, **never resolve more than one ticket per session** — with the exception of research tickets.

### Chart the map

User invokes with a loose idea.

1. **Name the destination.** Run a `/grilling` and `/domain-modeling` session to pin down what this map is finding its way to — the spec, decision, or change. The destination fixes the scope, so it's settled first. Done: Destination is named in one or two lines.
2. **Map the frontier.** Grill again, **breadth-first** this time: fan out across the whole space rather than deep on any one thread, surfacing the open decisions and the first steps takeable now. **If this surfaces no fog** ( the way to the destination is already clear, the whole journey small enough for one session ) you don't need a map. Stop and ask the user how they'd like to proceed. Done: open decisions and first takeable steps are named, or the user has been asked how to proceed without a map.
3. **Create the map** per the tracker and [[MAP.md]]: numbered sections (Destination, Notes, Decisions so far, Not yet specified, Out of scope); Destination and Notes filled in, Decisions-so-far empty, the fog sketched into **Not yet specified**. Done: `map.md` exists with those sections in that shape.
4. **Create the tickets you can specify now** as children of the map ( then wire blocking in a **second pass** per the tracker (tickets need identities before they can reference each other). Wiring sorts them into the frontier and the blocked; everything you can't yet specify stays in the fog ) the **Not yet specified** section. Done: every sharp question is a child ticket with blocking wired, and remaining fog lives only under **Not yet specified**.
5. **Fire the research subagents.** For each `research` ticket you just created, spin up a `/research` subagent to resolve it in parallel. Findings land where [[.agents/skills/research/SKILL.md]] writes. Leave a context pointer from the ticket. Done: every charted `research` ticket has a running or completed research subagent and a context pointer on the ticket.
6. Stop — charting is one session's work; it hand-resolves nothing. Done: no decision ticket was hand-resolved in this charting session.

### Work through the map

User invokes with a map. A ticket is **optional** — without one, you pick the next decision, not the user.

1. Load the **map** — the low-res view, not every ticket body. Done: Destination, Notes, Decisions so far, Not yet specified, and Out of scope are in hand.
2. Choose the ticket. If the user named one, use it. Otherwise take the first frontier ticket in order. **Claim it** per the tracker before any work. Done: one ticket is claimed and its Question is loaded.
3. Resolve it — **zoom as needed**: fetch the full body of any related or already resolved ticket on demand; invoke the skills the `## 2. Notes` block names. If in doubt, use `/grilling` and `/domain-modeling`. Done: the ticket's question has an answer ready to record (or the ticket is ruled out of scope).
4. Record the resolution per the tracker, and **append a context pointer** to the map's Decisions-so-far. Done: Answer is on the ticket, Status follows the tracker, and Decisions so far has the new gist line.
5. Add newly-surfaced tickets (create-then-wire); graduate any fog the answer has made specifiable, clearing each graduated patch from **Not yet specified** so it lives only as its new ticket. If the answer reveals a ticket ( this one or another ) sits beyond the destination, **rule it out of scope** rather than resolving it on the route. If the decision invalidates other parts of the map, update or delete those tickets. Done: frontier, fog, and Out of scope match the resolution; invalidated tickets are updated or removed.

The user may run frontier tickets in parallel, so expect other sessions to be editing the tracker concurrently.
