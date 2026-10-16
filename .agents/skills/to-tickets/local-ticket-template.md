<local-ticket-template>

# <NN> — <Ticket title>

**Status:** `defined` when fully specified (implement when Blocked-by is clear); `needs-info` when information is missing; or later `coded`. Never `blocked`, `ready-for-agent`, `ready-for-human`, or `ready-to-implement`.
**Blocked by:** the numbers and names of the tickets that gate this one, or "None — can start immediately".
**Trackers:** the tracker names this ticket touches.

## Context
Explain the scenario where the newly built feature will be applied.  Use paragraphs.  Write in the style of [[.agents/skills/wait-what/SKILL.md]].

## What to build
The end-to-end behaviour this ticket makes work, from the user's perspective — not a layer-by-layer implementation list. Write in the style of [[.agents/skills/wait-what/SKILL.md]].  Write as a checklist.
[ ] Behavior
[ ] Behavior

### Acceptance Criteria
The layer-by-layer implementation list, including the architecture decisions it enforces.

[ ] Acceptance criterion
[ ] Acceptance criterion

## Finish-up
Insert these verbatim:

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also
Each source link for this ticket, once.

</local-ticket-template>
