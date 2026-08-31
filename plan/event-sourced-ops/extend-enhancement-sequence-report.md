# Extend enhancement sequence — delegated report

Parent-facing summary of the /to-tickets draft extension. Full ticket text lives in [[to-tickets-draft.md]]. Branch verified: `w/event-sourced-ops` ([[git.md]]).

## What was done

- Re-read WORK.md, project docs, and [[post-poll-envelope-unify.md]].
- Preserved agreed initial graph Tickets 0–4 (`01`–`05`).
- Extended draft with project-sized later tickets 5–14 (`06`–`15`), dependency shape, architectural-shift and status-basis fields, new quiz questions.
- Did not publish `issues/`, edit WORK.md, change Stage, commit, or touch software.

## Added later tickets (10)

1. **Ticket 5 / `06` — Generalized Server Actor produce path** (after 2+3)
2. **Ticket 6 / `07` — Parse File realignment** (after 5)
3. **Ticket 7 / `08` — Advisory soft-lock product** (after 5; ∥ 6)
4. **Ticket 8 / `09` — Job identity, launch, cancel** (after 5, preferably 7)
5. **Ticket 9 / `10` — delete-against-edit recovery (decision→implement)** (after 2–4)
6. **Ticket 10 / `11` — Orphan-collection safety (decision)** (after 2–4; ∥ 9)
7. **Ticket 11 / `12` — Child-list approximation polish** (after 4)
8. **Ticket 12 / `13` — Completing-ops pattern beyond timing** (after 5)
9. **Ticket 13 / `14` — Post/poll envelope type unification (optional decision)** (after 0; optional)
10. **Ticket 14 / `15` — Unrestricted Undo desirability (decision only)** (after 3)

Parked (not tickets): Load packages / state endpoint, global revision model, Server-partial Local Graph.

## Dependency shape

- Critical: `0 ∥ 1 → 2 → 3 → 4`
- Then fan-out: `5 → 6` and `5 → 7 → 8`; `9 ∥ 10` after 2–4; `11` after 4; `12` after 5; `13` optional after 0; `14` after 3

## Major architectural shifts

- **0–4:** signal/poll split, amendment, rewind/replay, end Reject/reload for recoverable merge
- **5–6:** one Server Actor produce path; Parse leaves revision-CAS special case
- **7–8:** advisory reservation + job runtime (proposed surfaces)
- **9:** Owned `deleted` recovery only if decision accepts
- **13:** optional shared success **type** (channels stay separate)
- **14:** cross-Actor Undo only if open question answers yes

## Envelope unification answer

Belongs as **optional Ticket 13**, not on the critical path. Exact type unify is **proposed** and independent of Tickets 0–4; two channels stay **accepted**. See [[post-poll-envelope-unify.md]].

## Needs user input (quiz)

- Later split OK? Soft-lock before jobs; Parse ∥ soft-lock?
- Ticket 13 optional — agree?
- 9 / 10 / 14 remain decision-first?
- Unpark any parked Load/revision topic now?

## Recommended WORK.md mutations

- **refine** Active [[to-tickets-draft.md]] — quiz later sequence; publish only after approval
- **add** Pending [[extend-enhancement-sequence-report.md]] (optional parent synthesis pointer)
- **note** [[plan/relaxed-concurrency/]] is a build-upon layer (Stage done); gate removal delivered in issue 02.
