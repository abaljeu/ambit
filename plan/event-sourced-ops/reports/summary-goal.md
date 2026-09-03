# Summary rewrite — goal only

## New Summary

Give one semantic standard for how an Actor's Change enters a Graph so every Actor uses the same path and concurrent work merges instead of being refused.

## Why it matches wait-what and CONTEXT

[[.agents/skills/wait-what/SKILL.md]] asks for a re-pitch with a little context, ASD-STE100, and ubiquitous language from [[CONTEXT.md]]. The line states the goal from [[overview.md]] Objective, not the ticket list.

CONTEXT words: **Change** (not mutation), **Graph**, **Actor** (not Agent). **Op** is the mutation inside a Change; the unit that enters the Graph is the Change. **Server** merge and **History** are means; they are not the goal. **Browser** is one Actor, not a second path.

The three aims sit in that one sentence: one mutation path (`every Actor uses the same path`, so a long-running job is the same kind of Actor); merge not refuse (`concurrent work merges instead of being refused`); async work is not a separate product (same clause — every Actor, not a second writer).

## What changed

In [[project.md]] only `Summary:` and `Updated:`. Stage stays `active`. The artifact index below Summary is unchanged. Dropped `issues/01`–`15`, wire migration, Actor spine, recovery, permanent global history, and charting-docs inventory. Set `Updated: 2026-09-02`.
