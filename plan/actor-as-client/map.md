# Actor as client

Labels: wayfinder:map

## 1. Destination

Remake Server Actor hosting and mailbox orchestration so an Actor is a Core client in the same relation as the Browser. The Actor owns internal behavior. It talks Core for Graph, Events, and Changes. This chart finds that remake. It does not implement CoreActorPool ([[src/Server/Core/CoreActorPool.fs]]).

Provisional aims from Alan 2026-09-27 chat (not Decisions so far):

1. **Actor ≈ Core client** — same relation to Core as the Browser: owns internal behavior; talks Core for Graph, Events, and Changes (today mostly Poll, not push). Not a special mailbox citizen with a private Graph extract.
2. **Pass full Graph + start point** — stop requiring a subgraph extract for Actor start; handing an immutable Graph is O(1) reference share. Start ids (Zoom, Focus, Command or equivalent) select where work begins.
3. **Simple orchestration** — few long-lived workers (filesystem, database) behind queued messages; independent short pieces (parsers, graph synchronizers) do their own thing. Thin routing, not embedded logic in the mailbox.
4. **In-process start prefers curried functions** — a Command constructs an Actor function by closing over data and posts that function to the mailbox/pool. More flexible than fitting a narrow ActorStart shape and invoking by registered name. Named registry may remain for remote or cross-process start — that stays fog unless a ticket must lock it.

## 2. Notes

1. **Skills** — [[.agents/skills/wayfinder/SKILL.md]], [[.agents/skills/grilling/SKILL.md]], [[.agents/skills/domain-modeling/SKILL.md]], [[.agents/skills/project-work/SKILL.md]]. Use [[.agents/skills/implement-fsharp-feature/SKILL.md]] only after the way is clear.
2. **Part of** — [[plan/roadmap/epics/chapters/actors-supported.md]] on [[plan/roadmap/epics/robust-outliner.md]].
3. **Core baseline** — Existing pool and mailbox stay [[plan/core-creation/project.md]]. This Project charts the remake. It does not replace that baseline while the way is fog.
4. **Parse Actor** — First Actor definition stays [[plan/parse-actor/project.md]]. Parse stays outside Core. This map covers only how a Parse Actor *hosts* under the new scheme if hosting touches the remake. File→Graph algorithm body is out of scope.
5. **Provisional aims** — The four Destination locks are chat aims. They become Decisions so far only when a resolved ticket records them. First grillset that confirms or refines them: [01 — Actor-as-client duplex](issues/01-actor-as-client-duplex.md), [02 — Graph handoff](issues/02-graph-handoff.md), [03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md), [04 — Start surface](issues/04-start-surface.md). Next pass: [05 — Expand-contract first aspect](issues/05-expand-contract-first-aspect.md) starts at function-passing pool setup (pool receives a function; Actor calls it; no Graph/ids bag from the pool). Graph-resolver is not the first slice. It does not promote the aims.
6. **Mailbox clear-fast** — [[doc/Decisions/0004-core-mailbox-messages-clear-fast.md]] still says every Core mailbox message finishes quickly and slow work is an Actor. [03 — Mailbox orchestration shape](issues/03-mailbox-orchestration-shape.md) grills whether that still holds after worker queues.
7. **Today's extract** — ActorStart carries `graphIds` (Included expand of Zoom). CoreActorPool ([[src/Server/Core/CoreActorPool.fs]]) requires that extract. [02 — Graph handoff](issues/02-graph-handoff.md) grills dropping it.
8. **Today's start** — CoreActorPool registers `ActorFn` by `ActorName` / `PeerActorName` and `startActor` takes ActorStart plus a Graph getter. [04 — Start surface](issues/04-start-surface.md) grills curried in-process post versus that registry.

## 3. Decisions so far

None. The 2026-09-27 chat locks are provisional aims on Destination and Notes. They become Decisions when their tickets resolve.

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
