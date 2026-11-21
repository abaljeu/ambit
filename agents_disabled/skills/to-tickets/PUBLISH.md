# Publish tickets

Write one file per ticket under `plan/<feature-slug>/issues/<NN>-<slug>.md`. Number from `01` in dependency order (blockers first). Each file's **Blocked by** lists the numbers and names it depends on. Keep the template's fixed wrapper sections unnumbered. Inside **What to build**, number and name every checklist item per [[.agents/rules/refer-by-name.md]]. Checklist leaves are numbered tasks (`1. [ ]` / `1. [x]`); no bare `- [ ]`. Use the template below. One ticket per file. Name the defining spec and the decision files this ticket comes from, not only `spec.md`.

Put every source link in **See also** only. A source link is a Markdown link or a wikilink to a non-code file: an arch, a spec, a map, a project, another ticket, a decision, a doc, or a report. Write one **See also** link for each of those files the ticket names, including each arch file. If a returned section contains a source link, move it into **See also** and leave the name in the prose. **Context**, **What to build**, checklist items, **Blocked by**, and **Trackers** state those names in prose. **Trackers** lists tracker names. Do not write a **Binding arch** list. Each source appears once in **See also**. Description prose does not contain `src/` paths. A code file path appears at most once, inside **See also**, and only when the description depends on that file.

Record the **frontier** (unblocked tickets with Status `defined`, per [[doc/agents/triage-labels.md]]) for a later implement. For a purely linear chain that is the first ticket. Name it in the reply. Tickets behind blocking edges also use Status `defined` when fully specified; their **Blocked by** list carries dependency order. Do not write retired Status values. Do not rewrite Status on existing tickets unless the user asks or the ticket is otherwise touched.

Leave the parent issue as it is. Publish the child tickets only.

Existing tickets stay in their current shape. See [[.agents/rules/no-retrofit.md]].

Write tickets in this format:  [[local-ticket-template.md]]

Each fact has one home. **Context** and **What to build** mean what [[.agents/skills/wait-what/SKILL.md]] says. No section restates a fact another section states.

- **What to build** is one checklist. An architecture decision is written on the item it decides.
- **Acceptance Criteria** is the test name and the assertion. A doc, glossary, or Committed Decision item names the file and the change.

Write What to build and Acceptance Criteria as checklists. Inline a code snippet only when a prototype encoded a decision more precisely than prose can (state machine, reducer, schema, type shape). Note that it came from a prototype. Trim to the decision-rich parts — not a working demo, just the important bits.

Done when each ticket in the draft set is one file in that shape, link placement matches the rules above, the frontier is named, and the parent issue is unchanged.
