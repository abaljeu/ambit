---
name: implement
description: "Implement a piece of work based on a spec or set of tickets."
disable-model-invocation: true
---

# Implement

Entry for building a ticket. Start here. Keep going. Do not stop for review between a failing test and the code that makes it pass.

Git: follow [[.agents/skills/git-protocol/SKILL.md]].

F# layout, targeted tests, and the Client compile gate: [[.agents/skills/implement-fsharp-feature/SKILL.md]]. Shared.Tests coverage: [[.agents/skills/add-shared-test/SKILL.md]].

What a good test is, seams, and red-green: [[.agents/skills/tdd/SKILL.md]]. Use tdd at pre-agreed seams. Do not copy that loop here.

Run typechecking and the tests the F# skill names as you go. Run the full suite once at the end as a background task. Do not start the full suite before coding is complete. While you wait, use /code-review.

Time: on issues you touched, append `## Time` and keep `Actual:`; on the project set/keep `Started:` / `Finished:` / `Actual:` per [[doc/agents/issue-tracker.md]] (Time tracking). Backfill from this chat and commits when a session was not logged.

Finish as **agent-done** per [[.agents/skills/git-protocol/SKILL.md]].
