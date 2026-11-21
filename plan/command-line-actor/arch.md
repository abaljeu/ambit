# Command-line actor architecture

Spec: [spec.md](spec.md)

Marks: [x] exists today. [ ] required. [o] obsolescent.

This file is provisional. It names owners and seams from the existing actor pattern. It answers no open question in the [Spec](spec.md). Each [ ] entry waits on the questions it names.

## 1. Bounds

In scope: the owners that choose the actor, run the command, and build the children.

Out of scope: any choice that an open question in the Spec holds.

## 2. Types

Waits on open questions 1, 2, 3, and 19. The result type of the actor depends on the child shape and on stderr and exit code handling.

## 3. Interfaces

Waits on open questions 7, 9, 13, and 15. The start path and the choice rule are not decided.

## 4. Flow paths

1. Run on a focus node → Run choice → start of the command-line actor → shell run → child build. The order of the first two entries waits on open questions 7 and 13.

## 5. Module map

[x] Run choice. File: `src/Client/Commands.fs`, function `execRunOp`. Uses the Run text test.
[x] Run text test. Files: `src/Shared/CommandRequest.fs` (`isCommandText`, `isQueryText`) and `src/Shared/AmbleRun.fs` (`shouldExec`). The `>` test is in `shouldExec` today.
[x] Actor start and pool. Files: `src/Server/Core/CoreActorPool.fs` (`startFunction`) and `src/Server/RouteRegistration.fs` (`BootActors`). Actor pattern reference: [Actors](../../doc/current/actors.md).
[x] Admitted actor queue. File: `src/Server/Core/CoreMailboxActors.fs`. The `query` actor is the nearest pattern. See [Query Actor](../../doc/current/query-actor.md).
[ ] Command-line actor. A new actor function that takes the `ActorFunction` input. Uses Directory lookup and Shell run.
[ ] Directory lookup. Derives the focus node's directory. The Glossary entry for the enclosing artifact is the vocabulary. The non-file case is open question 6.
[ ] Shell run. Starts the process, reads stdout, and stops the process. Stderr, exit code, timeout, and cancel are open questions 2, 3, 4, and 18.
[ ] Child build. Turns stdout into children. The shape is open question 1. The re-run case is open question 5.

## 6. Seams

[ ] Run choice and Command-line actor. Tests cross the start of the actor from a `>` line.
[ ] Command-line actor and Shell run. Tests cross the process boundary with a fake runner.
[ ] Command-line actor and Child build. Tests cross the stdout text to the children.

## 7. Slicing strategy

`tracer-cut`. Slicing waits on the open questions the tickets name.

## 8. Open questions

All open questions are in [spec.md](spec.md).
