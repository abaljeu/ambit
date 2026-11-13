Check: pass

# 27 — Reject unknown Run commands before the queue

Ticket: [[27-reject-unknown-run-commands-before-the-queue-ticket.md]]
Spec: [[../../spec.md]]
Arch: [[../../arch.md]]

## 1. Standards

- **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` printed no line and exited 0.
- **Limits.** `tryStart` is 27 lines. `CommandRequest.fs` is 186 lines. Each changed source line is 100 characters or fewer.
- **Rules.** The diff matches `.agents/rules/fsharp-source.md`, `.agents/rules/core-api.md`, `.agents/rules/writing-markdown.md`, and `.agents/rules/refer-by-name.md`.

## 2. Spec

- **Spec file.** `plan/actor-as-client/spec.md` is absent. The ticket is the spec.
- **One job.** `CommandRequest.tryStart` returns `Ok` for the names `test` and `ai`. `actorNameFromText` lowercases the word after `?`, so `?TEST` is `test`. Every other `?` command returns `Error "unknown actor"`, including `?nope`.
- **Browser refusal.** `execRunOp` calls `queueRun` on `Ok`. The `Error` arm sets `CmdLastResult.Error (Some "Run", msg)` and returns `commitEffects`. `CmdLastResult.toDisplay` renders that value as `Run: unknown actor`.
- **Query.** `submitQuery` calls `tryQueryStart` and then `queueRun`.
- **Missing command.** A path with no `?` command returns `Error "No command found"` from `noRunnableActor`.
- **Scope.** The diff edits `src/Shared/CommandRequest.fs` and two test files. `postChosenStart`, `CoreMailboxApi.coreMailboxStartActor`, and `pool.startActor` stay as they are.

## 3. Arch

- **Run command.** `execRunOp` calls `queueRun` for an `Ok` start. `queueRun` calls `RunLaunch.queueStart`. Module map 16 — Run command names `execRunOp`. Module map 17 — Actor name names `actorNameFromText`.
- **Posted run.** The diff leaves `postActorStart`. Arch section Posted run sends every other name through `startChosen`. `postActorStart` calls `postChosenStart` for that case.
- **Error string.** The Actor name sentence about `No command found` is in Found elsewhere. The story paths and the module map hold.

## 4. Tests

- **Suite.** `bash scripts/check-test.sh 2941` printed `DONE` and `/tmp/ambit-full-suite-20261113-010118.log`. The script file has no execute bit, so the direct path reports `Permission denied`.
- **Server.** `tests/Server.Tests` passed 781, failed 0, skipped 1. The skip is ``Change.upload structure persistence preserves existing file mtime``.
- **Shared.** `tests/Shared.Tests` passed 1940, failed 0, skipped 3. The skips are ``reuses existing owned path without duplicating``, ``planServerFilePresentOps transitions only matching absent files``, and ``listForPush without git still returns walk``.
- **Outputs.** ``tryStart returns unknown actor for ?nope`` asserts `Error "unknown actor"`. ``tryStart returns Ok for ?ai`` asserts `Ok` and the zoom, focus, and run-definition ids. ``?nope Run leaves the queue and shows the Run error`` asserts an empty effect list, no pending `EventBody.ActorStart`, and `CmdLastResult.Error (Some "Run", "unknown actor")`. ``tryStart one-Node when Focus is the Command`` uses the `?test hello` node from `zoomWithChildren` and still returns `Ok`.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- **Accepted.** **Problem.** Arch section Actor name says the Run error string stays `No command found`. A missing command uses that string. An unknown `?` name uses `unknown actor`. **Fix.** Record both strings on the Run command in Arch, in the doc step. **Weigh.** The ticket requires `Error "unknown actor"` for every `?` command other than `test` and `ai`. `noRunnableActor` returns `No command found` when the path has no `?` command. `CmdLastResult.toDisplay` shows `Run: unknown actor` for this refusal. The sentence is outside the story paths and the module map. The doc step records both strings.
- **Accepted.** **Problem.** The ticket says that after this ticket `postChosenStart` has no production caller. `postActorStart` calls `postChosenStart` when `admittedQueue` returns none. **Fix.** Leave that call for 25 — Remove the coreMailboxStartActor door. Narrow the sentence to the Browser Run path. **Weigh.** `postChosenStart` is private, and `postActorStart` calls it. The ticket checkbox for that sentence is open. Do not load names `postChosenStart`. This ticket's Stop line leaves that function. 25 — Remove the coreMailboxStartActor door owns the door. On the Browser path, `tryStart` returns `Error "unknown actor"` and `execRunOp` leaves `queueRun`.
