Check: pass

`CommandRequest.tryStart` returns `Ok` for the names `test` and `ai`. Every other `?` command returns `Error "unknown actor"`, including `?nope`.

``?nope Run leaves the queue and shows the Run error`` returns `CmdLastResult.Error (Some "Run", "unknown actor")`. The pending list gains no `EventBody.ActorStart`.

`execRunOp` keeps that `Error` arm. `submitQuery` stays the query path into `queueRun`.

- `tests/Server.Tests` passed 781, failed 0, skipped 1.
- `tests/Shared.Tests` passed 1940, failed 0, skipped 3.
- `scripts/agent-test.sh` ran `CommandRequestTests` and `RunEditCommitTests` and passed 31, failed 0, skipped 0.
- ``tryStart returns unknown actor for ?nope`` passed 1, failed 0, skipped 0.
- ``tryStart returns Ok for ?ai`` passed 1, failed 0, skipped 0.
- ``?nope Run leaves the queue and shows the Run error`` passed 1, failed 0, skipped 0.
