Check: pass

The server search walks the full Graph from `request.startId`. The reply holds at most 200 hit Node ids. Those hits' Headers ride the Want answer `nodes` list.

`SearchDialog.applyServerReply` installs that list into the residency Graph. The dialog keeps the residency-search hits and appends the server hits whose Node ids the client search did not find, including a hit that was absent before the reply. The room under 200 is applied after that exclusion.

`SearchDialog.serverOnlyRows` reads each appended id from `model.graph.nodes`. The install runs before that read. A listed hit has its Header in the residency Graph.

`SearchPictureClient` passes the Want `nodes` list into `applyServerReply`. `SearchActor.actorStart` still records the root.

## Suites

- `scripts/agent-test.sh` with no arguments passed 18 in `tests/Server.Tests` and 9 in `tests/Shared.Tests`, with 0 failed and 0 skipped.
- ``server search returns the hits and their Want nodes`` passed 1, failed 0, skipped 0.
- ``dialog appends a server hit that was not resident`` passed 1, failed 0, skipped 0.
- ``reply walks from the request start`` passed 1, failed 0, skipped 0. Outputs leaves this test unnamed. It replaces ``reply walks from the root``.
- ``same text adds server hits and an empty reply keeps local rows`` passed inside the 18. The call now passes an empty node list.
- `tests/Server.Tests` with `AMBIT_FULL_SUITE=1` and `-nodeReuse:false` passed 774, failed 0, skipped 1. Total 775.
- `tests/Shared.Tests` with `AMBIT_FULL_SUITE=1` and `-nodeReuse:false` passed 1944, failed 0, skipped 3. Total 1947.
