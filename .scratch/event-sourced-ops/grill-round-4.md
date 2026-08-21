# Grill round 4

Worker report. User paused Q4/Q5 ("Enough about `/state`"). Those stay deferred, not dropped. [[WORK.md]] stays Active; no board mutations.

Facts: [[poll-load-conveyance.md]].

## User-facing (speak unchanged)

**Poll:** yes — GET `/ambit/poll` sends a `Change` list (`ops` inside each Change). The Browser applies them with `Op.apply`. No Graph blob.

**Load:** partial — POST `/ambit/load` sends that same Change tail **and** `packages` (a Node list, a Graph slice). The Browser `Op.apply`s the tail, then map-merges the packages. Packages are not Ops.

❓ **Q6** - **Should Load's residency stay a Graph transfer?**: Poll is already Ops. Load's extra job is residency you do not have as a log prefix (snapshot is the record; retention is a tail). Making packages into Ops is either genesis replay (rejected) or a verbose encoding of the same Nodes.

A) Keep Load mixed: Change tail as Ops; packages stay a Graph transfer.

B) Load becomes Ops-only. Then say how the Browser reconstructs Unloaded subgraphs without a full log.

C) Load drops the Change tail and is packages only; Poll is the only Op path.

➡️ **A.** Poll already is the Op path. Load exists because a partial view cannot replay what it never stored. Do not dress `packages` as Ops.

## Deferred (do not ask)

- Q4: producer Op-valid vs fail-closed (user paused `/state`).
- Q5: what a partial view may believe.
- Process boundary; Load freshness; holes; HITL vs agentic vs long-running.

## WORK.md mutations

None.
