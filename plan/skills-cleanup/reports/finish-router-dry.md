# Finish router DRY

Fresh `rg plan/index.md` from the repo root, then thin Ask-Matt. No replacement overview. [[plan/skills-cleanup/reports/strip-plan-index-references.md]] was not trusted until this recount. Free-space was not run (Alan corrected the target).

## Recount-and-strip

### Before

`rg plan/index.md` at the start of this pass: 4 matches in 2 files.

- [[plan/index.md]] — 1 match: the obsolete statement (allowed).
- [[plan/skills-cleanup/reports/strip-plan-index-references.md]] — 3 matches: names the path as the subject of that strip (allowed).

Zero hits in live skills, rules, [[CONTEXT.md]], [[doc/agents/]], or [[plan/roadmap/map.md]]. Ask-Matt and gambol already had no leftover overview path (sibling DRY, `5729de9`).

### After

Same 4 matches in those two files. This report also names [[plan/index.md]] as the subject of the strip (allowed). Nothing else to remove or retarget. No file in the strip set was edited.

## Thin Ask-Matt

[[.agents/skills/ask-matt/SKILL.md]] now keeps advisor role, default `/grill-me`, pointer at [[.agents/rules/gambol.md]], and pointer at [[.agents/skills/ask-matt/PHASE-BOUNDARIES.md]]. The Idea-to-ship, On-ramps, Vocabulary-and-health, Gambol-adapters, Standalone, and inline five-option phase list are gone. Catalog text stays only in gambol.

Dropped the retired catalog line `[[.agents/skills/projects-overview/SKILL.md]] — retired` from [[.agents/rules/gambol.md]]. Deleted that skill file. Historical reports may still name it.

## How verified

- Second `rg plan/index.md`: only allowed leftovers (obsolete file, old strip report, this report).
- Ask-Matt has no per-skill walkthrough headings and no `[[.agents/skills/.../SKILL.md]]` catalog lines.
- gambol Standalone list ends at writing-for-agents. Live `.agents/` and `.cursor/` have no projects-overview pointer.
- Cursor stub [[.cursor/rules/gambol.mdc]] stays an Obey pointer. Bridges not rewritten.

## Out of this pass

Stage-list copies, [[doc/index.md]] vs [[plan/roadmap/map.md]], [[scripts/gitready.sh]].
