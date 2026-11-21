pass

# 16 — Poll for nearby Absent Nodes check

## Verdict

[ ] Verdict is pass for [16 — Poll for nearby Absent Nodes](plan/client-never-fails/issues/16-poll-nearby-absent/16-poll-nearby-absent-ticket.md).

## Named filters

[ ] ``compose lists Included that miss Children first`` in `tests/Shared.Tests`: Passed 1, Failed 0
[ ] ``compose then installWantAnswer loads wanted parents and children`` in `tests/Shared.Tests`: Passed 1, Failed 0
[ ] ``compose wants unloaded children of folded members`` in `tests/Shared.Tests`: Passed 1, Failed 0
[ ] ``compose wants one rank past a folded loaded child`` in `tests/Shared.Tests`: Passed 1, Failed 0
[ ] ``compose is empty when Included is Loaded`` in `tests/Shared.Tests`: Passed 1, Failed 0

## Mapped run

[ ] `scripts/agent-test.sh` with no arguments: Passed 15, Failed 0, Total 15, `tests/Shared.Tests` `WantTests`

## Full suite

[ ] `scripts/agent-test.sh --full` Server: Passed 733, Failed 0, Skipped 1, Total 734
[ ] `scripts/agent-test.sh --full` Shared: Passed 1929, Failed 0, Skipped 3, Total 1932
[ ] Log: `/tmp/ambit-full-suite-20261103-170103.log`
