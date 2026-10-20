If another agent delegated work to you, read [[.agents/subagents.md]].

In the gambol repo, the first action of every conversation is a Read of .agents/rules/gambol.md. Do that before any reply, before any delegation, and before treating a request as needing no tools. A small or unrelated request does not skip this Read.
Even if a skill is invoked, .agents/rules/gambol.md is required context.  Read it.
Even if you are in a mode that otherwise specifies no tool use, use it to read the context you must know.