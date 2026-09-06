# How much of the Core Actor spine before first `?`

Type: grilling
Status: done
Blocked by:
Actual: 55m

## Question

How much of [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]] and [[plan/core-creation/issues/02-core-actor-pool.md]] must land before the first Run Agent is usable? Decide the minimum Actor spine. Do not implement in this ticket.

## Answer

First usable Run Agent needs: Core `launch` and `postChange` (already in code), `POST /ambit/actors` Create, this Project’s nodelist+root extract (do not revise [[plan/core-creation/issues/09-define-core-command-launch-contract.md]]), a registered Grok Bot Actor with the env/host key ([[02-which-llm-and-credentials.md]]), cookie auth ([[plan/core-creation/issues/20-client-presents-credential.md]], done), and drop-on-complete so Focus unlocks ([[plan/core-creation/issues/18-finish-and-drop.md]] — core-creation implements; this Project waits).

Not required for first usable Run Agent: cancel, HTTP query, or a JSON client token. Lock is about Changes: only Focus Changes, so lock Focus only. The Actor must return by itself. Cancel stays later (map destination).

## Comments

- 2026-09-02: Filed unclaimed from WORK.md. Map: [[../map.md]]. Actors: [[plan/event-sourced-ops/details/actors-and-jobs.md]].
- 2026-09-06: Q3: First usable `?` is long-running. Command returns after launch; Actor work is later; write-back is `postChange` and Browser Poll. Cancel and lock span are still open. Core already has `launch` and test `postChange`; no HTTP Command; no cancel.
- 2026-09-06: Q6: No cancel on first usable `?`. The Actor must return by itself. Q7: Launch locks Focus only. Fact: [[src/Server/Core/CoreActorPool.fs]] never drops a job when `ActorFn` returns; lock stays. Drop-on-complete is [[plan/core-creation/issues/18-finish-and-drop.md]].
- 2026-09-06: Q11: First usable `?` needs drop-on-complete so Focus unlocks when the Actor returns. Not cancel. Who lands 18 is still open.
- 2026-09-06: Q15: core-creation implements [[plan/core-creation/issues/18-finish-and-drop.md]]. This Project waits. Do not duplicate finish-drop here.
- 2026-09-06: Launch extract is no longer NodeRange. Core must extract the nodelist subgraph under rootnode (09 may-change: larger subgraph). Lock vs that extract still open.
- 2026-09-06: Q20: Lock is about Changes. Only Focus Changes, so lock Focus only. Extract may be larger.
- 2026-09-06: Q23: This Project changes extract/`LaunchRequest`. Do not revise [[plan/core-creation/issues/09-define-core-command-launch-contract.md]]. Q22: first `?` waits on a Core token standard.
- 2026-09-06: Q25: Token wait is issue 20, already done. Remaining spine wait: [[plan/core-creation/issues/18-finish-and-drop.md]].
- 2026-09-06: Q26: Lock. Spoken name is Run Agent. Answer written.
- 2026-09-06: Alan confirmed lock.

## Time

- 2026-09-06 5m — Q3 async launch; spine must include Server launch from Browser command (from chat)
- 2026-09-06 10m — Q6 no cancel, self-return; Q7 lock Focus (from chat)
- 2026-09-06 5m — Q11 drop-on-complete required (from chat)
- 2026-09-06 5m — Q15 wait on 18 (from chat)
- 2026-09-06 5m — extract = nodelist below rootnode (from chat)
- 2026-09-06 5m — Q20 lock = Focus Changes only (from chat)
- 2026-09-06 5m — Q23 this Project extract; Q22 wait token standard (from chat)
- 2026-09-06 5m — Q25 issue 20 is done (from chat)
- 2026-09-06 10m — Q26 lock; Run Agent name; wrote Answer (from chat)
