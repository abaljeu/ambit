---
name: implement
description: Build a ticket under plan/<slug>/issues/; consume arch.md when present.
disable-model-invocation: true
---

# Implement

Entry for building a ticket. Start here. Keep going. Do not stop for review between a failing test and the code that makes it pass.

## Process

### 1. Load the ticket

Prefer a ticket under `plan/<slug>/issues/`. Fetch the path or number the user names; read its full body and comments. Load the Project via [[.agents/skills/project-work/SKILL.md]]. Done: you have the ticket text and the Project slug.

### 2. Load architecture when present

If `plan/<slug>/arch.md` exists, read it. Respect its Module map names, Seams (especially the test seam), and Sequence when building. When `arch.md` is absent, build from the ticket alone — old Projects need no migration. Leave existing specs as written, including Implementation Decisions still on old specs. Done: either arch constraints are in hand, or you confirmed there is no `arch.md`.

### 3. Code

Git: follow [[.agents/skills/git-protocol/SKILL.md]].

F# layout, targeted tests, and the Client compile gate: [[.agents/skills/implement-fsharp-feature/SKILL.md]]. Shared.Tests coverage: [[.agents/skills/add-shared-test/SKILL.md]].

On first implement for this Project, set `Stage: build`.

What a good test is, seams, and red-green: [[.agents/skills/tdd/SKILL.md]]. Use tdd at the test seam from arch when present, else at pre-agreed seams. Do not copy that loop here.

Run typechecking and the directly relevant tests as you go. 

### 4. Check

Run the full suite once at the end as a background task. Do not start the full suite before coding is complete. While you wait, use [[.agents/skills/code-review/SKILL.md]].

Anything you write on tickets or under `reports/` — number and name every section and list item per [[.agents/rules/refer-by-name.md]].


### 4. Log time and finish
Done: the ticket's What to build is implemented and verified; Set ticket `**Status:** coded`. Do not set `done`. `done` is review approval only ([[doc/agents/triage-labels.md]]).

Time: on issues you touched, append `## Time` and keep `Actual:`; on the project set/keep `Started:` / `Finished:` / `Actual:` per [[doc/agents/issue-tracker.md]] (Time tracking). Backfill from this chat and commits when a session was not logged.
