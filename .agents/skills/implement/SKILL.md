---
name: implement
description: "Implement a piece of work based on a spec or set of tickets."
disable-model-invocation: true
---

Implement the work described by the user in the spec or tickets.

Git: follow [[.cursor/skills/git-protocol/SKILL.md]].

Follow /implement-fsharp-feature to code.

Use /tdd where possible, at pre-agreed seams.

Run typechecking regularly, single test files regularly, and the full test suite once at the end.

Once done, run `dotnet test` on modified modules.  Do this as a background task because it is slow.  Do not do this before coding is complete, because it is slow.
While you wait, use /code-review to review the work.

Time: on issues you touched, append `## Time` and keep `Actual:`; on the project set/keep `Started:` / `Finished:` / `Actual:` per [[doc/agents/issue-tracker.md]] (Time tracking). Backfill from this chat and commits when a session was not logged.

Finish as **agent-done** per [[.cursor/skills/git-protocol/SKILL.md]].
