# Command-line actor architecture

Spec: [spec.md](spec.md)

Marks: [x] exists today. [ ] required. [o] obsolescent.

The product owner answered every open question on 2026-11-21. The decisions are in the [Spec](spec.md). This file places them in owners and seams.

## 1. Bounds

In scope: the owners that choose the actor, run the command, and build the children.

Out of scope: the AMBLE exec language, and the Amble `=` and `?` paths.

## 2. Types

[ ] Command line. The parse of the text: the program is the first word after the `>`, and the arguments are the rest of the line. The parse is empty for a bare `>`. The type is in Shared so that tests cover it without a process.
[ ] Command order. The order that Run posts to `CommandActor`. It carries the start node and the command line.
[ ] Output line. One line of stdout or stderr, in arrival order.

## 3. Interfaces

1. The Run text test is true when the first character of the text is `>`. It does not trim. It is tested before the `=` test.
2. Run posts a Command order. A bare `>` posts nothing.
3. The actor builds children from Output lines and sets or clears the `error` class on the start node.

## 4. Flow paths

1. Run on a focus node → Run text test → Command order → `CommandActor` → working directory lookup → `Process.Start` → Output lines → child append → exit code → `error` class.
2. Cancel → actor cancel → process kill.
3. Owner scan reaches a `>` line → the same flow from the Run text test.

## 5. Module map

[x] Run choice. File: `src/Client/Commands.fs`, function `execRunOp`. [ ] The `>` test runs before `submitQuery`, and the client Amble `>` path (`execAmbleRunOp`, `legacyRun`) is removed for `>` lines.
[x] Run text test. Files: `src/Shared/CommandRequest.fs` (`isCommandText`, `isQueryText`) and `src/Shared/AmbleRun.fs` (`shouldExec`). [ ] The `>` test is first-character and wins over `=`. [o] The trimmed `>` test in `shouldExec`.
[x] Actor start and pool. Files: `src/Server/Core/CoreActorPool.fs` (`startFunction`) and `src/Server/RouteRegistration.fs` (`BootActors`). Actor pattern reference: [Actors](../../doc/current/actors.md).
[x] Admitted actor queue. File: `src/Server/Core/CoreMailboxActors.fs`. The `query` actor is the pattern. See [Query Actor](../../doc/current/query-actor.md).
[ ] `CommandActor`. A new actor function that takes the `ActorFunction` input, started by a Command order as the search actor is. It runs on its own thread and refuses a second Run while live.
[ ] Directory lookup. Derives the enclosing directory of the start node. The Glossary entry for the enclosing artifact is the vocabulary. `GraphQuery.enclosing` is the nearest owner.
[ ] Process run. Starts the process with the program as file name and the arguments as one argument string, with no shell. Reads stdout and stderr in arrival order. Kills the process on cancel. It has no timeout.
[ ] Child build. Appends one plain child per Output line after the start node's existing children, without removing them. At 1000 lines in the run it adds `output truncated` and stops adding children for that run.
[ ] Exit mark. Sets the `error` class on the start node for a non-zero exit and clears it for a zero exit.

## 6. Seams

[ ] Run choice and `CommandActor`. Tests cross the start of the actor from a `>` line.
[ ] `CommandActor` and Process run. Tests cross the process boundary with a fake runner.
[ ] `CommandActor` and Child build. Tests cross the Output lines to the children, the cap, and the re-run append.

## 7. Slicing strategy

`tracer-cut`. The tickets in `issues/` slice the flow in order: choice, process, children, errors, cancel, re-run, docs.

## 8. Open questions

None. See [spec.md](spec.md).
