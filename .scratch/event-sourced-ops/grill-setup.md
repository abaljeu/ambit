# Grill setup — event-sourced ops

Worker report. Parent applies [[WORK.md]] mutations. Do not treat this file as the user-facing grill.

## Project

- Path: [[.scratch/event-sourced-ops/]]
- Branch: `w/event-sourced-ops` (cut from `selective-client-sync`; tree was clean)
- Stage: `charting`

## Files created

- [[git.md]]
- [[project.md]]
- [[goal.md]] (goal sentence is a stub)
- this file
- Regenerated [[.scratch/index.md]]

No software changes. Map from [[.scratch/relaxed-concurrency/map.md]] was not copied.

## WORK.md mutations

- **add** (Active) — [[.scratch/event-sourced-ops/project.md]] — grill and sharpen the event-sourced ops architectural goal (related: [[.scratch/relaxed-concurrency/map.md]]; artifacts: [[.scratch/event-sourced-ops/goal.md]], [[.scratch/event-sourced-ops/grill-setup.md]])

No move, block, or remove.

## First grilling turn (speak unchanged)

No round may exceed 2000 characters. This is the full first frontier (one question). Wait for the answer before the next round.

❓ **Q1** - **What is "event sourcing" this time?**: [[.scratch/relaxed-concurrency/map.md]] already examined Event Modeling / Event Sourcing and rejected **full Event Sourcing with replay from genesis**. Facts, not opinions: Parse already logs **Op diffs** (not a "reparse this file" instruction). `GET /ambit/state` is a full Graph snapshot; Poll is a Change tail. The running system is event streaming against a snapshot of record. Genesis replay would send old files through new parsers.

You now want a **goal**: "clear correct semantics by the core design through event sourcing." Ideas on the table: send `/state` as Ops; implement Parse and reparse as Ops; run Parse asynchronously on the Server and deliver Ops through Poll.

Which proposal is this?

A) Reopen the rejection. The Change log is the source of truth. Snapshots are disposable. Replay from genesis (or a long log) is required.

B) Keep snapshot + tail. "Event sourcing" means every mutation that matters (Parse, reparse, residency, background work) arrives as Changes/Ops the Browser already applies. The `/state` blob is what you want to stop using as the Load path.

C) Event Modeling as a design/documentation technique only. No change to what is the source of truth.

D) Something else. Name the source of truth, and name what you will **not** reconstruct from the log.

➡️ **B.** Do not reopen genesis-replay Event Sourcing. Say: Ops are the only mutation path; Poll is how async work lands. Calling that Event Sourcing fights the map you accepted and invites a plug-in framework before one incremental path works.

## Grilling skill (for the parent)

- Design tree. Ask the **whole frontier** each round. Number questions. Give a recommended answer. Then **wait**.
- A question that depends on an open answer in this round belongs to a **later** round.
- No round over 2000 characters.
- Look up facts. Do not ask the user for filesystem or code facts.
- Do not implement or write the architecture until the user confirms shared understanding.
- Not a sympathetic design workshop.

## Assumptions from the map

- An Op is already per-Node or per-parent child list (eight cases). Attribute Ops are compare-and-swap; `Replace` is span compare-and-swap.
- The global revision gate causes rejection of unrelated edits. That is a liveness problem, not corruption.
- Rejection stays legal. No order-CRDT, no offline editing, no genesis replay.
- Parse already follows event discipline at emit time (known 5). Snapshot is the record (known 7).
- Hybrid authority (Graph log vs file text) and document-derived Node identity across reparse are still open (D, E).

## Contradictions

- "Parse as Ops" vs map known 5: the Server already records Parse as Op diffs.
- "No corruption because of guarantees" vs two live safety stories: drop the global revision gate ([[.scratch/relaxed-concurrency/spec.md]]) and also add async incremental Load.
- "Event sourcing" as the goal name vs the same map's rejection of Event Sourcing.
- A plug-in bus for HITL, agentic edits, and long-running commands vs no working incremental Load path yet.
- [[doc/arch.md]] still says last-write-wins and no client merge. Relaxed-concurrency slices 2–3 plan merge + replan.

## Next frontier (do not ask yet)

1. If the answer is B: what must "correct by core design" mean if it is not genesis replay — the apply function, invariants, tests, or prose?
2. Parse-as-Op is already true at emit. Is the new work only async Parse, with Ops arriving on Poll?
3. For incremental Load: what must the Browser see now, what may lag, and what is a lie? Then: HITL vs agentic vs long-running — one reject/apply model, or three products?
