# Publish tickets

Write one file per ticket under `plan/<feature-slug>/issues/<NN>-<slug>.md`. Number from `01` in dependency order (blockers first). Each file's **Blocked by** lists the numbers and names it depends on. Number every section and every checklist item; give each a name per [[.agents/rules/refer-by-name.md]]. Use the template below. One ticket per file. Include one or two pointers to defining specs from which the ticket was derived. Trace back to decision files, not only `spec.md`.

Record the **frontier** (unblocked tickets whose Status names the next action, per [[doc/agents/triage-labels.md]]) for a later implement. For a purely linear chain that is the first ticket. Name it in the reply. Do not write `ready-for-agent` or `ready-for-human`. Do not rewrite Status on existing tickets.

Leave the parent issue as it is. Publish the child tickets only.

Existing tickets stay in their current shape. See [[.agents/rules/no-retrofit.md]].

<local-ticket-template>

# <NN> — <Ticket title>

**Status:** a value from [[doc/agents/triage-labels.md]] that names the next action (`needs-info`, `blocked`, or later `coded`). Never `ready-for-agent` or `ready-for-human`.
**Blocked by:** the numbers and names of the tickets that gate this one, or "None — can start immediately".

## 1. Context
Explain the scenario where the newly built feature will be applied. Write in the style of [[.agents/skills/wait-what/SKILL.md]].

## 2. What to build
The end-to-end behaviour this ticket makes work, from the user's perspective — not a layer-by-layer implementation list. Write in the style of [[.agents/skills/wait-what/SKILL.md]].

1. [ ] <criterion name> — Acceptance criterion
2. [ ] <criterion name> — Acceptance criterion

## 3. See also
One or two wikilinks to the defining spec and decision files this ticket was derived from (not only the parent `spec.md`).

</local-ticket-template>

When a ticket has a comment, add a ## 4. Comments section with that comment. Omit ## Comments when there is none.

Write the ticket in prose. Inline a code snippet only when a prototype encoded a decision more precisely than prose can (state machine, reducer, schema, type shape). Note that it came from a prototype. Trim to the decision-rich parts — not a working demo, just the important bits.

Done when each approved ticket is one file in that shape, the frontier is named, and the parent issue is unchanged.
