# Function-shaped Actor start

Look up new Actor creation here. The ID-bag `ActorStart` / `graphIds` path stays as written on [[plan/core-creation/arch.md]].

The pool receives a function (`startFunction`). The Actor calls `getGraph` to obtain a Graph carrier. The pool does not supply a Graph or ids bag. TestActor is the pilot dispatcher (`hello` / `ping`).

Code: [[src/Server/Core/CoreActorPool.fs]] `startFunction`; [[src/Server/TestActor.fs]] `functionActor`. Locks and expand-alongside: [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md).
