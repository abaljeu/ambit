# 31 — Force-save the focused node

Ticket: [[31-force-save-the-focused-node.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

1. **Solution 11 stage line repeats commit, pull, and push.** **Hard.** [[.agents/rules/writing-markdown.md]] Repetition: "DO NOT REPEAT YOURSELF." Solution 11 — git Save sequence states in the persist stage that Save commits, pulls, and pushes. The next three stages are commit, pull, and push (`plan/github-transport/spec.md` lines 28–31).
2. **Persist wait polls the mailbox.** **Judgement.** Overcomplication. `waitUntilPersisted` reads the focus and the event tail, then calls itself. The persist thread has no signal into that wait. Source: [[plan/context/SMELLS.md]]. The hunk is `return! waitUntilPersisted changes after focusId` in `src/Server/GithubTransportActor.fs` lines 271–272.
3. **Two Save facts share one body.** **Judgement.** Duplicated Code. ``git Save persists focus then commits pulls and pushes`` and ``git Save marks an unparsed file parsed and writes it before commit`` each build the same pull, commit, and push stubs and the same `note.txt` focus. Source: [[plan/context/SMELLS.md]]. The facts are `tests/Server.Tests/GithubTransportActorTests.fs` lines 398–447 and lines 451–503.
4. **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` printed five bindings: `readFocusNode` 12 lines, `focusCanBeParsed` 12 lines, `persistFailedSince` 17 lines, `waitUntilPersisted` 22 lines, `forcePersistFocus` 14 lines. Each length is inside the 40-line limit. The scan printed no long line, no file-size break, no glossary phrase, and no refer-by-name hit.

## 2. Spec

1. **`eventsSince` on `ActorChanges`.** **Unnamed.** The ticket names `ActorChanges` and `CoreDoor.actorChanges`. The diff adds `eventsSince` on that record (`src/Shared/History.fs` line 97) and fills it in `CoreDoor.actorChanges` (`src/Server/Core/CoreDoor.fs` lines 71 and 82). Solution 11 — git Save sequence leaves that member unnamed.

## 3. Arch

- No findings.

## 4. Tests

1. **Suite.** `bash scripts/check-test.sh 2944` printed `DONE` and `/tmp/ambit-full-suite-20261108-193104.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 757, skipped 1, failed 0. `tests/Shared.Tests` passed 1937, skipped 3, failed 0. `scripts/check-test.sh` is mode `644`, so the direct path prints `Permission denied`.
2. **Persist focus.** ``git Save persists focus then commits pulls and pushes`` (`tests/Server.Tests/GithubTransportActorTests.fs` lines 398–447) creates `note.txt`, expects that file at commit, expects `ParseState.Parsed`, and expects gate, commit, pull, release, push.
3. **Unparsed file.** ``git Save marks an unparsed file parsed and writes it before commit`` (lines 451–503) starts from `Unparsed` and `Persisted`, expects the file at commit, and expects `Parsed`.
4. **Normal Node.** ``git Save on a Normal Node skips persist then commits pulls and pushes`` (lines 515–561) expects an empty persist-notice list, the same focus node, the same Workspace node, and gate, commit, pull, release, push.
5. **Gate.** ``git Save gates commit then releases before push`` (lines 364–395) uses the Workspace focus and expects the gate around commit and pull, then push.
6. **Pull failure.** ``git Save stops when the pull fails and does not push`` (lines 564–596) expects `ActorFailed` with the conflict text and the steps commit, pull.
7. **Reject shape.** ``Load and Save return the same conflict reject shape`` (lines 636–704) expects `ActorFailed` and the path `notes/today.md` for Load and for Save. Save uses the Workspace focus.
8. **Routed Save.** ``routed Git Save commits then pushes through Peer Actor`` (`tests/Server.Tests/LoadSaveCommandTests.fs` lines 495–519) uses the Workspace focus, expects `ActorSucceeded`, and expects `saved.txt` on the cloned remote.
9. **Wait budget.** Those Save facts call `waitForStop` with `1000`. `waitForStop` (lines 96–114) uses `Task.Delay 10` until that budget ends. The TDD skill says a test uses no timeout, sleep, or spin-wait unless the spec names that time.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

The body of pull request 552 has no Found elsewhere list.

- No findings.
