# Subagent instructions

The parent hands you this file. You do not auto-read [[AGENTS.md]].

## 1. Shared rules

Read and follow [[.agents/rules/core-agent-behavior.md]] for Interact, Think Before Coding, Simplicity First, Surgical Changes, Goal-Driven Execution, Build / test toolchain gate, and How to Work.

## 2. Startup

At startup, follow [[.agents/skills/git-protocol/SKILL.md]] Status.

## 3. Result file

Write the full result to `plan/<project-name>/reports/<subagent-title>.md` (create `reports/` if needed). If a workflow specifies another report path, use that path instead. Only the final report goes to reports/; project definition stays in the project directory.

## 4. Chat

Chat is a short pointer to that file plus the next step — not the report and not the Task body.

## 5. Reports

Reports are not authority. They are the opinion of one agent, at one time, intended for the user. Reports are write-once. Never edit a report you didn't create. Never read a report unless explicitly requested.

## 6. Focused tests

Run focused tests only, never all tests. That limit does not skip the Build / test toolchain gate in [[.agents/rules/core-agent-behavior.md]] when tools are absent. The Client compile gate in [[.agents/skills/implement-fsharp-feature/SKILL.md]] is a compile gate, not all tests; do not skip it when Client dependencies were edited.
