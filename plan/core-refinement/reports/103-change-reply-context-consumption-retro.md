# 103 — Change reply context-consumption retro

Work log: [Implement issue 103](0fe5f913-9a2d-4c43-a40b-c102f0be299a), 65 assistant turns. Last user turn is the coordinator brief (“Implement the ticket… pointed at line 25”). Last assistant turn reports the whole ticket coded, not committed.

This file is the full retro: context consumed by that agent, then the `subagents.md` briefing-miss addendum. Analysis only. No product edits. No commit.

**Line 25** of [103 — Change reply is the event id](../issues/103-change-reply-is-the-event-id.md) is item 2 **Resubmission id**: confirm that a post whose `submissionId` is already stored replies with that stored Ev’s id, then write [103 — Resubmission id](103-resubmission-id.md). It is a confirm-and-report check. The agent implemented items 1–5, the acceptance tests, the HTTP `message` field, and an ActorStart rebuild.

---

## 1. Findings

The session’s cost is almost entirely **scope**, then **ceremony**, then **scatter**. Design cohesion is real, but it is the third multiplier, not the first.

- **Asked:** line 25 only (resubmission confirm + report).
- **Briefed:** the full ticket, plus SageFs, TDD, git protocol, and a required-reading list.
- **Done:** 65 turns, ~96 file reads (~290KB of slices, ~72k tokens), a 125KB SageFs schema dump, 36 greps, 14 schema lookups, 21 MCP calls, 13 shells, 42 edits.
- **Line-25 work** would have been a handful of files and a report. That work was finished around turn 22. The remaining ~40 turns are the rest of the ticket.

`AGENTS.md` says not to use SageFs. The brief required SageFs. The agent followed the brief.

A separate main-agent miss: `AGENTS.md` line 37 requires every subagent brief to pass in [[.agents/subagents.md]]. Both the implement-103 brief and the original retro brief wrote “read it if you spawn.” Section 7 covers that.

---

## 2. What context was actually consumed

Rough scale of **file content loaded** (Read slices, not unique file size): **~290KB / ~6,400 lines / ~72k tokens**, across **~57 paths**. Assistant JSONL is 103KB. That count omits grep hits, test logs, and MCP results.

### 2.1 Biggest single items

- SageFs full namespace dump (`GetDynamicTools` `user-sagefs`): **125KB / 1,500 lines / ~31k tokens**. The agent then re-read 200 lines of the spilled dump.
- `GLOSSARY.md`: **25KB / 430 lines**, full read.
- `tests/Server.Tests/CoreMailboxDoorTests.fs`: **8 reads**, ~22KB of slices (file is 993 lines).
- `plan/core-refinement/cqrs-messages.md`: first **150 lines / 17KB**.
- `src/Server/Core/CoreMailboxApi.fs`: 3 reads, ~17KB of slices.
- `plan/core-refinement/project.md`: two 20-line windows that are still ~13.5KB (long lines).
- SageFs `design.md` + `testing-standards.md` + `SKILL.md`: ~23KB.

### 2.2 By category (Read bytes)

- Source (~86KB, 33 reads): `CoreMailboxApi.fs`, `CoreEventDispatch.fs`, `CoreMailboxActors.fs`, `CoreMailboxEvents.fs`, `FileAgent.fs`, `DbAgent.fs`, `History.fs`, `Api.fs`.
- Tests (~67KB, 29 reads): `CoreMailboxDoorTests.fs` dominates; also `FileAgentFailureTests.fs` (6 reads), `TestBackend.fs`, `ChangeAmendmentTests.fs`, `OrderedEventStreamTests.fs`.
- Issue/plan (~45KB, 8 reads): the ticket (3 times), `cqrs-messages.md`, [64 — Place Edit changes as Order.Change](../issues/64-merge-postevent-and-postgraphonly-into-change.md), `project.md`.
- Ceremony (~41KB): `AGENTS.md`, CONTEXT routers, empty `all-agents.md` / `main-agent-behavior.md`, `issue-tracker.md`, `triage-labels.md`.
- Skills (~27KB): SageFs skill + design + testing-standards, TDD, sage-scripts, git-protocol.
- Other (~23KB): the spilled SageFs schema file, `agent-test.sh`, `measure-fs-size.py`.

### 2.3 Tool ceremony

- 14 `GetDynamicTools` calls (full SageFs namespace, a pattern search, then per-tool schemas including `send_fsharp_code` twice).
- 21 MCP calls: session setup, 7 `send_fsharp_code`, 5 `get_session_status`, leases, 2 hard-resets.
- 13 shells: `gitstatus.sh`, 7 filtered `scripts/agent-test.sh` runs, 2 `sagefs-hard-reset.sh`, 2 `measure-fs-size.py`.
- 36 greps, most of them `CoreChangesAccepted` / `message` / `externalChanges` across `*.fs`.

