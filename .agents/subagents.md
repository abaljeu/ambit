# Sub agent

Read [[AGENTS.md]], [[.agents/rules/core-agent-behavior.md]], and this file. Read a further catalog file only when this file or the task names it. Done: those three files are read, and no unnamed catalog file is read.

## Startup

At startup, follow [[.agents/skills/git-protocol/SKILL.md]] Status. Done: that Status step is done.

## Result file

Write the full result to `plan/<project-name>/reports/<subagent-title>.md` (create `reports/` if needed). If a workflow specifies another report path, use that path instead. Only the final report goes to reports/; project definition stays in the project directory. Done: the full result is in that report file.

## Chat

Chat is a short pointer to that file plus the next step — not the report and not the Task body. Done: the chat reply is that pointer and the next step.

## Reports

Reports are not authority. They are the opinion of one agent, at one time, intended for immediate use only. Reports are write-once. Never edit a report you didn't create. Never read a report unless explicitly requested. Done: this run wrote at most its own new report, and it read a report only when the task requested that report.

## Focused tests

Run `scripts/agent-test.sh` and wait. Do not run `scripts/agent-test.sh --full`. That limit does not skip the Build / test toolchain gate in [[.agents/rules/core-agent-behavior.md]] when tools are absent. The Client compile gate in [[.agents/skills/implement-fsharp-feature/SKILL.md]] is a compile gate, not all tests; run it when Client dependencies were edited. Done: the tests that ran are that focused command or that compile gate, and `scripts/agent-test.sh --full` has not run.
