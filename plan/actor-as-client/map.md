# Actor as client

Labels: wayfinder:map

## 1. Destination

Remake Server Actor hosting and mailbox orchestration so an Actor is a Core client in the same relation as the Browser. The Actor owns internal behavior. It talks Core for Graph, Events, and Changes. This chart finds that remake. It does not implement CoreActorPool ([[src/Server/Core/CoreActorPool.fs]]).

Aims from Alan 2026-09-27 chat. Aims 1 and 2 stay provisional (not Decisions so far). Aims 3 and 4 are Decisions so far ([03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md), [04 — Start surface](issues/04-start-surface.md)):

1. **Actor ≈ Core client** — same relation to Core as the Browser: owns internal behavior; talks Core for Graph, Events, and Changes (today mostly Poll, not push). Not a special mailbox citizen with a private Graph extract. Provisional.
2. **Pass full Graph + start point** — stop requiring a subgraph extract for Actor start; handing an immutable Graph is O(1) reference share. Start ids (Zoom, Focus, Command or equivalent) select where work begins. Provisional. [04 — Start surface](issues/04-start-surface.md) drops `graphIds` from ActorStart; live versus snapshot Graph and start ids beyond cancel Focus stay on [02 — Graph handoff](issues/02-graph-handoff.md).
3. **Simple orchestration** — The mailbox is the single entry and a dispatcher, not a synchronous executioner: it stamps each Event with a sequence number (EventId) and routes it, and does not wait for completion. EventId is the watermark; there is no separate progress counter. A reader asks an entity “did Event N complete?” and waits until yes. Callers may address queues directly. The pipeline is one-directional (graph → file → parser → database). Each entity that implements an Event can be queried for completion on that Event.
4. **In-process start prefers curried functions** — In-process start posts a curried function to the mailbox. A Command closes over what the Actor needs and posts that function. CoreActorPool remains and retains cancel; the function receives focus ID (the cancel Focus) and name. Named registry for remote or cross-process start stays fog. ActorStart records focus ID and name for cancel and no longer carries `graphIds`.

## 2. Notes

1. **Deferred 2026-09-27** — This Feature-set is parked pending the reframing of Actors as functions that can take all kinds of parameters. It may resume around steps five or six; the intervening gaps are unknown. Keep this map and its tickets.
2. **Skills** — [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/grilling/SKILL.md]], [[.agents/skills/domain-modeling/SKILL.md]], [[.agents/skills/project-work/SKILL.md]]. Use [[.agents/skills/implement-fsharp-feature/SKILL.md]] only after the way is clear.
3. **Part of** — [[plan/roadmap/epics/chapters/actors-supported.md]] on [[plan/roadmap/epics/robust-outliner.md]].
4. **Core baseline** — Existing pool and mailbox stay [[plan/core-creation/project.md]]. This Project charts the remake. It does not replace that baseline while the way is fog.
5. **Parse Actor** — First Actor definition stays [[plan/parse-thread/project.md]]. Parse stays outside Core. This map covers only how a Parse Actor *hosts* under the new scheme if hosting touches the remake. File→Graph algorithm body is out of scope.
6. **Provisional aims** — Destination aims 1 and 2 stay chat aims until [01 — Actor-as-client duplex](issues/01-actor-as-client-duplex.md) and [02 — Graph handoff](issues/02-graph-handoff.md) resolve. Aims 3 and 4 are Decisions so far on [03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md) and [04 — Start surface](issues/04-start-surface.md). Next pass: [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md) starts at function-passing pool setup (pool receives a function; Actor calls it; no Graph/ids bag from the pool). Graph-resolver is not the first slice. It does not promote aims 1 or 2.
7. **Mailbox clear-fast** — [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]] still holds: the mailbox stamps and routes without waiting; slow work still leaves the mailbox. The mailbox is a dispatcher, not an executioner. Locked on [03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md).
8. **Today's extract** — Production ActorStart still carries `graphIds` (Included expand of Zoom). [04 — Start surface](issues/04-start-surface.md) locks that ActorStart drops that extract and keeps focus ID and name for cancel. Live versus snapshot Graph, and which start ids beyond cancel Focus, stay on [02 — Graph handoff](issues/02-graph-handoff.md).
9. **Today's start** — In-process start posts a curried function; it is not a named ActorFn registry lookup. CoreActorPool remains and cancels via focus ID and name on the function. Named registry stays fog. Locked on [04 — Start surface](issues/04-start-surface.md).
10. **Step-two hypothesis** — After function-passing ships, next seam may be flexibility across Actor types (dispatch chooses the function shape; polymorphism in signatures, not the pool). Working hypothesis only on [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md). Not a Decision.
11. **Slice one expand-alongside** — [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md) Status `coded` (historical). `startFunction` posts a function; the Actor calls `getGraph`; ID-bag `startActor` remains. Further slices are deferred with this Feature-set.
12. **2026-09-28 Server description** — [[plan/architecture/server-core.md]] confirms Actors as privilege-less external functions that only post to the mailbox. Core and the mailbox own create and destroy, and Graph, file, and git mutations. This Feature-set stays concept-only. No new implement tickets.

## 3. Decisions so far

1. **[03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md)** — Mailbox is the single entry and a dispatcher: stamp EventId and route, do not wait. EventId is the watermark. Queues are open-access; pipeline is graph → file → parser → database. Each entity answers “did Event N complete?”
2. **[04 — Start surface](issues/04-start-surface.md)** — In-process start posts a curried function; not a named ActorFn lookup. CoreActorPool remains and cancels via focus ID and name on the function. Named registry stays fog. ActorStart drops `graphIds` and records focus ID and name for cancel.

## 4. Not yet specified

1. **Remote or cross-process start** — Named ActorFn registry stays fog ([04 — Start surface](issues/04-start-surface.md)). How a Command reaches a worker in another process stays open.
2. **Existing Actor migration** — how TestActor, AI, GitHub Peer Actor, and Parse adopt the new host without a one-shot rewrite.
3. **Lifecycle Events after remake** — ActorStart records focus ID and name for cancel and drops `graphIds` ([04 — Start surface](issues/04-start-surface.md)). What ActorStop and any other Event fields must still record stays open.
4. **Worker isolation** — whether filesystem and database workers stay in-process with Core or later move out.
5. **Graph handoff remainder** — live versus snapshot Graph, and which start ids beyond cancel Focus, stay on [02 — Graph handoff](issues/02-graph-handoff.md).

## 5. Out of scope

1. **Browser residency wire** — Want-driven Graph→Browser. Pointer: [[plan/browser-residency/project.md]].
2. **GitHub transport / PathPick / Peer Actor App boundary** — Pointer: [[plan/github-transport/project.md]].
3. **Parse file→Graph algorithm body** — Pointer: [[plan/parse-thread/project.md]]. This map may touch only how a Parse Actor *hosts* under the new scheme.
4. **Rewriting all existing Actors in one go** — Migration of each Actor body is later work, not this chart.
5. **Product code on this chart** — The map finds the way. It does not implement CoreActorPool or mailbox F#.

These are exclusions from this map, not product-wide exclusions. See [[doc/agents/scope-vs-commitment.md]].