### 2.4 Peak vs cumulative

Cumulative tool intake is on the order of **150–200k tokens** (files + schema dump + greps + test output). Peak window is lower after compaction; the first 10 turns already contain the schema dump, GLOSSARY, cqrs-messages, and SageFs design notes.

---

## 3. Necessary vs avoidable

### 3.1 Necessary for line 25

- The issue file.
- `CoreEventDispatch.tryStored` / `commit` in `src/Server/Core/CoreEventDispatch.fs`.
- `CoreMailboxEvents.storedBySubmission` and the ActorStart/Cancel copies in `src/Server/Core/CoreMailboxActors.fs`.
- The matching lookups in `FileAgent.fs` and `DbAgent.fs` (to confirm they take the stored-Ev path).
- The report at [103 — Resubmission id](103-resubmission-id.md).
- Branch check via `scripts/gitstatus.sh` / git-protocol.

That is a small, named path. The agent had it by turn 22.

### 3.2 Avoidable even for the full ticket

- Full SageFs namespace dump, then 13 more schema fetches.
- Full `GLOSSARY.md` for a ticket that already names the tests.
- `cqrs-messages.md` and [64 — Place Edit changes as Order.Change](../issues/64-merge-postevent-and-postgraphonly-into-change.md) (See also). The ticket already states the reply type.
- SageFs `design.md` and `testing-standards.md` (pulled because the SageFs skill says to).
- Empty `all-agents.md` and `main-agent-behavior.md`.
- Four CONTEXT.md routers plus a glob of `plan/core-refinement/**`.
- `doc/agents/issue-tracker.md` and `triage-labels.md` to flip Status to `coded`.
- Eight re-reads of `CoreMailboxDoorTests.fs`.
- REPL-proving `fromPosted` for a function that is a zip/compare of two `Ev` lists.

### 3.3 Avoidable because the scope was the full ticket

- `CoreMsg` reply change, `CoreMailboxApi` rebuild, `message` leaving `CoreChangesAccepted`, HTTP `message = None`.
- ActorStart `expandStored` chase (turns 51–55).
- Seven filtered test runs and two SageFs rebuilds.
- Second report [103 — Change reply is the event id](103-change-reply-is-the-event-id.md) (`subagents.md` vs brief: “Do not create extra markdown docs”). See section 7: that clash is a briefing miss.

---

## 4. What would have reduced it

**Smaller brief / line-25-only scope.** The coordinator prompt is the main lever. It said implement the ticket, listed 10 required reads, and mandated SageFs + TDD. A brief that said “only item 2, confirm and report, stop” would have cut ~40 turns and most of the token burn.

**Skip the first-minute dump.** `AGENTS.md` already routes. Pasting the same list into the subagent, then having the agent follow `AGENTS.md` into GLOSSARY, `fsharp-source.md`, `core-api.md`, and every folder CONTEXT.md, doubles it. `all-agents.md` and `main-agent-behavior.md` are empty.

**SageFs / TDD / git preload.** Line 25 is a read of existing control flow. SageFs adds: skill (6KB) + design.md (10KB) + testing-standards (6.5KB) + 125KB schema dump + session warmup + leases + two hard-resets. TDD skill then asks for `agent-test.sh` red-green, which the agent mostly skipped (REPL red, then tests and implementation in the same turn). Git protocol is 29 lines; that one is cheap and was used (no commit).

**Colocated note instead of See also.** `plan/core-refinement/` has no `CONTEXT.md` (`plan/CONTEXT.md` says existing Projects are not backfilled). The ticket points at `cqrs-messages.md` (32KB architecture note). A short note on `CoreMailboxApi.fs` — mailbox replies `EventId`; this module rebuilds the ack — replaces that load.

**Fewer duplicate reads / greps.** After the first `CoreChangesAccepted` grep, the hit list is the edit set. Later greps of `message = None`, `accepted.message`, `CoreChanges.accepted` are the same search.

**Pass `.agents/subagents.md` in the first prompt.** Cheap to include (~280 bytes). Makes the report-file contract visible before “Do not create extra markdown docs.” Section 7.

---

## 5. Where design cohesion would reduce necessary loading

This is concrete to this codebase.

**Resubmission identity is four lookups.** The same `submissionId` scan lives in:

- `CoreEventDispatch.tryStored`
- `CoreMailboxEvents.storedBySubmission`
- `FileAgent.applyOne`
- `DbAgent.tryPersistedEvent`

Line 25 is “does resubmission reply the stored id?” Answering it meant opening four modules. One function, one name, and the confirm is one read.

