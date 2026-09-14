# Subagent long-output steering

Date: 2026-09-14. Instruction edit only. No application source.

## 1. Lead claim

Cursor subagents can load [[AGENTS.md]] → [[.agents/rules/gambol.md]] → [[.agents/rules/core-agent-behavior.md]] and still dump multi-page results into chat. The failure is instruction shape and audience, not a missing stub. Parents amplifying Task returns are part of the same failure.

## 2. How the rule is loaded

[[AGENTS.md]] is one line: follow [[.agents/rules/gambol.md]]. That file catalogs rules; it does not restate them. [[.cursor/rules/core-agent-behavior.mdc]] has `alwaysApply: true` and points at [[.agents/rules/core-agent-behavior.md]]. [[.cursor/rules/gambol.mdc]] is the same pattern for the catalog. Bridges ([[.agents/codex-context.md]], [[.agents/copilot-instructions.md]]) do not copy this policy.

So a subagent that "read AGENTS.md" still depends on attending to Interact, How to Work, and Multitasking inside the always-applied rule body. Loading is not the gap.

## 3. Why line 69 kept losing

### 3.1 Buried duplicate of Interact

Interact already capped chat at 60 lines (30 better). "Never output multiple pages" sat ~65 lines later in a mixed How to Work list (narrate thinking, announce tool goals, then the dump ban, then link style). "Multiple pages" is weaker than 60 lines. Two homes for one meaning thinned both.

### 3.2 Negation and no target path

[[.agents/skills/writing-for-agents/SKILL.md]] Gate: prompt the positive; a prohibition names the banned act. Line 69 named dumping. It said "a file" with no path. The reports path lived only in the later Subagent sentence, so a long-output agent had no default file to write.

### 3.3 Subagent-only subject, parent dump

Line 84 began "Subagent must… not chat." Parents skip that as not their job. Line 83 told the parent "When a subagent completes, summarize its conclusion for the user." Task already returns the subagent's last message. "Summarize" plus a long return becomes a paste. Cursor also asks subagents to put the answer in `final_summary` for the parent timeline. The platform default is relay; the repo rule must bind the parent on that relay.

### 3.4 Conflict with Task-return shape

The standing rule said report to a file, not chat, then also said summarize in chat. Subagents optimized for the channel that reaches the parent (the return message). Without "chat is a pointer to the report," the return stayed the full analysis.

## 4. Parent vs subagent

Both dump. The subagent writes a long last message because that is what Task returns. The parent then pastes that body into user chat, which is the multi-page violation the human sees. A rule that only says "Subagent must… not chat" cannot stop the second hop. The parent synthesizing a Task result uses the same chat-vs-file rule as the subagent.

## 5. Change made

One normative home: [[.agents/rules/core-agent-behavior.md]]. No catalog, stub, bridge, or skill copy.

### 5.1 Interact — chat vs file (everyone)

Positive length plus file target, 60-line cap kept as the paired guardrail. Replaces the old 60-line-only bullet and deletes the buried "multiple pages" line.

### 5.2 Multitasking — reports path (subagent and parent)

Positive: subagent writes `plan/<project-name>/reports/<subagent-title>.md`. Chat from the subagent, and from the parent after a Task returns, is a short pointer plus the next step. Paired guardrail: not the report, not the Task body. Dropped "summarize its conclusion for the user" (that was the paste cue). Kept workflow override and reports vs project-definition split, shorter.

## 6. What we did not change

Did not add a second home in [[AGENTS.md]], [[.agents/rules/gambol.md]], or `.mdc` stubs. Did not change [[.agents/skills/research/SKILL.md]] (it already writes under `reports/`). Did not invent a fallback project in the rule; this diagnostic report uses [[plan/planning-skills/project.md]] only because the parent named that path. Did not rewrite old reports.

## 7. Recommended parent-chat behavior

After a Task returns, open the report file if you need the body. User chat is a link to that file plus the next step. Do not paste the Task return.
