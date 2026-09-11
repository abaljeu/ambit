# Handoff: set 3 interactive code review (Create cloud-agent posts reply)

For a **new main Cursor cloud agent**. Do not continue as a subagent of the prior run. Do not start `/implement`. Do not review set 3 until Alan confirms the commit pin in chat.

Workspace: `/workspace` (github.com/abaljeu/ambit). Human: Alan.

## Suggested skills

Invoke these in the new session. Do **not** start `/implement`.

- [[.agents/skills/code-review/SKILL.md]] — Standards and Spec axes. Use the **interactive** protocol below (tell Alan what you see, confirm as you go). Do **not** spawn parallel review workers for set 3.
- [[.agents/skills/code-review-fsharp/SKILL.md]] — the set 3 branch is F# (`*.fs` / `*.fsi`). Measure binding size before Standards findings.
- [[.agents/skills/git-protocol/SKILL.md]] — first git step is [[scripts/gitstatus.sh]].
- [[.agents/skills/project-work/SKILL.md]] — rewind notes live under `plan/`. Do not change Project Stage for this review.

## Next session focus

Interactive `/code-review` of **set 3** (Create cloud-agent posts reply under Focus). Same protocol as sets 1–2: tell Alan what you see, confirm as you go, append confirmed misses to the rewind report, no product patches, rewind-and-redo later.

## Source of truth (do not duplicate)

Confirmed decisions for sets 1–2 live only in the rewind report. Read it. Do not copy it into this handoff or into chat.

- Path on branch `cursor/failed-actor-still-drops-b13e`: [[plan/core-creation/reports/actor-pool-rewind-review.md]]
- PR for that notes branch: https://github.com/abaljeu/ambit/pull/5
- In chat, Cloud blocks relative file links. Use GitHub URLs when you name a file. Example for the report: https://github.com/abaljeu/ambit/blob/cursor/failed-actor-still-drops-b13e/plan/core-creation/reports/actor-pool-rewind-review.md

Sets 1–2 in one line (detail is in the report): set 1 Core 18 (`e0f92b9` parent; `f3eb420`, `351ce6c`, `0c85dff`, merge `05eb06f`) — one Core mailbox, TaskPool vs registry, public number until mailbox delete-actor, admit+enqueue one message, drop = registry remove + async terminate if still running, any stop including failure, FIFO is mailbox order, issue 26 cancelled wrap patch; set 2 CloudAgents (`1b9874e`, `2c521f7`, merge `4f974f0`) — tests do not prove the stack, three test layers, await HTTP not Sleep, no exception leak, library cancel belongs now. After the full review: rewind, tighten spec, reimplement. Do not patch the current pool mailbox.

## First actions (in order)

1. Chat: confirm the set 3 commit pin with Alan **before** any findings. Do not start the review until he agrees the range.
2. Git: from `/workspace`, run [[scripts/gitstatus.sh]] (first git step). Extra git only if that output is not enough.
3. Fetch the PR 4 branch: `git fetch origin cursor/cloud-agent-actor-posts-reply-0b7d`
4. Sit notes on `cursor/failed-actor-still-drops-b13e`. Append confirmed set 3 findings to [[plan/core-creation/reports/actor-pool-rewind-review.md]] and **push that branch**. Do **not** merge to `ready`. Do **not** create wrap-patch tickets.

## Set 3 pin (confirm with Alan first)

- PR 4 DRAFT: https://github.com/abaljeu/ambit/pull/4
- Branch: `cursor/cloud-agent-actor-posts-reply-0b7d`
- Fetch: `git fetch origin cursor/cloud-agent-actor-posts-reply-0b7d`
- Commits vs `ready` (confirm these SHAs before findings):
  - `eb5c4bf` Add cloud-agent actor with POST /ambit/actors
  - `68d14a2` Add issue 05
  - `d79c706` Update llm-connector stage
  - `8ba1e97` Merge origin/ready
- Spec (read; do not paste into chat):
  - [[plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md]]
  - [[plan/llm-connector/reports/grill-run-agent-actor-2026-09-08.md]]
  - [[plan/llm-connector/reports/first-agent-cursor-cloud-agents.md]]
- GitHub URLs for chat (relative links fail in Cloud):
  - https://github.com/abaljeu/ambit/blob/cursor/cloud-agent-actor-posts-reply-0b7d/plan/llm-connector/issues/05-create-cloud-agent-posts-reply-under-focus.md
  - https://github.com/abaljeu/ambit/blob/cursor/cloud-agent-actor-posts-reply-0b7d/plan/llm-connector/reports/grill-run-agent-actor-2026-09-08.md
  - https://github.com/abaljeu/ambit/blob/cursor/cloud-agent-actor-posts-reply-0b7d/plan/llm-connector/reports/first-agent-cursor-cloud-agents.md

Review range after Alan confirms: three-dot diff of those commits vs `ready` on the fetched PR 4 branch. The notes/report branch (`cursor/failed-actor-still-drops-b13e`) is **not** the code under review.

## Protocol (sets 1–2, reuse)

Chat rules: [[.agents/rules/core-agent-behavior.md]] — short replies (prefer ≤30 lines), one Markdown link when naming a file, include Alan in the work.

- Interactive confirm-as-you-go. State what you see. Wait for Alan to confirm before treating it as decided.
- Standards and Spec stay separate axes. Do not merge or rerank findings across axes.
- Accumulate **confirmed** misses in the rewind report. Unconfirmed observations stay in chat until Alan agrees.
- No product patches on the current pool mailbox or on the set 3 branch.
- No `ready-for-agent` wrap-patch tickets (issue 26 was cancelled as a wrap patch; same rule).
- After all three sets: rewind, tighten spec, reimplement. Not this session’s job unless Alan says so.
- Do not spawn further workers / subagents for set 3.

## Constraints

- Do not implement.
- Do not merge to `ready`.
- Do not push `ready`.
- Do not create wrap-patch tickets.
- Do not duplicate the rewind report.
- Redact secrets. No API keys. Core must not see `CURSOR_API_KEY` (that is a spec rule, not a key to print).
- Prefer not committing except report appends on `cursor/failed-actor-still-drops-b13e`.

## Prior session (compact)

A cloud-agent run reviewed sets 1 and 2 interactively with Alan. Confirmed shape is in the rewind report (one Core mailbox, TaskPool vs registry, drop/finish/failure, FIFO as mailbox order; CloudAgents tests do not prove the stack; three test layers; await HTTP; no exception leak; library cancel now). Set 3 was **not** started. The parent asked for this handoff so a **new main** cloud agent can continue. Do not resume the old run’s workers.

## Success for the new agent

Alan has confirmed the set 3 SHAs. You have fetched PR 4. You have walked Standards and Spec interactively. Confirmed misses are appended to the rewind report on `cursor/failed-actor-still-drops-b13e` and that branch is pushed. No product code changed. No wrap tickets. Set 3 review is complete or paused only because Alan stopped you.
