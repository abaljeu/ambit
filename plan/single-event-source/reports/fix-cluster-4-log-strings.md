# Cluster 4 — log-string facts

Cluster 4 of [[plan/single-event-source/project.md]]. Two Server test facts. Tests aligned to production. Production log format did not change.

## 1. Cause per fact

### 1.1. HttpResponseLogTests — controlled bad request logs begin and end with request body

HEAD asserted `body=not valid change json`. The fact already posted `"not valid event json"` into [[tests/Server.Tests/HttpResponseLogTests.fs]] `contextForPost`.

[[src/Server/HttpResponseLog.fs]] `formatBegin` writes the captured request body after `body=`. Production logs the posted string. The BEGIN line contains `body=not valid event json`.

The assert now matches the posted body. No production edit.

### 1.2. StateEndpointTests — Log contains valid change data after POST

The fact required `content.StartsWith("00000000")` on [[src/Server/EventLogFile.fs]] `eventsPath`.

[[src/Server/EventLogFile.fs]] `entryLine` still uses `sprintf "%08d"` plus JSON. The 8-digit pad is the on-disk contract.

[[src/Shared/EventLog.fs]] comment: cursor 0 is before the first Ev. `empty.nextId` is `EventId.next EventId.zero` (1). The first stored Ev has id 1.

Dump of the file after POST: `00000001{"id":1,`. The prefix is padded EventId 1, not leftover Change id 0.

The assert now requires `00000001`. It still requires `logged-entry` in the JSON. No production edit.

## 2. Files

1. [[tests/Server.Tests/HttpResponseLogTests.fs]] — BEGIN-body assert uses `not valid event json`.
2. [[tests/Server.Tests/StateEndpointTests.fs]] — events-file prefix assert uses `00000001`.

No edit to [[src/Server/Core/CoreEventDispatch.fs]], [[src/Server/Core/DbAgent.fs]], or [[tests/Server.Tests/DatabaseProjectionContractTests.fs]].

## 3. Commands and outcomes

1. `scripts/gitstatus.sh` — dirty tree includes these two test files plus cluster-2 files left untouched.
2. `dotnet build tests/Server.Tests -c Debug` — succeeded, 0 warning, 0 error.
3. `dotnet test tests/Server.Tests -c Debug --no-build --filter "DisplayName~controlled bad request logs begin|DisplayName~Log contains valid change data after POST"` — first run: HttpResponseLog fact passed after the body-string align; StateEndpoint fact failed (`00000000` vs actual `00000001{"id":1,`). After the prefix align: Passed 2, Failed 0.
