# 19 — Want third child rank and cap
**Status**: 
One job: The outliner sees Poll bring Absent Nodes one child rank deeper than today, and each Poll Want is at most 100 ids in walk order. Alan's 2026-11-05 lock is the reason: a third child rank under Included, and a per-Poll Want cap of 100. The story Poll for nearby Absent Nodes in [Client never fails on server data](plan/client-never-fails/spec.md) is the parent story. This ticket extends path 7 after 16 — Poll for nearby Absent Nodes.

Type: coding
Blockers: [16 — Poll for nearby Absent Nodes](plan/client-never-fails/issues/16-poll-nearby-absent/16-poll-nearby-absent-ticket.md) (coded — proceed)

## Inputs

### Working

[ ] `Want.compose` in `src/Shared/Want.fs` has type `Graph -> SiteMap -> NodeId -> NodeId list`.
[ ] The walk is `IncludedDescendantIds.throughChildrenOfExpandedNodes`, then `IncludedDescendantIds.plusChildrenOfEach` twice, then the Absent Header filter.
[ ] The filter keeps an id when `Map.tryFind id graph.nodes` is `None`.
[ ] The module comment says Absent ids on the Included walk, then two ranks of children, including children of folded members.
[ ] The `compose` comment says the two child ranks still run, and "No throttle".
[ ] ``compose wants unloaded children of folded members`` in `tests/Shared.Tests/WantTests.fs` wants the Absent child of a folded member.
[ ] That same fact expects `[]` when that child still has a Header.
[ ] ``compose wants one rank past a folded loaded child`` in `tests/Shared.Tests/WantTests.fs` wants the Absent id one rank past a folded loaded child.
[ ] ``compose lists Included that miss Children first`` in `tests/Shared.Tests/WantTests.fs` wants an Included id that has no Header.
[ ] That same fact skips that id when the Header is present.
[ ] ``compose is empty when Included is Loaded`` in `tests/Shared.Tests/WantTests.fs` expects `[]` when every Included id has a Header and a child list.
[ ] `currentWant` in `src/Client/UpdateHelpers.fs` passes `Want.compose` into the Poll request and the Change request.
[ ] `absentChildWant` in `tests/Server.Tests/StateEndpointTests.fs` passes `Want.compose` into the client-path Poll loop from 18 — Read stored state on the client path.
[ ] ``Poll answer chains the next Included residency Want`` in `tests/Server.Tests/BrowserResidencyClientTests.fs` calls `Want.compose`.
[ ] ``compose after Loaded split does not want the parent`` in `tests/Shared.Tests/SplitOriginTraceTests.fs` calls `Want.compose`.
[ ] Path 7 step 2 in `plan/client-never-fails/arch.md` says the included walk and the two child ranks already list those ids.
[ ] The 2026-10-29 Notes bullet in `plan/client-never-fails/project.md` says Poll and child-id lists have no width limit, and Parse holds the safety valve for size.

### Reference

[ ] [Size limit](plan/context/size-limit.md) caps this ticket at four tests added or changed.

## Do not load

[ ] Closed: `GLOSSARY.md`. The glossary stays.
[ ] Closed: spec story 13, Remove Loaded and Unloaded, and the `includeWorkspace` chase in story 15, Delete the whole-Workspace request.
[ ] Closed: 08 — Load without a race, and its raced Load guard.
[ ] Closed: Core mailbox files and actor files.
[ ] Closed: `CommandRequest` and the `LoadTarget` path.
[ ] Closed: SiteMap fold. Fold stays. The third rank reads child ids under a folded row.
[ ] Closed: the 2026-10-29 decision that child-id lists have no width limit. Parse stays the size safety valve for a Graph payload.

## Process

1. [ ] If an input is missing or does not match this ticket, stop and name that input.
2. [ ] Change `Want.compose` so the walk calls `IncludedDescendantIds.plusChildrenOfEach` three times: Included, then rank 1, then rank 2, then rank 3. Keep the Absent Header filter. After that filter, keep at most 100 ids in that list order (`List.truncate 100` or the same cut). The signature stays `Graph -> SiteMap -> NodeId -> NodeId list`. Callers named in Inputs stay.
3. [ ] Write the module comment and the `compose` comment so they name three child ranks and a Want cap of 100, in place of the words "No throttle".
4. [ ] Add the two facts below in `tests/Shared.Tests/WantTests.fs`. Change an existing WantTests fact only when its assert would be false after the third rank. Added and changed facts stay at four or fewer.
   - ``compose wants two ranks past a folded loaded child`` — an Absent id two ranks past a folded loaded child is wanted. That id is the third child rank, a great-grandchild of an Included row. Mirror ``compose wants one rank past a folded loaded child`` one level deeper.
   - ``compose returns the first 100 Absent ids in walk order`` — when more than 100 Absent ids sit on the walk, `compose` returns the first 100 of the filtered list, in that order.
5. [ ] Change path 7 in `plan/client-never-fails/arch.md`. The Want step that says two child ranks names three child ranks and the Want cap of 100.
6. [ ] Add this Notes bullet on `plan/client-never-fails/project.md`: Alan 2026-11-05 — Want third child rank + per-poll Want cap 100 (Want list only; child-id list width decision from 2026-10-29 unchanged).

## Outputs

[ ] `src/Shared/Want.fs`
[ ] `tests/Shared.Tests/WantTests.fs` — ``compose wants two ranks past a folded loaded child``
[ ] `tests/Shared.Tests/WantTests.fs` — ``compose returns the first 100 Absent ids in walk order``
[ ] `plan/client-never-fails/arch.md` — path 7 names three child ranks and the Want cap of 100
[ ] `plan/client-never-fails/project.md` — the 2026-11-05 Notes bullet
[ ] `plan/client-never-fails/issues/19-want-third-child-rank-and-cap/19-want-third-child-rank-and-cap-ticket.md`

## Stop when

[ ] Record the stop at `plan/client-never-fails/reports/19-want-third-child-rank-and-cap-stop.md`, then ask.
[ ] Stop when the edit needs a file outside Inputs.
[ ] Stop when the edit contradicts the Spec story named in the One job line.
[ ] Stop when the cap needs a Poll wire change past the length of the Want id list.

## Check

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test the ticket names:
  - `tests/Shared.Tests`: ``compose wants two ranks past a folded loaded child``
  - `tests/Shared.Tests`: ``compose returns the first 100 Absent ids in walk order``
[ ] All executed tests pass.
[ ] Write the verdict to `plan/client-never-fails/reports/19-want-third-child-rank-and-cap-check.md`. The first line is `pass` or `fail: <why>`. A fail stops the run.
[ ] A reader who reads only the One job line can say what the ticket builds.
