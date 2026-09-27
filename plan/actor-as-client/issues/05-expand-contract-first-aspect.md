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
3. **Expand then contract** — Expand: the pool can accept or register a function-shaped start alongside (or instead of) today’s ID-bag ActorStart path. Contract: drop bag-of-IDs supply from the pool once Actors call the function.
4. **Next seams** — Stay fog until this slice ships. Do not hard-lock Graph handoff as #2.
5. **Pilot Actor** — Confirm TestActor proves this slice without rewriting all Actors.

Do not implement.

## 2. Step-two working hypothesis

Working hypothesis only (Alan 2026-09-27 voice). Not a Decision. Not Status `done`. Refine once step one lands. Next seams stay fog until then.

After function-passing at pool construction ships (step one):

1. **Next seam** — flexibility across Actor types.
2. **Shapes** — Agent Actors keep their function shape; other Actors (for example a Parse function) bring a different input shape.
3. **Dispatch** — the switch happens at the HTTP / actor-call dispatch layer: it says “I want a parse” and passes the information that Parse requires; then everything proceeds from there.
4. **Command** — which command is running determines the information that goes with the command.
5. **Polymorphism** — lives in the function signatures, not in the pool or the mailbox.

## Comments

- 2026-09-27 — Filed as next-pass sequencing after the first grillset. Status `defined`.
- 2026-09-27 — Alan voice lock: first expand-contract step is function-passing at Actor pool construction / start. Pool receives a function; Actor calls it for needed info; pool does not supply a Graph/ids bag. Replaces Graph-resolver as first step. Status stays `defined`.
- 2026-09-27 — Alan voice: step-two working hypothesis appended (flexibility across Actor types). Not a Decision. Status stays `defined`.
