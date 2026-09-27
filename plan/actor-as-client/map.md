# Actor as client

Labels: wayfinder:map

## 1. Destination

Remake Server Actor hosting and mailbox orchestration so an Actor is a Core client in the same relation as the Browser. The Actor owns internal behavior. It talks Core for Graph, Events, and Changes. This chart finds that remake. It does not implement CoreActorPool ([[src/Server/Core/CoreActorPool.fs]]).

Aims from Alan 2026-09-27 chat. Aims 1, 2, and 4 stay provisional (not Decisions so far). Aim 3 is a Decision so far ([03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md)):

1. **Actor ≈ Core client** — same relation to Core as the Browser: owns internal behavior; talks Core for Graph, Events, and Changes (today mostly Poll, not push). Not a special mailbox citizen with a private Graph extract. Provisional.
2. **Pass full Graph + start point** — stop requiring a subgraph extract for Actor start; handing an immutable Graph is O(1) reference share. Start ids (Zoom, Focus, Command or equivalent) select where work begins. Provisional.
3. **Simple orchestration** — The mailbox is the single entry and a dispatcher, not a synchronous executioner: it stamps each Event with a sequence number (EventId) and routes it, and does not wait for completion. EventId is the watermark; there is no separate progress counter. A reader asks an entity “did Event N complete?” and waits until yes. Callers may address queues directly. The pipeline is one-directional (graph → file → parser → database). Each entity that implements an Event can be queried for completion on that Event.
4. **In-process start prefers curried functions** — a Command constructs an Actor function by closing over data and posts that function to the mailbox/pool. More flexible than fitting a narrow ActorStart shape and invoking by registered name. Named registry may remain for remote or cross-process start — that stays fog unless a ticket must lock it. Provisional.

## 2. Notes

1. **Deferred 2026-09-27** — This Feature-set is parked pending the reframing of Actors as functions that can take all kinds of parameters. It may resume around steps five or six; the intervening gaps are unknown. Keep this map and its tickets.
2. **Skills** — [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/grilling/SKILL.md]], [[.agents/skills/domain-modeling/SKILL.md]], [[.agents/skills/project-work/SKILL.md]]. Use [[.agents/skills/implement-fsharp-feature/SKILL.md]] only after the way is clear.
3. **Part of** — [[plan/roadmap/epics/chapters/actors-supported.md]] on [[plan/roadmap/epics/robust-outliner.md]].
4. **Core baseline** — Existing pool and mailbox stay [[plan/core-creation/project.md]]. This Project charts the remake. It does not replace that baseline while the way is fog.
5. **Parse Actor** — First Actor definition stays [[plan/parse-actor/project.md]]. Parse stays outside Core. This map covers only how a Parse Actor *hosts* under the new scheme if hosting touches the remake. File→Graph algorithm body is out of scope.
6. **Provisional aims** — Destination aims 1, 2, and 4 stay chat aims until [01 — Actor-as-client duplex](issues/01-actor-as-client-duplex.md), [02 — Graph handoff](issues/02-graph-handoff.md), and [04 — Start surface](issues/04-start-surface.md) resolve. Aim 3 is a Decision so far on [03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md). Next pass: [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md) starts at function-passing pool setup (pool receives a function; Actor calls it; no Graph/ids bag from the pool). Graph-resolver is not the first slice. It does not promote aims 1, 2, or 4.
7. **Mailbox clear-fast** — [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]] still holds: the mailbox stamps and routes without waiting; slow work still leaves the mailbox. The mailbox is a dispatcher, not an executioner. Locked on [03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md).
8. **Today's extract** — ActorStart carries `graphIds` (Included expand of Zoom). CoreActorPool ([[src/Server/Core/CoreActorPool.fs]]) requires that extract. [02 — Graph handoff](issues/02-graph-handoff.md) grills dropping it.
9. **Today's start** — CoreActorPool registers `ActorFn` by `ActorName` / `PeerActorName` and `startActor` takes ActorStart plus a Graph getter. [04 — Start surface](issues/04-start-surface.md) grills curried in-process post versus that registry.
10. **Step-two hypothesis** — After function-passing ships, next seam may be flexibility across Actor types (dispatch chooses the function shape; polymorphism in signatures, not the pool). Working hypothesis only on [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md). Not a Decision.
11. **Slice one expand-alongside** — [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md) Status `coded` (historical). `startFunction` posts a function; the Actor calls `getGraph`; ID-bag `startActor` remains. Further slices are deferred with this Feature-set.

## 3. Decisions so far

1. **[03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md)** — Mailbox is the single entry and a dispatcher: stamp EventId and route, do not wait. EventId is the watermark. Queues are open-access; pipeline is graph → file → parser → database. Each entity answers “did Event N complete?”

## 4. Not yet specified

1. **Remote or cross-process start** — whether a named ActorFn registry remains, and how a Command reaches a worker in another process.
2. **Existing Actor migration** — how TestActor, AI, GitHub Peer Actor, and Parse adopt the new host without a one-shot rewrite.
3. **Lifecycle Events after remake** — what ActorStart and ActorStop must still record once start ids and curried functions land.
4. **Worker isolation** — whether filesystem and database workers stay in-process with Core or later move out.

## 5. Out of scope

1. **Browser residency wire** — Want-driven Graph→Browser. Pointer: [[plan/browser-residency/project.md]].
2. **GitHub transport / PathPick / Peer Actor App boundary** — Pointer: [[plan/github-transport/project.md]].
3. **Parse file→Graph algorithm body** — Pointer: [[plan/parse-actor/project.md]]. This map may touch only how a Parse Actor *hosts* under the new scheme.
4. **Rewriting all existing Actors in one go** — Migration of each Actor body is later work, not this chart.
5. **Product code on this chart** — The map finds the way. It does not implement CoreActorPool or mailbox F#.

These are exclusions from this map, not product-wide exclusions. See [[doc/agents/scope-vs-commitment.md]].
