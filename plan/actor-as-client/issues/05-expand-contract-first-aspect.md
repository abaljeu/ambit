# 05 — Expand-contract first aspect

**Type:** grilling
**Status:** defined
Blocked by: None

## 1. Question

Which aspect migrates first under expand-and-contract?

Alan 2026-09-27 voice: the first slice is **function-passing at Actor pool construction / start**. The pool receives a function rather than a bag of IDs. The Actor calls that function to obtain the information it needs. The pool does not supply a Graph/ids bag to the Actor. This replaces the Graph-resolver idea entirely for now. Start here; let the migration reveal the next seam.

Keep the four provisional Destination aims and their gaps. This ticket locks the first expand-contract step only. It does not rewrite those aims.

Grill:

1. **First aspect** — Confirm first migrate = function-passing at Actor pool construction / start (pool holds or receives a function; Actor invokes it for needed info).
2. **Retire Graph-resolver as first step** — Graph-resolver / “full Graph + start ids as first slice” is not step one. It may remain later fog or later tickets. Do not make it the first expand-contract slice.
3. **Expand then contract** — Expand (prove phase): the pool can accept or register a function-shaped start alongside today’s ID-bag ActorStart path. Contract: hard cut when proven — switch everything over at once and drop the old path. No drain of in-flight ID-bag messages. No dual-lifetime management. See [Lock: hard-cut contract](#3-lock-hard-cut-contract).
4. **Next seams** — Stay fog until this slice ships. Do not hard-lock Graph handoff as #2.
5. **Pilot Actor** — Settled: TestActor is the pilot. It is a dispatcher function that routes on its argument to different operations. Proving the pattern there proves that one function can carry different information per operation, which is the shape step two needs. See [Lock: TestActor is the pilot](#4-lock-testactor-is-the-pilot).

Do not implement.

## 2. Step-two working hypothesis

Working hypothesis only (Alan 2026-09-27 voice). Not a Decision. Not Status `done`. Refine once step one lands. Next seams stay fog until then.

After function-passing at pool construction ships (step one):

1. **Next seam** — flexibility across Actor types.
2. **Shapes** — Agent Actors keep their function shape; other Actors (for example a Parse function) bring a different input shape.
3. **Dispatch** — the switch happens at the HTTP / actor-call dispatch layer: it says “I want a parse” and passes the information that Parse requires; then everything proceeds from there.
4. **Command** — which command is running determines the information that goes with the command.
5. **Polymorphism** — lives in the function signatures, not in the pool or the mailbox.

## 3. Lock: hard-cut contract

Alan 2026-09-27 voice. Settled lock on this ticket. The ticket stays Status `defined`. This is not the whole-ticket Answer and is not a map Decision so far.

**Contract flip is a hard cut.** No draining in-flight messages on the old ID-bag path. Once the function-shaped start is proven, switch everything over at once and drop the old path. **No dual-lifetime management.**

Expand-alongside during the prove phase stays accurate: the pool can accept or register a function-shaped start alongside today’s ID-bag ActorStart path while proving. Contract is a hard flip when proven — not a gradual drain of the old path.

## 4. Lock: TestActor is the pilot

Alan 2026-09-27 voice. Settled lock on this ticket. The ticket stays Status `defined`. This is not the whole-ticket Answer and is not a map Decision so far.

**TestActor is the pilot.** It is a **dispatcher function** that routes on its argument to different operations. Proving the pattern there proves that **one function can carry different information per operation**, which is the shape step two needs.

## 5. Lock: Graph is the universal carrier

Alan 2026-09-27 voice. Settled lock on this ticket (data model). The ticket stays Status `defined`. This is not the whole-ticket Answer and is not a map Decision so far.

The Graph is the universal carrier. Any particular function constructs a Graph and passes it in. The Graph can hold any information needed (full Graph, subgraph, key Nodes, etc.). The receiving function has a specific job to do with that Graph and can simulate any generic function an Actor might want. The **pool and dispatch layer stay dumb** — they just move the Graph. Tests can simulate future Actor shapes by building different Graphs.

## Comments

- 2026-09-27 — Filed as next-pass sequencing after the first grillset. Status `defined`.
- 2026-09-27 — Alan voice lock: first expand-contract step is function-passing at Actor pool construction / start. Pool receives a function; Actor calls it for needed info; pool does not supply a Graph/ids bag. Replaces Graph-resolver as first step. Status stays `defined`.
- 2026-09-27 — Alan voice: step-two working hypothesis appended (flexibility across Actor types). Not a Decision. Status stays `defined`.
- 2026-09-27 — Alan voice lock: contract flip is a hard cut. No drain of in-flight ID-bag messages. No dual-lifetime management. Switch everything over at once when proven. Status stays `defined`.
- 2026-09-27 — Alan voice lock: TestActor is the pilot. Dispatcher function; one function carries different information per operation. Status stays `defined`.
- 2026-09-27 — Alan voice lock: Graph is the universal carrier. Functions construct and pass a Graph; pool and dispatch stay dumb. Status stays `defined`.
