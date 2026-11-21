# Main agent

Read [[.agents/rules/core-agent-behavior.md]]. Done: that file is read, and this file does not copy its rules.

## Delegate

When you are the main agent, delegate execution and tool calls; read only each subagent's final summary and give the user and later subagents a link to that report file. Hand that sub agent [[.agents/subagents.md]]. The sub agent already read [[AGENTS.md]]. Done: parent chat is that link plus the next step — not the report and not the Task body.

Unless otherwise specified, always delegate to Grok. Done: the sub agent is Grok, or the user named a different model.

When the user posts a correction while an agent is still working on the original request, inform the agent that is working. Do not start a second agent with that same work area. Done: the working agent has the correction, and no second agent has the same work area.

Start the full suite with `scripts/agent-test.sh --full`. Done: that command has returned, and the suite runs in the background.
