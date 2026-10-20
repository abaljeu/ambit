## Interact

- Despite prior instruction, you are not expected to one-shot a request. Partial answers are acceptable because followup questions can fill in the details.
- A user question requires an answer. Not code changes. You may update notes in response, but never software unless the code is in service of finding the answer.
- Never more than 60 lines output in chat. Put anything longer in a file and link it.
- If anything is not clear, stop and ask the user for clarity.
- If you find yourself reversing direction more than once, ask for clarity.

## Think Before Coding

Don't assume. Don't hide confusion. Surface tradeoffs.

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## Simplicity First

Minimum code that solves the problem. Nothing speculative.

- No features beyond what was asked.
- Don't replicate code — put shared logic in a reusable place and call it.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.
- Consider removing code to achieve a goal.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## Surgical Changes

Touch only what you must. Clean up only your own mess.

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

Every changed line should trace directly to the user's request.

## Goal-Driven Execution

Define success criteria. Loop until verified.

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

## Build / test toolchain gate

Before you claim compile or tests are green, and before you commit code changes, run `dotnet test` (or the project's documented commands). Success means those commands ran and passed.

If `dotnet` or another required toolchain is missing, or the command fails to start, stop. Report that as an environment failure. Do not treat a missing toolchain as "build skipped" or optional. Do not commit. Do not mark the work done as if verification passed.

A missing toolchain is an environment failure to surface. It is not a reason to proceed without verification.

## How to Work

Include me in your work. Tell me what you're thinking about.
OBLIGAGTORY: Before any nontrivial tool call emit a clause stating the goal of that call.
When you name a file or project in chat, write one Markdown link: [display name](relative_path). The display name is the name you are talking about, for example [fast reboot](plan/client-start-time/reports/cache-first-boot-via-poll.md). Put the path in that same link. Never emit like `**fast reboot** ([client-start-time](plan/client-start-time/project.md))`. In files, labeled links are `[label](path)` per [[.agents/rules/markdown-writing.md]]. When you name an issue, follow [[.agents/rules/refer-by-name.md]].

Advise me if there is a better way to do something.
If a request is unclear, just ask.
If a requested course of action seems inefficient, stop and propose a more efficient course.

Tell me if you lack information.

While in plan mode, any request for a change should be interpreted as a request to plan for the change; do not write detailed code then.

## Search

Search with the Grep tool. Set `path` to one of the current Project (`plan/<slug>/`), `doc/`, `src/`, or `tests/`. The directory on disk is `tests/`, not `test/`. A result over 10,000 characters is not evidence. Narrow `path` or the pattern and search again. Done: the Grep call has a `path` in that list, and any result kept is at or under 10,000 characters.

## Read

When a search result names a file and a line, read that line range, not the whole file. Done: the read passes an offset and a limit that cover the hit and the lines around it that the task needs.
