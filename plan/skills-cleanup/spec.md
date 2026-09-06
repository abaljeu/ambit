# Skills cleanup

The Locked section on [[plan/skills-cleanup/project.md]] is the destination. This spec is the build order for later obedience. It is not a ticket.

## Problem Statement

Agents follow two homes, two routers, and two Status lists. Live tickets still say `open` and `resolved`. Skills still teach setup-matt, extra git places, and a stop-for-review test loop. Alan locked one destination. The files do not yet obey it.

## Solution

Cut only instruction files and `plan/` tickets. Do not touch product F#.

**First operation (mandatory):** move repo-shared skills from [[.cursor/skills/]] to [[.agents/skills/]] so later edits happen in one home. Then flip the prepare-agent-instruction-change rule that forbids that home. Then write the locked Stage and Status lists into the canonical tracker docs. Then edit skill and rule bodies so they agree (Ask-Matt and gambol.mdc, Wayfinder points at the tracker, no duplicate implement loops). Last, migrate live ticket Status values and bold field labels.

This is several slices, not one pull request.

## User Stories

1. As a coding agent, I want one skill home under [[.agents/skills/]], so that I do not guess between two directories.
2. As a developer, I want the first operation to be that move, so that later body edits stay clean.
3. As a coding agent, I want Ask-Matt and gambol.mdc to name the same jobs in the same sequence, so that I do not skip Gambol adapters.
4. As a developer, I want Ask-Matt to stay the human-advisor skill, so that a human-advisor route remains without a vendor merge.
5. As a coding agent, I want one Stage list (`chart` | `spec` | `slice` | `build` | `done` | `dead`), so that I do not write `charting` or `tickets` as a Stage.
6. As a coding agent, I want one ticket Status list (`ready-for-agent` | `ready-for-human` | `needs-info` | `blocked` | `done` | `cancelled`), so that takeable and closed mean one thing.
7. As a coding agent, I want takeable tickets to be `ready-for-agent` or `ready-for-human`, so that the frontier is a Status scan, not a claim verb.
8. As a coding agent, I want closed work to be `done` only, so that I do not leave `resolved`, `closed`, or `agent-done` on tickets.
9. As a coding agent, I want `cancelled` for reject or abandon of a ticket, so that I do not use Stage `dead` on a ticket.
10. As a coding agent, I want bold field labels (`**Status:**`, `**Type:**`, and the same for other inline properties), so that ticket headers have one form.
11. As a developer, I want both ticket skills kept, so that tracer-bullet slices and cohesive capabilities stay distinct.
12. As a coding agent, I want a shared ticket publish core later, so that the two ticket skills do not copy the same template twice.
13. As a coding agent, I want Wayfinder to be process and the issue-tracker doc to be structure, so that I do not copy GitHub-shaped ops into the skill.
14. As a coding agent, I want all git work on `dev` (then `ready` / `master`), so that I do not open `research/`, `prototype/`, `vendor/`, or `update/` places.
15. As a developer, I want the vendor merge and setup-matt bootstrap gone, so that coding agents do not re-scaffold a tracker this repo already has.
16. As a coding agent, I want `/grill-me` as the default grill entry, so that I do not start `/grill-with-docs` by habit.
17. As a coding agent, I want reports under `plan/<slug>/reports/`, so that research, review, and architecture notes land in one place.
18. As a coding agent, I want architecture review as Markdown in that reports folder, so that I do not write HTML in OS temp.
19. As a developer, I want request-refactor-plan to create and chart a new Project, so that it is not a third spec or ticket skill in a line.
20. As a coding agent, I want domain-modeling to write [[CONTEXT.md]] and say Committed Decision, so that I do not open a second glossary or say ADR.
21. As a coding agent, I want `/implement` as the entry, with tdd and the F# skills as references that augment it, so that I do not run two test loops.
22. As a developer, I want the testing-workflow always-apply rule gone, so that stop-for-review does not fight `/implement`.
23. As a developer, I want the triage skill deleted, so that coding agents do not apply GitHub labels to Markdown files.
24. As a developer, I want QA kept as the conversational file-issues skill, so that bugs still get filed without becoming a pointer to the ticket template.
25. As a coding agent, I want to-spec to publish `spec.md` on the Project only, so that a spec is not a ticket with `ready-for-agent`.
26. As a developer, I want live tickets migrated after the instruction files agree, so that `open` becomes takeable and old closed words become `done`.
27. As a coding agent, I want `blocked` kept even when no live ticket uses it, so that a named wait still has a Status.
28. As a coding agent, I want no setup-matt precondition, so that I start from the live tracker docs.

## Implementation Decisions

Where we cut: instruction files under [[.agents/skills/]], [[.cursor/skills/]], [[.cursor/rules/]], [[doc/agents/]], and [[CONTEXT.md]]; then live `plan/` ticket headers. Not product F#.

