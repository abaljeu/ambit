Check: pass

# 28 — Limit the AI extract to the visible list

Ticket: [[28-limit-the-ai-extract-to-the-visible-list-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

## 1. Standards

- **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` exited 0. The measure lines are `actorGraphFrom` at 16 lines, `packExtract` at 4 lines, and `runBody` at 21 lines. Each is under the 40-line limit. No `LONG`, `TAB`, `MUTABLE`, `BARE_ID`, `PHRASE`, `BLANK_BLANK`, or `FILE` line.
- **Rules.** The diff matches `.agents/rules/fsharp-source.md`, `.agents/rules/core-api.md`, `.agents/rules/writing-markdown.md`, and `.agents/rules/refer-by-name.md`.
- **Size.** The source diff is `src/Server/Core/CoreActorPool.fs` and `src/Server/RunAgentActor.fs`. The test diff is `tests/Server.Tests/AiExtractPackTests.fs` and `tests/Server.Tests/AgentAskTests.fs`.

## 2. Spec

- **Spec file.** `plan/actor-as-client/spec.md` is absent. The ticket is the spec.
- **One job.** `RunAgentActor.packExtract` calls `CoreActorPool.actorGraphFrom input.graph start` and passes that graph to `AiExtractPack.packExtract`. `actorGraphFrom` keeps a node only when its id is in `ActorStart.graphIds` and the loaded graph has that node. `AiExtractPack.writePresent` writes a node only when that map contains the id, and it skips a child id with no node. The text sent to the AI is the part of `graphIds` reachable from Zoom.
- **Loaded graph.** `RunAgentActor.inputFor` still sets `input.graph` from `run.getGraph ()`. `commandArgs`, `planOnCurrent`, and `postAdd` still read that graph. `planOnCurrent` and `postAdd` read `changes.graphAtEvent ()` first and use `input.graph` when that read fails. `postReplace` posts the ops.
- **Pool callers.** `actorGraphFrom` is public. Its body is unchanged. `putActorStart` and `runStartActor` still call it.
- **Stop lines.** `ActorOrder` has no new field. `input.graph` stays the loaded graph. The AI path calls `actorGraphFrom` once. The packed text includes a node only when that id is in `start.graphIds`.

## 3. Arch

- **AI actor.** `RunAgentActor.actorFn` remains the Actor function for the `ActorStart` on that Run. The pack reads `graphIds` from that same `start`.
- **Module map.** Module map 13 — AI actor names `src/Server/RunAgentActor.fs` `actorFn` and uses Pool start. `actorFn` calls `CoreActorPool.actorGraphFrom` in that module.
- **Story paths.** The flow paths name Posted run and Pool start. They leave the extract text unnamed. The diff leaves those paths as written.

## 4. Tests

- **Suite.** `bash scripts/check-test.sh 2889` printed `DONE` and `/tmp/ambit-full-suite-20261113-150540.log`. `scripts/check-test.sh` has no execute bit, so the direct path reports `Permission denied`.
- **Server.** `tests/Server.Tests` passed 764, failed 0, skipped 1. The skip is ``Change.upload structure persistence preserves existing file mtime``.
- **Shared.** `tests/Shared.Tests` passed 1940, failed 0, skipped 3. The skips are ``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, and ``listForPush without git still returns walk``.
- **Pack.** ``pack omits a folded child's descendants and keeps an expanded child's`` builds a graph that still contains the folded row's child, calls `CoreActorPool.actorGraphFrom` with `graphIds` that omit that child, then calls `AiExtractPack.packExtract`. The text contains `expanded-child` and `folded-row`. The text omits `folded-child`.
- **Prompt.** ``?ai prompt keeps an expanded child's text and omits a folded child's descendant`` posts the folded row's child with `CoreMailboxApi.postGraphOnly`, omits that child from `ActorStart.graphIds`, starts through `startAiRun` inside `withFake`, and reads the prompt. The prompt contains `expanded-child-text` and `folded-row-text`. The prompt omits `folded-child-text`.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- **Rejected.** **Problem.** Process step 4 names `postAdd`. The build note says the function is `postReplace`. **Fix.** Leave the step as `postAdd`. **Weigh.** `RunAgentActor.postAdd` reads `changes.graphAtEvent ()` and uses `input.graph` when that read fails. `postReplace` posts the ops. Step 4 names the function that still reads the loaded graph.
