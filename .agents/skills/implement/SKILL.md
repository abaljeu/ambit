---
name: implement
description: Build a ticket under plan/<slug>/issues/; read every binding arch.md before coding.
disable-model-invocation: true
---

# Implement

Entry for building a ticket. Start here. Keep going. Do not stop for review between a failing test and the code that makes it pass.

## Process

### 1. Load the ticket

Prefer a ticket under `plan/<slug>/issues/`. Fetch the path or number the user names; read its full body and comments. Load the Project via [[.agents/skills/project-work/SKILL.md]]. Done: you have the ticket text and the Project slug.

### 2. Load binding architecture

Binding arch files are the ticket's own `plan/<slug>/arch.md`, every file named on the ticket **Binding arch** line, and every other `plan/*/arch.md` whose modules or doors the change touches. Find those others by grepping `plan/*/arch.md` for the modules, files, and doors the change names. Before coding, those names come from the plan. When a diff exists, they come from that diff. Read each binding file. Respect its Module map names, Seams (especially the test seam), and Sequence. When no binding `arch.md` exists, build from the ticket alone. Old Projects need no migration. Leave existing specs as written, including Implementation Decisions still on old specs.

A planned change that contradicts a binding rule stops here. Report the arch file, the section, and the conflict. Leave the code unchanged until the rule and the plan agree.

Done: binding rules are in hand, or you confirmed no `arch.md` binds this change. A contradiction is reported and coding has not started.

### 3. Code

Git: follow [[.agents/skills/git-protocol/SKILL.md]].

F# layout, targeted tests, Client compile gate, and the end-of-build suite: [[.agents/skills/implement-fsharp-feature/SKILL.md]]. Shared.Tests coverage: [[.agents/skills/add-shared-test/SKILL.md]].

On first implement for this Project, set `Stage: build`.

What a good test is, seams, and red-green: [[.agents/skills/tdd/SKILL.md]]. Use tdd at the test seam from arch when present, else at pre-agreed seams. Do not copy that loop here.

Run typechecking and the directly relevant tests as you go. Done: the ticket's What to build is coded; targeted checks from the F# skill have been run.

### 4. Check

While the end-of-build suite from [[.agents/skills/implement-fsharp-feature/SKILL.md]] runs in the background, use [[.agents/skills/code-review/SKILL.md]]. The Arch pass in that review is the re-check of the binding rules from step 2. When this run writes a PR body, use [[.agents/skills/pr/SKILL.md]] after that review. [[.agents/skills/retro/SKILL.md]] is a separate human invoke.

Anything you write on tickets or under `reports/` — number and name every section and list item per [[.agents/rules/refer-by-name.md]]. Done: that Arch pass has been run against the binding rules from step 2. Code-review has been run for this change. The suite was started only after coding finished.

### 5. Log time and finish

Done: the ticket's What to build is implemented and verified. Set ticket `**Status:** coded` only when the step 4 re-check found no contradiction. Do not set `done`. `done` is review approval only ([[doc/agents/triage-labels.md]]).

Time: on issues you touched, append `## Time` and keep `Actual:`; on the project set/keep `Started:` / `Finished:` / `Actual:` per [[doc/agents/issue-tracker.md]] (Time tracking). Backfill from this chat and commits when a session was not logged.