**Change-reply identity is not one concept.** Before the work, the ack was assembled in:

- `CoreChangesAccepted` / `CoreChanges.accepted` (`CoreChanges.fs`)
- `FileAgent.accepted` and `DbAgent.accepted` (still building the record, `message` always `None`)
- `CoreMailboxEvents.replyList` / `acceptEvents`
- `CoreMailboxApi.mergeReplies` / `replyOf`
- `Api.acceptedChangeResult` (HTTP `message` is a separate field)

So “reply is the event id; rebuild the ack from the log; `message` leaves” is five seams. The agent grepped the repo for `message` and `CoreChangesAccepted` because there was no single owner. If `CoreMailboxApi` were the only builder of `CoreChangesAccepted`, FileAgent, DbAgent, and the HTTP mapper would not be on the path. The HTTP ack would not keep a `message` field the domain type dropped (`src/Server/Api.fs` still writes `message = None`).

**`PostEvent` is a mixed door.** `CoreMsg.PostEvent` carries Change, Undo, Redo, ActorStart, and Cancel on one reply type. Narrowing that reply to `EventId` dropped the ActorStart+ActorStop pair that used to ride in `CoreChangesAccepted.events`. Turns 51–55 rediscovered `eventsForStored` in `CoreMailboxEvents.fs` and reimplemented it as `CoreMailboxApi.expandStored`. A Change-only reply (or `Mailbox.PlaceOrder` as the ticket’s own context already names) keeps ActorStart off this ticket.

**Tests are the map, and the map is one 993-line file.** `CoreMailboxDoorTests.fs` was read eight times. `.agents/rules/fsharp-source.md` already says tests split to match source. A `CoreMailboxApi` test file would have been the seam for items 3–5; line 25 would not have needed it.

**If those lived in one named place**, the agent would not have had to chase FileAgent, DbAgent, `Api.fs`, ActorStart expansion, `FileAgentFailureTests.fs` / `DbAgentFailureTests.fs` `message` assertions, or `History.fs` / `ChangeAmendmentTests.fs` for a confirm that `applyPosted` already documents.

---

## 6. Recommendations

- **Briefs:** a cited line is the scope. “Code this” on line 25 means item 2, then stop. Do not paste a required-reading list that `AGENTS.md` already names. Pass [[.agents/subagents.md]] in every subagent prompt (`AGENTS.md` line 37).
- **SageFs:** `AGENTS.md` and the coordinator brief currently contradict. Pick one. A full `user-sagefs` namespace dump is ~31k tokens; fetch one tool schema.
- **GLOSSARY:** drop “read GLOSSARY first” from the TDD on-ramp for tickets that already name the tests. 430 lines is a large tax.
- **Tickets:** put a confirm-and-stop item in its own file, or mark it so a line citation cannot be read as “implement What to build.”
- **Modules:** one owner for Change-reply identity — `EventId` out of the mailbox, ack rebuilt from the log, resubmission is that stored id. FileAgent/DbAgent persist; they do not build `CoreChangesAccepted`. HTTP `message` follows the domain type or goes.
- **Colocation:** a 10-line note on `src/Server/Core/CoreMailboxApi.fs` (or a `plan/core-refinement/CONTEXT.md` that points at it). Point tickets there instead of `cqrs-messages.md`.
- **Tests:** split `CoreMailboxDoorTests.fs` to match `CoreMailboxApi` / actors / events, as `.agents/rules/fsharp-source.md` already says.

---

## 7. Addendum: `subagents.md` was a briefing miss

`.agents/subagents.md` is five lines. It says: read `CONTEXT.md`, then write the full result to `plan/<project-name>/reports/<subagent-title>.md`.

`AGENTS.md` line 37 is a **main-agent** rule: every subagent brief must pass that file in. The implement-103 brief and the original retro brief both wrote “read `.agents/subagents.md` **if you spawn**.” That is the failure. The worker already **is** the spawn.

### 7.1 What skipping it did

**Extra context:** almost none. The file is ~280 bytes. [Implement issue 103](0fe5f913-9a2d-4c43-a40b-c102f0be299a) still opened it on turn 3 because the brief also said to obey `AGENTS.md`. The retro agent skipped it on the first pass, following the “if you spawn” clause.

**Wrong behavior:** the parent treated `subagents.md` as a nested-spawn rule. Workers then guessed whether it applied to them.

**Extra docs:** the cost is the **report file**, not the five-line read.

- Implement-103 was told “Do not create extra markdown docs,” then wrote [103 — Change reply is the event id](103-change-reply-is-the-event-id.md) once it found `subagents.md`. The ticket already named [103 — Resubmission id](103-resubmission-id.md). The second report is the briefing clash: the parent forbade extra markdown without passing in the file that requires a report.
- The retro, on the first pass, returned chat-only and did not write `plan/core-refinement/reports/…`. That matched “analysis only”; it did not match `subagents.md`.

