# Section filter lock (HITL 2026-08-28)

Docs lock only. Branch `w/expr`. No `src/` / `tests/` in this pass. No commit.

## Locked meaning

- **`section`**: new builtin pure filter. A section is a named Normal Node. Zero-argument, same family as `dir` / `normal`. Unnamed Normal Nodes are not sections.
- **`subsection`**: new builtin function (generator / search). Spoken catalog spelling of cluster `#`, parallel to `tree` for `**`. Required name argument: `subsection "todo"` equals `#todo`. Downward search that bypasses unnamed Normal Nodes and stops at named ones (sections below).
- `/` is unchanged. No union operator. `named` remains the name-glob pure filter. Do not implement ticket 23.

Correction: a first pass treated `subsection` as description-only (prose for `#`, not a catalog spelling). That reading is wrong. `subsection` is a function.

## Edits

- [[.scratch/expression-language/reports/hash-search-word.md]] section 7 records the lock. Prior brainstorm (tagged / heading / seek) stays. Tagged, heading, and seek are out.
- [[.scratch/expression-language/spec.md]]: catalog rows `section` (pure filter, `Node ⇒ Node`, no argument) and `subsection` (`subsection`; cluster `#`). Chapter 1, 4, 6, 7 search rules, 9, and 11 describe `#` as subsection search. Ticket 03 Answer amended: `#` is not short `named`.
- [[CONTEXT.md]]: **section** (named Normal Node) and **subsection** (`#` search function).
- [[.scratch/expression-language/map.md]]: one Decisions-so-far line.

## WORK.md mutations (parent applies; this agent did not edit [[WORK.md]])

- `add` [[.scratch/expression-language/issues/26-section-and-subsection-catalog-rows.md]] — implement `section` filter and `subsection` spelling of `#` in ExprPrimitive (branch: `w/expr`; lock: this report). Place in Pending. Blocked by none.
