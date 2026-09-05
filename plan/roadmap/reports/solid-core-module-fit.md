# Solid core — module fit

Date: 2026-09-04 (discussion), written 2026-09-05

Links: [[../epics/robust-outliner.md]], [[../../event-sourced-ops/overview.md]]

## Purpose

Which existing modules adapt toward the Solid core bar vs what does not fit easily.

## Fits / kernel candidate

- Shared apply/amend stack (inner apply path for Changes) — small kernel candidate (~couple thousand lines of apply path inside ~80k-line codebase)
- ESO spine: one mutation path, amend, Actors hand Changes into inner apply (issues 07–08, actors-and-jobs)

## Partial / gaps on the ACID bar (db mode)

- Dual File/DB paths — file mode is view-only/rollback; db is authority; dual path complicates "apply succeeds iff durable commit"
- Timeout-abandon that can persist after a refused apply
- Non-Change startup writers that bypass the Change path

## Hard to fold into the kernel without redesign (leave outside for now)

- Persistence layer broadly
- Parse (Parse File today is request-scoped Actor, not a managed pool)
- Upload / Workspace load (also counterexamples to incremental ops on the Epic)

## Parked relative to Solid core bar

- Process crash isolation
- Full Upload/Load redesign
- Every connector

## Establish effort (from chat)

~1–2 weeks ACID cleanup + ~2–3 weeks managed actor pool ≈ ~3–5 weeks focused for four-call + ACID + pool.