Passing the file in would not have cut SageFs, GLOSSARY, or the full-ticket scope. Those came from the rest of the brief.

### 7.2 What would have changed if it were in the first prompt

The parent would have seen the contract before launch: the worker writes `plan/core-refinement/reports/<title>.md`. Then either:

- drop “Do not create extra markdown docs,” or
- say the ticket’s named report is the `subagents.md` result (for line 25: [103 — Resubmission id](103-resubmission-id.md) only)

[Implement issue 103](0fe5f913-9a2d-4c43-a40b-c102f0be299a) would have known the report path on turn 1 instead of discovering it through `AGENTS.md`. The extra report would still exist unless the brief reconciled the clash.

This file is that retro report, written after the parent passed `.agents/subagents.md` in. `.agents/skills/retro/SKILL.md` names no other path, so the write path is `plan/core-refinement/reports/<subagent-title>.md`. `plan/core-refinement/` has no `CONTEXT.md`.

---

## 8. Friction → one improvement each

Ordered by how many turns show it. Format from `.agents/skills/retro/SKILL.md`.

1. **Scope expanded from line 25 to the full ticket** (turns 1, 5, 28–64). Change: coordinator / implement briefs treat a file:line citation as that item only. Path: `.agents/rules/main-agent-behavior.md` (file is empty today; that is the hole the brief filled with a dump).
2. **First-minute required-reading cascade** (turns 2–4, 13, 15). Change: `AGENTS.md` stays the router; subagent prompts name the ticket path, pass in `.agents/subagents.md`, and say “follow AGENTS.md,” not a 10-file list. Path: `AGENTS.md`.
3. **Full SageFs schema dump, then 13 more schema fetches** (turns 4–6, 11, 21, 35, 44). Change: fetch one tool; never `GetDynamicTools` the whole `user-sagefs` namespace. Path: `.agents/skills/sage-scripts/SKILL.md`.
4. **GLOSSARY + cqrs-messages + issue 64 for answers the ticket already held** (turns 4–5, 10, 15). Change: TDD skill stops requiring GLOSSARY up front; tickets point at `CoreMailboxApi.fs`, not `cqrs-messages.md`. Paths: `.agents/skills/tdd/SKILL.md`, `plan/core-refinement/issues/103-change-reply-is-the-event-id.md`.
5. **Repo-wide greps for `message` / `CoreChangesAccepted` because the ack has several builders** (turns 6–18, 32–34). Change: `CoreMailboxApi` is the only module that builds `CoreChangesAccepted`. Path: `src/Server/Core/CoreMailboxApi.fs` (product shape; owner would be a later architecture ticket).
6. **`CoreMailboxDoorTests.fs` read eight times** (turns 9, 11, 21, 22, 38, 40, 42, 46). Change: split that file to match source. Path: `tests/Server.Tests/CoreMailboxDoorTests.fs`, per `.agents/rules/fsharp-source.md`.
7. **ActorStart rebuild redo after the reply type change** (turns 51–55). Change: Change gets its own door so ActorStart/Cancel stay off this ticket (the ticket’s own pointer to `Mailbox.PlaceOrder`). Path: `src/Server/Core/CoreMsg.fs` / `src/Server/Core/CoreMailboxActors.fs`.
8. **SageFs mandated while `AGENTS.md` says not to use it** (turns 3–7, 19–27, 35–37, 58–61). Change: one sentence in `AGENTS.md` is the rule; briefs do not override it.
9. **TDD skill loaded, then tests and implementation written together** (turns 3, 28). Change: if TDD is in force, the first persist is one failing test via `scripts/agent-test.sh --filter`. Path: `.agents/skills/tdd/SKILL.md`.
10. **`subagents.md` not passed in; brief also forbade extra markdown** (implement-103 turn 1, then turn 62 report). Change: main agent puts `.agents/subagents.md` in every subagent prompt and reconciles it with “no extra markdown” before launch. Path: `.agents/rules/main-agent-behavior.md` (empty); the rule as written is `AGENTS.md` line 37.

---

## 9. Paths this retro used

- Ticket: [103 — Change reply is the event id](../issues/103-change-reply-is-the-event-id.md)
- Line-25 report: [103 — Resubmission id](103-resubmission-id.md)
- Implementing agent’s ticket report: [103 — Change reply is the event id](103-change-reply-is-the-event-id.md)
- Agent contract: `AGENTS.md`, `.agents/subagents.md`
- Retro skill: `.agents/skills/retro/SKILL.md` (names no other write path)
