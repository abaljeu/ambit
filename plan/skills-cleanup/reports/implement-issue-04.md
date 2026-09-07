# Implement issue 04 — remove vendor merge and setup-matt

Deleted the live vendor-merge skill and the setup-matt seed copies. Live skills no longer say run setup-matt first. Ask-Matt remains. Product F# was not touched. Sibling-owned files were not edited.

## What changed

- Deleted [[.agents/skills/update-matt-skills/]] (skill plus pull/flatten/commit/merge scripts). It is not a live workflow.
- Deleted leftover seed copies under [[.agents/skills/setup-matt-pocock-skills/]].
- Stripped the run-setup-matt-first line from [[.agents/skills/to-tickets/SKILL.md]], [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/to-spec/SKILL.md]], and [[.agents/skills/triage/SKILL.md]].
- Stripped the Ask-Matt **Precondition** that named `/setup-matt-pocock-skills`. The router list was not rewritten.
- [[.agents/skills/to-feature-tickets/SKILL.md]] had no setup-matt line; left unchanged.
- Ticket [[plan/skills-cleanup/issues/04-remove-vendor-merge-and-setup-matt.md]] is `Status: done`.

## How verified

- No `SKILL.md` under [[.agents/skills/]] contains `setup-matt`, `update-matt`, `vendor/mattpocock`, or `update/mattpocock`.
- `test -e` on both deleted skill directories is ABSENT.
- [[.agents/skills/ask-matt/SKILL.md]] still exists and no longer names setup-matt.

## Leftover risks

- [[.cursor/rules/gambol.mdc]] still lists [[.agents/skills/update-matt-skills/SKILL.md]] as a live workflow. Ticket 10 owns that file; leave the line for that worker.
- Chart-time reports, [[plan/skills-cleanup/spec.md]], and [[plan/done/]] still name the deleted skills and the `vendor/` / `update/` places. Those are history, not live instruction.
- Ask-Matt still names `/triage` and a `prototype/<name>` branch. Tickets 11, 12, and 05 own those rewrites.
- to-spec still says publish to the issue tracker and apply `ready-for-agent`. Ticket 06 owns that.
- Wayfinder still copies GitHub-shaped ops and `research/<name>` places. Ticket 05 owns that. Only the setup-matt sentence was removed.
- Triage still exists as a live skill. Ticket 11 deletes it. Only the setup-matt sentence was removed.
