pass

## 08 — Load without a race check

[x] `scripts/agent-test.sh` with no arguments passed 75 tests: ClientHistoryRuntimeTests, SplitOriginTraceTests, SyncLogicTests.
[x] ``applyLoadResponse answer-only with pending applies the Load reply`` passed. This fact replaces ``applyLoadResponse answer-only with pending is raced Load answer``.
[x] ``applyLoadResponse applies a Notice with a nodes-only Load answer`` passed. This fact replaces ``applyLoadResponse ignores a Notice when guarding a raced Load answer``.
[x] ``answer-only Load at the same event id applies with pending local work`` passed. This fact replaces ``answer-only Load at the same event id refuses pending local work``.
[x] ``answer-only Load applies when the event id differs`` passed. This fact replaces ``answer-only Load refuses an event id mismatch``.
[x] ``answer-only Load at the same event id preserves ClientHistory`` passed. That fact stays.
[x] `scripts/agent-test.sh --full` did not compile `tests/Server.Tests`. FS3511 is an error in `PersistApplyTests.fs`, `DbAgentTests.fs`, and `StateEndpointTests.fs`. The same three errors reproduce on a clean staging tree. Log: `/tmp/ambit-full-suite-20261111-193123.log`.
[x] Shared tests with `AMBIT_FULL_SUITE=1` passed 1936, skipped 3, failed 0, total 1939.
[x] Server tests with `AMBIT_FULL_SUITE=1` and `WarningsNotAsErrors=FS3511` passed 777, skipped 1, failed 0, total 778.
[x] Log: `/tmp/ambit-full-suite-20261111-194500.log`. The log ends `full suite finished`.
