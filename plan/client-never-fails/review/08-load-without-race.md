# 08 — Load without a race

Ticket: [[../issues/08-load-without-race/08-load-without-race-ticket.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- **Scan.** `python3 scripts/standards-scan.py --diff origin/staging` printed no line. Exit 0.
- **Ignored Load parameters.** **Judgement.** Middle Man. Speculative Generality. `applyLoadResponse` only calls `applySyncResponse`. `_responseEventId` and `_hasPendingLocal` are unused. Source: [[plan/context/SMELLS.md]]. The hunk is `applySyncResponse response state` in `src/Shared/SyncLogic.fs`. LoadDone still builds `hasPendingLocal` and passes `responseRevision` into that call.

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

- **Script suite.** `bash scripts/check-test.sh 4620` printed `DONE` and `/tmp/ambit-full-suite-20261111-193123.log`. `tests/Server.Tests` did not compile. FS3511 is an error in `PersistApplyTests.fs`, `DbAgentTests.fs`, and `StateEndpointTests.fs`. The same three errors reproduce on a clean staging tree.
- **Project suite.** Shared tests with `AMBIT_FULL_SUITE=1` passed 1936, skipped 3, failed 0. Server tests with `AMBIT_FULL_SUITE=1` and `WarningsNotAsErrors=FS3511` passed 777, skipped 1, failed 0. Log: `/tmp/ambit-full-suite-20261111-194500.log`. The log ends `full suite finished`.
- **Pending apply.** ``applyLoadResponse answer-only with pending applies the Load reply`` expects `Ok`, children `hello`, `suffix`, `sib`, texts `hel` and `lo`, and the same pending queue.
- **Notice apply.** ``applyLoadResponse applies a Notice with a nodes-only Load answer`` expects `Ok`, event id 9, that same split, and the same pending queue.
- **Pending flag.** ``answer-only Load at the same event id applies with pending local work`` expects `Ok`, the same ClientHistory, event id 4, and `Loaded`.
- **Event id.** ``answer-only Load applies when the event id differs`` expects `Ok`, the same ClientHistory, stored event id 4, and `Loaded`.

## 5. Miss

- No findings.

## 6. Found elsewhere

"The following issues were found beyond the ticket scope.  The fixes are suggested."

- **Problem.** [Client never fails on server data map](../map.md) line 17 says a Load reply that follows only Notices is refused with `raced Load answer`. Line 22 says a Load reply with no Change Events is refused with `raced Load answer` when the event id moved.
- **Fix.** Rewrite those two sentences so a Load reply applies like any other server reply. Leave the solution sentence on line 43 as it is.
