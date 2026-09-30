# Extract subagent instructions

Project: [skills-review](plan/skills-review/project.md). Instruction split only. No application source. No commit.

## 1. Result

Subagent-run rules now live in [subagents](.agents/subagents.md). Parent-only delegation rules stay in [core-agent-behavior](.agents/rules/core-agent-behavior.md). Shared sections stay in core-agent-behavior; subagents.md points at them and does not copy them.

## 2. Moved into subagents.md

1. **Startup** — at startup, follow [git-protocol](.agents/skills/git-protocol/SKILL.md) Status.
2. **Result file** — write the full result to `plan/<project-name>/reports/<subagent-title>.md` (create `reports/` if needed); workflow path override; only the final report goes to reports/; project definition stays in the project directory.
3. **Chat** — chat is a short pointer to that file plus the next step, not the report and not the Task body.
4. **Reports** — reports are not authority; write-once; never edit a report you did not create; never read a report unless explicitly requested.
5. **Focused tests** — run focused tests only, never all tests; that limit does not skip the Build / test toolchain gate when tools are absent; Client compile gate in [implement-fsharp-feature](.agents/skills/implement-fsharp-feature/SKILL.md) is a compile gate, not all tests.
6. **Toolchain sentence** — the former core-agent-behavior line that subagents still need a working toolchain and must stop and report if tools are missing is folded into Focused tests via a pointer at the shared Build / test toolchain gate. It is not restated as a second copy of that gate.

## 3. Stayed in core-agent-behavior.md

1. **Shared** — Interact, Think Before Coding, Simplicity First, Surgical Changes, Goal-Driven Execution, Build / test toolchain gate (general), How to Work. Unchanged except the subagent-only toolchain sentence was removed from the gate section.
2. **Hand the file** — when delegating, have them first read [subagents](.agents/subagents.md). Subagent-run rules live there.
3. **Delegate** — unless otherwise specified, always delegate to Grok 4.6. Restored the HEAD model name; the dirty tree had abbreviated it to Grok.
4. **Use subagents** — use subagents to carry out tasks.
5. **Parent chat** — chat from the parent after a Task returns is a short pointer to the report file plus the next step, not the report and not the Task body.
6. **Correction in flight** — if the user posts a correction while an agent is still working, inform the agent that is working. Do not start a second agent with overlapping work area.
7. **All-tests subagent** — you may create a subagent specifically to run all tests of a project.

## 4. What was not duplicated

1. **One home** — each moved rule has one home in [subagents](.agents/subagents.md). core-agent-behavior points at that file; it does not restate those rules.
2. **Shared pointer** — [subagents](.agents/subagents.md) section 1. Shared rules names the shared sections and points at [core-agent-behavior](.agents/rules/core-agent-behavior.md). It does not copy them.
3. **Catalog** — [gambol](.agents/rules/gambol.md) was not updated. This is a handoff file, not a new always-loaded rule. The parent already points at it. No `.cursor/rules/` stub: subagents do not auto-read this file.

## 5. Next step

Parent: open [extract-subagent-instructions](plan/skills-review/reports/extract-subagent-instructions.md) for the split. On the next Task, hand [subagents](.agents/subagents.md). Do not commit unless Alan asks.
