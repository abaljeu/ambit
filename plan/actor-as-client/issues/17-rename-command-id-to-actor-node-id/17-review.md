# 17 — Rename commandId to actorNodeId

Ticket: [[17-rename-command-id-to-actor-node-id-ticket.md]]
Spec: `plan/actor-as-client/tickets/17-rename-command-id-to-actor-node-id` is absent. This project has no `spec.md`. The ticket is the spec.
Arch: [[plan/actor-as-client/arch.md]]

## 1. Standards

- **Scan.** `python` is absent. `python3 scripts/standards-scan.py --diff origin/staging` printed three touched bindings, each under 40 lines: `commandIsLive`, `noRunnableActor`, and `actorNodeOnOwnerPath`. No `LONG`, `TAB`, `MUTABLE`, `BARE_ID`, `PHRASE`, or `FILE` line.
- **Wire key stated three times.** **Judgement.** Duplicated Code. The sentence that the wire key for `actorNodeId` is `"commandId"` is in `doc/current/actors.md:27`, `doc/current/query-actor.md:27`, and `doc/current/search-actor.md:84`. Source: [[plan/context/SMELLS.md]]. The hunk is `The wire key for `actorNodeId` is `"commandId"``.
- **Shotgun Surgery.** Suppressed. The same rename touches the Output files. [17 — Rename commandId to actorNodeId](17-rename-command-id-to-actor-node-id-ticket.md) says the rename does not compile in slices and stays one ticket.

## 2. Spec

- **One job.** The actor node id is `actorNodeId` on `ActorStart`, `ActorInput`, `FunctionStart`, and the pool live row. `src/Shared/EventJson.fs:48` and `src/Shared/EventJson.fs:63` keep the wire key `"commandId"`. `src/Server/RunAgentActor.fs:347` keeps `CommandId` on the wake record. `isCommandText` and `No command found` stay. `commandOnOwnerPath` is `actorNodeOnOwnerPath`. `noRunnableCommand` is `noRunnableActor`.
- **Local names and fact names.** **Extra.** The ticket names the record field and the two helpers. The diff also renames local bindings and fact display names. `tests/Shared.Tests/ActorLiveTests.fs:229`, `tests/Server.Tests/AgentAskTests.fs:111`, `tests/Server.Tests/AskCancelHarness.fs:14`, `tests/Server.Tests/CoreActorPoolDeliverTests.fs:134`, `tests/Shared.Tests/CommandRequestTests.fs:124`, `tests/Server.Tests/CoreMailboxDoorTests.fs:476`.

## 3. Arch

- No findings.

## 4. Tests

- **Suite.** `bash scripts/check-test.sh 20123` printed `DONE` and `/tmp/ambit-full-suite-20261104-173735.log`. The log ends `full suite finished`. `tests/Server.Tests` passed 742, skipped 1, failed 0. `tests/Shared.Tests` passed 1931, skipped 3, failed 0. `scripts/check-test.sh` is not executable. The bare path prints `Permission denied`.
- **Wire.** ``ActorStart Command request round-trips named Core ids`` in `tests/Shared.Tests/SerializationTests.fs:816` asserts the JSON contains `"commandId"`.
- **Owner path.** ``actorNodeOnOwnerPath finds ? ancestor and keeps Focus distinct`` and ``actorNodeOnOwnerPath is none when Zoom is below the Command`` call `CommandRequest.actorNodeOnOwnerPath`.
- **Persist handlers file.** `tests/Server.Tests/Issue42PersistHandlersTests.fs:19` has facts. `tests/Server.Tests/Gambol.Server.Tests.fsproj` has no Compile item for that file on `origin/staging` or on this branch. The per-file filter matched no test. That gap is pre-existing. It is not a missing test this diff removed.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- **Problem.** `tests/Server.Tests/Issue42PersistHandlersTests.fs` is absent from `tests/Server.Tests/Gambol.Server.Tests.fsproj`, so its six facts do not run. Accepted.
- **Fix.** Add the Compile item, or drop the file from the ticket outputs if it is retired. The user confirms the fix.
