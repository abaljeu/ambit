# Implement issue 11 — delete triage; QA files valid tracker issues

Deleted [[.agents/skills/triage/SKILL.md]] and its companions. [[.agents/skills/qa/SKILL.md]] stays the conversational file-issues skill. An issue it files is a tracker file: bold `**Status:**` with a locked Status value, pointing at [[doc/agents/issue-tracker.md]]. It does not copy the `/to-tickets` template. Product F# was not touched. [[.cursor/rules/gambol.mdc]] and [[.agents/skills/ask-matt/SKILL.md]] were not edited.

## What changed

- Deleted the triage skill and companions: SKILL.md, AGENT-BRIEF.md, OUT-OF-SCOPE.md, agents/openai.yaml.
- QA publish step requires numbered `NN-slug.md`, a title heading, and bold `**Status:**`. Newly filed issues with reproduction steps use `ready-for-agent`. Tracker fields follow [[doc/agents/issue-tracker.md]]. The body stays What happened / expected / steps, not Context / What to build.
- Ticket [[11-delete-triage-qa-files-tracker-issues.md]] is `**Status:** done`.

## How verified

- `.agents/skills/triage/` has no remaining files.
- QA has no `/triage` and no GitHub-label apply step. Both templates carry `**Status:** ready-for-agent`. QA names [[doc/agents/issue-tracker.md]] and [[.agents/skills/to-tickets/SKILL.md]] (do not copy).
- `rg "GitHub label|/triage"` on owned QA path is empty.

## Leftover risks

- [[.agents/skills/ask-matt/SKILL.md]] still names `/triage` (bugs on-ramp; grilling list). Left for ticket 12.
- [[.cursor/rules/gambol.mdc]] still names [[doc/agents/triage-labels.md]] as triage-role vocabulary. It does not name `/triage`. Ticket 12 owns that index.
- [[.agents/skills/setup-matt-pocock-skills/]] still mentions `/triage` and installing triage. Ticket 04.
- Live `plan/**/issues/` headers are not migrated (ticket 13).
- QA is not listed in gambol.mdc. Ticket 12.