1. **First operation.** Move repo-shared skills from [[.cursor/skills/]] to [[.agents/skills/]]. After this, later edits happen in one home. Today's split is not the destination. Retarget pointers that still name the old directory. This slice is mandatory and comes first.
2. **Rule flip.** Instruction edits go through [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]] (inventory, edit the most specific place, drop copies, update gambol.mdc when files move). That skill currently forbids storing repo-shared skills in [[.agents/skills/]]. Flip that rule to match the home lock. Do this immediately after the move so the next slices are legal.
3. **Canonical docs.** Write Stage and Status v2 into [[doc/agents/project-status.md]] (Stage already matches) and [[doc/agents/triage-labels.md]] plus [[doc/agents/issue-tracker.md]] (Status still copies the old set). Takeable = `ready-for-agent` or `ready-for-human`. Closed = `done`. `cancelled` is ticket reject or abandon, not Stage `dead`. Drop `needs-triage`, `wontfix`, `open`, `resolved`, `claimed`, `closed`, `agent-done`, `in-progress`.
4. **Routers agree.** Ask-Matt is the human advisor. gambol.mdc is the primary agent instruction file. They must list the same jobs, same skill names, same sequence. Do not skip Gambol adapters. Do not delete one router.
5. **Ticket skills.** Keep both. Later extract the common publish path, template, and tracker wiring. Leave both named skills.
6. **Wayfinder.** Process only (when to research, grill, or prototype; how to chart a destination). Point at [[doc/agents/issue-tracker.md]] for claim, Type, Status, frontier, and file layout. Do not copy tracker ops. Do not teach topic branches.
7. **Git.** All work on `dev`, then `ready` / `master`. Drop `research/`, `prototype/`, `vendor/`, and `update/` teaching.
8. **Obsolete vendor path.** Do not run update-matt-skills or setup-matt. Delete or archive those skills and seed copies later. Ask-Matt stays.
9. **Grill.** Default entry is grill-me. Other grill skills are not the default invoke. Grilling is a method, not a Stage.
10. **Reports.** `plan/<slug>/reports/` unless a skill already names another path. Architecture review is Markdown there, not OS temp HTML.
11. **Planning.** request-refactor-plan creates and charts a new Project (like Wayfinder). to-spec publishes `spec.md` only. It must not file a spec as a ticket.
12. **Glossary.** domain-modeling writes [[CONTEXT.md]]. Always say Committed Decision, not ADR. ubiquitous-language is not a second glossary owner. Do not create UBIQUITOUS_LANGUAGE.md.
13. **Implement.** Entry is `/implement`. Ditch the testing-workflow always-apply rule. tdd, implement-fsharp-feature, and add-shared-test stay as referenced augmentations. They must not copy a second loop.
14. **Triage and QA.** Delete the triage skill and its pointers. Keep QA as process. Issues QA files must be valid tracker files (bold Status and the locked Status values) without turning QA into a pointer skill.
15. **Live ticket migrate (last).** `open` → takeable (`ready-for-agent` unless the ticket is clearly human). `resolved` / `closed` / `agent-done` → `done`. Any `wontfix` → `cancelled`. Unbolded `Status:` → `**Status:**` (same for other fields). Do not migrate in the same slice as the skill-home move.
16. **Size.** Several slices. Not one pull request. Forced Stage gates wait; this spec does not install them.

## Testing Decisions

This is not F# product work. A good test checks published instruction behavior, not “did we edit the file.”

Prefer repo search invariants after each slice:

- Repo-shared skills live under [[.agents/skills/]] (no leftover live workflow skills only under [[.cursor/skills/]]).
- One Status list in the canonical tracker docs; ticket headers do not use unbolded `Status:`.
- Ask-Matt skill names match the gambol.mdc index (same jobs, same sequence).
- `/implement` and tdd do not publish two contradictory loops.
- No skill says run setup-matt first.
- Wayfinder does not copy claim, Status verbs, or topic-branch ops from a second tracker story.
- Reports skills name `plan/<slug>/reports/` (architecture review included).
- to-spec names `spec.md` and does not apply `ready-for-agent` to a spec-as-ticket.

Prior art: the Status inventory search in [[plan/skills-cleanup/reports/status-reconsider-inventory.md]] (first-token counts on `plan/**/issues/`). Re-run that class of search after migrate.

## Out of Scope

Out of scope for [[plan/skills-cleanup/spec.md]]:

- Product F# and Core.
- Rewriting the whole Roadmap map body.
- Status fields on Committed Decisions or Learning Records.
- Forced Stage gates (skills write Stage; they do not refuse work by Stage yet).
- Installing git-guardrails.
- Foreign-stack skills (TypeScript, Husky, shoehorn, prototype `pnpm`/`bun`) unless they auto-invoke.
- loop-me unless someone runs it here.
- One-shot delivery of every later-obedience item in a single change.

## Further Notes

Locked decisions live on [[plan/skills-cleanup/project.md]]. Later-obedience punch list: [[plan/skills-cleanup/reports/unresolved-from-initial-chart.md]]. Status set: [[plan/skills-cleanup/reports/lock-status-set-v2.md]].

Who writes Stage (already locked): `/wayfinder` → `chart`; `/to-spec` → `spec`; ticket skills → `slice`; first implement → `build`; delivered → `done`; abandon → `dead`. Epic and Chapter skip `slice`. Roadmap has no Stage and no Status. Tickets have no Stage.

This file is the spec. It is not a ticket. It has no `**Status:**`.
