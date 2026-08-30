---
name: implement
description: "Implement a piece of work based on a spec or set of tickets."
disable-model-invocation: true
---

Implement the work described by the user in the spec or tickets.

Follow /implement-fsharp-feature to code.

Use /tdd where possible, at pre-agreed seams.

Run typechecking regularly, single test files regularly, and the full test suite once at the end.

Once done, run `dotnet test` on modified modules.  Do this as a background task because it is slow.  Do not do this before coding is complete, because it is slow.
While you wait, use /code-review to review the work.


Finish as **agent-done** per [[.cursor/rules/environment.mdc]] / [[CONTEXT.md]]: only commit on the current project branch (`w/*`). If not on `w/*`, leave the dirty tree, suggest a commit message, and offer to create `w/<slug>`.
